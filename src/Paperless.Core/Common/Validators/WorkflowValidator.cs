using FluentValidation;
using Paperless.Core.Workflows.Entities;

namespace Paperless.Core.Common.Validators;

/// <summary>
/// Validates the <see cref="Workflow"/> entity.
/// </summary>
public sealed class WorkflowValidator : AbstractValidator<Workflow>
{
    public WorkflowValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Triggers)
            .NotEmpty()
            .WithMessage("A workflow must have at least one trigger.");
    }
}
