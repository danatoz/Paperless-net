using Paperless.Core.AI.Models;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.AI.Interfaces;

/// <summary>
/// Defines the contract for AI-powered chat/QA over documents (RAG).
/// Maps to the chat logic from paperless_ai/chat.py.
/// NOTE: LlamaIndex → Semantic Kernel or custom RAG implementation.
/// </summary>
public interface IChatEngine
{
    /// <summary>
    /// Streams a chat response for the given query with the provided document context.
    /// </summary>
    /// <param name="query">The user's query.</param>
    /// <param name="context">The relevant document context for the query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable stream of chat messages.</returns>
    IAsyncEnumerable<ChatMessage> ChatAsync(string query, IEnumerable<Document> context, CancellationToken ct = default);
}
