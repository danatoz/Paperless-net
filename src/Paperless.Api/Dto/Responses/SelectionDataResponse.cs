using System.Text.Json.Serialization;

namespace Paperless.Api.Dto.Responses;

/// <summary>
/// DRF-compatible response for the selection_data endpoint.
/// Provides data needed by the frontend for bulk operation dialogs.
/// </summary>
public class SelectionDataResponse
{
    [JsonPropertyName("selected")]
    public SelectedDocumentsInfo Selected { get; init; } = new();

    [JsonPropertyName("all")]
    public AllDocumentsInfo All { get; init; } = new();
}

/// <summary>
/// Information about explicitly selected documents.
/// </summary>
public class SelectedDocumentsInfo
{
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<int> DocumentIds { get; init; } = Array.Empty<int>();

    [JsonPropertyName("correspondents")]
    public IReadOnlyCollection<int> CorrespondentIds { get; init; } = Array.Empty<int>();

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<int> TagIds { get; init; } = Array.Empty<int>();

    [JsonPropertyName("document_types")]
    public IReadOnlyCollection<int> DocumentTypeIds { get; init; } = Array.Empty<int>();

    [JsonPropertyName("storage_paths")]
    public IReadOnlyCollection<int> StoragePathIds { get; init; } = Array.Empty<int>();
}

/// <summary>
/// Information about all documents matching the current filter.
/// </summary>
public class AllDocumentsInfo
{
    [JsonPropertyName("document_count")]
    public int DocumentCount { get; init; }

    [JsonPropertyName("correspondents")]
    public IReadOnlyCollection<IdNamePair> Correspondents { get; init; } = Array.Empty<IdNamePair>();

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<IdNamePair> Tags { get; init; } = Array.Empty<IdNamePair>();

    [JsonPropertyName("document_types")]
    public IReadOnlyCollection<IdNamePair> DocumentTypes { get; init; } = Array.Empty<IdNamePair>();

    [JsonPropertyName("storage_paths")]
    public IReadOnlyCollection<IdNamePair> StoragePaths { get; init; } = Array.Empty<IdNamePair>();
}

/// <summary>
/// Simple id + name pair used in selection data responses.
/// </summary>
public class IdNamePair
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
