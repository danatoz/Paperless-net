using FluentAssertions;
using MediatR;
using NSubstitute;
using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.Events;
using Paperless.Core.Tests.Helpers;
using Paperless.Core.Workflows.Entities;
using Paperless.Core.Workflows.Interfaces;
using Paperless.Core.Workflows.Services;

namespace Paperless.Core.Tests.Workflows;

/// <summary>
/// Unit tests for the WorkflowEngine orchestration logic.
/// Verifies trigger matching, action execution, ordering, error handling,
/// and domain event publishing.
/// Maps to the workflow processing logic from documents/workflows/actions.py.
/// </summary>
public class WorkflowEngineTests
{
    #region Helpers

    private static WorkflowEngine CreateEngine(
        out IWorkflowTriggerEvaluator triggerEvaluator,
        out IWorkflowActionExecutor actionExecutor,
        out IPublisher publisher)
    {
        triggerEvaluator = Substitute.For<IWorkflowTriggerEvaluator>();
        actionExecutor = Substitute.For<IWorkflowActionExecutor>();
        publisher = Substitute.For<IPublisher>();
        return new WorkflowEngine(triggerEvaluator, actionExecutor, publisher);
    }

    private static DocumentContext CreateContext(string eventType = "Consumption")
    {
        return new DocumentContext
        {
            Document = TestData.CreateDocument(),
            EventType = eventType,
        };
    }

    private static Workflow CreateWorkflow(
        string name = "Test Workflow",
        int order = 1,
        bool enabled = true,
        WorkflowTriggerType triggerType = WorkflowTriggerType.Consumption,
        WorkflowActionType actionType = WorkflowActionType.SetTag)
    {
        var workflowId = order;

        var trigger = new WorkflowTrigger
        {
            Id = workflowId * 10 + 1,
            WorkflowId = workflowId,
            Type = triggerType,
        };

        var action = new WorkflowAction
        {
            Id = workflowId * 10 + 2,
            WorkflowId = workflowId,
            Type = actionType,
            Order = 1,
            ActionParameters = """{"tag_ids": [1]}""",
        };

        return new Workflow
        {
            Id = workflowId,
            Name = name,
            Order = order,
            Enabled = enabled,
            Triggers = new List<WorkflowTrigger> { trigger },
            Actions = new List<WorkflowAction> { action },
        };
    }

    #endregion

    // ── Basic trigger type tests ─────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_ConsumptionTrigger_ExecutesActions()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowActionResult.Success("ok"));

