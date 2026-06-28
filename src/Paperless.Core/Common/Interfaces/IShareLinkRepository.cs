using Paperless.Core.Documents.Entities;

namespace Paperless.Core.Common.Interfaces;

/// <summary>
/// Repository interface for ShareLink and ShareLinkBundle entity operations.
/// </summary>
public interface IShareLinkRepository
{
    /// <summary>
    /// Gets a share link by its unique identifier.
    /// </summary>
    Task<ShareLink?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all share links.
    /// </summary>
    Task<IReadOnlyCollection<ShareLink>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a share link by its URL slug.
    /// </summary>
    Task<ShareLink?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Gets all share links for a specific document.
    /// </summary>
    Task<IReadOnlyCollection<ShareLink>> GetByDocumentIdAsync(int documentId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new share link.
    /// </summary>
    Task<ShareLink> AddAsync(ShareLink shareLink, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing share link as updated.
    /// </summary>
    void Update(ShareLink shareLink);

    /// <summary>
    /// Soft-deletes a share link.
    /// </summary>
    void Delete(ShareLink shareLink);

    /// <summary>
    /// Gets a share link bundle by its unique identifier.
    /// </summary>
    Task<ShareLinkBundle?> GetBundleByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets a share link bundle by its URL slug, including all links.
    /// </summary>
    Task<ShareLinkBundle?> GetBundleBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Adds a new share link bundle.
    /// </summary>
    Task<ShareLinkBundle> AddBundleAsync(ShareLinkBundle bundle, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a share link bundle.
    /// </summary>
    void DeleteBundle(ShareLinkBundle bundle);
}
