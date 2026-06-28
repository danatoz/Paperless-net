using Paperless.Api.Extensions;

// ────────────────────────────────────────────────────────────
//  Paperless-ngx .NET backend — ASP.NET Core Web API host
// ────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── Logging ─────────────────────────────────────────────────
// Replace the default ILogger<T> pipeline with Serilog.
// Sinks, levels, and enrichers are read from the "Serilog"
// section in appsettings.json (+ environment overrides).
builder.Host.UseSerilogWithConfiguration();

// ── Services ────────────────────────────────────────────────
// Register all application services via the central extension
// method. This includes Core, Infrastructure, API-layer, and
// Hangfire background job processing.
builder.Services.AddApplicationServices(builder.Configuration);

// ── Kestrel ─────────────────────────────────────────────────
// Configure Kestrel from the "Kestrel" section in appsettings.json.
// Default port is 8000 (HTTP) — overridable via environment.
builder.WebHost.ConfigureKestrel(options =>
{
    // Limits and endpoint configuration are read from
    // "Kestrel" config section by ASP.NET Core automatically.
    // This explicit call ensures our defaults apply.
});

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────
// Order is critical — see ApplicationBuilderExtensions for details.
app.UseApplicationMiddleware();

// ── Start ───────────────────────────────────────────────────
app.Run();
