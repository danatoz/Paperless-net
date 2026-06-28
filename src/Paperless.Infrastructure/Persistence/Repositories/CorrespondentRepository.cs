using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Correspondent entity operations.
/// </summary>
public class CorrespondentRepository : RepositoryBase<Correspondent>, ICorrespondentRepository
{
    public CorrespondentRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }

    /// <inheritdoc />
    public async Task<PagedResult<Correspondent>> GetAllAsync(ISpecification<Correspondent> spec, CancellationToken ct = default)
        => await ApplySpecificationAsync(spec, ct);
}
