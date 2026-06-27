namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Base class for all domain entities providing common audit and soft-delete properties.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the entity was first created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last modified (UTC).
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft-delete flag. When true, the entity is considered deleted
    /// but remains in the database.
    /// </summary>
    public bool IsDeleted { get; set; }
}
