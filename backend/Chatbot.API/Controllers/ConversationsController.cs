using Chatbot.API.DTOs.Common;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.Exceptions;
using Chatbot.API.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Controllers;

[ApiController]
[Route("api/v1/conversations")]
[Produces("application/json")]
public class ConversationsController : BaseApiController
{
    private readonly IConversationService _conversationService;
    private readonly IValidator<StartSessionRequest> _startSessionValidator;
    private readonly IValidator<AddMessageRequest> _addMessageValidator;

    public ConversationsController(
        IConversationService conversationService,
        IValidator<StartSessionRequest> startSessionValidator,
        IValidator<AddMessageRequest> addMessageValidator)
    {
        _conversationService = conversationService;
        _startSessionValidator = startSessionValidator;
        _addMessageValidator = addMessageValidator;
    }

    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSessionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConversationSessionResponse>>> StartSession(
        [FromBody] StartSessionRequest request)
    {
        var validationResult = await _startSessionValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var result = await _conversationService.StartSessionAsync(request);
        return CreatedSuccess($"/api/v1/conversations/sessions/{result.Id}", result, "Conversation session started.");
    }

    [HttpGet("children/{childId:guid}/sessions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConversationSessionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConversationSessionResponse>>>> GetChildSessions(
        Guid childId,
        [FromQuery] int limit = 20)
    {
        var sessions = await _conversationService.GetChildSessionsAsync(childId, limit);
        return Success(sessions);
    }

    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionDetailResponse>>> GetSessionDetail(Guid sessionId)
    {
        var session = await _conversationService.GetSessionDetailAsync(sessionId);
        return Success(session);
    }

    [HttpPost("sessions/{sessionId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<ConversationMessageResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConversationMessageResponse>>> AddMessage(
        Guid sessionId,
        [FromBody] AddMessageRequest request)
    {
        var validationResult = await _addMessageValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var result = await _conversationService.AddMessageAsync(sessionId, request);
        return CreatedSuccess($"/api/v1/conversations/sessions/{sessionId}/messages/{result.Id}", result, "Message recorded.");
    }

    [HttpPatch("sessions/{sessionId:guid}/end")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConversationSessionResponse>>> EndSession(
        Guid sessionId,
        [FromBody] EndSessionRequest request)
    {
        var result = await _conversationService.EndSessionAsync(sessionId, request);
        return Success(result, "Session completed successfully.");
    }
}
