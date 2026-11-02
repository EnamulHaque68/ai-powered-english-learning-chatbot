using Chatbot.API.DTOs.Common;
using Chatbot.API.DTOs.LearningLevel;
using Chatbot.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Controllers;

public class LearningLevelsController : BaseApiController
{
    private readonly ILearningLevelService _learningLevelService;

    public LearningLevelsController(ILearningLevelService learningLevelService)
    {
        _learningLevelService = learningLevelService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LearningLevelResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LearningLevelResponse>>>> GetAll()
    {
        var levels = await _learningLevelService.GetAllLevelsAsync();
        return Success(levels);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<LearningLevelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LearningLevelResponse>>> GetById(int id)
    {
        var level = await _learningLevelService.GetLevelByIdAsync(id);
        return Success(level);
    }
}
