using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Paperless.Core.Common.Interfaces;
using Paperless.Infrastructure.Persistence;
using Paperless.Infrastructure.Persistence.Interceptors;

namespace Paperless.Infrastructure.Tests.Persistence;

/// <summary>
/// Base class for repository integration tests.
/// Creates an in-memory SQLite database for isolated test runs.
/// </summary>
public abstract class RepositoryTestsBase : IDisposable
{
    /// <summary>
    /// Creates a mock <see cref="IUnitOfWork"/> for use in tests.
    /// </summary>
    protected static IUnitOfWork CreateUnitOfWorkMock() => Substitute.For<IUnitOfWork>();
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;
    private readonly AuditInterceptor _auditInterceptor;

    protected RepositoryTestsBase()
    {
        // Use an in-memory SQLite database for testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbOptions = Options.Create(new DatabaseOptions
        {
            Provider = "SQLite",
            ConnectionString = "DataSource=:memory:"
        });

        // Create interceptors for testing.
        // SoftDeleteInterceptor requires no dependencies.
        // AuditInterceptor gracefully handles missing IHttpContextAccessor.
        _softDeleteInterceptor = new SoftDeleteInterceptor();
        _auditInterceptor = new AuditInterceptor(new HttpContextAccessor());

        // Create the schema
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    protected AppDbContext CreateContext()
    {
        var context = new AppDbContext(
            _options,
            _dbOptions,
            _softDeleteInterceptor,
            _auditInterceptor);
        return context;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
