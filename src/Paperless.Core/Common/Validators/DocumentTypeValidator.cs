using FluentValidation;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="DocumentType"/> entity.
/// </summary>
public sealed class DocumentTypeValidator : AbstractValidator<DocumentType>
{
    public DocumentTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);
    }
}
