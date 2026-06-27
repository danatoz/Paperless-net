namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Defines the storage format for document files.
/// Maps to the storage type constants from the original paperless-ngx data model.
/// </summary>
public enum StorageType
{
    /// <summary>PDF format (archive, preview).</summary>
    Pdf = 1,

    /// <summary>PNG format (thumbnails).</summary>
    Png = 2,
}
