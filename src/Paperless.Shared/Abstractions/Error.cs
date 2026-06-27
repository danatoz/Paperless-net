namespace Paperless.Shared.Abstractions;

/// <summary>
/// Represents a domain error with a code, message, and optional details.
/// Used as the error carrier in the <see cref="Result{T}"/> and <see cref="Result"/> types.
/// </summary>
public sealed record Error(string Code, string Message, Dictionary<string, object?>? Details = null);
