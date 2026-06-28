using System.Text.RegularExpressions;
using FluentValidation;
using Paperless.Api.Dto.Requests;

namespace Paperless.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateTagRequest"/>.
/// </summary>
public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Color)
            .Must(BeValidHexColor)
            .When(x => x.Color is not null)
            .WithMessage("Color must be a valid hex color code (e.g., #FFFFFF).");

        RuleFor(x => x.TextColor)
            .Must(BeValidHexColor)
            .When(x => x.TextColor is not null)
            .WithMessage("Text color must be a valid hex color code (e.g., #FFFFFF).");

        RuleFor(x => x.MatchingAlgorithm)
            .InclusiveBetween(0, 6)
            .When(x => x.MatchingAlgorithm.HasValue);
    }

    private static bool BeValidHexColor(string? color) =>
        color is not null && HexColorRegex.IsMatch(color);
}

/// <summary>
/// Validator for <see cref="UpdateTagRequest"/>.
/// </summary>
public sealed class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Color)
            .Must(BeValidHexColor)
            .When(x => x.Color is not null)
            .WithMessage("Color must be a valid hex color code (e.g., #FFFFFF).");

        RuleFor(x => x.TextColor)
            .Must(BeValidHexColor)
            .When(x => x.TextColor is not null)
            .WithMessage("Text color must be a valid hex color code (e.g., #FFFFFF).");

        RuleFor(x => x.MatchingAlgorithm)
            .InclusiveBetween(0, 6)
            .When(x => x.MatchingAlgorithm.HasValue);
    }

    private static bool BeValidHexColor(string? color) =>
        color is not null && HexColorRegex.IsMatch(color);
}

/// <summary>
/// Validator for <see cref="PatchTagRequest"/>.
/// </summary>
public sealed class PatchTagRequestValidator : AbstractValidator<PatchTagRequest>
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public PatchTagRequestValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(128);
        });

        When(x => x.Color is not null, () =>
        {
            RuleFor(x => x.Color)
                .Must(BeValidHexColor)
                .WithMessage("Color must be a valid hex color code (e.g., #FFFFFF).");
        });

        When(x => x.TextColor is not null, () =>
        {
            RuleFor(x => x.TextColor)
                .Must(BeValidHexColor)
                .WithMessage("Text color must be a valid hex color code (e.g., #FFFFFF).");
        });

        When(x => x.MatchingAlgorithm.HasValue, () =>
        {
            RuleFor(x => x.MatchingAlgorithm!.Value)
                .InclusiveBetween(0, 6);
        });
    }

    private static bool BeValidHexColor(string? color) =>
        color is not null && HexColorRegex.IsMatch(color);
}
