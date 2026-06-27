using Paperless.Core.Common.Models;

namespace Paperless.Core.Consumer.Pipeline;

/// <summary>
/// Common base interface for all consumer pipeline stages.
/// Provides the contract for capability checks and execution.
/// </summary>
public interface IConsumerPipelineStage
{
    /// <summary>
    /// Determines whether this stage can handle the given consumer context.
    /// </summary>
    /// <param name="context">The consumer pipeline context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if this stage should process the context.</returns>
    Task<bool> CanHandleAsync(ConsumerContext context, CancellationToken ct = default);

    /// <summary>
    /// Executes the stage's processing logic.
    /// </summary>
    /// <param name="context">The consumer pipeline context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the processing step.</returns>
    Task<ConsumerResult> HandleAsync(ConsumerContext context, CancellationToken ct = default);
}

/// <summary>
/// Defines a pre-consumption plugin that runs before the main consumption pipeline.
/// Responsible for validation, deduplication, and pre-processing tasks.
/// Maps to the pre-consume stage from paperless-ngx consumer.py.
/// </summary>
public interface IPreConsumePlugin : IConsumerPipelineStage
{
}
