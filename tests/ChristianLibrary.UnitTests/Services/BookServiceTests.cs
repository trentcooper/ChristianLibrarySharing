using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChristianLibrary.UnitTests.Services;

public class BookServiceTests
{
    // -------------------------------------------------------
    // Setup helpers — every test gets a fresh in-memory DB
    // and a real BookService wired to it
    // -------------------------------------------------------

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique per test
            .Options;

        return new ApplicationDbContext(options);
    }

    private static BookService CreateService(ApplicationDbContext context)
    {
        var logger = new Mock<ILogger<BookService>>().Object;

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        return new BookService(context, userManagerMock.Object, logger);
    }
    
    // -------------------------------------------------------
    // Seed helper — adds sample books to the in-memory DB
    // -------------------------------------------------------

    private static async Task SeedBooksAsync(ApplicationDbContext context)
    {
        context.Books.AddRange(
            new Book
            {
                Title = "Mere Christianity",
                Author = "C.S. Lewis",
                Isbn = "9780060652920",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "The Screwtape Letters",
                Author = "C.S. Lewis",
                Isbn = "9780060652927",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                IsAvailable = false,
                IsDeleted = false
            },
            new Book
            {
                Title = "Radical",
                Author = "David Platt",
                Isbn = "9781601422217",
                Genre = BookGenre.ChristianLiving,
                OwnerId = "user-2",
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "Deleted Book",
                Author = "Some Author",
                Isbn = "9780000000000",
                Genre = BookGenre.ChristianLiving,
                OwnerId = "user-1",
                IsAvailable = true,
                IsDeleted = true  // should never appear in results
            }
        );

        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------
    // SearchBooksAsync Tests
    // -------------------------------------------------------

    [Fact]
    public async Task SearchBooksAsync_ByTitle_ReturnsMatchingBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("mere");

        // Assert
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Mere Christianity");
    }

    [Fact]
    public async Task SearchBooksAsync_ByAuthor_ReturnsAllMatchingBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchBooksAsync_ByISBN_ReturnsMatchingBook()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("9780060652920");

        // Assert
        results.Should().HaveCount(1);
        results.First().Author.Should().Be("C.S. Lewis");
    }

    [Fact]
    public async Task SearchBooksAsync_ExcludesSoftDeletedBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("deleted");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchBooksAsync_WithGenreFilter_ReturnsOnlyMatchingGenre()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis", genre: "Theology");

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(b => b.Genre == BookGenre.Theology);
    }

    [Fact]
    public async Task SearchBooksAsync_WithAvailableOnly_ExcludesUnavailableBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis", availableOnly: true);

        // Assert
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Mere Christianity");
    }

    [Fact]
    public async Task SearchBooksAsync_WithInvalidGenre_ReturnsResultsIgnoringGenreFilter()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis", genre: "NotARealGenre");

        // Assert
        // Invalid genre is ignored - all lewis books still returned
        results.Should().HaveCount(2);
    }

    // -------------------------------------------------------
    // GetMyBooksAsync Tests
    // -------------------------------------------------------

    [Fact]
    public async Task GetMyBooksAsync_ReturnsOnlyBooksForOwner()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetMyBooksAsync("user-1");

        // Assert
        results.Should().HaveCount(2); // Deleted book excluded
        results.Should().OnlyContain(b => b.OwnerId == "user-1");
    }

    [Fact]
    public async Task GetMyBooksAsync_ExcludesSoftDeletedBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetMyBooksAsync("user-1");

        // Assert
        results.Should().NotContain(b => b.IsDeleted);
    }

    [Fact]
    public async Task GetMyBooksAsync_ReturnsEmptyList_WhenUserHasNoBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetMyBooksAsync("user-with-no-books");

        // Assert
        results.Should().BeEmpty();
    }
}