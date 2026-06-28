using FluentAssertions;
using Paperless.Core.Workflows.Entities;
using Paperless.Infrastructure.Persistence.Repositories;

namespace Paperless.Infrastructure.Tests.Persistence;

public class WorkflowRepositoryTests : RepositoryTestsBase
{
    [Fact]
    public async Task AddAsync_Should_Persist_Workflow_With_Triggers_And_Actions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new WorkflowRepository(context);

        var workflow = new Workflow
        {
            Name = "Test Workflow",
            Order = 1,
            Enabled = true,
            Triggers =
            {
                new WorkflowTrigger
                {
                    Type = Core.Documents.Enums.WorkflowTriggerType.Consumption
                }
            },
            Actions =
            {
                new WorkflowAction
                {
                    Type = Core.Documents.Enums.WorkflowActionType.SetCorrespondent,
                    Order = 1
                }
            }
        };

        // Act
        await repo.AddAsync(workflow);
        await context.SaveChangesAsync();

        // Assert
        workflow.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_Includes_Triggers_And_Actions()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new WorkflowRepository(context);

        var workflow = new Workflow
        {
            Name = "Workflow With Children",
            Order = 1,
            Enabled = true,
            Triggers =
            {
                new WorkflowTrigger
                {
                    Type = Core.Documents.Enums.WorkflowTriggerType.DocumentAdded
                }
            },
            Actions =
            {
                new WorkflowAction
                {
                    Type = Core.Documents.Enums.WorkflowActionType.SetTag,
                    Order = 1
                }
            }
        };
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdAsync(workflow.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Workflow With Children");
        result.Triggers.Should().HaveCount(1);
        result.Actions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEnabledWorkflowsAsync_Returns_Only_Enabled_Ordered()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new WorkflowRepository(context);

        context.Workflows.AddRange(
            new Workflow { Name = "Wf B", Order = 2, Enabled = true },
            new Workflow { Name = "Wf A", Order = 1, Enabled = true },
            new Workflow { Name = "Wf Disabled", Order = 3, Enabled = false }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetEnabledWorkflowsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Wf A");
        result.Last().Name.Should().Be("Wf B");
    }
}
