using Chatbot.API.DTOs.Common;
using Chatbot.API.DTOs.LearningProgress;
using Chatbot.API.Exceptions;
using Chatbot.API.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Controllers;

[ApiController]
[Route("api/v1/children/{childId:guid}/progress")]
[Produces("application/json")]
public class LearningProgressController : BaseApiController
{
    private readonly ILearningProgressService _progressService;
    private readonly IValidator<RecordProgressRequest> _recordValidator;

    public LearningProgressController(
        ILearningProgressService progressService,
        IValidator<RecordProgressRequest> recordValidator)
    {
        _progressService = progressService;
        _recordValidator = recordValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LearningProgressResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LearningProgressResponse>>>> GetProgress(Guid childId)
    {
        var records = await _progressService.GetChildProgressAsync(childId);
        return Success(records);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<ChildProgressSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChildProgressSummaryResponse>>> GetSummary(Guid childId)
    {
        var summary = await _progressService.GetSummaryAsync(childId);
        return Success(summary);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LearningProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LearningProgressResponse>>> RecordProgress(
        Guid childId,
        [FromBody] RecordProgressRequest request)
    {
        var validationResult = await _recordValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var result = await _progressService.RecordProgressAsync(childId, request);
        return Success(result, "Progress recorded successfully.");
    }
}
