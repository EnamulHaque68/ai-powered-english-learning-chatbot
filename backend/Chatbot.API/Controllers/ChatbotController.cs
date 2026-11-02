using Chatbot.API.DTOs.Common;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.Entities;
using Chatbot.API.Services.Chatbot;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ChatbotController : BaseApiController
{
    private readonly IRuleBasedChatbotEngine _engine;

    public ChatbotController(IRuleBasedChatbotEngine engine)
    {
        _engine = engine;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<ChatbotReplyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChatbotReplyResponse>>> SendMessage([FromBody] ChatbotMessageRequest request)
    {
        if (request.ChildId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Fail("ChildId must be specified."));

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(ApiResponse<object>.Fail("Message cannot be empty."));

        var reply = await _engine.ProcessMessageAsync(request);
        return Success(reply, "Message processed successfully.");
    }

    [HttpPost("switch-mode")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<string>>> SwitchMode([FromBody] SwitchModeRequest request)
    {
        var newMode = await _engine.SwitchModeAsync(request.SessionId, request.NewMode);
        return Success(newMode.ToString(), $"Chatbot switched to {newMode} mode.");
    }

    [HttpGet("sessions/{sessionId:guid}/context")]
    [ProducesResponseType(typeof(ApiResponse<SessionContextDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionContextDto>>> GetContext(Guid sessionId)
    {
        var context = await _engine.GetSessionContextAsync(sessionId);
        return Success(context);
    }
}
