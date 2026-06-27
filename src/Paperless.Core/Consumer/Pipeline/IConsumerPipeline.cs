using Paperless.Core.Common.Models;

namespace Paperless.Core.Consumer.Pipeline;

/// <summary>
/// Orchestrates the consumer pipeline: accepts a consumable document,
/// runs it through all registered plugins, and returns the final result.
/// Maps to the main consumer logic from paperless-ngx consumer.py.
/// </summary>
public interface IConsumerPipeline
{
    /// <summary>
    /// Processes a consumable document through the entire pipeline.
    /// </summary>
    /// <param name="document">The document to consume.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The final result after all pipeline stages.</returns>
    Task<ConsumerResult> ProcessAsync(ConsumableDocument document, CancellationToken ct = default);
}
