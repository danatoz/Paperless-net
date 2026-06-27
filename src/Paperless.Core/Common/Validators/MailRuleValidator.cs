using FluentValidation;
using Paperless.Core.Mail.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="MailRule"/> entity.
/// </summary>
public sealed class MailRuleValidator : AbstractValidator<MailRule>
{
    public MailRuleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.AccountId)
            .GreaterThan(0)
            .WithMessage("A valid Account ID must be specified.");
    }
}
