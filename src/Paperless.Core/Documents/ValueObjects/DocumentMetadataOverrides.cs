namespace Paperless.Core.Documents.ValueObjects;

/// <summary>
/// Optional metadata overrides that can be applied when consuming a document.
/// All fields are nullable — only provided fields override the default or auto-detected values.
/// Maps to the DocumentMetadataOverrides dataclass from documents/data_models.py.
/// </summary>
public sealed record DocumentMetadataOverrides
{
    /// <summary>
    /// Override for the correspondent ID.
    /// </summary>
    public int? CorrespondentId { get; init; }

    /// <summary>
    /// Override for the document type ID.
    /// </summary>
    public int? DocumentTypeId { get; init; }

    /// <summary>
    /// Override for the tag IDs to assign.
    /// </summary>
    public IReadOnlyCollection<int>? TagIds { get; init; }

    /// <summary>
    /// Override for the document creation date.
    /// </summary>
    public DateTime? Created { get; init; }
}