        var context = CreateContext("Consumption");
        var workflows = new[] { CreateWorkflow() };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().HaveCount(1);
        results[0].IsSuccess.Should().BeTrue();
        results[0].Message.Should().Be("ok");
    }

    [Fact]
    public async Task ProcessDocumentAsync_DocumentAddedTrigger_ExecutesActions()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowActionResult.Success("added"));

        var context = CreateContext("DocumentAdded");
        var workflows = new[] { CreateWorkflow(triggerType: WorkflowTriggerType.DocumentAdded) };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().HaveCount(1);
        results[0].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDocumentAsync_DocumentUpdatedTrigger_ExecutesActions()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowActionResult.Success("updated"));

        var context = CreateContext("DocumentUpdated");
        var workflows = new[] { CreateWorkflow(triggerType: WorkflowTriggerType.DocumentUpdated) };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().HaveCount(1);
        results[0].IsSuccess.Should().BeTrue();
    }

    // ── Disabled workflow ───────────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_DisabledWorkflow_NotExecuted()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        // Evaluator should never be called for disabled workflows
        var context = CreateContext();
        var workflows = new[] { CreateWorkflow(enabled: false) };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().BeEmpty();
        await evaluator.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default!);
        await executor.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!);
    }

    // ── Execution order ─────────────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_RespectsOrder()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var executionOrder = new List<string>();
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var action = callInfo.Arg<WorkflowAction>();
                executionOrder.Add(action.WorkflowId.ToString());
                return WorkflowActionResult.Success("ok");
            });

        var context = CreateContext();
        var workflows = new[]
        {
            CreateWorkflow("Second", order: 2),
            CreateWorkflow("First", order: 1),
            CreateWorkflow("Third", order: 3),
        };

        await engine.ProcessDocumentAsync(context, workflows);

        executionOrder.Should().Equal(["1", "2", "3"]);
    }

    [Fact]
    public async Task ProcessDocumentAsync_SameOrder_ExecutesBoth()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var executed = 0;
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                executed++;
                return WorkflowActionResult.Success("ok");
            });

        var context = CreateContext();
        var workflows = new[]
        {
            CreateWorkflow("A", order: 1),
            CreateWorkflow("B", order: 1),
        };

        await engine.ProcessDocumentAsync(context, workflows);

        executed.Should().Be(2);
    }

    // ── Error handling ──────────────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_OneWorkflowFails_ContinuesWithOthers()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var callCount = 0;
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("First workflow failed");
                return WorkflowActionResult.Success("second ok");
            });

        var context = CreateContext();
        var workflows = new[]
        {
            CreateWorkflow("Failing", order: 1),
            CreateWorkflow("Succeeding", order: 2),
        };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().HaveCount(2);
        results[0].IsSuccess.Should().BeFalse();
        results[0].ErrorMessage.Should().Contain("First workflow failed");
        results[1].IsSuccess.Should().BeTrue();
        results[1].Message.Should().Be("second ok");
    }

    [Fact]
    public async Task ProcessDocumentAsync_TriggerEvaluatorThrows_DoesNotBlockOtherWorkflows()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var trigger = callInfo.Arg<WorkflowTrigger>();
                if (trigger.WorkflowId == 1)
                    throw new InvalidOperationException("Evaluator failed for Wf1");
                return true;
            });
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowActionResult.Success("ok"));

        var context = CreateContext();
        var workflows = new[]
        {
            CreateWorkflow("FailingEval", order: 1),
            CreateWorkflow("Good", order: 2),
        };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().HaveCount(2);
        results[0].IsSuccess.Should().BeFalse();
        results[1].IsSuccess.Should().BeTrue();
    }

    // ── No matching trigger ─────────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_NoMatchingTrigger_ReturnsEmpty()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var context = CreateContext();
        var workflows = new[] { CreateWorkflow() };

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().BeEmpty();
        await executor.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!);
    }

    [Fact]
    public async Task ProcessDocumentAsync_EmptyWorkflows_ReturnsEmpty()
    {
        var engine = CreateEngine(out _, out _, out _);
        var context = CreateContext();
        var workflows = Array.Empty<Workflow>();

        var results = await engine.ProcessDocumentAsync(context, workflows);

        results.Should().BeEmpty();
    }

    // ── Null argument guards ────────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_NullContext_ThrowsArgumentNullException()
    {
        var engine = CreateEngine(out _, out _, out _);

        var act = async () => await engine.ProcessDocumentAsync(null!, Array.Empty<Workflow>());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task ProcessDocumentAsync_NullWorkflows_ThrowsArgumentNullException()
    {
        var engine = CreateEngine(out _, out _, out _);
        var context = CreateContext();

        var act = async () => await engine.ProcessDocumentAsync(context, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("workflows");
    }

    // ── Domain event publishing ─────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_PublishesWorkflowExecutedEvent()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out var publisher);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowActionResult.Success("ok"));

        var context = CreateContext();
        var workflows = new[] { CreateWorkflow("EventTest", order: 1) };

        await engine.ProcessDocumentAsync(context, workflows);

        await publisher.Received(1).Publish(
            Arg.Is<WorkflowExecutedEvent>(e =>
                e.WorkflowId == 1 &&
                e.WorkflowName == "EventTest" &&
                e.DocumentId == context.Document.Id &&
                e.SuccessCount == 1 &&
                e.FailureCount == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDocumentAsync_EventNotPublished_WhenNoTriggerMatches()
    {
        var engine = CreateEngine(out var evaluator, out _, out var publisher);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var context = CreateContext();
        var workflows = new[] { CreateWorkflow() };

        await engine.ProcessDocumentAsync(context, workflows);

        await publisher.DidNotReceiveWithAnyArgs().Publish(default!, default!);
    }

    // ── Cancellation ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_CancellationToken_StopsProcessing()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(WorkflowActionResult.Success("ok"));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = CreateContext();
        var workflows = new[] { CreateWorkflow() };

        var act = async () => await engine.ProcessDocumentAsync(context, workflows, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Modifications propagation ───────────────────────────────────

    [Fact]
    public async Task ProcessDocumentAsync_ActionModifications_PropagateToSubsequentActions()
    {
        var engine = CreateEngine(out var evaluator, out var executor, out _);
        evaluator.EvaluateAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var callIndex = 0;
        executor.ExecuteAsync(Arg.Any<WorkflowAction>(), Arg.Any<DocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callIndex++;
                var ctx = callInfo.Arg<DocumentContext>();
                if (callIndex == 1)
                {
                    // First action: verify no correspondent yet, then set it
                    ctx.Document.CorrespondentId.Should().BeNull();
                    return WorkflowActionResult.Success(
                        "set correspondent",
                        new DocumentModifications { CorrespondentId = 5 });
                }

                // Second action: should see the correspondent set by first action
                ctx.Document.CorrespondentId.Should().Be(5);
                return WorkflowActionResult.Success(
                    "set document type",
                    new DocumentModifications { DocumentTypeId = 3 });
            });

        var context = CreateContext();
        var workflow = CreateWorkflow();
        workflow.Actions = new List<WorkflowAction>
        {
            new() { Id = 1, Type = WorkflowActionType.SetCorrespondent, Order = 1, ActionParameters = """{"correspondent_id": 5}""" },
            new() { Id = 2, Type = WorkflowActionType.SetDocumentType, Order = 2, ActionParameters = """{"document_type_id": 3}""" },
        };

        var results = await engine.ProcessDocumentAsync(context, new[] { workflow });

        results.Should().HaveCount(2);
        results.All(r => r.IsSuccess).Should().BeTrue();
    }
}
