using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.ValueObjects;

namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a correspondent (sender/creator) of documents.
/// Inherits matching capabilities via <see cref="IMatchingModel"/>.
/// Maps to the Correspondent model from the original paperless-ngx data model.
/// </summary>
public class Correspondent : BaseEntity, IMatchingModel
{
    /// <summary>
    /// The display name of the correspondent.
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
    /// Documents associated with this correspondent.
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
