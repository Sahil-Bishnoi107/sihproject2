using BidGuard.Application.Common.Exceptions;
using BidGuard.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Infrastructure;

/// <summary>Maps application and domain exceptions to RFC 7807 ProblemDetails responses.</summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, problem) = exception switch
        {
            ValidationException v => (StatusCodes.Status400BadRequest, "Validation failed",
                (ProblemDetails)new ValidationProblemDetails(v.Errors)),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", new ProblemDetails()),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", new ProblemDetails()),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", new ProblemDetails()),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", new ProblemDetails()),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated", new ProblemDetails()),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", new ProblemDetails()),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        problem.Status = status;
        problem.Title = title;
        problem.Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message;
        problem.Instance = context.Request.Path;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
