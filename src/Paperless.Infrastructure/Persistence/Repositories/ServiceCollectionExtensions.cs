using Microsoft.Extensions.DependencyInjection;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Extension methods for registering Infrastructure services (repositories, UnitOfWork, etc.).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all repository implementations with the DI container.
    /// </summary>
    /// <param name="services">The service collection to add repositories to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
    {
        // ── Unit of Work ───────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Document module repositories ────────────────────────────
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICorrespondentRepository, CorrespondentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IStoragePathRepository, StoragePathRepository>();
        services.AddScoped<ICustomFieldRepository, CustomFieldRepository>();

        // ── Workflow repository ─────────────────────────────────────
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();

        // ── Sharing repositories ────────────────────────────────────
        services.AddScoped<IShareLinkRepository, ShareLinkRepository>();

        // ── Saved view repository ───────────────────────────────────
        services.AddScoped<ISavedViewRepository, SavedViewRepository>();

        return services;
    }
}
