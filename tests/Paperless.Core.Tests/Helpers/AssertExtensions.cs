using FluentAssertions;
using Paperless.Shared.Abstractions;

namespace Paperless.Core.Tests.Helpers;

/// <summary>
/// Extension methods for FluentAssertions to simplify assertions on the Result/Error pattern.
/// </summary>
public static class AssertExtensions
{
    /// <summary>
    /// Asserts that the result is successful and returns the value.
    /// </summary>
    public static T ShouldBeSuccess<T>(this Result<T> result)
    {
        result.IsSuccess.Should().BeTrue("expected a successful result");
        return result.Value;
    }

    /// <summary>
    /// Asserts that the void result is successful.
    /// </summary>
    public static void ShouldBeSuccess(this Result result)
    {
        result.IsSuccess.Should().BeTrue("expected a successful result");
    }

    /// <summary>
    /// Asserts that the result is a failure and returns the error.
    /// </summary>
    public static Error ShouldBeFailure<T>(this Result<T> result)
    {
        result.IsFailure.Should().BeTrue("expected a failed result");
        return result.Error;
    }

    /// <summary>
    /// Asserts that the void result is a failure and returns the error.
    /// </summary>
    public static Error ShouldBeFailure(this Result result)
    {
        result.IsFailure.Should().BeTrue("expected a failed result");
        return result.Error;
    }

    /// <summary>
    /// Asserts that the error has the expected error code.
    /// </summary>
    public static Error ShouldHaveCode(this Error error, string expectedCode)
    {
        error.Code.Should().Be(expectedCode);
        return error;
    }
}
