using FluentValidation;
using Paperless.Api.Dto.Requests;

namespace Paperless.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateDocumentTypeRequest"/>.
/// </summary>
public sealed class CreateDocumentTypeRequestValidator : AbstractValidator<CreateDocumentTypeRequest>
{
    public CreateDocumentTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.MatchingAlgorithm)
            .InclusiveBetween(0, 6)
            .When(x => x.MatchingAlgorithm.HasValue);
    }
}

/// <summary>
/// Validator for <see cref="UpdateDocumentTypeRequest"/>.
/// </summary>
public sealed class UpdateDocumentTypeRequestValidator : AbstractValidator<UpdateDocumentTypeRequest>
{
    public UpdateDocumentTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.MatchingAlgorithm)
            .InclusiveBetween(0, 6)
            .When(x => x.MatchingAlgorithm.HasValue);
    }
}

/// <summary>
/// Validator for <see cref="PatchDocumentTypeRequest"/>.
/// </summary>
public sealed class PatchDocumentTypeRequestValidator : AbstractValidator<PatchDocumentTypeRequest>
{
    public PatchDocumentTypeRequestValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(256);
        });

        When(x => x.MatchingAlgorithm.HasValue, () =>
        {
            RuleFor(x => x.MatchingAlgorithm!.Value)
                .InclusiveBetween(0, 6);
        });
    }
}
