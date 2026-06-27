using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Extension methods for registering FluentValidation validators from Paperless.Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all FluentValidation validators from the Paperless.Core assembly.
    /// </summary>
    /// <param name="services">The service collection to add validators to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCoreValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<DocumentValidator>();
        return services;
    }
}
