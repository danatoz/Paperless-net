using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Paperless.Core.Documents.Enums;
using Paperless.Core.Tests.Helpers;
using Paperless.Core.Workflows.Entities;
using Paperless.Core.Workflows.Interfaces;
using Paperless.Core.Workflows.Services;

namespace Paperless.Core.Tests.Workflows;

/// <summary>
/// Unit tests for the IWorkflowTriggerEvaluator contract.
/// Tests evaluate whether trigger conditions (type, correspondent, tags, document type)
/// match a given document context.
/// Maps to trigger evaluation logic from documents/workflows/actions.py.
/// </summary>
public class WorkflowTriggerEvaluatorTests
{
    #region Test implementation

    /// <summary>
    /// JSON serializer options with case-insensitive property matching
    /// to handle snake_case JSON keys from the original Python model.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Test implementation of IWorkflowTriggerEvaluator that mimics
    /// the trigger evaluation logic from documents/workflows/actions.py.
    /// </summary>
    private sealed class TestWorkflowTriggerEvaluator : IWorkflowTriggerEvaluator
    {
        public Task<bool> EvaluateAsync(
            WorkflowTrigger trigger,
            DocumentContext context,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Check trigger type matches the event type
            var triggerTypeName = trigger.Type switch
            {
                WorkflowTriggerType.Consumption => "Consumption",
                WorkflowTriggerType.DocumentAdded => "DocumentAdded",
                WorkflowTriggerType.DocumentUpdated => "DocumentUpdated",
                _ => trigger.Type.ToString(),
            };

            if (!string.Equals(triggerTypeName, context.EventType, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(false);

            // 2. If no filter rules, the trigger matches on type alone
            if (string.IsNullOrWhiteSpace(trigger.FilterRules))
                return Task.FromResult(true);

            try
            {
                var filters = JsonSerializer.Deserialize<TriggerFilterRules>(trigger.FilterRules, JsonOptions);
                if (filters is null)
                    return Task.FromResult(true); // no parseable filters → match

                // Check correspondent filter
                if (filters.CorrespondentId.HasValue)
                {
                    if (context.Document.CorrespondentId != filters.CorrespondentId.Value)
                        return Task.FromResult(false);
                }

                // Check document type filter
                if (filters.DocumentTypeId.HasValue)
                {
                    if (context.Document.DocumentTypeId != filters.DocumentTypeId.Value)
                        return Task.FromResult(false);
                }

                // Check tags filter
                if (filters.TagIds is { Count: > 0 })
                {
                    var docTagIds = context.Document.Tags
                        .Select(t => t.Id)
                        .ToHashSet();

                    if (filters.TagsMatchAll)
                    {
                        // ALL specified tags must be present on the document
                        if (!filters.TagIds.All(t => docTagIds.Contains(t)))
                            return Task.FromResult(false);
                    }
                    else
                    {
                        // ANY specified tag must be present on the document
                        if (!filters.TagIds.Any(t => docTagIds.Contains(t)))
                            return Task.FromResult(false);
                    }
                }
            }
            catch (JsonException)
            {
                // Malformed filter rules → no filtering applied (match)
            }

            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// JSON model for trigger filter rules.
    /// Matches the structure from documents/workflows/actions.py filter parsing.
    /// Properties use JsonPropertyName for snake_case JSON keys.
    /// </summary>
    private sealed record TriggerFilterRules
    {
        [JsonPropertyName("correspondent_id")]
        public int? CorrespondentId { get; init; }

        [JsonPropertyName("document_type_id")]
        public int? DocumentTypeId { get; init; }

        [JsonPropertyName("tag_ids")]
        public List<int>? TagIds { get; init; }

        [JsonPropertyName("tags_match_all")]
        public bool TagsMatchAll { get; init; } = true;
    }

    #endregion

    private static WorkflowTriggerEvaluatorTests.TestWorkflowTriggerEvaluator CreateEvaluator()
        => new();

    private static DocumentContext CreateContext(
        int? correspondentId = null,
        int? documentTypeId = null,
        string eventType = "Consumption",
        params int[] tagIds)
    {
        var doc = TestData.CreateDocument(d =>
        {
            d.CorrespondentId = correspondentId;
            d.DocumentTypeId = documentTypeId;
        });

        foreach (var tagId in tagIds)
        {
            doc.Tags.Add(TestData.CreateTag(id: tagId, name: $"Tag{tagId}"));
        }

        return new DocumentContext
        {
            Document = doc,
            EventType = eventType,
        };
    }

    private static WorkflowTrigger CreateTrigger(
        WorkflowTriggerType type = WorkflowTriggerType.Consumption,
        string? filterRules = null)
    {
        return new WorkflowTrigger
        {
            Id = 1,
            Type = type,
            FilterRules = filterRules,
        };
    }

    // ── Trigger type matching ────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_ConsumptionTrigger_WithConsumptionEvent_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(WorkflowTriggerType.Consumption);
        var context = CreateContext(eventType: "Consumption");

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_DocumentAddedTrigger_WithAddedEvent_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(WorkflowTriggerType.DocumentAdded);
        var context = CreateContext(eventType: "DocumentAdded");

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_DocumentUpdatedTrigger_WithUpdatedEvent_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(WorkflowTriggerType.DocumentUpdated);
        var context = CreateContext(eventType: "DocumentUpdated");

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_TriggerTypeMismatch_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(WorkflowTriggerType.Consumption);
        var context = CreateContext(eventType: "DocumentUpdated");

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_EventTypeCaseInsensitive_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(WorkflowTriggerType.Consumption);
        var context = CreateContext(eventType: "consumption"); // lowercase

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    // ── No filters ──────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_NoFilterRules_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(filterRules: null);
        var context = CreateContext();

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyFilterRules_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(filterRules: "");
        var context = CreateContext();

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhitespaceFilterRules_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(filterRules: "   ");
        var context = CreateContext();

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_MalformedFilterRules_DoesNotThrowAndReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger(filterRules: "not-json-at-all");
        var context = CreateContext();

        var act = async () => await evaluator.EvaluateAsync(trigger, context);

        await act.Should().NotThrowAsync();
        (await act()).Should().BeTrue();
    }

    // ── Correspondent filter ────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_FilterByMatchingCorrespondent_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"correspondent_id": 5}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(correspondentId: 5);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByNonMatchingCorrespondent_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"correspondent_id": 5}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(correspondentId: 99);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByCorrespondent_DocumentHasNoCorrespondent_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"correspondent_id": 5}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(correspondentId: null);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    // ── Document type filter ────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_FilterByMatchingDocumentType_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"document_type_id": 3}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(documentTypeId: 3);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByNonMatchingDocumentType_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"document_type_id": 3}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(documentTypeId: 7);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByDocumentType_DocumentHasNoType_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"document_type_id": 3}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(documentTypeId: null);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    // ── Tags filter ─────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_FilterByTagsAny_OneTagMatches_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"tag_ids": [1, 2, 3], "tags_match_all": false}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(tagIds: 2); // document has tag 2

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByTagsAny_NoMatches_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"tag_ids": [1, 2, 3], "tags_match_all": false}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(tagIds: [4, 5]); // document has tags 4, 5

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByTagsAll_AllMatch_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"tag_ids": [1, 2, 3], "tags_match_all": true}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(tagIds: [1, 2, 3]);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByTagsAll_OneMissing_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"tag_ids": [1, 2, 3], "tags_match_all": true}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(tagIds: [1, 2]); // missing tag 3

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByTagsAllDefault_DocumentHasNoTags_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        // tags_match_all defaults to true
        var rules = """{"tag_ids": [1]}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(); // no tags

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_FilterByTagsAny_DocumentHasNoTags_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"tag_ids": [1], "tags_match_all": false}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(); // no tags

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    // ── Combined filters ────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_AllFiltersMatch_ReturnsTrue()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"correspondent_id": 5, "document_type_id": 3, "tag_ids": [1, 2], "tags_match_all": true}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(correspondentId: 5, documentTypeId: 3, tagIds: [1, 2]);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OneFilterFailsAmongMany_ReturnsFalse()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"correspondent_id": 5, "document_type_id": 3, "tag_ids": [1, 2], "tags_match_all": true}""";
        var trigger = CreateTrigger(filterRules: rules);
        // correspondent matches, but document type does not
        var context = CreateContext(correspondentId: 5, documentTypeId: 7, tagIds: [1, 2]);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeFalse();
    }

    // ── Cancellation ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_CancellationToken_StopsEvaluation()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger();
        var context = CreateContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await evaluator.EvaluateAsync(trigger, context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Edge cases ──────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_EmptyTagIdsList_NoFilterApplied()
    {
        var evaluator = CreateEvaluator();
        var rules = """{"tag_ids": [], "tags_match_all": true}""";
        var trigger = CreateTrigger(filterRules: rules);
        var context = CreateContext(tagIds: 1);

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_UnknownTriggerType_HandledGracefully()
    {
        var evaluator = CreateEvaluator();
        var trigger = CreateTrigger((WorkflowTriggerType)999);
        var context = CreateContext(eventType: "999");

        var result = await evaluator.EvaluateAsync(trigger, context);

        result.Should().BeTrue();
    }
}
