using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for StoragePath entity operations.
/// </summary>
public class StoragePathRepository : RepositoryBase<StoragePath>, IStoragePathRepository
{
    public StoragePathRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }
}
