using Iroute.Commerce.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Iroute.Commerce.Api.Middleware;

/// <summary>
/// Traduce excepciones a respuestas ProblemDetails (RFC 7807).
/// ValidationException -> 400, NotFoundException -> 404, ConflictException -> 409,
/// cualquier otra -> 500 sin exponer detalles internos.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException ve => CreateValidationProblem(ve),
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = exception.Message },
            ConflictException => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = exception.Message },
            OperationCanceledException => null,
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado al procesar la solicitud."
            }
        };

        if (problem is null) return true; // el cliente canceló la petición

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado en {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = exception.Message };
        if (exception.Errors.Count > 0) problem.Extensions["errors"] = exception.Errors;
        return problem;
    }
}
