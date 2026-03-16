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

public class RecentBooksTests
{
    // -------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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
    // Seed helper
    // -------------------------------------------------------

    private static async Task SeedRecentBooksAsync(ApplicationDbContext context)
    {
        var userPortland = new ApplicationUser
        {
            Id = "user-portland",
            UserName = "portland@test.com",
            Email = "portland@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "Portland",
                LastName = "User",
                UserId = "user-portland",
                Latitude = 45.5051m,
                Longitude = -122.6750m
            }
        };

        var userSeattle = new ApplicationUser
        {
            Id = "user-seattle",
            UserName = "seattle@test.com",
            Email = "seattle@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "Seattle",
                LastName = "User",
                UserId = "user-seattle",
                Latitude = 47.6062m,
                Longitude = -122.3321m
            }
        };

        context.Users.AddRange(userPortland, userSeattle);

        context.Books.AddRange(
            new Book
            {
                Title = "Recent Book 1",
                Author = "Author A",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "user-portland",
                Owner = userPortland,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5) // 5 days ago
            },
            new Book
            {
                Title = "Recent Book 2",
                Author = "Author B",
                Genre = BookGenre.ChristianLiving,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "user-portland",
                Owner = userPortland,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15) // 15 days ago
            },
            new Book
            {
                Title = "Old Book",
                Author = "Author C",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "user-portland",
                Owner = userPortland,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60) // 60 days ago - outside 30 day window
            },
            new Book
            {
                Title = "Deleted Recent Book",
                Author = "Author D",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "user-portland",
                Owner = userPortland,
                IsAvailable = true,
                IsDeleted = true,
                IsVisible = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3) // recent but deleted
            },
            new Book
            {
                Title = "Seattle Recent Book",
                Author = "Author E",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "user-seattle",
                Owner = userSeattle,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-7) // recent but far away
            }
        );

        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------
    // GetRecentBooksAsync Tests
    // -------------------------------------------------------

    [Fact]
    public async Task GetRecentBooksAsync_ReturnsOnlyBooksWithinDateWindow()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act - default 30 day window
        var results = await service.GetRecentBooksAsync(daysSince: 30);

        // Assert - Old Book (60 days) and deleted book excluded
        results.Should().NotContain(r => r.Book.Title == "Old Book");
        results.Should().NotContain(r => r.Book.Title == "Deleted Recent Book");
    }

    [Fact]
    public async Task GetRecentBooksAsync_ExcludesSoftDeletedBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetRecentBooksAsync();

        // Assert
        results.Should().NotContain(r => r.Book.IsDeleted);
    }

    [Fact]
    public async Task GetRecentBooksAsync_ReturnsNewestBooksFirst()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetRecentBooksAsync();

        // Assert
        results.Should().BeInDescendingOrder(r => r.Book.CreatedAt);
    }

    [Fact]
    public async Task GetRecentBooksAsync_RespectsLimit()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@test.com",
            Email = "user@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "Test",
                LastName = "User",
                UserId = "user-1"
            }
        };

        context.Users.Add(user);

        // Add 10 recent books
        for (int i = 1; i <= 10; i++)
        {
            context.Books.Add(new Book
            {
                Title = $"Book {i}",
                Author = "Author",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "user-1",
                Owner = user,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act - limit to 5
        var results = await service.GetRecentBooksAsync(limit: 5);

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentBooksAsync_WithShorterWindow_ReturnsFewerBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act - 7 day window should exclude 15-day-old book
        var results = await service.GetRecentBooksAsync(daysSince: 7);

        // Assert
        results.Should().NotContain(r => r.Book.Title == "Recent Book 2");
    }

    [Fact]
    public async Task GetRecentBooksAsync_WithLocation_FiltersBooksByRadius()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act - 50 mile radius from Portland excludes Seattle (~146 miles)
        var results = await service.GetRecentBooksAsync(
            latitude: 45.5051,
            longitude: -122.6750,
            radiusMiles: 50);

        // Assert
        results.Should().NotContain(r => r.Book.Title == "Seattle Recent Book");
    }

    [Fact]
    public async Task GetRecentBooksAsync_WithLocation_AttachesDistance()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetRecentBooksAsync(
            latitude: 45.5051,
            longitude: -122.6750,
            radiusMiles: 50);

        // Assert - Portland books should have near-zero distance
        results.Should().OnlyContain(r => r.DistanceMiles.HasValue);
    }

    [Fact]
    public async Task GetRecentBooksAsync_WithoutLocation_ReturnsNullDistance()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedRecentBooksAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.GetRecentBooksAsync();

        // Assert
        results.Should().OnlyContain(r => !r.DistanceMiles.HasValue);
    }

    [Fact]
    public async Task GetRecentBooksAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var results = await service.GetRecentBooksAsync();

        // Assert
        results.Should().BeEmpty();
    }
}