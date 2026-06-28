using System.Diagnostics;

namespace Paperless.Api.Middleware;

/// <summary>
/// Middleware that logs incoming HTTP requests and their responses,
/// including method, path, status code, and elapsed time.
///
/// <para>
/// This middleware should be placed early in the pipeline (after exception
/// handling but before CORS) to capture all request/response activity.
/// </para>
///
/// <para>
/// Full implementation with structured logging, sensitive-data masking,
/// and configurable exclusion patterns is in M2-16. This initial version
/// provides basic pass-through logging to unblock the pipeline setup.
/// </para>
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
