namespace Paperless.Infrastructure.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that require a real database.
/// Provides a shared <see cref="DbFixture"/> instance per test class
/// via xUnit's <see cref="IClassFixture{T}"/> pattern.
///
/// <para>
/// All tests inheriting from this base are automatically tagged with
/// <c>[Trait("Category", "Integration")]</c> for CI filtering.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public class MyDatabaseTests : IntegrationTestBase
/// {
///     public MyDatabaseTests(DbFixture fixture) : base(fixture) { }
///
///     [Fact]
///     public async Task Should_Query_Database()
///     {
///         await using var context = Fixture.CreateContext();
///         // ... test logic
///     }
/// }
/// </code>
/// </example>
[Trait("Category", TestCategories.Integration)]
public abstract class IntegrationTestBase : IClassFixture<DbFixture>
{
    /// <summary>
    /// Gets the <see cref="DbFixture"/> instance shared by all tests in this class.
    /// </summary>
    protected DbFixture Fixture { get; }

    /// <summary>
    /// Initializes a new instance with the shared fixture.
    /// </summary>
    /// <param name="fixture">The database fixture injected by xUnit.</param>
    protected IntegrationTestBase(DbFixture fixture)
    {
        Fixture = fixture;
    }
}
