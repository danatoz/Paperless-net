namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Defines the algorithm used for matching a <see cref="Entities.Correspondent"/>,
/// <see cref="Entities.Tag"/>, <see cref="Entities.DocumentType"/>, or
/// <see cref="Entities.StoragePath"/> against document data.
/// Maps to the matching algorithm choices in the original paperless-ngx data model.
/// </summary>
public enum MatchingAlgorithm
{
    /// <summary>Match if any of the terms appear (logical OR).</summary>
    Any = 1,

    /// <summary>Match only if all terms appear (logical AND).</summary>
    All = 2,

    /// <summary>Match the exact literal string.</summary>
    Literal = 3,

    /// <summary>Match using a regular expression pattern.</summary>
    Regex = 4,

    /// <summary>Match using a fuzzy (approximate) string comparison.</summary>
    Fuzzy = 5,

    /// <summary>Automatically determine the best matching strategy.</summary>
    Auto = 6,
}
