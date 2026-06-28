using Paperless.Core.Common.Interfaces;
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
}
