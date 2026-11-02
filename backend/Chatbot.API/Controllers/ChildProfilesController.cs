using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.Common;
using Chatbot.API.Exceptions;
using Chatbot.API.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Controllers;

public class ChildProfilesController : BaseApiController
{
    private readonly IChildProfileService _childProfileService;
    private readonly IValidator<CreateChildProfileRequest> _createValidator;
    private readonly IValidator<UpdateChildProfileRequest> _updateValidator;

    public ChildProfilesController(
        IChildProfileService childProfileService,
        IValidator<CreateChildProfileRequest> createValidator,
        IValidator<UpdateChildProfileRequest> updateValidator)
    {
        _childProfileService = childProfileService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ChildProfileResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ChildProfileResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _childProfileService.GetAllAsync(page, pageSize, search);
        return Success(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChildProfileDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChildProfileDetailResponse>>> GetById(Guid id)
    {
        var result = await _childProfileService.GetByIdAsync(id);
        return Success(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ChildProfileResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChildProfileResponse>>> Create([FromBody] CreateChildProfileRequest request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var result = await _childProfileService.CreateAsync(request);
        return CreatedSuccess($"/api/v1/childprofiles/{result.Id}", result, "Child profile created successfully.");
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChildProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChildProfileResponse>>> Update(
        Guid id,
        [FromBody] UpdateChildProfileRequest request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new Exceptions.ValidationException(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var result = await _childProfileService.UpdateAsync(id, request);
        return Success(result, "Child profile updated successfully.");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        await _childProfileService.DeleteAsync(id);
        return Success(true, "Child profile deleted successfully.");
    }
}
