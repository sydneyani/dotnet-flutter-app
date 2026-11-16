using FluentValidation;
using Meridian.Application.Common;
using Meridian.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, errors) = Map(exception);
        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request rejected with {Status}: {Message}", status, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status >= StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Type = $"https://meridian.local/errors/{status}",
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception exception) =>
        exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                validation.Errors
                    .GroupBy(error => ToCamel(error.PropertyName))
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray())),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "The request conflicts with a business rule.", null),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource was not found.", null),
            ForbiddenException => (StatusCodes.Status403Forbidden, "You do not have access to this action.", null),
            ConflictException => (StatusCodes.Status409Conflict, "The request conflicts with existing data.", null),
            UnauthorizedAppException => (StatusCodes.Status401Unauthorized, "Authentication failed.", null),
            _ => (StatusCodes.Status500InternalServerError, "Server error.", null)
        };

    private static string ToCamel(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "request";
        }

        var leaf = name.Split('.')[^1];
        return char.ToLowerInvariant(leaf[0]) + leaf[1..];
    }
}
