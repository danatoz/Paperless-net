using Hangfire;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using Paperless.Api.Middleware;
using Paperless.Infrastructure.Queue;

namespace Paperless.Api.Extensions;

/// <summary>
/// Extension methods for configuring the ASP.NET Core middleware pipeline.
///
/// <para>
/// Usage in <c>Program.cs</c>:
/// <code>
///   app.UseApplicationMiddleware();
/// </code>
/// </para>
///
/// <para>
/// Middleware order (top = first to execute):
/// <list type="number">
///   <item>ExceptionHandlingMiddleware — outermost, catches all exceptions</item>
///   <item>RequestLoggingMiddleware — logs all requests/responses</item>
///   <item>CORS — must be before Auth for preflight requests</item>
///   <item>Authentication — validates JWT tokens</item>
///   <item>Authorization — enforces policies</item>
///   <item>OpenAPI / Swagger — serves spec + Swagger UI</item>
///   <item>Static files — serves Angular frontend (optional)</item>
///   <item>Hangfire Dashboard — /hangfire endpoint</item>
///   <item>Controllers — MVC endpoint routing (last in pipeline)</item>
/// </list>
/// </para>
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline with all Paperless middleware
    /// in the correct order. Call this after <c>app.Build()</c>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // 1. Exception handling (outermost — catches everything)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 2. Request logging
        app.UseMiddleware<RequestLoggingMiddleware>();

        // 3. CORS (must be before auth to handle preflight OPTIONS requests)
        app.UseCors("AngularSpa");

        // 4. Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // 5. OpenAPI / Swagger UI
        //    MapOpenApi() serves /openapi/{documentName}.json using the built-in
        //    .NET OpenAPI support (Microsoft.AspNetCore.OpenApi).
        //    UseSwaggerUI() serves the Swagger UI HTML interface.
        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Paperless API v1");
                options.RoutePrefix = "swagger";
            });
        }
        else
        {
            // In production, Swagger UI is still available
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Paperless API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // 6. Static files (serves the Angular SPA in production)
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // 7. Hangfire Dashboard
        var hangfireOptions = app.Services
            .GetRequiredService<IOptions<HangfireOptions>>().Value;

        if (hangfireOptions.EnableDashboard)
        {
            app.UseHangfireDashboard(
                hangfireOptions.DashboardPath,
                new DashboardOptions
                {
                    // In production, restrict this dashboard with authorization
                    Authorization = app.Environment.IsDevelopment()
                        ? []
                        : [new HangfireAuthorizationFilter()],
                    StatsPollingInterval = 2000
                });
        }

        // 8. Controllers (last — endpoint routing)
        app.MapControllers();

        return app;
    }
}

/// <summary>
/// Minimal Hangfire authorization filter that allows all requests in development
/// and denies in production (override in M2-02 with proper auth).
/// </summary>
internal class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => false;
}
