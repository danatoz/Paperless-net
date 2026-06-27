using FluentAssertions;
using Paperless.Core.Consumer.Matching;

namespace Paperless.Core.Tests.Matching;

/// <summary>
/// Unit tests for the FuzzyMatcher class — covers Levenshtein distance,
/// similarity ratio calculation, and configurable threshold matching.
/// </summary>
public class FuzzyMatcherTests
{
    #region Constructor

    [Fact]
    public void Constructor_DefaultThreshold_Is_0_8()
    {
        var matcher = new FuzzyMatcher();

        matcher.Threshold.Should().Be(0.8);
    }

    [Fact]
    public void Constructor_CustomThreshold_Is_Set()
    {
        var matcher = new FuzzyMatcher(0.5);

        matcher.Threshold.Should().Be(0.5);
    }

    [Fact]
    public void Constructor_ThresholdAtLowerBound_DoesNotThrow()
    {
        var act = () => new FuzzyMatcher(0.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ThresholdAtUpperBound_DoesNotThrow()
    {
        var act = () => new FuzzyMatcher(1.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ThresholdBelowZero_Throws()
    {
        var act = () => new FuzzyMatcher(-0.1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("threshold");
    }

    [Fact]
    public void Constructor_ThresholdAboveOne_Throws()
    {
        var act = () => new FuzzyMatcher(1.1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("threshold");
    }

    #endregion

    #region LevenshteinDistance

    [Fact]
    public void LevenshteinDistance_EmptyStrings_ReturnsZero()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("", "");

        distance.Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("hello", "hello");

        distance.Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_FirstStringEmpty_ReturnsSecondLength()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("", "hello");

        distance.Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_SecondStringEmpty_ReturnsFirstLength()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("hello", "");

        distance.Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_OneSubstitution_ReturnsOne()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("cat", "car");

        distance.Should().Be(1);
    }

    [Fact]
    public void LevenshteinDistance_OneInsertion_ReturnsOne()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("cat", "cats");

        distance.Should().Be(1);
    }

    [Fact]
    public void LevenshteinDistance_OneDeletion_ReturnsOne()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("cats", "cat");

        distance.Should().Be(1);
    }

    [Fact]
    public void LevenshteinDistance_CompletelyDifferent_ReturnsMaxLength()
    {
        // "abc" vs "xyz" — need 3 substitutions
        var distance = FuzzyMatcher.LevenshteinDistance("abc", "xyz");

        distance.Should().Be(3);
    }

    [Fact]
    public void LevenshteinDistance_KittenToSitting_ReturnsThree()
    {
        // Classic example: kitten → sitting (subst k→s, subst e→i, insert g)
        var distance = FuzzyMatcher.LevenshteinDistance("kitten", "sitting");

        distance.Should().Be(3);
    }

    [Fact]
    public void LevenshteinDistance_SingleCharacters_Different_ReturnsOne()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("a", "b");

        distance.Should().Be(1);
    }

    [Fact]
    public void LevenshteinDistance_SingleCharacters_Same_ReturnsZero()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("a", "a");

        distance.Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_NullFirst_ReturnsSecondLength()
    {
        var distance = FuzzyMatcher.LevenshteinDistance(null!, "hello");

        distance.Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_NullSecond_ReturnsFirstLength()
    {
        var distance = FuzzyMatcher.LevenshteinDistance("hello", null!);

        distance.Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_BothNull_ReturnsZero()
    {
        var distance = FuzzyMatcher.LevenshteinDistance(null!, null!);

        distance.Should().Be(0);
    }

    #endregion

    #region CalculateRatio

    [Fact]
    public void CalculateRatio_IdenticalStrings_ReturnsOne()
    {
        var matcher = new FuzzyMatcher();

        var ratio = matcher.CalculateRatio("hello", "hello");

        ratio.Should().Be(1.0);
    }

    [Fact]
    public void CalculateRatio_CompletelyDifferent_ReturnsZero()
    {
        var matcher = new FuzzyMatcher();

        // "a" vs "b" — distance 1, maxLen 1, ratio = 1 - 1/1 = 0.0
        var ratio = matcher.CalculateRatio("a", "b");

        ratio.Should().Be(0.0);
    }

    [Fact]
    public void CalculateRatio_PartialSimilarity_ReturnsCorrectValue()
    {
        var matcher = new FuzzyMatcher();

        // "cat" vs "car" — distance 1, maxLen 3, ratio = 1 - 1/3 ≈ 0.6667
        var ratio = matcher.CalculateRatio("cat", "car");

        ratio.Should().BeApproximately(0.6667, 0.001);
    }

    [Fact]
    public void CalculateRatio_FirstEmpty_ReturnsZero()
    {
        var matcher = new FuzzyMatcher();

        var ratio = matcher.CalculateRatio("", "hello");

        ratio.Should().Be(0.0);
    }

    [Fact]
    public void CalculateRatio_SecondEmpty_ReturnsZero()
    {
        var matcher = new FuzzyMatcher();

        var ratio = matcher.CalculateRatio("hello", "");

        ratio.Should().Be(0.0);
    }

    [Fact]
    public void CalculateRatio_BothEmpty_ReturnsOne()
    {
        var matcher = new FuzzyMatcher();

        var ratio = matcher.CalculateRatio("", "");

        ratio.Should().Be(1.0);
    }

    [Fact]
    public void CalculateRatio_OneCharDifferentOnLongString_ReturnsHighRatio()
    {
        var matcher = new FuzzyMatcher();

        // "abcdefgh" vs "abcdefxh" — distance 1, maxLen 8, ratio = 1 - 1/8 = 0.875
        var ratio = matcher.CalculateRatio("abcdefgh", "abcdefxh");

        ratio.Should().BeApproximately(0.875, 0.001);
    }

    [Fact]
    public void CalculateRatio_DifferentLengths_ReturnsCorrectRatio()
    {
        var matcher = new FuzzyMatcher();

        // "hello" vs "helloo" — distance 1 (insertion), maxLen 6, ratio = 1 - 1/6 ≈ 0.8333
        var ratio = matcher.CalculateRatio("hello", "helloo");

        ratio.Should().BeApproximately(0.8333, 0.001);
    }

    #endregion

    #region IsMatch

    [Fact]
    public void IsMatch_IdenticalStrings_ReturnsTrue()
    {
        var matcher = new FuzzyMatcher();

        var result = matcher.IsMatch("hello", "hello");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_AboveThreshold_ReturnsTrue()
    {
        var matcher = new FuzzyMatcher(0.6);

        // "cat" vs "car" — ratio ≈ 0.667, above 0.6 threshold
        var result = matcher.IsMatch("cat", "car");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_BelowThreshold_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher(0.9);

        // "cat" vs "car" — ratio ≈ 0.667, below 0.9 threshold
        var result = matcher.IsMatch("cat", "car");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_AtThreshold_ReturnsTrue()
    {
        var matcher = new FuzzyMatcher(2.0 / 3.0); // ~0.6667

        // "cat" vs "car" — ratio ≈ 0.6667, at threshold
        var result = matcher.IsMatch("cat", "car");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_EmptyPattern_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher();

        var result = matcher.IsMatch("", "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_EmptyValue_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher();

        var result = matcher.IsMatch("hello", "");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_BothEmpty_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher();

        var result = matcher.IsMatch("", "");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_NullPattern_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher();

        var result = matcher.IsMatch(null!, "hello");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_NullValue_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher();

        var result = matcher.IsMatch("hello", null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_ThresholdOne_OnlyExactMatch_ReturnsTrue()
    {
        var matcher = new FuzzyMatcher(1.0);

        var result = matcher.IsMatch("exact", "exact");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMatch_ThresholdOne_SlightlyDifferent_ReturnsFalse()
    {
        var matcher = new FuzzyMatcher(1.0);

        var result = matcher.IsMatch("exact", "exactt");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMatch_ThresholdZero_Everything_ReturnsTrue()
    {
        var matcher = new FuzzyMatcher(0.0);

        var result = matcher.IsMatch("anything", "something");

        result.Should().BeTrue();
    }

    #endregion
}
