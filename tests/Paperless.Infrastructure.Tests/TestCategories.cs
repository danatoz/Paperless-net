namespace Paperless.Infrastructure.Tests;

/// <summary>
/// Constants for test categorization traits used in the CI pipeline.
/// Apply with <c>[Trait("Category", TestCategories.Integration)]</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Integration tests that require a real database or external services.
    /// Filter with: <c>dotnet test --filter "Category=Integration"</c>
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// Unit tests that run without external dependencies.
    /// Filter with: <c>dotnet test --filter "Category=Unit"</c>
    /// </summary>
    public const string Unit = "Unit";
}
