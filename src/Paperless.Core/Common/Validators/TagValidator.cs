using System.Text.RegularExpressions;
using FluentValidation;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="Tag"/> entity.
/// </summary>
public sealed class TagValidator : AbstractValidator<Tag>
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public TagValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Color)
            .Must(BeValidHexColor)
            .When(x => x.Color is not null)
            .WithMessage("Color must be a valid hex color code (e.g., #FFFFFF).");
    }

    private static bool BeValidHexColor(string? color) =>
        color is not null && HexColorRegex.IsMatch(color);
}
