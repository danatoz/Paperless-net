using FluentAssertions;
using Paperless.Core.Tests.Helpers;

namespace Paperless.Core.Tests.Helpers;

/// <summary>
/// Verifies that the TestData builders produce valid entities.
/// </summary>
public class TestDataBuilderTests
{
    [Fact]
    public void CreateDocument_ShouldSetDefaultProperties()
    {
        var doc = TestData.CreateDocument();

        doc.Id.Should().Be(1);
        doc.Title.Should().Be("Test Document");
        doc.Content.Should().Be("Sample OCR text content");
        doc.Checksum.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateCorrespondent_ShouldSetDefaultProperties()
    {
        var correspondent = TestData.CreateCorrespondent();

        correspondent.Name.Should().Be("ACME Corp");
        correspondent.Match.Should().Be("ACME Corp");
        correspondent.MatchingAlgorithm.Should().Be(Documents.Enums.MatchingAlgorithm.Auto);
    }

    [Fact]
    public void CreateTag_ShouldSetDefaultProperties()
    {
        var tag = TestData.CreateTag();

        tag.Name.Should().Be("Important");
        tag.Color.Should().NotBeNullOrEmpty();
        tag.IsInboxTag.Should().BeFalse();
    }

    [Fact]
    public void CreateDocumentWithCorrespondent_ShouldLinkEntities()
    {
        var doc = TestData.CreateDocumentWithCorrespondent("Test Corp");

        doc.Correspondent.Should().NotBeNull();
        doc.Correspondent!.Name.Should().Be("Test Corp");
        doc.CorrespondentId.Should().Be(doc.Correspondent.Id);
    }

    [Fact]
    public void CreateDocumentWithTags_ShouldAssignTags()
    {
        var doc = TestData.CreateDocumentWithTags("Urgent", "Finance");

        doc.Tags.Should().HaveCount(2);
        doc.Tags.Select(t => t.Name).Should().Contain(["Urgent", "Finance"]);
    }

    [Fact]
    public void CreateConsumableDocument_ShouldSetDefaultProperties()
    {
        var cd = TestData.CreateConsumableDocument();

        cd.Title.Should().Be("uploaded_document.pdf");
        cd.OriginalFilePath.Should().Be("/tmp/test_document.pdf");
    }

    [Fact]
    public void CreateConsumerContext_ShouldSetDefaultProperties()
    {
        var ctx = TestData.CreateConsumerContext();

        ctx.Document.Should().NotBeNull();
        ctx.DocumentEntity.Should().BeNull();
        ctx.WorkingDirectory.Should().Be("/tmp/paperless");
        ctx.StageStatuses.Should().BeEmpty();
    }
}
