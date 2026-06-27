using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.ValueObjects;

namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a storage path template that determines where document files
/// are saved on the filesystem. The path is rendered using a template engine
/// (Fluid, replacing the original Jinja2).
/// Inherits matching capabilities via <see cref="IMatchingModel"/>.
/// Maps to the StoragePath model from the original paperless-ngx data model.
/// </summary>
public class StoragePath : BaseEntity, IMatchingModel
{
    /// <summary>
    /// The display name of the storage path configuration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A URL-safe slug derived from the name.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// The path template string (Fluid syntax, replacing Jinja2 templates
    /// from the original paperless-ngx). Example: "{correspondent}/{title}" .
    /// </summary>
    public string? PathTemplate { get; set; }

    // ── IMatchingModel ─────────────────────────────────────────────

    /// <inheritdoc />
    public string? Match { get; set; }

    /// <inheritdoc />
    public MatchingAlgorithm MatchingAlgorithm { get; set; } = MatchingAlgorithm.Auto;

    /// <inheritdoc />
    public bool IsInsensitive { get; set; } = true;
}
