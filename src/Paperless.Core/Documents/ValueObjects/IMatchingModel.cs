using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Documents.ValueObjects;

/// <summary>
/// Shared interface for entities that support pattern-based matching against document data.
/// Implemented by <see cref="Entities.Correspondent"/>, <see cref="Entities.Tag"/>,
/// <see cref="Entities.DocumentType"/>, and <see cref="Entities.StoragePath"/>.
/// Maps to the MatchingModel mixin from the original paperless-ngx data model.
/// </summary>
public interface IMatchingModel
{
    /// <summary>
    /// The match pattern or term used by the <see cref="MatchingAlgorithm"/>.
    /// </summary>
    string? Match { get; }

    /// <summary>
    /// The algorithm used to evaluate the <see cref="Match"/> pattern against document data.
    /// </summary>
    MatchingAlgorithm MatchingAlgorithm { get; }

    /// <summary>
    /// When true, the matching is case-insensitive.
    /// </summary>
    bool IsInsensitive { get; }
}
