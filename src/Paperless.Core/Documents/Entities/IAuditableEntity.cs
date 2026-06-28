namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Defines audit-tracking properties for entities that need
/// creation and modification timestamps and user tracking.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Timestamp when the entity was first created (UTC).
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was last modified (UTC).
    /// </summary>
    DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// Identifier of the user who last modified the entity.
    /// </summary>
    string? ModifiedBy { get; set; }
}
