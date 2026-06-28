using System.Net;
using System.Text.Json;

namespace Paperless.Api.Middleware;

/// <summary>
/// Middleware that catches unhandled exceptions and returns a structured JSON
/// error response. This is the outermost middleware in the pipeline — any
/// exception thrown downstream is caught here, logged, and translated into
/// an RFC 7807 Problem Details response.
///
/// <para>
/// Behaviour by exception type:
/// <list type="bullet">
///   <item><see cref="NotFoundException"/> → 404 Not Found</item>
///   <item><see cref="ValidationException"/> → 400 Bad Request with details</item>
///   <item><see cref="UnauthorizedAccessException"/> → 403 Forbidden</item>
///   <item>All other exceptions → 500 Internal Server Error</item>
/// </list>
/// </para>
///
/// <para>
/// Full implementation is in M2-16. This initial version provides a working
/// pass-through with basic error handling to unblock the pipeline setup.
/// </para>
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, (int)HttpStatusCode.NotFound, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, (int)HttpStatusCode.BadRequest, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, (int)HttpStatusCode.Forbidden, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteProblemDetailsAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                "An internal server error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title = ((HttpStatusCode)statusCode).ToString(),
            status = statusCode,
            detail,
            instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key)
        : base($"Resource \"{name}\" ({key}) was not found.") { }
}

/// <summary>
/// Exception thrown when validation of a request fails.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
