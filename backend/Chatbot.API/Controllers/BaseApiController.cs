using Chatbot.API.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse<T>> CreatedSuccess<T>(string uri, T data, string message = "Resource created successfully")
    {
        return Created(uri, ApiResponse<T>.Ok(data, message));
    }
}
