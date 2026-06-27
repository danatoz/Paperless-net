using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Mail.Entities;

/// <summary>
/// Defines a rule for processing emails from a mail account.
/// Maps to the MailRule model from paperless_mail/models.py.
/// </summary>
public class MailRule : BaseEntity
{
    /// <summary>
    /// A user-friendly name for this rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the associated <see cref="MailAccount"/>.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="MailAccount"/>.
    /// </summary>
    public MailAccount? Account { get; set; }

    /// <summary>
    /// The IMAP folder to search (e.g., "INBOX").
    /// </summary>
    public string? Folder { get; set; }

    /// <summary>
    /// JSON-serialized filter rules (from, to, subject patterns, etc.).
    /// </summary>
    public string? FilterRules { get; set; }

    /// <summary>
    /// The action type to take when the rule matches (e.g., "create_document").
    /// </summary>
    public string ActionType { get; set; } = "create_document";

    /// <summary>
    /// Navigation property: processed mail records created by this rule.
    /// </summary>
    public ICollection<ProcessedMail> ProcessedMails { get; set; } = new List<ProcessedMail>();
}
