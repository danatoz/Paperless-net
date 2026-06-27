using Paperless.Core.Common.Models;

namespace Paperless.Core.Consumer.Pipeline;

/// <summary>
/// Defines a post-consumption plugin that runs after the main consumption pipeline.
/// Responsible for classification, workflow execution, storage path rendering, and saving.
/// Maps to the post-consume stage from paperless-ngx consumer.py.
/// </summary>
public interface IPostConsumePlugin : IConsumerPipelineStage
{
}
