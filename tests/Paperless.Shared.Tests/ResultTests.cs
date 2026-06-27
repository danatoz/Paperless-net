using FluentAssertions;
using Paperless.Shared.Abstractions;

namespace Paperless.Shared.Tests;

public class ResultTests
{
    // ===========================
    // Result<T> — Success
    // ===========================

    [Fact]
    public void ResultT_Success_ShouldSetIsSuccessTrue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Success_ShouldThrowOnAccessError()
    {
        var result = Result<int>.Success(42);

        var act = () => result.Error;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*successful*");
    }

    // ===========================
    // Result<T> — Failure
    // ===========================

    [Fact]
    public void ResultT_Failure_ShouldSetIsFailureTrue()
    {
        var error = new Error("NOT_FOUND", "Item not found");
        var result = Result<int>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ResultT_Failure_ShouldThrowOnAccessValue()
    {
        var result = Result<int>.Failure(new Error("ERR", "msg"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed*");
    }

    // ===========================
    // Result<T> — Implicit operators
    // ===========================

    [Fact]
    public void ResultT_ImplicitConversion_FromValue_ShouldSucceed()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ResultT_ImplicitConversion_FromError_ShouldFail()
    {
        Result<string> result = new Error("ERR", "something went wrong");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ERR");
    }

    // ===========================
    // Result<T> — Error details
    // ===========================

    [Fact]
    public void ResultT_Error_ShouldCarryDetails()
    {
        var details = new Dictionary<string, object?>
        {
            ["document_id"] = 123,
            ["cause"] = "corrupt file"
        };
        var error = new Error("VALIDATION", "Document validation failed", details);
        var result = Result<int>.Failure(error);

        result.Error.Details.Should().ContainKey("document_id");
        result.Error.Details!["document_id"].Should().Be(123);
    }

    // ===========================
    // Result (void) — Success
    // ===========================

    [Fact]
    public void ResultVoid_Success_ShouldSetIsSuccessTrue()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void ResultVoid_Success_ShouldThrowOnAccessError()
    {
        var result = Result.Success();

        var act = () => result.Error;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*successful*");
    }

    // ===========================
    // Result (void) — Failure
    // ===========================

    [Fact]
    public void ResultVoid_Failure_ShouldSetIsFailureTrue()
    {
        var error = new Error("ERR", "failure");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    // ===========================
    // Result (void) — Implicit operator
    // ===========================

    [Fact]
    public void ResultVoid_ImplicitConversion_FromError_ShouldFail()
    {
        Result result = new Error("ERR", "void error");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ERR");
    }

    // ===========================
    // Deconstruction
    // ===========================

    [Fact]
    public void ResultT_Deconstruct_ShouldReturnComponents()
    {
        var successResult = Result<int>.Success(99);
        var (isSuccess, value, error) = successResult;

        isSuccess.Should().BeTrue();
        value.Should().Be(99);
        error.Should().BeNull();

        var failResult = Result<int>.Failure(new Error("ERR", "fail"));
        var (isSuccess2, value2, error2) = failResult;

        isSuccess2.Should().BeFalse();
        value2.Should().Be(default);
        error2.Should().NotBeNull();
        error2!.Code.Should().Be("ERR");
    }

    [Fact]
    public void ResultVoid_Deconstruct_ShouldReturnComponents()
    {
        var successResult = Result.Success();
        var (isSuccess, error) = successResult;

        isSuccess.Should().BeTrue();
        error.Should().BeNull();

        var failResult = Result.Failure(new Error("ERR", "void fail"));
        var (isSuccess2, error2) = failResult;

        isSuccess2.Should().BeFalse();
        error2.Should().NotBeNull();
        error2!.Code.Should().Be("ERR");
    }

    // ===========================
    // Edge cases
    // ===========================

    [Fact]
    public void Error_WithNullDetails_ShouldBeAllowed()
    {
        var error = new Error("CODE", "message");

        error.Details.Should().BeNull();
    }

    [Fact]
    public void Error_WithEmptyDetails_ShouldBeAllowed()
    {
        var error = new Error("CODE", "message", []);

        error.Details.Should().BeEmpty();
    }
}
