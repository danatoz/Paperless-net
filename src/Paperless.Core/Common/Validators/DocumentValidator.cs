using System.Text.RegularExpressions;
using FluentValidation;
using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="Document"/> entity.
/// Maps to validation rules from DRF serializers in the original paperless-ngx.
/// </summary>
public sealed class DocumentValidator : AbstractValidator<Document>
{
    private static readonly Regex Sha256HexRegex = new("^[A-Fa-f0-9]{64}$", RegexOptions.Compiled);

    public DocumentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Checksum)
            .Must(BeValidSha256Hex)
            .When(x => x.Checksum is not null)
            .WithMessage("Checksum must be a valid SHA-256 hex string (64 hex characters).");

        RuleFor(x => x.ArchiveChecksum)
            .Must(BeValidSha256Hex)
            .When(x => x.ArchiveChecksum is not null)
            .WithMessage("Archive checksum must be a valid SHA-256 hex string (64 hex characters).");

        RuleFor(x => x.Created)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.Created is not null)
            .WithMessage("Document created date cannot be in the future.");

        RuleFor(x => x.Content)
            .MaximumLength(10_000_000)
            .When(x => x.Content is not null);
    }

    private static bool BeValidSha256Hex(string? checksum) =>
        checksum is not null && Sha256HexRegex.IsMatch(checksum);
}
