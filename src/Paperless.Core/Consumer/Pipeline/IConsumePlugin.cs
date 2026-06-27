using Paperless.Core.Common.Models;

namespace Paperless.Core.Consumer.Pipeline;

/// <summary>
/// Defines the main consumption plugin that performs the core document processing.
/// Responsible for OCR, text extraction, barcode detection, and conversion.
/// Maps to the consume stage from paperless-ngx consumer.py.
/// </summary>
public interface IConsumePlugin : IConsumerPipelineStage
{
}
