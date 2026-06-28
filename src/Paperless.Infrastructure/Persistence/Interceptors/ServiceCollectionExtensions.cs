using Microsoft.Extensions.DependencyInjection;

namespace Paperless.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Extension methods for registering EF Core interceptors with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core <see cref="SaveChangesInterceptor"/> implementations
    /// (<see cref="SoftDeleteInterceptor"/> and <see cref="AuditInterceptor"/>)
    /// with the DI container as scoped services.
    /// </summary>
    /// <param name="services">The service collection to add interceptors to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddPersistenceInterceptors(this IServiceCollection services)
    {
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<AuditInterceptor>();
        return services;
    }
}
