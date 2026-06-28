using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence;

/// <summary>
/// Implementation of <see cref="IUnitOfWork"/> that wraps <see cref="AppDbContext"/>
/// and provides transaction management with automatic domain event dispatch.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SaveChangesAsync"/> is called, the implementation:
/// <list type="number">
///   <item>Collects all pending domain events from tracked entities implementing <see cref="IHasDomainEvents"/>.</item>
///   <item>Clears the events from the entities to prevent re-dispatch.</item>
///   <item>Saves changes to the database via EF Core.</item>
///   <item>Dispatches the collected domain events via MediatR <c>IPublisher</c>.</item>
/// </list>
/// </para>
/// <para>
/// This pattern ensures that domain events are only dispatched after
/// successful persistence (atomicity), which is analogous to how Django
/// signals are fired after the database commit.
/// </para>
/// <para>
/// For transaction support (<see cref="BeginTransactionAsync"/>, <see cref="CommitAsync"/>,
/// <see cref="RollbackAsync"/>), the implementation delegates to
/// <c>DbContext.Database</c> transaction APIs, which is the EF Core equivalent
/// of Django's <c>transaction.atomic()</c>.
/// </para>
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<UnitOfWork> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The application EF Core DbContext.</param>
    /// <param name="publisher">MediatR publisher for dispatching domain events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public UnitOfWork(
        AppDbContext context,
        IPublisher publisher,
        ILogger<UnitOfWork> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Capture pending domain events from all tracked entities
        // before SaveChanges so we don't lose them if the change tracker is cleared.
        var entitiesWithEvents = _context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Clear events from entities to prevent re-dispatch on subsequent saves
        // or if an exception occurs during dispatch (the save already succeeded).
        foreach (var entity in entitiesWithEvents)
        {
            entity.Entity.ClearDomainEvents();
        }

        _logger.LogDebug(
            "Captured {EventCount} domain event(s) from {EntityCount} entity(ies) before SaveChanges",
            domainEvents.Count,
            entitiesWithEvents.Count);

        // Persist changes to the database
        var result = await _context.SaveChangesAsync(ct);

        _logger.LogDebug(
            "SaveChanges completed: {RowsAffected} row(s) written",
            result);

        // Dispatch domain events only after successful persistence.
        // This is the key difference from the interceptor-based approach:
        // events are fired after the commit, not before.
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogTrace(
                "Dispatching domain event {EventType}",
                domainEvent.GetType().Name);

            await _publisher.Publish(domainEvent, ct);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Beginning database transaction");
        await _context.Database.BeginTransactionAsync(ct);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Committing database transaction");
        await _context.Database.CommitTransactionAsync(ct);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Rolling back database transaction");
        await _context.Database.RollbackTransactionAsync(ct);
    }
}
