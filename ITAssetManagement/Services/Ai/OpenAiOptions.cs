namespace ITAssetManagement.Services.Ai;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-5-mini";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
}
