using Paperless.Core.Mail.Entities;

namespace Paperless.Core.Mail.Interfaces;

/// <summary>
/// Service interface for fetching emails from an IMAP mail account.
/// Maps to the IMAP client logic from paperless_mail/mail.py.
/// </summary>
public interface IMailFetcher
{
    /// <summary>
    /// Connects to the mail server using the provided account credentials.
    /// </summary>
    Task ConnectAsync(MailAccount account, CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the mail server.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches new (unprocessed) emails from the configured account and folder.
    /// </summary>
    /// <param name="account">The mail account configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of processed mail records with attachments.</returns>
    Task<IReadOnlyCollection<ProcessedMail>> FetchNewMailsAsync(MailAccount account, CancellationToken ct = default);
}
