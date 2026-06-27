namespace Paperless.Core.AI.Models;

/// <summary>
/// The result of an AI classification operation.
/// Maps to the classification output from paperless_ai/ai_classifier.py.
/// </summary>
public sealed record ClassificationResult
{
    /// <summary>
    /// The ID of the document that was classified.
    /// </summary>
    public int DocumentId { get; init; }

    /// <summary>
    /// The suggested correspondent ID, if any.
    /// </summary>
    public int? SuggestedCorrespondentId { get; init; }

    /// <summary>
    /// The suggested document type ID, if any.
    /// </summary>
    public int? SuggestedTypeId { get; init; }

    /// <summary>
    /// The suggested tag IDs to assign.
    /// </summary>
    public IReadOnlyCollection<int> SuggestedTags { get; init; } = Array.Empty<int>();

    /// <summary>
    /// The confidence level of the classification (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }
}
