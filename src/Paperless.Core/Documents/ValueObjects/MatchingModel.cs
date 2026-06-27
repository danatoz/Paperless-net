using Paperless.Core.Documents.Enums;
using Paperless.Shared.Abstractions;

namespace Paperless.Core.Documents.ValueObjects;

/// <summary>
/// Value object that encapsulates the matching configuration for a matching-based entity.
/// Implements <see cref="IMatchingModel"/> and provides structural equality.
/// Can be used standalone or embedded within entities that need matching logic.
/// Maps to the MatchingModel mixin from the original paperless-ngx data model.
/// </summary>
public sealed class MatchingModel : ValueObject, IMatchingModel
{
    /// <inheritdoc />
    public string? Match { get; }

    /// <inheritdoc />
    public MatchingAlgorithm MatchingAlgorithm { get; }

    /// <inheritdoc />
    public bool IsInsensitive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchingModel"/> class.
    /// </summary>
    /// <param name="match">The match pattern or term.</param>
    /// <param name="algorithm">The matching algorithm.</param>
    /// <param name="isInsensitive">Whether matching is case-insensitive.</param>
    public MatchingModel(string? match, MatchingAlgorithm algorithm, bool isInsensitive)
    {
        Match = match;
        MatchingAlgorithm = algorithm;
        IsInsensitive = isInsensitive;
    }

    /// <summary>
    /// Returns a default MatchingModel with Auto algorithm and case-insensitive matching.
    /// </summary>
    public static MatchingModel Default => new(null, MatchingAlgorithm.Auto, true);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Match;
        yield return MatchingAlgorithm;
        yield return IsInsensitive;
    }
}
