namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between <see cref="Document"/>
/// and <see cref="CustomField"/>. Stores the actual value assigned to a document
/// for a specific custom field.
/// Maps to the Document-CustomField through table from the original paperless-ngx data model.
/// </summary>
public class DocumentCustomField
{
    /// <summary>
    /// Foreign key to the <see cref="Document"/>.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="Document"/>.
    /// </summary>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Foreign key to the <see cref="CustomField"/>.
    /// </summary>
    public int CustomFieldId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="CustomField"/>.
    /// </summary>
    public CustomField CustomField { get; set; } = null!;

    /// <summary>
    /// The value of the custom field for this document.
    /// Stored as a string; parsing depends on <see cref="CustomField.Type"/>.
    /// </summary>
    public string? Value { get; set; }
}
