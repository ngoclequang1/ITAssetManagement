using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ITAssetManagement.Data;
using ITAssetManagement.DTOs.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITAssetManagement.Services.Ai;

public sealed class AssetAiAgent : IAssetAiAgent
{
    private const int MaxToolRounds = 4;
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _db;
    private readonly OpenAiOptions _options;
    private readonly ILogger<AssetAiAgent> _logger;

    public AssetAiAgent(
        HttpClient httpClient,
        ApplicationDbContext db,
        IOptions<OpenAiOptions> options,
        ILogger<AssetAiAgent> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _options = options.Value;
        _logger = logger;

        if (Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
            _httpClient.BaseAddress = baseUri;
    }

    public async Task<AssetAgentResponse> AskAsync(string question, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "AI agent is not configured. Set OpenAI__ApiKey using user-secrets or an environment variable.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var usedTools = new HashSet<string>(StringComparer.Ordinal);
        string? previousResponseId = null;
        object input = question.Trim();

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var payload = new
            {
                model = _options.Model,
                instructions = """
                    You are a read-only IT asset management assistant. Answer in the user's language.
                    Use tools for every claim about company data. Never invent counts, asset identifiers,
                    dates, owners, or statuses. Clearly distinguish facts from recommendations. You cannot
                    modify data; when asked to modify it, explain the appropriate approval workflow.
                    Keep responses concise and mention the relevant asset control numbers when available.
                    """,
                input,
                previous_response_id = previousResponseId,
                tools = ToolDefinitions,
                tool_choice = "auto"
            };

            using var response = await _httpClient.PostAsJsonAsync("responses", payload, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI Responses API failed with {StatusCode}: {Body}",
                    response.StatusCode, Truncate(responseBody, 500));
                throw new InvalidOperationException("The AI provider is temporarily unavailable.");
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            previousResponseId = root.GetProperty("id").GetString();
            var calls = ReadToolCalls(root).ToList();

            if (calls.Count == 0)
                return new AssetAgentResponse(ReadOutputText(root), usedTools.ToArray());

            var outputs = new List<object>();
            foreach (var call in calls)
            {
                usedTools.Add(call.Name);
                var result = await ExecuteToolAsync(call.Name, call.Arguments, cancellationToken);
                outputs.Add(new { type = "function_call_output", call_id = call.CallId, output = result });
            }

            input = outputs;
        }

        throw new InvalidOperationException("The AI agent exceeded its safe tool-call limit.");
    }

    private async Task<string> ExecuteToolAsync(
        string name,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        using var arguments = JsonDocument.Parse(argumentsJson);
        var root = arguments.RootElement;

        object result = name switch
        {
            "get_asset_summary" => await GetAssetSummaryAsync(cancellationToken),
            "get_expiring_licenses" => await GetExpiringLicensesAsync(
                ReadInt(root, "days", 30, 1, 365), cancellationToken),
            "search_assets" => await SearchAssetsAsync(
                ReadString(root, "query"), ReadInt(root, "limit", 10, 1, 25), cancellationToken),
            "get_pending_requests" => await GetPendingRequestsAsync(
                ReadInt(root, "limit", 10, 1, 25), cancellationToken),
            _ => new { error = $"Unknown tool: {name}" }
        };

        return JsonSerializer.Serialize(result);
    }

    private async Task<object> GetAssetSummaryAsync(CancellationToken cancellationToken)
    {
        var hardwareTotal = await _db.ITAssets.AsNoTracking().CountAsync(cancellationToken);
        var hardwareByStatus = await _db.ITAssets.AsNoTracking()
            .GroupBy(asset => asset.Status != null ? asset.Status.StatusName : "Unknown")
            .Select(group => new { status = group.Key, count = group.Count() })
            .ToListAsync(cancellationToken);
        var activeLicenses = await _db.Licenses.AsNoTracking()
            .CountAsync(license => !license.IsDeleted && license.LicenseStatus == "Active", cancellationToken);
        var availableSeats = await _db.Licenses.AsNoTracking()
            .Where(license => !license.IsDeleted)
            .SumAsync(license => license.NumberAvailable, cancellationToken);

        return new { hardwareTotal, hardwareByStatus, activeLicenses, availableLicenseSeats = availableSeats };
    }

