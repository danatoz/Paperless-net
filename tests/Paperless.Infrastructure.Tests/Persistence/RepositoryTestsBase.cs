using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Paperless.Infrastructure.Persistence;

namespace Paperless.Infrastructure.Tests.Persistence;

/// <summary>
/// Base class for repository integration tests.
/// Creates an in-memory SQLite database for isolated test runs.
/// </summary>
public abstract class RepositoryTestsBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IOptions<DatabaseOptions> _dbOptions;

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

        // Create the schema
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    protected AppDbContext CreateContext()
    {
        var context = new AppDbContext(_options, _dbOptions);
        return context;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
