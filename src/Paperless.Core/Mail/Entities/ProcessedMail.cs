using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Mail.Entities;

/// <summary>
/// Records that an email has been processed by a mail rule.
/// Maps to the ProcessedMail model from paperless_mail/models.py.
/// </summary>
public class ProcessedMail : BaseEntity
{
    /// <summary>
    /// Foreign key to the <see cref="MailRule"/> that processed this email.
    /// </summary>
    public int MailRuleId { get; set; }

    /// <summary>
    /// Navigation property to the <see cref="MailRule"/> that processed this email.
    /// </summary>
    public MailRule? MailRule { get; set; }

    /// <summary>
    /// Foreign key to the <see cref="Documents.Entities.Document"/> created from this email.
    /// </summary>
    public int? DocumentId { get; set; }

    /// <summary>
    /// The processing status (e.g., "success", "failed", "skipped").
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// The date and time when the email was received (from the email header).
    /// </summary>
    public DateTime Received { get; set; } = DateTime.UtcNow;
}
