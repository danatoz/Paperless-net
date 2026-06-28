using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for DocumentType entity operations.
/// </summary>
public class DocumentTypeRepository : RepositoryBase<DocumentType>, IDocumentTypeRepository
{
    public DocumentTypeRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }

    /// <inheritdoc />
    public async Task<PagedResult<DocumentType>> GetAllAsync(ISpecification<DocumentType> spec, CancellationToken ct = default)
        => await ApplySpecificationAsync(spec, ct);
}
