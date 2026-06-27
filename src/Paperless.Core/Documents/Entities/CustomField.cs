using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a user-defined custom field that can be assigned to documents.
/// Maps to the CustomField model from the original paperless-ngx data model.
/// </summary>
public class CustomField : BaseEntity
{
    /// <summary>
    /// The display name of the custom field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The data type of this custom field.
    /// </summary>
    public CustomFieldType Type { get; set; } = CustomFieldType.Text;

    /// <summary>
    /// Optional extra configuration data stored as JSON.
    /// Used for type-specific settings (e.g., select options, validation rules).
    /// </summary>
    public string? ExtraData { get; set; }

    // ── Navigation ─────────────────────────────────────────────────

    /// <summary>
    /// Many-to-many via join entity: the document-field value assignments.
    /// </summary>
    public ICollection<DocumentCustomField> DocumentCustomFields { get; set; } = new List<DocumentCustomField>();
}
