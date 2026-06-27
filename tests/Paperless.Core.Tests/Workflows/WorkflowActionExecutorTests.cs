using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.Entities;
using Paperless.Core.Tests.Helpers;
using Paperless.Core.Workflows.Entities;
using Paperless.Core.Workflows.Interfaces;
using Paperless.Core.Workflows.Services;

namespace Paperless.Core.Tests.Workflows;

/// <summary>
/// Unit tests for the IWorkflowActionExecutor contract.
/// Tests execution of all action types: SetCorrespondent, SetTag,
/// SetDocumentType, SetStoragePath, and Webhook.
/// Maps to action execution logic from documents/workflows/actions.py
/// and documents/workflows/mutations.py.
/// </summary>
public class WorkflowActionExecutorTests
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
    /// JSON model for action parameters.
    /// Properties use JsonPropertyName for snake_case JSON keys.
    /// </summary>
    private sealed record ActionParams
    {
        [JsonPropertyName("correspondent_id")]
        public int? CorrespondentId { get; init; }

        [JsonPropertyName("tag_ids")]
        public List<int>? TagIds { get; init; }

        [JsonPropertyName("document_type_id")]
        public int? DocumentTypeId { get; init; }

        [JsonPropertyName("storage_path_id")]
        public int? StoragePathId { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("payload")]
        public string? Payload { get; init; }

        [JsonPropertyName("http_method")]
        public string? HttpMethod { get; init; }
    }

    /// <summary>
    /// Test implementation of IWorkflowActionExecutor that mimics
    /// the action execution logic from documents/workflows/actions.py.
    /// </summary>
    private sealed class TestWorkflowActionExecutor : IWorkflowActionExecutor
    {
        private readonly HttpClient _httpClient;

        public TestWorkflowActionExecutor(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<WorkflowActionResult> ExecuteAsync(
            WorkflowAction action,
            DocumentContext context,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(context);

            ct.ThrowIfCancellationRequested();

            ActionParams? parameters = null;
            if (!string.IsNullOrWhiteSpace(action.ActionParameters))
            {
                try
                {
                    parameters = JsonSerializer.Deserialize<ActionParams>(action.ActionParameters, JsonOptions);
                }
                catch (JsonException)
                {
                    return WorkflowActionResult.Failure($"Invalid action parameters JSON: {action.ActionParameters}");
                }
            }

            return action.Type switch
            {
                WorkflowActionType.SetCorrespondent => await ExecuteSetCorrespondent(context, parameters, ct),
                WorkflowActionType.SetTag => await ExecuteSetTag(context, parameters, ct),
                WorkflowActionType.SetDocumentType => await ExecuteSetDocumentType(context, parameters, ct),
                WorkflowActionType.SetStoragePath => await ExecuteSetStoragePath(context, parameters, ct),
                WorkflowActionType.Webhook => await ExecuteWebhook(context, parameters, ct),
                _ => WorkflowActionResult.Failure($"Unknown action type: {action.Type}"),
            };
        }

        private static Task<WorkflowActionResult> ExecuteSetCorrespondent(
            DocumentContext context,
            ActionParams? parameters,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (parameters?.CorrespondentId is null)
            {
                return Task.FromResult(
                    WorkflowActionResult.Success("No correspondent to set (no parameters)"));
            }

            var modifications = new DocumentModifications
            {
                CorrespondentId = parameters.CorrespondentId.Value,
            };

            context.Document.CorrespondentId = parameters.CorrespondentId.Value;
            return Task.FromResult(
                WorkflowActionResult.Success(
                    $"Set correspondent to {parameters.CorrespondentId}",
                    modifications));
        }

        private static Task<WorkflowActionResult> ExecuteSetTag(
            DocumentContext context,
            ActionParams? parameters,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (parameters?.TagIds is null || parameters.TagIds.Count == 0)
            {
                return Task.FromResult(
                    WorkflowActionResult.Success("No tags to set (no parameters)"));
            }

            // Add tags to the document model
            foreach (var tagId in parameters.TagIds)
            {
                if (context.Document.Tags.All(t => t.Id != tagId))
                {
                    context.Document.Tags.Add(new Tag
                    {
                        Id = tagId,
                        Name = $"Tag{tagId}",
                    });
                }
            }

            var modifications = new DocumentModifications
            {
                TagIds = parameters.TagIds.AsReadOnly(),
            };

            return Task.FromResult(
                WorkflowActionResult.Success(
                    $"Added tags: {string.Join(", ", parameters.TagIds)}",
                    modifications));
        }

        private static Task<WorkflowActionResult> ExecuteSetDocumentType(
            DocumentContext context,
            ActionParams? parameters,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (parameters?.DocumentTypeId is null)
            {
                return Task.FromResult(
                    WorkflowActionResult.Success("No document type to set (no parameters)"));
            }

            var modifications = new DocumentModifications
            {
                DocumentTypeId = parameters.DocumentTypeId.Value,
            };

            context.Document.DocumentTypeId = parameters.DocumentTypeId.Value;
            return Task.FromResult(
                WorkflowActionResult.Success(
                    $"Set document type to {parameters.DocumentTypeId}",
                    modifications));
        }

        private static Task<WorkflowActionResult> ExecuteSetStoragePath(
            DocumentContext context,
            ActionParams? parameters,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (parameters?.StoragePathId is null)
            {
                return Task.FromResult(
                    WorkflowActionResult.Success("No storage path to set (no parameters)"));
            }

            var modifications = new DocumentModifications
            {
                StoragePathId = parameters.StoragePathId.Value,
            };

            return Task.FromResult(
                WorkflowActionResult.Success(
                    $"Set storage path to {parameters.StoragePathId}",
                    modifications));
        }

        private async Task<WorkflowActionResult> ExecuteWebhook(
            DocumentContext context,
            ActionParams? parameters,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(parameters?.Url))
            {
                return WorkflowActionResult.Failure("Webhook URL is required");
            }

            try
            {
                var requestMethod = parameters.HttpMethod?.ToUpperInvariant() switch
                {
                    "PUT" => HttpMethod.Put,
                    "PATCH" => HttpMethod.Patch,
                    "DELETE" => HttpMethod.Delete,
                    _ => HttpMethod.Post,
                };

                var request = new HttpRequestMessage(requestMethod, parameters.Url);

                if (!string.IsNullOrWhiteSpace(parameters.Payload))
                {
                    request.Content = new StringContent(
                        parameters.Payload,
                        System.Text.Encoding.UTF8,
                        "application/json");
                }

                var response = await _httpClient.SendAsync(request, ct);

                return response.IsSuccessStatusCode
                    ? WorkflowActionResult.Success($"Webhook sent to {parameters.Url} (status: {(int)response.StatusCode})")
                    : WorkflowActionResult.Failure(
                        $"Webhook to {parameters.Url} returned {(int)response.StatusCode}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return WorkflowActionResult.Failure($"Webhook failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// A DelegatingHandler that captures the last request and returns a canned response.
    /// </summary>
    private sealed class FakeHttpMessageHandler : DelegatingHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public int CallCount { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : null;

            return new HttpResponseMessage(StatusCode);
        }
    }

    #endregion

    private static TestWorkflowActionExecutor CreateExecutor(HttpMessageHandler? handler = null)
    {
        var httpClient = handler is not null ? new HttpClient(handler) : new HttpClient();
        return new TestWorkflowActionExecutor(httpClient);
    }

    private static DocumentContext CreateContext()
    {
        return new DocumentContext
        {
            Document = TestData.CreateDocument(),
            EventType = "Consumption",
        };
    }

    private static WorkflowAction CreateAction(
        WorkflowActionType type,
        string? parameters = null,
        int order = 1)
    {
        return new WorkflowAction
        {
            Id = order,
            Type = type,
            Order = order,
            ActionParameters = parameters,
        };
    }

    // ── SetCorrespondent ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SetCorrespondent_ReturnsModifications()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetCorrespondent, """{"correspondent_id": 5}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().NotBeNull();
        result.Modifications!.CorrespondentId.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteAsync_SetCorrespondent_UpdatesDocumentContext()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetCorrespondent, """{"correspondent_id": 5}""");

        await executor.ExecuteAsync(action, context);

        context.Document.CorrespondentId.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteAsync_SetCorrespondent_NoParameters_ReturnsSuccessWithNoChanges()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetCorrespondent, null);

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_SetCorrespondent_OverwritesExistingCorrespondent()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        context.Document.CorrespondentId = 3;
        var action = CreateAction(WorkflowActionType.SetCorrespondent, """{"correspondent_id": 7}""");

        await executor.ExecuteAsync(action, context);

        context.Document.CorrespondentId.Should().Be(7);
    }

    // ── SetTag ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SetTag_ReturnsModificationsWithTagIds()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetTag, """{"tag_ids": [10, 20]}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().NotBeNull();
        result.Modifications!.TagIds.Should().BeEquivalentTo([10, 20]);
    }

    [Fact]
    public async Task ExecuteAsync_SetTag_AddsTagsToDocument()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetTag, """{"tag_ids": [10, 20]}""");

        await executor.ExecuteAsync(action, context);

        context.Document.Tags.Select(t => t.Id).Should().Contain([10, 20]);
    }

    [Fact]
    public async Task ExecuteAsync_SetTag_NoDuplicateTags()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        context.Document.Tags.Add(TestData.CreateTag(id: 10, name: "Existing"));
        var action = CreateAction(WorkflowActionType.SetTag, """{"tag_ids": [10, 20]}""");

        await executor.ExecuteAsync(action, context);

        context.Document.Tags.Select(t => t.Id).Should().Contain([10, 20]);
        context.Document.Tags.Count(t => t.Id == 10).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_SetTag_NoParameters_ReturnsSuccessWithNoChanges()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetTag, """{"tag_ids": []}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
    }

    // ── SetDocumentType ────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SetDocumentType_ReturnsModifications()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetDocumentType, """{"document_type_id": 3}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().NotBeNull();
        result.Modifications!.DocumentTypeId.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_SetDocumentType_UpdatesDocumentContext()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetDocumentType, """{"document_type_id": 3}""");

        await executor.ExecuteAsync(action, context);

        context.Document.DocumentTypeId.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_SetDocumentType_NoParameters_ReturnsSuccessWithNoChanges()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetDocumentType, null);

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().BeNull();
    }

    // ── SetStoragePath ─────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SetStoragePath_ReturnsModifications()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetStoragePath, """{"storage_path_id": 4}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().NotBeNull();
        result.Modifications!.StoragePathId.Should().Be(4);
    }

    [Fact]
    public async Task ExecuteAsync_SetStoragePath_NoParameters_ReturnsSuccessWithNoChanges()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetStoragePath, null);

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Modifications.Should().BeNull();
    }

    // ── Webhook ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Webhook_SendsHttpRequest()
    {
        var handler = new FakeHttpMessageHandler();
        var executor = CreateExecutor(handler);
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.Webhook, """{"url": "https://example.com/hook"}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("https://example.com/hook");
        handler.CallCount.Should().Be(1);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be("https://example.com/hook");
        handler.LastRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task ExecuteAsync_Webhook_WithPayload_SendsPayload()
    {
        var handler = new FakeHttpMessageHandler();
        var executor = CreateExecutor(handler);
        var context = CreateContext();
        var action = CreateAction(
            WorkflowActionType.Webhook,
            """{"url": "https://example.com/hook", "payload": "{\"event\": \"processed\"}"}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequestBody.Should().Contain("processed");
    }

    [Fact]
    public async Task ExecuteAsync_Webhook_NoUrl_ReturnsFailure()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.Webhook, """{"url": ""}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("URL");
    }

    [Fact]
    public async Task ExecuteAsync_Webhook_ServerError_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler { StatusCode = HttpStatusCode.InternalServerError };
        var executor = CreateExecutor(handler);
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.Webhook, """{"url": "https://example.com/hook"}""");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task ExecuteAsync_Webhook_WithPutMethod_SendsPut()
    {
        var handler = new FakeHttpMessageHandler();
        var executor = CreateExecutor(handler);
        var context = CreateContext();
        var action = CreateAction(
            WorkflowActionType.Webhook,
            """{"url": "https://example.com/hook", "http_method": "PUT"}""");

        await executor.ExecuteAsync(action, context);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
    }

    // ── Invalid action type ────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_UnknownActionType_ReturnsFailure()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction((WorkflowActionType)999, null);

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown");
    }

    // ── Invalid parameters ─────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_InvalidJsonParameters_ReturnsFailure()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetCorrespondent, "not-json");

        var result = await executor.ExecuteAsync(action, context);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid action parameters");
    }

    // ── Null guard ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NullAction_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor();
        var context = CreateContext();

        var act = async () => await executor.ExecuteAsync(null!, context);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor();
        var action = CreateAction(WorkflowActionType.SetCorrespondent, """{"correspondent_id": 5}""");

        var act = async () => await executor.ExecuteAsync(action, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    // ── Cancellation ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CancellationToken_StopsExecution()
    {
        var executor = CreateExecutor();
        var context = CreateContext();
        var action = CreateAction(WorkflowActionType.SetCorrespondent, """{"correspondent_id": 5}""");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await executor.ExecuteAsync(action, context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Multiple sequential actions ─────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_MultipleActionsSequential_AllApplied()
    {
        var handler = new FakeHttpMessageHandler();
        var executor = CreateExecutor(handler);
        var context = CreateContext();

        // Execute SetCorrespondent
        var setCorr = CreateAction(WorkflowActionType.SetCorrespondent, """{"correspondent_id": 5}""");
        var r1 = await executor.ExecuteAsync(setCorr, context);
        r1.IsSuccess.Should().BeTrue();

        // Execute SetDocumentType
        var setDocType = CreateAction(WorkflowActionType.SetDocumentType, """{"document_type_id": 3}""");
        var r2 = await executor.ExecuteAsync(setDocType, context);
        r2.IsSuccess.Should().BeTrue();

        // Execute SetTag
        var setTag = CreateAction(WorkflowActionType.SetTag, """{"tag_ids": [10, 20]}""");
        var r3 = await executor.ExecuteAsync(setTag, context);
        r3.IsSuccess.Should().BeTrue();

        // Execute Webhook
        var webhook = CreateAction(WorkflowActionType.Webhook, """{"url": "https://example.com/done"}""");
        var r4 = await executor.ExecuteAsync(webhook, context);
        r4.IsSuccess.Should().BeTrue();

        // Verify all applied
        context.Document.CorrespondentId.Should().Be(5);
        context.Document.DocumentTypeId.Should().Be(3);
        context.Document.Tags.Select(t => t.Id).Should().Contain([10, 20]);
        handler.CallCount.Should().Be(1);
    }
}
