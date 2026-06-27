using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.ValueObjects;

namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a classification category for documents (e.g. "Invoice", "Receipt", "Letter").
/// Inherits matching capabilities via <see cref="IMatchingModel"/>.
/// Maps to the DocumentType model from the original paperless-ngx data model.
/// </summary>
public class DocumentType : BaseEntity, IMatchingModel
{
    /// <summary>
    /// The display name of the document type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A URL-safe slug derived from the name.
    /// </summary>
    public string? Slug { get; set; }

    // ── IMatchingModel ─────────────────────────────────────────────

    /// <inheritdoc />
    public string? Match { get; set; }

    /// <inheritdoc />
    public MatchingAlgorithm MatchingAlgorithm { get; set; } = MatchingAlgorithm.Auto;

    /// <inheritdoc />
    public bool IsInsensitive { get; set; } = true;

    // ── Navigation ─────────────────────────────────────────────────

    /// <summary>
    /// Documents associated with this document type.
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
