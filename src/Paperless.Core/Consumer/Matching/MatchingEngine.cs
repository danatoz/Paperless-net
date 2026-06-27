using System.Text.RegularExpressions;
using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Consumer.Matching;

/// <summary>
/// Pure matching logic engine that evaluates whether a value matches against patterns
/// using various algorithms. No dependencies on databases or external services.
/// Maps to the matching logic from documents/matching.py.
/// </summary>
public sealed class MatchingEngine
{
    private readonly FuzzyMatcher _fuzzyMatcher;

    /// <summary>
    /// Initializes a new instance with the default fuzzy matcher (threshold: 0.8).
    /// </summary>
    public MatchingEngine()
        : this(new FuzzyMatcher())
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom fuzzy matcher.
    /// </summary>
    /// <param name="fuzzyMatcher">The fuzzy matcher to use for <see cref="MatchingAlgorithm.Fuzzy"/> and <see cref="MatchingAlgorithm.Auto"/> algorithms.</param>
    public MatchingEngine(FuzzyMatcher fuzzyMatcher)
    {
        _fuzzyMatcher = fuzzyMatcher ?? throw new ArgumentNullException(nameof(fuzzyMatcher));
    }

    /// <summary>
    /// Evaluates whether a single pattern matches the given value using the specified algorithm.
    /// </summary>
    /// <param name="algorithm">The matching algorithm to use.</param>
    /// <param name="pattern">The pattern to match.</param>
    /// <param name="value">The value to test against the pattern.</param>
    /// <param name="isInsensitive">Whether matching should be case-insensitive (default: true).</param>
    /// <returns>True if the pattern matches the value; otherwise false.</returns>
    public bool Match(MatchingAlgorithm algorithm, string pattern, string value, bool isInsensitive = true)
    {
        if (string.IsNullOrEmpty(pattern)) return false;
        if (value is null) return false;

        return algorithm switch
        {
            MatchingAlgorithm.None => true,
            MatchingAlgorithm.Any => MatchAny(new[] { pattern }, value, isInsensitive),
            MatchingAlgorithm.All => MatchAll(new[] { pattern }, value, isInsensitive),
            MatchingAlgorithm.Literal => MatchLiteral(pattern, value, isInsensitive),
            MatchingAlgorithm.Regex => MatchRegex(pattern, value, isInsensitive),
            MatchingAlgorithm.Fuzzy => MatchFuzzy(pattern, value),
            MatchingAlgorithm.Auto => MatchAuto(pattern, value, isInsensitive),
            _ => false
        };
    }

    /// <summary>
    /// Match if at least one of the patterns appears in the value (logical OR).
    /// </summary>
    /// <param name="patterns">The list of patterns to search for.</param>
    /// <param name="value">The value to search within.</param>
    /// <param name="insensitive">Whether matching is case-insensitive (default: true).</param>
    /// <returns>True if any pattern is found in the value.</returns>
    public bool MatchAny(IEnumerable<string> patterns, string value, bool insensitive = true)
    {
        var comparison = insensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return patterns.Any(pattern =>
            !string.IsNullOrEmpty(pattern) &&
            value.Contains(pattern, comparison));
    }

    /// <summary>
    /// Match only if all patterns appear in the value (logical AND).
    /// </summary>
    /// <param name="patterns">The list of patterns to search for.</param>
    /// <param name="value">The value to search within.</param>
    /// <param name="insensitive">Whether matching is case-insensitive (default: true).</param>
    /// <returns>True if all patterns are found in the value.</returns>
    public bool MatchAll(IEnumerable<string> patterns, string value, bool insensitive = true)
    {
        var comparison = insensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return patterns.All(pattern =>
            !string.IsNullOrEmpty(pattern) &&
            value.Contains(pattern, comparison));
    }

    /// <summary>
    /// Match the exact literal string.
    /// </summary>
    /// <param name="pattern">The exact string to match.</param>
    /// <param name="value">The value to compare.</param>
    /// <param name="insensitive">Whether matching is case-insensitive (default: true).</param>
    /// <returns>True if the value equals the pattern.</returns>
    public bool MatchLiteral(string pattern, string value, bool insensitive = true)
    {
        if (string.IsNullOrEmpty(pattern)) return false;

        var comparison = insensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return value.Equals(pattern, comparison);
    }

    /// <summary>
    /// Match using a regular expression pattern.
    /// Invalid regex patterns return false (no exception thrown).
    /// </summary>
    /// <param name="pattern">The regex pattern.</param>
    /// <param name="value">The value to test against the regex.</param>
    /// <param name="insensitive">Whether matching is case-insensitive (default: true).</param>
    /// <returns>True if the regex matches the value; false on invalid regex or no match.</returns>
    public bool MatchRegex(string pattern, string value, bool insensitive = true)
    {
        if (string.IsNullOrEmpty(pattern)) return false;

        try
        {
            var options = insensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.IsMatch(value, pattern, options);
        }
        catch (RegexParseException)
        {
            // Invalid regex returns false (matching.py behavior)
            return false;
        }
    }

    /// <summary>
    /// Match using fuzzy (approximate) string comparison based on Levenshtein distance.
    /// </summary>
    /// <param name="pattern">The pattern to match.</param>
    /// <param name="value">The value to compare.</param>
    /// <returns>True if the fuzzy similarity ratio meets or exceeds the configured threshold.</returns>
    public bool MatchFuzzy(string pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern)) return false;
        return _fuzzyMatcher.IsMatch(pattern, value);
    }

    /// <summary>
    /// Automatically determines the best matching strategy:
    /// - If the pattern contains regex special characters, use regex matching.
    /// - Otherwise, use fuzzy matching.
    /// </summary>
    /// <param name="pattern">The pattern to match.</param>
    /// <param name="value">The value to test.</param>
    /// <param name="insensitive">Whether matching is case-insensitive (default: true).</param>
    /// <returns>True if the auto-detected algorithm matches the value.</returns>
    public bool MatchAuto(string pattern, string value, bool insensitive = true)
    {
        if (string.IsNullOrEmpty(pattern)) return false;

        // If the pattern contains regex special characters, use regex
        if (ContainsRegexSpecialChars(pattern))
        {
            return MatchRegex(pattern, value, insensitive);
        }

        // Otherwise use fuzzy matching
        return MatchFuzzy(pattern, value);
    }

    /// <summary>
    /// Determines whether a pattern contains regex special characters.
    /// </summary>
    private static bool ContainsRegexSpecialChars(string pattern)
    {
        // Check for common regex special characters
        return pattern.Any(c => c is '.' or '*' or '+' or '?' or '^' or '$' or '(' or ')' or '[' or ']' or '{' or '}' or '|' or '\\');
    }
}
