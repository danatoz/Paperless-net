using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Paperless.Core.Common.Interfaces;
using Paperless.Core.Documents.Entities;
using Paperless.Core.Documents.Events;
using Paperless.Infrastructure.Persistence;
using Paperless.Infrastructure.Persistence.Repositories;

namespace Paperless.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for <see cref="UnitOfWork"/> verifying transaction semantics
/// and domain event dispatch after SaveChanges.
/// </summary>
public class UnitOfWorkTests : RepositoryTestsBase
{
    private static ILogger<UnitOfWork> CreateLogger() => NullLogger<UnitOfWork>.Instance;

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Data()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());

        var document = new Document { Title = "Test Document", Checksum = "uow-test-1" };
        context.Documents.Add(document);

        // Act
        var rowsAffected = await uow.SaveChangesAsync();

        // Assert
        rowsAffected.Should().Be(1);
        document.Id.Should().BeGreaterThan(0);

        // Verify data persists in a fresh context
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Documents.FindAsync(document.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Document");
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Return_Rows_Affected()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());

        context.Documents.Add(new Document { Title = "Doc1", Checksum = "uow-rows-1" });
        context.Documents.Add(new Document { Title = "Doc2", Checksum = "uow-rows-2" });
        context.Documents.Add(new Document { Title = "Doc3", Checksum = "uow-rows-3" });

        // Act
        var rowsAffected = await uow.SaveChangesAsync();

        // Assert
        rowsAffected.Should().Be(3);
    }

    [Fact]
    public async Task CommitAsync_Should_Persist_Data_After_Transaction()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());

        var document = new Document { Title = "Transactional Doc", Checksum = "uow-commit-1" };
        context.Documents.Add(document);

        // Act
        await uow.BeginTransactionAsync();
        await uow.SaveChangesAsync();
        await uow.CommitAsync();

        // Assert - data should be persisted and visible from a new context
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Documents.FindAsync(document.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Transactional Doc");
    }

    [Fact]
    public async Task CommitAsync_Should_Persist_Multiple_Entities()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());

        var doc1 = new Document { Title = "Commit Doc 1", Checksum = "uow-multi-1" };
        var doc2 = new Document { Title = "Commit Doc 2", Checksum = "uow-multi-2" };
        context.Documents.AddRange(doc1, doc2);

        // Act
        await uow.BeginTransactionAsync();
        await uow.SaveChangesAsync();
        await uow.CommitAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var allDocs = await verifyContext.Documents.ToListAsync();
        allDocs.Should().HaveCount(2);
    }

    [Fact]
    public async Task RollbackAsync_Should_Not_Persist_Data()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());

        var document = new Document { Title = "Rollback Doc", Checksum = "uow-rollback-1" };
        context.Documents.Add(document);

        // Act
        await uow.BeginTransactionAsync();
        await uow.SaveChangesAsync();
        await uow.RollbackAsync();

        // Assert - data should NOT be persisted
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Documents.FindAsync(document.Id);
        saved.Should().BeNull();
    }

    [Fact]
    public async Task RollbackAsync_Should_Not_Persist_After_Update()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());

        // First create a document outside the transaction
        var document = new Document { Title = "Original Title", Checksum = "uow-rollback-upd" };
        context.Documents.Add(document);
        await uow.SaveChangesAsync();

        var documentId = document.Id;
        var originalTitle = document.Title;

        // Detach the document so it's not tracked locally during verification
        context.Entry(document).State = EntityState.Detached;

        // Act - start a transaction, make update, save, then rollback
        await uow.BeginTransactionAsync();

        // Reload the document within the transaction and update it
        var docToUpdate = await context.Documents.FindAsync(documentId);
        docToUpdate!.Title = "Updated Within Transaction";
        await uow.SaveChangesAsync();

        // Rollback the transaction (undoes the SaveChangesAsync)
        await uow.RollbackAsync();

        // Assert - verify with a COMPLETELY fresh context (no local cache)
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Documents.FindAsync(documentId);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be(originalTitle);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Dispatch_Domain_Events()
    {
        // Arrange
        await using var context = CreateContext();
        var publisher = Substitute.For<IPublisher>();
        var uow = new UnitOfWork(context, publisher, CreateLogger());

        var document = new Document { Title = "Event Doc", Checksum = "uow-event-1" };
        var domainEvent = new DocumentCreatedEvent(0, DateTime.UtcNow);
        document.AddDomainEvent(domainEvent);
        context.Documents.Add(document);

        // Act
        await uow.SaveChangesAsync();

        // Assert - domain event should have been dispatched via MediatR
        await publisher.Received(1).Publish(
            Arg.Is<DocumentCreatedEvent>(e => e == domainEvent),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Dispatch_Multiple_Domain_Events()
    {
        // Arrange
        await using var context = CreateContext();
        var publisher = Substitute.For<IPublisher>();
        var uow = new UnitOfWork(context, publisher, CreateLogger());

        var document = new Document { Title = "Multi Event Doc", Checksum = "uow-event-2" };
        document.AddDomainEvent(new DocumentCreatedEvent(0, DateTime.UtcNow));
        document.AddDomainEvent(new DocumentUpdatedEvent(0, new HashSet<string> { "title" }, DateTime.UtcNow));
        context.Documents.Add(document);

        // Act
        await uow.SaveChangesAsync();

        // Assert - both events should have been dispatched
        await publisher.Received(1).Publish(
            Arg.Any<DocumentCreatedEvent>(),
            Arg.Any<CancellationToken>());
        await publisher.Received(1).Publish(
            Arg.Any<DocumentUpdatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Clear_Domain_Events_After_Dispatch()
    {
        // Arrange
        await using var context = CreateContext();
        var publisher = Substitute.For<IPublisher>();
        var uow = new UnitOfWork(context, publisher, CreateLogger());

        var document = new Document { Title = "Clear Event Doc", Checksum = "uow-clear-1" };
        document.AddDomainEvent(new DocumentCreatedEvent(0, DateTime.UtcNow));
        context.Documents.Add(document);

        // Act
        await uow.SaveChangesAsync();

        // Assert - events should be cleared from the entity
        document.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_Without_Events_Should_Not_Call_Publisher()
    {
        // Arrange
        await using var context = CreateContext();
        var publisher = Substitute.For<IPublisher>();
        var uow = new UnitOfWork(context, publisher, CreateLogger());

        var document = new Document { Title = "No Event Doc", Checksum = "uow-noevent-1" };
        context.Documents.Add(document);

        // Act
        await uow.SaveChangesAsync();

        // Assert - publisher should not have been called
        await publisher.DidNotReceiveWithAnyArgs().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Multiple_Saves_With_Events_Should_Dispatch_Each_Time()
    {
        // Arrange
        await using var context = CreateContext();
        var publisher = Substitute.For<IPublisher>();
        var uow = new UnitOfWork(context, publisher, CreateLogger());

        // First save with event
        var doc1 = new Document { Title = "Doc1", Checksum = "uow-multisave-1" };
        doc1.AddDomainEvent(new DocumentCreatedEvent(0, DateTime.UtcNow));
        context.Documents.Add(doc1);
        await uow.SaveChangesAsync();

        // Second save with event
        var doc2 = new Document { Title = "Doc2", Checksum = "uow-multisave-2" };
        doc2.AddDomainEvent(new DocumentCreatedEvent(0, DateTime.UtcNow));
        context.Documents.Add(doc2);
        await uow.SaveChangesAsync();

        // Assert - publisher called twice (once per save)
        await publisher.Received(2).Publish(
            Arg.Any<DocumentCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Through_Uow_Should_Be_Visible_After_Commit()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = new UnitOfWork(context, Substitute.For<IPublisher>(), CreateLogger());
        var repo = new DocumentRepository(context, uow);

        var document = new Document { Title = "To Delete via UoW", Checksum = "uow-delete-1" };
        await repo.AddAsync(document);
        await uow.SaveChangesAsync();

        // Act
        await uow.BeginTransactionAsync();
        repo.Delete(document);
        await uow.SaveChangesAsync();
        await uow.CommitAsync();

        // Assert - document should be soft-deleted
        await using var verifyContext = CreateContext();
        var deleted = await verifyContext.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == document.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }
}
