using System.Text.RegularExpressions;
using Paperless.Shared.Abstractions;

namespace Paperless.Core.Documents.ValueObjects;

/// <summary>
/// Represents a SHA-256 checksum value used for document deduplication and integrity verification.
/// Provides structural equality and format validation (64 hex characters).
/// Maps to the checksum fields in the original paperless-ngx data model.
/// </summary>
public sealed partial class Checksum : ValueObject
{
    private const int Sha256HexLength = 64;

    // SHA-256 produces a 256-bit hash, represented as 64 hex characters.
    [GeneratedRegex("^[0-9a-fA-F]{64}$", RegexOptions.Compiled)]
    private static partial Regex Sha256HexRegex();

    /// <summary>
    /// Gets the SHA-256 hex string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Checksum"/> class.
    /// </summary>
    /// <param name="value">The SHA-256 hex string.</param>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid SHA-256 hex string.</exception>
    public Checksum(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValid(value))
            throw new ArgumentException(
                $"The value '{value}' is not a valid SHA-256 checksum. " +
                $"Expected a {Sha256HexLength}-character hexadecimal string.",
                nameof(value));

        Value = value;
    }

    /// <summary>
    /// Validates whether the given string is a valid SHA-256 checksum (64 hex characters).
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid SHA-256 checksum; otherwise, false.</returns>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Length == Sha256HexLength && Sha256HexRegex().IsMatch(value);
    }

    /// <summary>
    /// Creates a <see cref="Checksum"/> from the specified string value.
    /// Returns null if the value is null, empty, or not a valid SHA-256 checksum.
    /// </summary>
    /// <param name="value">The SHA-256 hex string.</param>
    /// <returns>A <see cref="Checksum"/> instance, or null if the value is invalid.</returns>
    public static Checksum? FromString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return IsValid(value) ? new Checksum(value) : null;
    }

    /// <summary>
    /// Returns the checksum as a lowercase hex string.
    /// </summary>
    public override string ToString() => Value.ToLowerInvariant();

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Implicitly converts a <see cref="Checksum"/> to its string representation.
    /// </summary>
    public static implicit operator string(Checksum checksum) => checksum.Value;
}
