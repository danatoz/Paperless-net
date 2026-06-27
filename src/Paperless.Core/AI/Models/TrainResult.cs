namespace Paperless.Core.AI.Models;

/// <summary>
/// The result of a classifier training operation.
/// Maps to the training output from paperless_ai/ai_classifier.py.
/// </summary>
public sealed record TrainResult
{
    /// <summary>
    /// Whether the training completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The number of documents used for training.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// The accuracy metric of the trained model (0.0 to 1.0).
    /// </summary>
    public double Accuracy { get; init; }

    /// <summary>
    /// An error message if training failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
