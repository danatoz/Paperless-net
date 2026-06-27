using Paperless.Core.Documents.Entities;
using Paperless.Core.Documents.Enums;
using Paperless.Core.Documents.ValueObjects;
using Paperless.Shared.Abstractions;
using DocTaskStatus = Paperless.Core.Documents.Enums.TaskStatus;

namespace Paperless.Core.Tests.Helpers;

/// <summary>
/// Factory methods to create test data for domain entities.
/// Simplifies test setup and reduces duplication across test classes.
/// </summary>
public static class TestData
{
    // ── Document ────────────────────────────────────────────────────

    public static Document CreateDocument(Action<Document>? configure = null)
    {
        var doc = new Document
        {
            Id = 1,
            Title = "Test Document",
            Content = "Sample OCR text content",
            Created = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Added = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            Modified = new DateTime(2024, 1, 15, 12, 30, 0, DateTimeKind.Utc),
            Checksum = "abc123def456",
            Filename = "test_document.pdf",
            StoragePath = "documents/2024/test_document.pdf",
            OwnerId = 1,
        };
        configure?.Invoke(doc);
        return doc;
    }

    public static Document CreateDocumentWithCorrespondent(string correspondentName = "ACME Corp")
    {
        var correspondent = CreateCorrespondent(name: correspondentName);
        var doc = CreateDocument(d =>
        {
            d.Correspondent = correspondent;
            d.CorrespondentId = correspondent.Id;
        });
        return doc;
    }

    public static Document CreateDocumentWithTags(params string[] tagNames)
    {
        var tags = tagNames.Select(name => CreateTag(name: name)).ToList();
        var doc = CreateDocument(d =>
        {
            foreach (var tag in tags)
                d.Tags.Add(tag);
        });
        return doc;
    }

    // ── Correspondent ───────────────────────────────────────────────

    public static Correspondent CreateCorrespondent(
        int id = 1,
        string name = "ACME Corp",
        string? match = null,
        MatchingAlgorithm algorithm = MatchingAlgorithm.Auto,
        bool isInsensitive = true)
    {
        return new Correspondent
        {
            Id = id,
            Name = name,
            Match = match ?? name,
            MatchingAlgorithm = algorithm,
            IsInsensitive = isInsensitive,
        };
    }

    // ── Tag ─────────────────────────────────────────────────────────

    public static Tag CreateTag(
        int id = 1,
        string name = "Important",
        string? color = null,
        bool isInboxTag = false)
    {
        return new Tag
        {
            Id = id,
            Name = name,
            Color = color ?? "#FF5733",
            IsInboxTag = isInboxTag,
        };
    }

    // ── DocumentType ────────────────────────────────────────────────

    public static DocumentType CreateDocumentType(
        int id = 1,
        string name = "Invoice",
        string? match = null,
        MatchingAlgorithm algorithm = MatchingAlgorithm.Auto,
        bool isInsensitive = true)
    {
        return new DocumentType
        {
            Id = id,
            Name = name,
            Match = match ?? name,
            MatchingAlgorithm = algorithm,
            IsInsensitive = isInsensitive,
        };
    }

    // ── StoragePath ─────────────────────────────────────────────────

    public static StoragePath CreateStoragePath(
        int id = 1,
        string name = "Default",
        string path = "{correspondent}/{title}")
    {
        return new StoragePath
        {
            Id = id,
            Name = name,
            PathTemplate = path,
        };
    }

    // ── CustomField ─────────────────────────────────────────────────

    public static CustomField CreateCustomField(
        int id = 1,
        string name = "Invoice Number",
        CustomFieldType type = CustomFieldType.Text)
    {
        return new CustomField
        {
            Id = id,
            Name = name,
            Type = type,
        };
    }

    // ── PaperlessTask ───────────────────────────────────────────────

    public static PaperlessTask CreateTask(
        int id = 1,
        DocTaskStatus status = DocTaskStatus.Pending,
        string taskId = "hangfire-task-001")
    {
        return new PaperlessTask
        {
            Id = id,
            TaskId = taskId,
            Status = status,
            Name = "Test Task",
            Acknowledged = false,
        };
    }

    // ── ConsumableDocument ──────────────────────────────────────────

    public static Common.Models.ConsumableDocument CreateConsumableDocument(
        string? title = null,
        string? filePath = null,
        int? correspondentId = null,
        int? documentTypeId = null)
    {
        return new Common.Models.ConsumableDocument
        {
            Title = title ?? "uploaded_document.pdf",
            OriginalFilePath = filePath ?? "/tmp/test_document.pdf",
            CorrespondentId = correspondentId,
            DocumentTypeId = documentTypeId,
            Added = DateTime.UtcNow,
        };
    }

    // ── ConsumerResult ──────────────────────────────────────────────

    public static Common.Models.ConsumerResult CreateSuccessResult(int? documentId = null) =>
        Common.Models.ConsumerResult.Success(documentId, "Processed successfully");

    public static Common.Models.ConsumerResult CreateFailureResult(string code = "ERROR", string message = "Something went wrong") =>
        Common.Models.ConsumerResult.Failure(new Error(code, message), message);

    // ── ConsumerContext ─────────────────────────────────────────────

    public static Common.Models.ConsumerContext CreateConsumerContext(
        Common.Models.ConsumableDocument? document = null,
        Document? documentEntity = null)
    {
        return new Common.Models.ConsumerContext
        {
            Document = document ?? CreateConsumableDocument(),
            DocumentEntity = documentEntity,
            WorkingDirectory = "/tmp/paperless",
            StageStatuses = new Dictionary<string, bool>(),
            Metadata = new Dictionary<string, object?>(),
        };
    }
}
