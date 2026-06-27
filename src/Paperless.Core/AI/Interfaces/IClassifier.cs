using Paperless.Core.AI.Models;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.AI.Interfaces;

/// <summary>
/// Defines the contract for AI-based document classification.
/// Maps to the classifier logic from paperless_ai/ai_classifier.py.
/// </summary>
public interface IClassifier
{
    /// <summary>
    /// Classifies a document and returns the suggested metadata.
    /// </summary>
    /// <param name="document">The document to classify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The classification result with suggested metadata.</returns>
    Task<ClassificationResult> ClassifyAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Trains the classifier on a set of reference documents.
    /// </summary>
    /// <param name="documents">The training set of documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training result indicating success and metrics.</returns>
    Task<TrainResult> TrainAsync(IEnumerable<Document> documents, CancellationToken ct = default);
}
