namespace BidGuard.Application.Common.Exceptions;

public sealed class ValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation failures occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");

public sealed class ForbiddenException(string message = "You do not have access to this resource.")
    : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class UnauthorizedException(string message = "Invalid credentials.") : Exception(message);
