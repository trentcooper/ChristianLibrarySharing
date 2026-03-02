using ChristianLibrary.Data.Context;
using ChristianLibrary.Data.Repositories;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChristianLibrary.UnitTests.Repositories
{
    public class RepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Repository<Book> _repository;

        public RepositoryTests()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new Repository<Book>(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntity()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                OwnerId = "user123",
                Condition = BookCondition.Good
            };

            // Act
            var result = await _repository.AddAsync(book);
            await _context.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            var savedBook = await _context.Books.FindAsync(result.Id);
            savedBook.Should().NotBeNull();
            savedBook!.Title.Should().Be("Test Book");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                OwnerId = "user123",
                Condition = BookCondition.Good
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(book.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(book.Id);
            result.Title.Should().Be("Test Book");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            // Arrange
            _context.Books.AddRange(
                new Book { Title = "Book 1", Author = "Author 1", OwnerId = "user1", Condition = BookCondition.Good },
                new Book { Title = "Book 2", Author = "Author 2", OwnerId = "user2", Condition = BookCondition.VeryGood },
                new Book { Title = "Book 3", Author = "Author 3", OwnerId = "user3", Condition = BookCondition.LikeNew }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task FindAsync_ShouldReturnMatchingEntities()
        {
            // Arrange
            _context.Books.AddRange(
                new Book { Title = "C# Book", Author = "Author 1", OwnerId = "user1", Condition = BookCondition.Good },
                new Book { Title = "Java Book", Author = "Author 2", OwnerId = "user2", Condition = BookCondition.Good },
                new Book { Title = "Python Book", Author = "Author 3", OwnerId = "user3", Condition = BookCondition.VeryGood }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FindAsync(b => b.Title.Contains("Book"));

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_ShouldReturnFirstMatch()
        {
            // Arrange
            _context.Books.AddRange(
                new Book { Title = "Book A", Author = "Author 1", OwnerId = "user1", Condition = BookCondition.Good },
                new Book { Title = "Book B", Author = "Author 1", OwnerId = "user1", Condition = BookCondition.VeryGood }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FirstOrDefaultAsync(b => b.Author == "Author 1");

            // Assert
            result.Should().NotBeNull();
            result!.Author.Should().Be("Author 1");
        }

        [Fact]
        public async Task AnyAsync_ShouldReturnTrue_WhenMatchExists()
        {
            // Arrange
            _context.Books.Add(new Book 
            { 
                Title = "Test Book", 
                Author = "Test Author", 
                OwnerId = "user1",
                Condition = BookCondition.Good 
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.AnyAsync(b => b.Author == "Test Author");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            _context.Books.AddRange(
                new Book { Title = "Book 1", Author = "Author 1", OwnerId = "user1", Condition = BookCondition.Good },
                new Book { Title = "Book 2", Author = "Author 2", OwnerId = "user2", Condition = BookCondition.Good },
                new Book { Title = "Book 3", Author = "Author 3", OwnerId = "user3", Condition = BookCondition.VeryGood }
            );
            await _context.SaveChangesAsync();

            // Act
            var totalCount = await _repository.CountAsync();
            var filteredCount = await _repository.CountAsync(b => b.Condition == BookCondition.Good);

            // Assert
            totalCount.Should().Be(3);
            filteredCount.Should().Be(2);
        }

        [Fact]
        public async Task Update_ShouldModifyEntity()
        {
            // Arrange
            var book = new Book
            {
                Title = "Original Title",
                Author = "Test Author",
                OwnerId = "user1",
                Condition = BookCondition.Good
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            book.Title = "Updated Title";
            _repository.Update(book);
            await _context.SaveChangesAsync();

            // Assert
            var updatedBook = await _context.Books.FindAsync(book.Id);
            updatedBook!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task Remove_ShouldDeleteEntity()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                OwnerId = "user1",
                Condition = BookCondition.Good
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            var bookId = book.Id;

            // Act
            _repository.Remove(book);
            await _context.SaveChangesAsync();

            // Assert
            var deletedBook = await _context.Books.FindAsync(bookId);
            deletedBook.Should().BeNull();
        }

        [Fact]
        public async Task GetPagedAsync_ShouldReturnCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                _context.Books.Add(new Book
                {
                    Title = $"Book {i}",
                    Author = $"Author {i}",
                    OwnerId = "user1",
                    Condition = BookCondition.Good
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var page1 = await _repository.GetPagedAsync(1, 3);
            var page2 = await _repository.GetPagedAsync(2, 3);

            // Assert
            page1.Should().HaveCount(3);
            page2.Should().HaveCount(3);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
