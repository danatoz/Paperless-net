using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Paperless.Infrastructure.Persistence;
using Paperless.Infrastructure.Persistence.Interceptors;

namespace Paperless.Infrastructure.Tests.Infrastructure;

/// <summary>
/// xUnit <see cref="IAsyncLifetime"/> fixture that manages a SQLite in-memory database
/// with the full EF Core schema (<see cref="AppDbContext"/>). Each test class that
/// implements <see cref="IClassFixture{T}"/> receives a fresh database instance.
///
/// <para>
/// The schema is created via <see cref="DatabaseFacade.EnsureCreatedAsync"/> which
/// produces the same table structure as the Npgsql migrations, without requiring
/// a real PostgreSQL instance.
/// </para>
///
/// <para>
/// For tests that need PostgreSQL-specific features (e.g. JSONB, full-text search),
/// use <see cref="PostgreSqlContainer"/> from Testcontainers instead.
/// </para>
/// </summary>
public sealed class DbFixture : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;

    /// <summary>
    /// Gets the <see cref="DbContextOptions{TContext}"/> configured for SQLite in-memory.
    /// </summary>
    public DbContextOptions<AppDbContext> Options => _options;

    /// <summary>
    /// Gets the <see cref="DatabaseOptions"/> matching the SQLite provider.
    /// </summary>
    public IOptions<DatabaseOptions> DbOptions { get; private set; } = null!;

    /// <summary>
    /// Gets the <see cref="SoftDeleteInterceptor"/> instance shared by contexts.
    /// </summary>
    public SoftDeleteInterceptor SoftDeleteInterceptor { get; private set; } = null!;

    /// <summary>
    /// Gets the <see cref="AuditInterceptor"/> instance shared by contexts.
    /// </summary>
    public AuditInterceptor AuditInterceptor { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbOptions = Microsoft.Extensions.Options.Options.Create(new DatabaseOptions
        {
            Provider = "SQLite",
            ConnectionString = "DataSource=:memory:"
        });

        SoftDeleteInterceptor = new SoftDeleteInterceptor();
        AuditInterceptor = new AuditInterceptor(new HttpContextAccessor());

        // Create the full EF Core schema (tables, indexes, relationships)
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> instance connected to the
    /// same in-memory SQLite database.
    /// </summary>
    public AppDbContext CreateContext()
    {
        return new AppDbContext(
            _options,
            DbOptions,
            SoftDeleteInterceptor,
            AuditInterceptor);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
    }
}
