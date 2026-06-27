namespace Paperless.Core.AI.Interfaces;

/// <summary>
/// Defines the contract for generating vector embeddings from text.
/// Maps to the embedding logic from paperless_ai/embedding.py.
/// NOTE: sentence-transformers via ONNX Runtime or Python-microservice.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generates a single embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The embedding as a float array.</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An array of embedding vectors.</returns>
    Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
