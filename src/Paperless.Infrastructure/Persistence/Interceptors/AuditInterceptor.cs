using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that automatically sets
/// audit properties on entities implementing <see cref="IAuditableEntity"/>.
/// <list type="bullet">
/// <item><description>On add: sets <see cref="IAuditableEntity.CreatedAt"/>, <see cref="IAuditableEntity.CreatedBy"/></description></item>
/// <item><description>On update: sets <see cref="IAuditableEntity.ModifiedAt"/>, <see cref="IAuditableEntity.ModifiedBy"/></description></item>
/// </list>
/// The current user is resolved from <see cref="IHttpContextAccessor"/> if available.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditInterceptor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">
    /// Accessor for the current HTTP context, used to resolve the current user identifier.
    /// Can be null if running outside of an HTTP context (e.g., migrations, background jobs).
    /// </param>
    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContextEventData eventData)
    {
        if (eventData.Context is null)
            return;

        var now = DateTime.UtcNow;
        var currentUser = ResolveCurrentUser();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.ModifiedBy = currentUser;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = currentUser;
                    break;
            }
        }
    }

    /// <summary>
    /// Resolves the current user identifier from the HTTP context.
    /// Returns null if the HTTP context is not available.
    /// </summary>
    private string? ResolveCurrentUser()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }
        catch
        {
            // Gracefully handle missing HTTP context (e.g., migrations, background jobs)
            return null;
        }
    }
}
