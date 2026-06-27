using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Mail.Entities;

/// <summary>
/// Represents a mail account configured for fetching documents from email.
/// Maps to the MailAccount model from paperless_mail/models.py.
/// </summary>
public class MailAccount : BaseEntity
{
    /// <summary>
    /// A user-friendly name for this mail account.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The IMAP server hostname.
    /// </summary>
    public string ImapServer { get; set; } = string.Empty;

    /// <summary>
    /// The IMAP server port (default: 993 for SSL).
    /// </summary>
    public int ImapPort { get; set; } = 993;

    /// <summary>
    /// The username for IMAP authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The password for IMAP authentication (may be encrypted at rest).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// OAuth2 provider identifier (e.g., "google", "microsoft") if using OAuth.
    /// </summary>
    public string? OauthId { get; set; }

    /// <summary>
    /// Navigation property: mail rules associated with this account.
    /// </summary>
    public ICollection<MailRule> MailRules { get; set; } = new List<MailRule>();
}
