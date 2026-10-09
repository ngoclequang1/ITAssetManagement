using ITAssetManagement.DTOs.Ai;
using ITAssetManagement.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ITAssetManagement.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai")]
[Route("api/ai-agent")]
public sealed class AiAgentController(IAssetAiAgent agent) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<AssetAgentResponse>> Ask(
        [FromBody] AssetAgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await agent.AskAsync(request.Question, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
