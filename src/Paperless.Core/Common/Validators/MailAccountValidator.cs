using FluentValidation;
using Paperless.Core.Mail.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="MailAccount"/> entity.
/// </summary>
public sealed class MailAccountValidator : AbstractValidator<MailAccount>
{
    public MailAccountValidator()
    {
        RuleFor(x => x.ImapServer)
            .NotEmpty()
            .WithMessage("Mail server host must not be empty.");

        RuleFor(x => x.ImapPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535.");
    }
}
