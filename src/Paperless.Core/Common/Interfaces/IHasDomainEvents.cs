using MediatR;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Defines an interface for domain entities that can hold domain events.
/// Domain events are collected during business operations and dispatched
/// after the unit of work successfully persists changes to the database.
/// </summary>
/// <remarks>
/// Analogous to Django signals raised during model lifecycle operations.
/// Events are dispatched via MediatR <see cref="INotification"/> handlers.
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the read-only collection of pending domain events
    /// that have not yet been dispatched.
    /// </summary>
    IReadOnlyCollection<INotification> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to the entity's pending event collection.
    /// The event will be dispatched after the next <c>SaveChangesAsync</c>
    /// on the unit of work.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    void AddDomainEvent(INotification domainEvent);

    /// <summary>
    /// Clears all pending domain events from the entity.
    /// Called automatically after events are dispatched.
    /// </summary>
    void ClearDomainEvents();
}
