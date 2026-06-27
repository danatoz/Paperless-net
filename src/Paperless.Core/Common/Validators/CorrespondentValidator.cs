using FluentValidation;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="Correspondent"/> entity.
/// </summary>
public sealed class CorrespondentValidator : AbstractValidator<Correspondent>
{
    public CorrespondentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);
    }
}
