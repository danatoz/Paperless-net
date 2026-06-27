namespace Paperless.Core.AI.Models;

/// <summary>
/// Configuration for an AI provider connection.
/// Maps to the AI provider configuration from paperless_ai/base_model.py.
/// </summary>
public sealed record AiConfig
{
    /// <summary>
    /// The AI provider name (e.g., "openai", "ollama", "huggingface").
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// The model identifier (e.g., "gpt-4", "llama3", "mistral").
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// The API endpoint URL for the provider.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// The API key for authentication (if required).
    /// </summary>
    public string? ApiKey { get; init; }
}
