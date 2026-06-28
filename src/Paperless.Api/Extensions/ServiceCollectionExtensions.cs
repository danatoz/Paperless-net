using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Paperless.Api.Auth;
using Paperless.Core.Common.Validators;
using Paperless.Infrastructure.Caching;
using Paperless.Infrastructure.FileStorage;
using Paperless.Infrastructure.Persistence;
using Paperless.Infrastructure.Persistence.Interceptors;
using Paperless.Infrastructure.Persistence.Repositories;
using Paperless.Infrastructure.Queue;
using Paperless.Infrastructure.Search;

namespace Paperless.Api.Extensions;

/// <summary>
/// Centralises all service registrations for the Paperless application.
///
/// <para>
/// Usage in <c>Program.cs</c>:
/// <code>
///   builder.Services.AddApplicationServices(builder.Configuration);
/// </code>
/// </para>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services in the correct order:
    /// Core → Infrastructure → API-specific → Hangfire.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Core ─────────────────────────────────────────────────────
        services.AddCoreValidators();

        // ── Infrastructure ───────────────────────────────────────────
        services.AddInfrastructureServices(configuration);

        // ── API ──────────────────────────────────────────────────────
        services.AddApiServices(configuration);

        // ── Hangfire (cross-cutting: storage in infra, server in API) ─
        services.AddHangfireConfiguration(configuration);

        return services;
    }

    /// <summary>
    /// Registers all Infrastructure-layer services: database, repositories,
    /// file storage, search, caching, and MediatR.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Configuration binding ────────────────────────────────────
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));

        // ── ASP.NET Core essentials ──────────────────────────────────
        // Required by AuditInterceptor (uses IHttpContextAccessor for user context).
        services.AddHttpContextAccessor();

        // ── MediatR ──────────────────────────────────────────────────
        // Register MediatR handlers from Core and Infrastructure assemblies.
        // Handlers for domain events (DocumentCreatedEvent, etc.) are in Core;
        // index-sync handlers (DocumentSearchIndexEventHandler) are in Infrastructure.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(Paperless.Core.Documents.Entities.Document).Assembly);
            cfg.RegisterServicesFromAssembly(
                typeof(Paperless.Infrastructure.Persistence.AppDbContext).Assembly);
        });

        // ── Database ─────────────────────────────────────────────────
        // DbContext is configured via its OnConfiguring method which reads
        // DatabaseOptions from DI and adds interceptors automatically.
        services.AddDbContext<AppDbContext>();

        // ── Repositories ─────────────────────────────────────────────
        services.AddInfrastructureRepositories();

        // ── EF Core Interceptors ─────────────────────────────────────
        services.AddPersistenceInterceptors();

        // ── Cache ────────────────────────────────────────────────────
        services.AddRedisCache(configuration);

        // ── File storage ─────────────────────────────────────────────
        services.AddFileStorage(configuration);

        // ── Search ───────────────────────────────────────────────────
        services.AddLuceneSearch();

        return services;
    }

    /// <summary>
    /// Registers API-layer services: controllers, authentication, CORS,
    /// OpenAPI, and JWT bearer configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── FluentValidation ───────────────────────────────────────
        // Must be registered before controllers so that the auto-validation
        // middleware can intercept requests with invalid DTOs.
        services.AddFluentValidationAutoValidation(cfg =>
        {
            // Disable the built-in DataAnnotations validation to let
            // FluentValidation handle all request validation.
            cfg.DisableDataAnnotationsValidation = true;
        });

        // Register all FluentValidation validators from the API assembly.
        services.AddValidatorsFromAssemblyContaining<Program>();

        // ── Controllers ──────────────────────────────────────────────
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        // ── CORS ─────────────────────────────────────────────────────
        var corsOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("AngularSpa", policy =>
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // ── JwtOptions binding ───────────────────────────────────────
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // ── Authentication (JWT) ─────────────────────────────────────
        // Two schemes are supported:
        //   1. "Bearer"  — standard Authorization: Bearer <jwt> (JwtBearerHandler)
        //   2. "Token"   — DRF-compatible Authorization: Token <jwt> (TokenAuthHandler)
        //
        // A composite "Fallback" policy scheme forwards to the correct handler
        // based on the Authorization header prefix. This allows both the Angular
        // SPA (which may use Bearer) and legacy clients (which use Token) to
        // authenticate seamlessly.
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? "Default-Dev-Key-Not-For-Production-!";
        var jwtIssuer = jwtSection["Issuer"] ?? "Paperless";
        var jwtAudience = jwtSection["Audience"] ?? "Paperless-SPA";

        const string compositeScheme = "Fallback";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = compositeScheme;
            options.DefaultChallengeScheme = compositeScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddScheme<AuthenticationSchemeOptions, TokenAuthHandler>("Token", null)
        .AddPolicyScheme(compositeScheme, "Fallback", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader?.StartsWith("Token ", StringComparison.OrdinalIgnoreCase) == true)
                    return "Token";
                return JwtBearerDefaults.AuthenticationScheme;
            };
        });

        // ── Authorization ────────────────────────────────────────────
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
            options.AddPolicy("UserOrAdmin", policy =>
                policy.RequireAuthenticatedUser());
        });

        // ── Auth Service ─────────────────────────────────────────────
        services.Configure<DefaultUsersOptions>(
            configuration.GetSection(DefaultUsersOptions.SectionName));
        services.AddScoped<IAuthService, AuthService>();

        // ── OpenAPI ──────────────────────────────────────────────────
        // Uses the built-in .NET OpenAPI support (Microsoft.AspNetCore.OpenApi),
        // which provides AddOpenApi() / MapOpenApi() in the pipeline.
        // This replaces the classic Swashbuckle AddSwaggerGen approach.
        services.AddOpenApi(options =>
        {
            // Configure OpenAPI document options if needed in the future.
            // Security schemes can be added here for JWT support.
        });

        return services;
    }
}
