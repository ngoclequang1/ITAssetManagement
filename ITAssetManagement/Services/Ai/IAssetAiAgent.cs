using ITAssetManagement.DTOs.Ai;

namespace ITAssetManagement.Services.Ai;

public interface IAssetAiAgent
{
    Task<AssetAgentResponse> AskAsync(string question, CancellationToken cancellationToken);
}
