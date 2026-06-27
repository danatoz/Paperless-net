using Paperless.Core.Common.Models;
using Paperless.Shared.Abstractions;

namespace Paperless.Core.Consumer.Pipeline;

/// <summary>
/// Orchestrates the consumer pipeline by running the registered plugins
/// in the correct order: pre-consume → consume → post-consume.
/// Each stage checks plugin capability via <c>CanHandleAsync</c>;
/// the pipeline stops immediately if any plugin returns a failure result.
/// Maps to the main consumer orchestration logic from paperless-ngx consumer.py.
/// </summary>
public sealed class ConsumerPipeline : IConsumerPipeline
{
    private readonly IEnumerable<IPreConsumePlugin> _preConsumePlugins;
    private readonly IEnumerable<IConsumePlugin> _consumePlugins;
    private readonly IEnumerable<IPostConsumePlugin> _postConsumePlugins;

    /// <summary>
    /// Initializes a new instance with the three plugin collections registered via DI.
    /// Each collection can be empty; the pipeline simply skips stages with no plugins.
    /// </summary>
    /// <param name="preConsumePlugins">Plugins for the pre-consumption stage.</param>
    /// <param name="consumePlugins">Plugins for the main consumption stage.</param>
    /// <param name="postConsumePlugins">Plugins for the post-consumption stage.</param>
    public ConsumerPipeline(
        IEnumerable<IPreConsumePlugin> preConsumePlugins,
        IEnumerable<IConsumePlugin> consumePlugins,
        IEnumerable<IPostConsumePlugin> postConsumePlugins)
    {
        _preConsumePlugins = preConsumePlugins ?? throw new ArgumentNullException(nameof(preConsumePlugins));
        _consumePlugins = consumePlugins ?? throw new ArgumentNullException(nameof(consumePlugins));
        _postConsumePlugins = postConsumePlugins ?? throw new ArgumentNullException(nameof(postConsumePlugins));
    }

    /// <inheritdoc />
    public async Task<ConsumerResult> ProcessAsync(ConsumableDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var context = new ConsumerContext
        {
            Document = document,
            WorkingDirectory = Path.GetTempPath(),
            StageStatuses = new Dictionary<string, bool>(3),
            Metadata = new Dictionary<string, object?>()
        };

        // Stage 1: Pre-consume plugins (validation, dedup, pre-processing)
        var preConsumeResult = await RunStageAsync("PreConsume", _preConsumePlugins, context, ct);
        if (!preConsumeResult.IsSuccess)
            return preConsumeResult;
        context.StageStatuses["PreConsume"] = true;

        // Stage 2: Consume plugins (OCR, text extraction, barcode detection)
        var consumeResult = await RunStageAsync("Consume", _consumePlugins, context, ct);
        if (!consumeResult.IsSuccess)
            return consumeResult;
        context.StageStatuses["Consume"] = true;

        // Propagate DocumentId if any consume plugin produced one
        var documentId = consumeResult.DocumentId;

        // Stage 3: Post-consume plugins (classification, workflow, storage, save)
        var postConsumeResult = await RunStageAsync("PostConsume", _postConsumePlugins, context, ct);
        if (!postConsumeResult.IsSuccess)
            return postConsumeResult;
        context.StageStatuses["PostConsume"] = true;

        // Use the most specific DocumentId available
        return ConsumerResult.Success(
            documentId: documentId ?? postConsumeResult.DocumentId,
            messages: CombineMessages(preConsumeResult, consumeResult, postConsumeResult));
    }

    /// <summary>
    /// Runs a collection of pipeline stage plugins.
    /// Only plugins that return true from <c>CanHandleAsync</c> are executed.
    /// Stops at the first plugin that returns a non-success result.
    /// </summary>
    private static async Task<ConsumerResult> RunStageAsync(
        string stageName,
        IEnumerable<IConsumerPipelineStage> stages,
        ConsumerContext context,
        CancellationToken ct)
    {
        var messages = new List<string>();

        foreach (var stage in stages)
        {
            if (ct.IsCancellationRequested)
                return ConsumerResult.Failure(
                    new Error("PipelineCancelled", $"Pipeline was cancelled during the {stageName} stage."));

            try
            {
                var canHandle = await stage.CanHandleAsync(context, ct);
                if (!canHandle)
                    continue;

                var result = await stage.HandleAsync(context, ct);
                messages.AddRange(result.Messages);

                if (!result.IsSuccess)
                {
                    return ConsumerResult.Failure(
                        result.Error ?? new Error("PluginError", $"The {stageName} stage failed in {stage.GetType().Name}."),
                        messages.ToArray());
                }

                // If this plugin produced a DocumentId and we don't have one yet, propagate it
                if (result.DocumentId.HasValue)
                {
                    return ConsumerResult.Success(
                        documentId: result.DocumentId.Value,
                        messages: messages.ToArray());
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return ConsumerResult.Failure(
                    new Error("PipelineCancelled", $"Pipeline was cancelled during the {stageName} stage."));
            }
            catch (Exception ex)
            {
                return ConsumerResult.Failure(
                    new Error("PluginException", $"The {stageName} stage threw an exception in {stage.GetType().Name}: {ex.Message}"),
                    messages.ToArray());
            }
        }

        return ConsumerResult.Success(messages: messages.ToArray());
    }

    /// <summary>
    /// Combines messages from all three pipeline stages into a single array.
    /// </summary>
    private static string[] CombineMessages(params ConsumerResult[] results)
    {
        return results
            .Where(r => r.Messages.Count > 0)
            .SelectMany(r => r.Messages)
            .ToArray();
    }
}
