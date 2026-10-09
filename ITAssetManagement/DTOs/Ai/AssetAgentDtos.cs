using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.DTOs.Ai;

public sealed class AssetAgentRequest
{
    [Required, StringLength(2000, MinimumLength = 2)]
    public string Question { get; init; } = string.Empty;
}

public sealed record AssetAgentResponse(string Answer, IReadOnlyCollection<string> ToolsUsed);
