namespace Paperless.Core.Consumer.Matching;

/// <summary>
/// Provides fuzzy string matching using Levenshtein distance.
/// Maps to fuzzy matching logic from documents/matching.py.
/// </summary>
public sealed class FuzzyMatcher
{
    private readonly double _threshold;

    /// <summary>
    /// Initializes a new instance with the specified similarity threshold.
    /// </summary>
    /// <param name="threshold">
    /// The minimum similarity ratio (0.0 to 1.0) required for a match.
    /// Default is 0.8 (80% similarity).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is not in the range [0.0, 1.0].
    /// </exception>
    public FuzzyMatcher(double threshold = 0.8)
    {
        if (threshold is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be between 0.0 and 1.0.");
        _threshold = threshold;
    }

    /// <summary>
    /// Gets the current similarity threshold.
    /// </summary>
    public double Threshold => _threshold;

    /// <summary>
    /// Determines whether two strings match based on the configured similarity threshold.
    /// </summary>
    /// <param name="pattern">The pattern to match against.</param>
    /// <param name="value">The value to compare.</param>
    /// <returns>True if the similarity ratio is at or above the threshold; otherwise false.</returns>
    public bool IsMatch(string pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(value))
            return false;

        var ratio = CalculateRatio(pattern, value);
        return ratio >= _threshold;
    }

    /// <summary>
    /// Calculates the similarity ratio between two strings using Levenshtein distance.
    /// Returns a value between 0.0 (completely different) and 1.0 (identical).
    /// </summary>
    public double CalculateRatio(string a, string b)
    {
        if (a == b) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

        var distance = LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// The Levenshtein distance is the minimum number of single-character edits
    /// (insertions, deletions, substitutions) required to change one string into the other.
    /// </summary>
    public static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var lenA = a.Length;
        var lenB = b.Length;

        // Use a single-row optimization for space efficiency
        var previous = new int[lenB + 1];
        var current = new int[lenB + 1];

        // Initialize the first row
        for (var j = 0; j <= lenB; j++)
            previous[j] = j;

        for (var i = 1; i <= lenA; i++)
        {
            current[0] = i;

            for (var j = 1; j <= lenB; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                current[j] = Math.Min(
                    Math.Min(
                        current[j - 1] + 1,     // Insertion
                        previous[j] + 1),        // Deletion
                    previous[j - 1] + cost);     // Substitution
            }

            // Swap rows
            (previous, current) = (current, previous);
        }

        return previous[lenB];
    }
}
