using System.Diagnostics.CodeAnalysis;

namespace Paperless.Shared.Abstractions;

/// <summary>
/// A discriminated union representing either success with a value of <typeparamref name="T"/>,
/// or failure with an <see cref="Abstractions.Error"/>.
/// Provides implicit conversions from <typeparamref name="T"/> and <see cref="Abstractions.Error"/>.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    /// <summary>
    /// Initializes a successful result with the given value.
    /// </summary>
    private Result(T value)
    {
        _value = value;
        _error = null;
    }

    /// <summary>
    /// Initializes a failed result with the given error.
    /// </summary>
    private Result(Error error)
    {
        _value = default;
        _error = error;
    }

    /// <summary>
    /// Returns true if the result represents a success.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_error))]
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess => _error is null;

    /// <summary>
    /// Returns true if the result represents a failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_error))]
    public bool IsFailure => _error is not null;

    /// <summary>
    /// Gets the success value. Throws <see cref="InvalidOperationException"/> if the result is a failure.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access value of a failed result. Error: {_error?.Code}");

    /// <summary>
    /// Gets the error. Throws <see cref="InvalidOperationException"/> if the result is a success.
    /// </summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of a successful result.");

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>
    /// Implicitly converts an <see cref="Abstractions.Error"/> to a failed result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => new(error);

    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Deconstructs the result into success flag, value, and error for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out T? value, out Error? error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = _error;
    }
}

/// <summary>
/// A void-version of <see cref="Result{T}"/> representing either success or failure with an <see cref="Abstractions.Error"/>.
/// Provides an implicit conversion from <see cref="Abstractions.Error"/>.
/// </summary>
public class Result
{
    private readonly Error? _error;

    /// <summary>
    /// Initializes a successful result.
    /// </summary>
    private Result()
    {
        _error = null;
    }

    /// <summary>
    /// Initializes a failed result with the given error.
    /// </summary>
    private Result(Error error)
    {
        _error = error;
    }

    /// <summary>
    /// Returns true if the result represents a success.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_error))]
    public bool IsSuccess => _error is null;

    /// <summary>
    /// Returns true if the result represents a failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_error))]
    public bool IsFailure => _error is not null;

    /// <summary>
    /// Gets the error. Throws <see cref="InvalidOperationException"/> if the result is a success.
    /// </summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of a successful result.");

    /// <summary>
    /// Implicitly converts an <see cref="Abstractions.Error"/> to a failed result.
    /// </summary>
    public static implicit operator Result(Error error) => new(error);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new();

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Deconstructs the result into success flag and error for pattern matching.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out Error? error)
    {
        isSuccess = IsSuccess;
        error = _error;
    }
}
