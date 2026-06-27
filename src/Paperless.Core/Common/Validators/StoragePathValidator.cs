using FluentValidation;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="StoragePath"/> entity.
/// </summary>
public sealed class StoragePathValidator : AbstractValidator<StoragePath>
{
    public StoragePathValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.PathTemplate)
            .NotEmpty()
            .WithMessage("Path template must not be empty.");
    }
}
