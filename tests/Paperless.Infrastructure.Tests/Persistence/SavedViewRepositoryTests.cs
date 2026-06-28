using FluentAssertions;
using Paperless.Core.Documents.Entities;
using Paperless.Infrastructure.Persistence.Repositories;

namespace Paperless.Infrastructure.Tests.Persistence;

public class SavedViewRepositoryTests : RepositoryTestsBase
{
    [Fact]
    public async Task AddAndGetSavedView()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new SavedViewRepository(context);

        var view = new SavedView
        {
            Name = "My Saved View",
            SortField = "created",
            SortReverse = true,
            ShowInDashboard = true,
            ShowInSidebar = false
        };

        // Act
        await repo.AddAsync(view);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(view.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("My Saved View");
        retrieved.ShowInDashboard.Should().BeTrue();
        retrieved.ShowInSidebar.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboardViewsAsync_Returns_Dashboard_Views()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new SavedViewRepository(context);

        context.SavedViews.AddRange(
            new SavedView { Name = "Dashboard View 1", ShowInDashboard = true, ShowInSidebar = false },
            new SavedView { Name = "Sidebar View", ShowInDashboard = false, ShowInSidebar = true },
            new SavedView { Name = "Dashboard View 2", ShowInDashboard = true, ShowInSidebar = true }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetDashboardViewsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(v => v.Name == "Dashboard View 1");
        result.Should().Contain(v => v.Name == "Dashboard View 2");
    }

    [Fact]
    public async Task GetSidebarViewsAsync_Returns_Sidebar_Views()
    {
        // Arrange
        await using var context = CreateContext();
        var repo = new SavedViewRepository(context);

        context.SavedViews.AddRange(
            new SavedView { Name = "Dashboard Only", ShowInDashboard = true, ShowInSidebar = false },
            new SavedView { Name = "Sidebar Only", ShowInDashboard = false, ShowInSidebar = true },
            new SavedView { Name = "Both", ShowInDashboard = true, ShowInSidebar = true }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetSidebarViewsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(v => v.Name == "Sidebar Only");
        result.Should().Contain(v => v.Name == "Both");
    }
}
