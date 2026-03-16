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
        var user1 = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user1@test.com",
            Email = "user1@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "User",
                LastName = "One",
                UserId = "user-1"
            }
        };

        var user2 = new ApplicationUser
        {
            Id = "user-2",
            UserName = "user2@test.com",
            Email = "user2@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "User",
                LastName = "Two",
                UserId = "user-2"
            }
        };

        context.Users.AddRange(user1, user2);

        context.Books.AddRange(
            new Book
            {
                Title = "Mere Christianity",
                Author = "C.S. Lewis",
                Isbn = "9780060652920",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                OwnerId = "user-1",
                Owner = user1,
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "The Screwtape Letters",
                Author = "C.S. Lewis",
                Isbn = "9780060652927",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                OwnerId = "user-1",
                Owner = user1,
                IsAvailable = false,
                IsDeleted = false
            },
            new Book
            {
                Title = "Radical",
                Author = "David Platt",
                Isbn = "9781601422217",
                Genre = BookGenre.ChristianLiving,
                Condition = BookCondition.Good,
                OwnerId = "user-2",
                Owner = user2,
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "Deleted Book",
                Author = "Some Author",
                Isbn = "9780000000000",
                Genre = BookGenre.ChristianLiving,
                Condition = BookCondition.Poor,
                OwnerId = "user-1",
                Owner = user1,
                IsAvailable = true,
                IsDeleted = true
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

    // -------------------------------------------------------
// SearchBooksAsync Advanced Filter Tests (US-05.03)
// -------------------------------------------------------

    [Fact]
    public async Task SearchBooksAsync_ByCondition_ReturnsOnlyMatchingCondition()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);

        context.Books.Add(new Book
        {
            Title = "Well Worn Book",
            Author = "John Piper", // unique author not in seed data
            Genre = BookGenre.Theology,
            Condition = BookCondition.Acceptable,
            OwnerId = "user-1",
            Owner = context.Users.First(u => u.Id == "user-1"),
            IsAvailable = true,
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act - search by unique author + condition filter
        var results = await service.SearchBooksAsync("piper", condition: "Acceptable");

        // Assert
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Well Worn Book");
    }

    [Fact]
    public async Task SearchBooksAsync_WithInvalidCondition_ReturnsResultsIgnoringConditionFilter()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis", condition: "NotARealCondition");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchBooksAsync_ByChurchAffiliation_ReturnsOnlyMatchingBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var userWithChurch = new ApplicationUser
        {
            Id = "user-church",
            UserName = "church@test.com",
            Email = "church@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "Church",
                LastName = "User",
                UserId = "user-church",
                ChurchName = "Grace Community Church"
            }
        };

        context.Users.Add(userWithChurch);

        context.Books.AddRange(
            new Book
            {
                Title = "Mere Christianity",
                Author = "C.S. Lewis",
                Genre = BookGenre.Theology,
                OwnerId = "user-church",
                Owner = userWithChurch,
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "Radical",
                Author = "David Platt",
                Genre = BookGenre.ChristianLiving,
                OwnerId = "user-church",
                Owner = userWithChurch,
                IsAvailable = true,
                IsDeleted = false
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("", churchAffiliation: "Grace");

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(b => b.OwnerId == "user-church");
    }

    [Fact]
    public async Task SearchBooksAsync_MultipleFilters_AppliedSimultaneously()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var userWithChurch = new ApplicationUser
        {
            Id = "user-church",
            UserName = "church@test.com",
            Email = "church@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "Church",
                LastName = "User",
                UserId = "user-church",
                ChurchName = "Grace Community Church"
            }
        };

        context.Users.Add(userWithChurch);

        context.Books.AddRange(
            new Book
            {
                Title = "Mere Christianity",
                Author = "C.S. Lewis",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                OwnerId = "user-church",
                Owner = userWithChurch,
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "The Screwtape Letters",
                Author = "C.S. Lewis",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Acceptable,
                OwnerId = "user-church",
                Owner = userWithChurch,
                IsAvailable = true,
                IsDeleted = false
            },
            new Book
            {
                Title = "Radical",
                Author = "David Platt",
                Genre = BookGenre.ChristianLiving,
                Condition = BookCondition.Good,
                OwnerId = "user-church",
                Owner = userWithChurch,
                IsAvailable = true,
                IsDeleted = false
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act - search lewis + Theology genre + Good condition simultaneously
        var results = await service.SearchBooksAsync(
            "lewis",
            genre: "Theology",
            condition: "Good");

        // Assert - only Mere Christianity matches all three filters
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Mere Christianity");
    }

// -------------------------------------------------------
// Sorting Tests (US-05.04)
// -------------------------------------------------------

    [Fact]
    public async Task SearchBooksAsync_SortByTitle_ReturnsAlphabeticalOrder()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis", sortBy: "title");

        // Assert
        results.Should().BeInAscendingOrder(b => b.Title);
    }

    [Fact]
    public async Task SearchBooksAsync_SortByTitleDescending_ReturnsReverseAlphabeticalOrder()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("lewis", sortBy: "title", sortDirection: "desc");

        // Assert
        results.Should().BeInDescendingOrder(b => b.Title);
    }

    [Fact]
    public async Task SearchBooksAsync_SortByAuthor_ReturnsAlphabeticalOrder()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act — search all non-deleted books
        var results = await service.SearchBooksAsync("a", sortBy: "author");

        // Assert
        results.Should().BeInAscendingOrder(b => b.Author);
    }

    [Fact]
    public async Task SearchBooksAsync_SortByDateAdded_ReturnsChronologicalOrder()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var user1 = context.Users.FirstOrDefault(u => u.Id == "user-1")
                    ?? new ApplicationUser
                    {
                        Id = "user-1",
                        UserName = "user1@test.com",
                        Email = "user1@test.com",
                        IsActive = true,
                        Profile = new UserProfile
                        {
                            FirstName = "User",
                            LastName = "One",
                            UserId = "user-1"
                        }
                    };

        if (!context.Users.Any(u => u.Id == "user-1"))
            context.Users.Add(user1);

        // Add books with known CreatedAt dates
        context.Books.AddRange(
            new Book
            {
                Title = "Oldest Book",
                Author = "Author A",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                Owner = user1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Book
            {
                Title = "Middle Book",
                Author = "Author A",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                Owner = user1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Book
            {
                Title = "Newest Book",
                Author = "Author A",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                Owner = user1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("author a", sortBy: "dateadded", sortDirection: "asc");

        // Assert
        results.Should().BeInAscendingOrder(b => b.CreatedAt);
        results.First().Title.Should().Be("Oldest Book");
        results.Last().Title.Should().Be("Newest Book");
    }

    [Fact]
    public async Task SearchBooksAsync_SortByDateAddedDescending_ReturnsNewestFirst()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var user1 = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user1@test.com",
            Email = "user1@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "User",
                LastName = "One",
                UserId = "user-1"
            }
        };

        context.Users.Add(user1);
        context.Books.AddRange(
            new Book
            {
                Title = "Oldest Book",
                Author = "Author A",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                Owner = user1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Book
            {
                Title = "Newest Book",
                Author = "Author A",
                Genre = BookGenre.Theology,
                OwnerId = "user-1",
                Owner = user1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksAsync("author a", sortBy: "dateadded", sortDirection: "desc");

        // Assert
        results.First().Title.Should().Be("Newest Book");
        results.Last().Title.Should().Be("Oldest Book");
    }

    [Fact]
    public async Task SearchBooksAsync_SortByRelevance_ExactTitleMatchRanksFirst()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act — "Mere Christianity" is an exact match, "The Screwtape Letters" is not
        var results = await service.SearchBooksAsync(
            "Mere Christianity", sortBy: "relevance");

        // Assert — exact match should be first
        results.First().Title.Should().Be("Mere Christianity");
    }

    [Fact]
    public async Task SearchBooksAsync_UnrecognizedSortBy_DefaultsToRelevance()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksAsync(context);
        var service = CreateService(context);

        // Act — invalid sort value should not crash
        var act = async () => await service.SearchBooksAsync("lewis", sortBy: "notavalidsort");

        // Assert — returns results without throwing
        await act.Should().NotThrowAsync();
    }
}