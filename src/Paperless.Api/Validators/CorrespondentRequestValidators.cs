using FluentValidation;
using Paperless.Api.Dto.Requests;

namespace Paperless.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateCorrespondentRequest"/>.
/// </summary>
public sealed class CreateCorrespondentRequestValidator : AbstractValidator<CreateCorrespondentRequest>
{
    public CreateCorrespondentRequestValidator()
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
/// Validator for <see cref="UpdateCorrespondentRequest"/>.
/// </summary>
public sealed class UpdateCorrespondentRequestValidator : AbstractValidator<UpdateCorrespondentRequest>
{
    public UpdateCorrespondentRequestValidator()
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
/// Validator for <see cref="PatchCorrespondentRequest"/>.
/// </summary>
public sealed class PatchCorrespondentRequestValidator : AbstractValidator<PatchCorrespondentRequest>
{
    public PatchCorrespondentRequestValidator()
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
