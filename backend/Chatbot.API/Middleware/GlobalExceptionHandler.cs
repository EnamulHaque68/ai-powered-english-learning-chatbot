using System.Text.Json;
using Chatbot.API.DTOs.Common;
using Chatbot.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, title, errors) = exception switch
        {
            ValidationException valEx => (
                StatusCodes.Status400BadRequest,
                valEx.Message,
                valEx.Errors
            ),
            FluentValidation.ValidationException fValEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                fValEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                notFoundEx.Message,
                (IDictionary<string, string[]>?)null
            ),
            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                conflictEx.Message,
                (IDictionary<string, string[]>?)null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected server error occurred. Please try again later.",
                (IDictionary<string, string[]>?)null
            )
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(title, errors);

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment()
            }),
            cancellationToken);

        return true;
    }
}
