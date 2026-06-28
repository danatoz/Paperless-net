using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Paperless.Infrastructure.Tests.Infrastructure;

namespace Paperless.Infrastructure.Tests.Basic;

/// <summary>
/// Basic integration tests verifying that the database schema
/// (tables, indexes, relationships) is created correctly.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class SchemaCreationTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public SchemaCreationTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that all expected tables exist in the database.
    /// </summary>
    [Fact]
    public async Task All_Expected_Tables_Exist()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var tables = await context.Database
            .SqlQuery<string>($"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'")
            .ToListAsync();

        // Assert
        tables.Should().Contain("Documents");
        tables.Should().Contain("Correspondents");
        tables.Should().Contain("Tags");
        tables.Should().Contain("DocumentTypes");
        tables.Should().Contain("StoragePaths");
        tables.Should().Contain("CustomFields");
        tables.Should().Contain("DocumentCustomFields");
        tables.Should().Contain("DocumentVersions");
        tables.Should().Contain("DocumentTags");
        tables.Should().Contain("Workflows");
        tables.Should().Contain("WorkflowTriggers");
        tables.Should().Contain("WorkflowActions");
        tables.Should().Contain("ShareLinks");
        tables.Should().Contain("ShareLinkBundles");
        tables.Should().Contain("SavedViews");
        tables.Should().Contain("PaperlessTasks");
        tables.Should().Contain("MailAccounts");
        tables.Should().Contain("MailRules");
        tables.Should().Contain("ProcessedMails");
        tables.Should().Contain("ApplicationConfigurations");
    }

    /// <summary>
    /// Verifies that the Documents table has the expected columns.
    /// </summary>
    [Fact]
    public async Task Documents_Table_Has_Expected_Columns()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var columns = await context.Database
            .SqlQuery<string>($"SELECT name FROM pragma_table_info('Documents')")
            .ToListAsync();

        // Assert
        columns.Should().Contain("Id");
        columns.Should().Contain("Title");
        columns.Should().Contain("Content");
        columns.Should().Contain("CorrespondentId");
        columns.Should().Contain("DocumentTypeId");
        columns.Should().Contain("Checksum");
        columns.Should().Contain("ArchiveChecksum");
        columns.Should().Contain("Filename");
        columns.Should().Contain("StoragePath");
        columns.Should().Contain("OwnerId");
        columns.Should().Contain("ArchiveSerialNumber");
        columns.Should().Contain("Created");
        columns.Should().Contain("Added");
        columns.Should().Contain("Modified");
        columns.Should().Contain("CreatedAt");
        columns.Should().Contain("ModifiedAt");
        columns.Should().Contain("IsDeleted");
    }

    /// <summary>
    /// Verifies that foreign key relationships are set up correctly.
    /// </summary>
    [Fact]
    public async Task Documents_Has_ForeignKey_To_Correspondents()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var foreignKeys = await context.Database
            .SqlQuery<ForeignKeyInfo>($"SELECT \"table\", \"from\", \"to\" FROM pragma_foreign_key_list('Documents')")
            .ToListAsync();

        // Assert
        foreignKeys.Should().Contain(fk => fk.Table == "Correspondents");
    }

    /// <summary>
    /// Record used to map the result of pragma_foreign_key_list.
    /// </summary>
    private record ForeignKeyInfo(string Table, string From, string To);
}
