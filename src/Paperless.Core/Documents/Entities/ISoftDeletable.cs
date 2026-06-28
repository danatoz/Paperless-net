namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Defines soft-delete properties for entities that should not be
/// physically removed from the database when deleted.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Soft-delete flag. When true, the entity is considered deleted
    /// but remains in the database.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the entity was soft-deleted (UTC). Null if not deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
