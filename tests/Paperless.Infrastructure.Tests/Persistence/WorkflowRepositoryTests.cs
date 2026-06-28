using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        var repo = new WorkflowRepository(context, CreateUnitOfWorkMock());

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
        var repo = new WorkflowRepository(context, CreateUnitOfWorkMock());

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
        var repo = new WorkflowRepository(context, CreateUnitOfWorkMock());

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

    [Fact]
    public async Task Delete_Workflow_Should_SoftDelete()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new WorkflowRepository(context, CreateUnitOfWorkMock());

        var workflow = new Workflow
        {
            Name = "Delete Test Workflow",
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
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync();

        // Act
        repo.Delete(workflow);
        await context.SaveChangesAsync();

        // Assert - Workflow should be soft-deleted
        var deleted = await context.Workflows
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == workflow.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();

        // Triggers and Actions should still exist (not cascade-deleted with soft delete)
        var triggers = await context.Set<WorkflowTrigger>()
            .IgnoreQueryFilters()
            .Where(t => t.WorkflowId == workflow.Id)
            .ToListAsync();
        triggers.Should().HaveCount(1);
        triggers[0].IsDeleted.Should().BeFalse();

        var actions = await context.Set<WorkflowAction>()
            .IgnoreQueryFilters()
            .Where(a => a.WorkflowId == workflow.Id)
            .ToListAsync();
        actions.Should().HaveCount(1);
        actions[0].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Repository_Delete_Does_Not_Return_Soft_Deleted_Workflow()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new WorkflowRepository(context, CreateUnitOfWorkMock());

        var workflow = new Workflow
        {
            Name = "Should Not Appear",
            Order = 1,
            Enabled = true
        };
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync();

        // Act - soft delete via repository
        repo.Delete(workflow);
        await context.SaveChangesAsync();

        // Assert - workflow should not appear in standard queries
        var byId = await repo.GetByIdAsync(workflow.Id);
        byId.Should().BeNull();

        var all = await repo.GetAllAsync();
        all.Should().NotContain(w => w.Id == workflow.Id);
    }
}