    private async Task<object> GetExpiringLicensesAsync(int days, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var until = today.AddDays(days);
        var licenses = await _db.Licenses.AsNoTracking()
            .Where(license => !license.IsDeleted && license.ExpiryDate >= today && license.ExpiryDate <= until)
            .OrderBy(license => license.ExpiryDate)
            .Take(50)
            .Select(license => new
            {
                license.LicenseManagementNumber,
                license.InstallationName,
                license.ExpiryDate,
                license.NumberOfLicenses,
                license.NumberAvailable,
                department = license.ManagementDepartment != null
                    ? license.ManagementDepartment.DepartmentName : null
            })
            .ToListAsync(cancellationToken);

        return new { asOfUtc = today, withinDays = days, count = licenses.Count, licenses };
    }

    private async Task<object> SearchAssetsAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new { error = "query is required" };

        query = query.Trim();
        var assets = await _db.ITAssets.AsNoTracking()
            .Where(asset =>
                (asset.AssetControlNumber != null && asset.AssetControlNumber.Contains(query)) ||
                (asset.AssetName != null && asset.AssetName.Contains(query)) ||
                (asset.Manufacturer != null && asset.Manufacturer.Contains(query)) ||
                (asset.Model != null && asset.Model.Contains(query)) ||
                (asset.SerialNumber != null && asset.SerialNumber.Contains(query)))
            .OrderBy(asset => asset.AssetControlNumber)
            .Take(limit)
            .Select(asset => new
            {
                asset.AssetControlNumber,
                asset.AssetName,
                asset.Manufacturer,
                asset.Model,
                asset.SerialNumber,
                status = asset.Status != null ? asset.Status.StatusName : null,
                department = asset.Department != null ? asset.Department.DepartmentName : null,
                location = asset.Location != null ? asset.Location.LocationName : null,
                asset.WarrantyExpiry
            })
            .ToListAsync(cancellationToken);

        return new { query, count = assets.Count, assets };
    }

    private async Task<object> GetPendingRequestsAsync(int limit, CancellationToken cancellationToken)
    {
        var requests = await _db.Requests.AsNoTracking()
            .Where(request => request.ApprovedAt == null && request.RejectedAt == null)
            .OrderBy(request => request.CreatedAt)
            .Take(limit)
            .Select(request => new
            {
                request.RequestId,
                type = request.RequestType != null ? request.RequestType.TypeName : null,
                status = request.Status != null ? request.Status.StatusName : null,
                request.RequestDescription,
                request.CreatedAt,
                assetControlNumber = request.Asset != null ? request.Asset.AssetControlNumber : null
            })
            .ToListAsync(cancellationToken);

        return new { count = requests.Count, requests };
    }

    private static IEnumerable<ToolCall> ReadToolCalls(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output)) yield break;
        foreach (var item in output.EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "function_call") continue;
            yield return new ToolCall(
                item.GetProperty("call_id").GetString() ?? string.Empty,
                item.GetProperty("name").GetString() ?? string.Empty,
                item.GetProperty("arguments").GetString() ?? "{}");
        }
    }

    private static string ReadOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output)) return "No answer was generated.";
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content)) continue;
            foreach (var part in content.EnumerateArray())
                if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text))
                    return text.GetString() ?? string.Empty;
        }
        return "No answer was generated.";
    }

    private static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static int ReadInt(JsonElement root, string name, int fallback, int min, int max) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, min, max) : fallback;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ToolCall(string CallId, string Name, string Arguments);

    private static readonly object[] ToolDefinitions =
    [
        Function("get_asset_summary", "Get current hardware and software-license totals grouped by status.",
            new { type = "object", properties = new { }, required = Array.Empty<string>(), additionalProperties = false }),
        Function("get_expiring_licenses", "List licenses expiring within a number of days.",
            new { type = "object", properties = new { days = new { type = "integer", minimum = 1, maximum = 365 } }, required = new[] { "days" }, additionalProperties = false }),
        Function("search_assets", "Search hardware by control number, name, manufacturer, model, or serial number.",
            new { type = "object", properties = new { query = new { type = "string" }, limit = new { type = "integer", minimum = 1, maximum = 25 } }, required = new[] { "query", "limit" }, additionalProperties = false }),
        Function("get_pending_requests", "Get asset requests that have not yet been approved or rejected.",
            new { type = "object", properties = new { limit = new { type = "integer", minimum = 1, maximum = 25 } }, required = new[] { "limit" }, additionalProperties = false })
    ];

    private static object Function(string name, string description, object parameters) =>
        new { type = "function", name, description, parameters, strict = true };
}
