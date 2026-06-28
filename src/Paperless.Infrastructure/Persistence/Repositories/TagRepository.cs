using Paperless.Core.Common.Interfaces;
using Paperless.Core.Common.Models;
using Paperless.Core.Common.Specifications;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Tag entity operations.
/// </summary>
public class TagRepository : RepositoryBase<Tag>, ITagRepository
{
    public TagRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }

    /// <inheritdoc />
    public async Task<PagedResult<Tag>> GetAllAsync(ISpecification<Tag> spec, CancellationToken ct = default)
        => await ApplySpecificationAsync(spec, ct);
}
