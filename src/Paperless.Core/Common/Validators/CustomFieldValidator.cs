using FluentValidation;
using Paperless.Core.Documents.Entities;
using Paperless.Core.Documents.Enums;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="CustomField"/> entity.
/// </summary>
public sealed class CustomFieldValidator : AbstractValidator<CustomField>
{
    public CustomFieldValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Custom field type must be a valid CustomFieldType enum value.");
    }
}
