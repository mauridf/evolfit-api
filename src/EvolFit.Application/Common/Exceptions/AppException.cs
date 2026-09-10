namespace EvolFit.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    protected AppException(string message, int statusCode, string errorCode)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404, "RESOURCE_NOT_FOUND") { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409, "CONFLICT") { }
}

public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Uma ou mais validações falharam.", 400, "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(message, 401, "UNAUTHORIZED") { }
}
