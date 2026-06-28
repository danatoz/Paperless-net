using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for CustomField entity operations.
/// </summary>
public class CustomFieldRepository : RepositoryBase<CustomField>, ICustomFieldRepository
{
    public CustomFieldRepository(AppDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork)
    {
    }
}
