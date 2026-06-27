using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.ValueObjects;

namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a tag that can be assigned to documents for classification.
/// Inherits matching capabilities via <see cref="IMatchingModel"/>.
/// Maps to the Tag model from the original paperless-ngx data model.
/// </summary>
public class Tag : BaseEntity, IMatchingModel
{
    /// <summary>
    /// The display name of the tag.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A URL-safe slug derived from the name.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// The background color of the tag (hex code, e.g. "#FF5733").
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// The text color of the tag (hex code, e.g. "#FFFFFF").
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// When true, documents tagged with this tag appear in the "Inbox" view
    /// and are considered unprocessed.
    /// </summary>
    public bool IsInboxTag { get; set; }

    // ── IMatchingModel ─────────────────────────────────────────────

    /// <inheritdoc />
    public string? Match { get; set; }

    /// <inheritdoc />
    public MatchingAlgorithm MatchingAlgorithm { get; set; } = MatchingAlgorithm.Auto;

    /// <inheritdoc />
    public bool IsInsensitive { get; set; } = true;

    // ── Navigation ─────────────────────────────────────────────────

    /// <summary>
    /// Documents associated with this tag (many-to-many).
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
