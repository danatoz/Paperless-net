using FluentAssertions;
using Paperless.Core.Consumer.Matching;
using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Tests.Matching;

/// <summary>
/// Unit tests for the MatchingEngine class — covers all 6 matching algorithms
/// (Any, All, Literal, Regex, Fuzzy, Auto) plus edge cases.
/// Maps to matching logic from paperless-ngx documents/matching.py.
/// </summary>
public class MatchingEngineTests
{
    #region MatchAny

    [Fact]
    public void MatchAny_AtLeastOnePatternMatches_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny(["hello", "world"], "hello universe");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAny_NoPatternMatches_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny(["xyz", "abc"], "hello world");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAny_EmptyPatternsList_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny([], "hello world");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAny_CaseInsensitiveByDefault_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny(["HELLO"], "hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAny_CaseSensitive_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny(["HELLO"], "hello world", insensitive: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAny_CaseSensitive_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny(["Hello"], "Hello World", insensitive: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAny_WithNullPatternInList_SkipsNullAndContinues()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny([null!, "hello"], "hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAny_AllPatternsNullOrEmpty_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAny([null!, "", "   "], "hello world");

        result.Should().BeFalse();
    }

    #endregion

    #region MatchAll

    [Fact]
    public void MatchAll_AllPatternsMatch_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll(["hello", "world"], "hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAll_OnePatternDoesNotMatch_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll(["hello", "xyz"], "hello world");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAll_EmptyPatternsList_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll([], "anything");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAll_CaseInsensitiveByDefault_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll(["HELLO", "WORLD"], "hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAll_CaseSensitive_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll(["Hello", "World"], "hello world", insensitive: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAll_WithNullPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll([null!, "hello"], "hello world");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAll_WithEmptyPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAll(["", "hello"], "hello world");

        result.Should().BeFalse();
    }

    #endregion

    #region MatchLiteral

    [Fact]
    public void MatchLiteral_ExactMatch_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchLiteral("hello", "hello", insensitive: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchLiteral_CaseInsensitive_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchLiteral("Hello", "hello", insensitive: true);

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchLiteral_NoMatch_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchLiteral("hello", "world");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchLiteral_EmptyPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchLiteral("", "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchLiteral_ExactMatchCaseSensitive_FailsOnDifferentCase()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchLiteral("Hello", "hello", insensitive: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchLiteral_WithWhitespace_MatchesExactly()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchLiteral("  hello  ", "  hello  ", insensitive: false);

        result.Should().BeTrue();
    }

    #endregion

    #region MatchRegex

    [Fact]
    public void MatchRegex_ValidPatternMatches_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchRegex(@"^\d{4}-\d{2}-\d{2}$", "2024-01-15");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchRegex_NoMatch_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchRegex(@"^\d+$", "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchRegex_InvalidPattern_ReturnsFalseWithoutException()
    {
        var engine = new MatchingEngine();

        var act = () => engine.MatchRegex("[invalid", "hello");

        act.Should().NotThrow();
        act().Should().BeFalse();
    }

    [Fact]
    public void MatchRegex_EmptyPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchRegex("", "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchRegex_CaseInsensitive_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchRegex("^hello", "HELLO world", insensitive: true);

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchRegex_CaseSensitive_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchRegex("^hello", "HELLO world", insensitive: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchRegex_PartialMatch_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchRegex("hello", "say hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchRegex_MalformedPattern_DoesNotThrow()
    {
        var engine = new MatchingEngine();

        var results = new[]
        {
            engine.MatchRegex(@"\", "test"),
            engine.MatchRegex("[a-z", "test"),
            engine.MatchRegex("(unclosed", "test"),
            engine.MatchRegex(@"\p{Invalid}", "test"),
        };

        results.Should().AllBeEquivalentTo(false);
    }

    #endregion

    #region MatchFuzzy

    [Fact]
    public void MatchFuzzy_ExactMatch_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchFuzzy("hello", "hello");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchFuzzy_HighSimilarity_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        // "hello" vs "hllo" — 4 out of 5 chars match → ratio 0.8 → meets default threshold 0.8
        var result = engine.MatchFuzzy("hello", "hllo");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchFuzzy_LowSimilarity_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        // "hello" vs "xyz" — completely different
        var result = engine.MatchFuzzy("hello", "xyz");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchFuzzy_EmptyPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchFuzzy("", "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchFuzzy_EmptyValue_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchFuzzy("hello", "");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchFuzzy_WithCustomThreshold_LowThreshold_ReturnsTrue()
    {
        // Threshold 0.5 — even "hello" vs "hxllo" (1 substitution) should match
        var fuzzy = new FuzzyMatcher(threshold: 0.5);
        var engine = new MatchingEngine(fuzzy);

        var result = engine.MatchFuzzy("hello", "hxllo");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchFuzzy_WithCustomThreshold_HighThreshold_ReturnsFalse()
    {
        // Threshold 0.95 — "hello" vs "hxllo" (80% similarity) should NOT match
        var fuzzy = new FuzzyMatcher(threshold: 0.95);
        var engine = new MatchingEngine(fuzzy);

        var result = engine.MatchFuzzy("hello", "hxllo");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchFuzzy_IdenticalStrings_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchFuzzy("identical", "identical");

        result.Should().BeTrue();
    }

    #endregion

    #region MatchAuto

    [Fact]
    public void MatchAuto_PatternWithSpecialChars_UsesRegexAndReturnsTrue()
    {
        var engine = new MatchingEngine();

        // Pattern contains regex special char '+' → should use regex
        var result = engine.MatchAuto("hel+o", "heloooo");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAuto_PatternWithSpecialChars_NoRegexMatch_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        // Pattern contains regex special char '.' → should use regex
        var result = engine.MatchAuto("hel.o", "world");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAuto_PatternWithoutSpecialChars_UsesFuzzyAndReturnsTrue()
    {
        var engine = new MatchingEngine();

        // Simple pattern, no special chars → fuzzy match
        // "hello" vs "hllo" ~80% similarity → meets default 0.8 threshold
        var result = engine.MatchAuto("hello", "hllo");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAuto_PatternWithoutSpecialChars_LowSimilarity_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAuto("hello", "xyz");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAuto_EmptyPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAuto("", "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchAuto_PatternWithAsterisk_UsesRegex()
    {
        var engine = new MatchingEngine();

        // Pattern contains '*' → regex match
        var result = engine.MatchAuto("hel*o", "heloooo");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAuto_PatternWithDot_UsesRegex()
    {
        var engine = new MatchingEngine();

        // Pattern contains '.' → regex match (any char)
        var result = engine.MatchAuto("hel.o", "helxo");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAuto_PatternWithParentheses_UsesRegex()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAuto("(hello)", "hello");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAuto_PatternWithPipe_UsesRegex()
    {
        var engine = new MatchingEngine();

        var result = engine.MatchAuto("hello|world", "world");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchAuto_AllSpecialChars_DetectedCorrectly()
    {
        var engine = new MatchingEngine();

        // Each of these patterns contains at least one regex special char
        var patterns = new[] { ".", "*", "+", "?", "^", "$", "(", ")", "[", "]", "{", "}", "|", "\\" };

        foreach (var pattern in patterns)
        {
            // They should all attempt regex matching (no exception expected)
            var act = () => engine.MatchAuto(pattern, "test");
            act.Should().NotThrow("pattern '{0}' should not throw", pattern);
        }
    }

    #endregion

    #region Match (single pattern via MatchingAlgorithm enum)

    [Fact]
    public void Match_NoneAlgorithm_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.None, "pattern", "value");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_AnyAlgorithm_SinglePattern_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Any, "hello", "hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_AnyAlgorithm_SinglePattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Any, "xyz", "hello world");

        result.Should().BeFalse();
    }

    [Fact]
    public void Match_AllAlgorithm_SinglePattern_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.All, "hello", "hello world");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_AllAlgorithm_SinglePattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.All, "xyz", "hello world");

        result.Should().BeFalse();
    }

    [Fact]
    public void Match_LiteralAlgorithm_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Literal, "hello", "hello");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_RegexAlgorithm_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Regex, @"^\d+$", "12345");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_FuzzyAlgorithm_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Fuzzy, "hello", "hello");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_AutoAlgorithm_ReturnsTrue()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Auto, "hello", "hllo");

        result.Should().BeTrue();
    }

    [Fact]
    public void Match_UnknownAlgorithm_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.Match((MatchingAlgorithm)999, "pattern", "value");

        result.Should().BeFalse();
    }

    [Fact]
    public void Match_NullValue_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Literal, "pattern", null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void Match_NullPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Literal, null!, "value");

        result.Should().BeFalse();
    }

    [Fact]
    public void Match_EmptyPattern_ReturnsFalse()
    {
        var engine = new MatchingEngine();

        var result = engine.Match(MatchingAlgorithm.Literal, "", "value");

        result.Should().BeFalse();
    }

    #endregion
}
