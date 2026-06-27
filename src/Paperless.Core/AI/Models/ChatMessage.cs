namespace Paperless.Core.AI.Models;

/// <summary>
/// A single message in a chat conversation.
/// Maps to chat message types from paperless_ai/chat.py.
/// </summary>
public sealed record ChatMessage
{
    /// <summary>
    /// The role of the message sender (e.g., "user", "assistant", "system").
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// The UTC timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
