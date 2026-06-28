using MediatR;
using Paperless.Core.Common.Interfaces;

namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Base class for all domain entities providing common audit, soft-delete,
/// and domain event properties.
/// Implements <see cref="IAuditableEntity"/>, <see cref="ISoftDeletable"/>,
/// and <see cref="IHasDomainEvents"/>.
/// </summary>
public abstract class BaseEntity : IAuditableEntity, ISoftDeletable, IHasDomainEvents
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public int Id { get; set; }

    // ── IAuditableEntity ─────────────────────────────────────────────

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    // ── ISoftDeletable ───────────────────────────────────────────────

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    // ── IHasDomainEvents ────────────────────────────────────────────

    private readonly List<INotification> _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void AddDomainEvent(INotification domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
