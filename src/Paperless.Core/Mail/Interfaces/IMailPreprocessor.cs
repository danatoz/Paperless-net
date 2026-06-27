namespace Paperless.Core.Mail.Interfaces;

/// <summary>
/// Service interface for preprocessing email attachments before consumption.
/// Maps to the preprocessor logic from paperless_mail/preprocessor.py.
/// Currently supports PGP decryption of encrypted attachments.
/// </summary>
public interface IMailPreprocessor
{
    /// <summary>
    /// Preprocesses an attachment stream (e.g., PGP-decrypts it).
    /// </summary>
    /// <param name="attachmentStream">The raw attachment stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The processed (decrypted) stream, or the original stream if no processing was needed.</returns>
    Task<Stream> PreprocessAttachmentAsync(Stream attachmentStream, CancellationToken ct = default);
}
