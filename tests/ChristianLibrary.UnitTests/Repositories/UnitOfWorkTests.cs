using ChristianLibrary.Data.Context;
using ChristianLibrary.Data.Repositories;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChristianLibrary.UnitTests.Repositories
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitOfWork _unitOfWork;
        private bool _disposed = false;

        public UnitOfWorkTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
        }

        [Fact]
        public void UnitOfWork_ShouldProvideAccessToRepositories()
        {
            // Assert
            _unitOfWork.Books.Should().NotBeNull();
            _unitOfWork.UserProfiles.Should().NotBeNull();
            _unitOfWork.BorrowRequests.Should().NotBeNull();
            _unitOfWork.Loans.Should().NotBeNull();
            _unitOfWork.Messages.Should().NotBeNull();
            _unitOfWork.Notifications.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldPersistAllChanges()
        {
            // Arrange
            var book1 = new Book
            {
                Title = "Book 1",
                Author = "Author 1",
                OwnerId = "user1",
                Condition = BookCondition.Good
            };

            var book2 = new Book
            {
                Title = "Book 2",
                Author = "Author 2",
                OwnerId = "user2",
                Condition = BookCondition.VeryGood
            };

            // Act
            await _unitOfWork.Books.AddAsync(book1);
            await _unitOfWork.Books.AddAsync(book2);
            var result = await _unitOfWork.SaveChangesAsync();

            // Assert
            result.Should().Be(2); // 2 entities saved
            var books = await _unitOfWork.Books.GetAllAsync();
            books.Should().HaveCount(2);
        }

        [Fact]
        public async Task MultipleRepositories_ShouldWorkTogether()
        {
            // Arrange
            var book = new Book
            {
                Title = "Multi Repo Test",
                Author = "Test Author",
                OwnerId = "user1",
                Condition = BookCondition.Good
            };

            var notification = new Notification
            {
                UserId = "user1",
                Type = NotificationType.System,
                Title = "Test Notification",
                Message = "Test message"
            };

            // Act
            await _unitOfWork.Books.AddAsync(book);
            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            // Assert
            var books = await _unitOfWork.Books.GetAllAsync();
            var notifications = await _unitOfWork.Notifications.GetAllAsync();

            books.Should().HaveCount(1);
            notifications.Should().HaveCount(1);
        }

        [Fact]
        public void Repositories_ShouldUseLazyInitialization()
        {
            // Arrange & Act
            var books1 = _unitOfWork.Books;
            var books2 = _unitOfWork.Books;

            // Assert - Same instance should be returned (lazy initialization)
            books1.Should().BeSameAs(books2);
        }

        // Note: Transaction tests are skipped because InMemory database doesn't support real transactions
        // These would need to be tested with integration tests using a real database

        [Fact(Skip = "InMemory database doesn't support transactions. Use integration tests with real DB for this.")]
        public async Task Transaction_ShouldCommitSuccessfully()
        {
            // This test is skipped for unit tests
            // It should be tested in integration tests with a real database
            await Task.CompletedTask;
        }

        [Fact(Skip = "InMemory database doesn't support transactions. Use integration tests with real DB for this.")]
        public async Task Transaction_ShouldRollbackOnError()
        {
            // This test is skipped for unit tests
            // It should be tested in integration tests with a real database
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Don't dispose the UnitOfWork here as it will dispose the context
                // Just dispose the context directly since we created it
                _context?.Database.EnsureDeleted();
                _context?.Dispose();
                _disposed = true;
            }
        }
    }
}