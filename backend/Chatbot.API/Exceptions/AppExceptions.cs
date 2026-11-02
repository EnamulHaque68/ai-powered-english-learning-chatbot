namespace Chatbot.API.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.", 404)
    {
    }

    public NotFoundException(string message) : base(message, 404)
    {
    }
}

public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", 400)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base("Validation failed.", 400)
    {
        Errors = new Dictionary<string, string[]>
        {
            [field] = new[] { error }
        };
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409)
    {
    }
}
