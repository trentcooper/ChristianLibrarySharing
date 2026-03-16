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

public class GeoSearchTests
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
    // Seed helper — books with owners at known locations
    // -------------------------------------------------------

    private static async Task SeedBooksWithLocationsAsync(ApplicationDbContext context)
    {
        // Portland, OR — used as the search origin in tests
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

        // Salem, OR — ~47 miles from Portland
        var userSalem = new ApplicationUser
        {
            Id = "user-salem",
            UserName = "salem@test.com",
            Email = "salem@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "Salem",
                LastName = "User",
                UserId = "user-salem",
                Latitude = 44.9429m,
                Longitude = -123.0351m
            }
        };

        // Seattle, WA — ~174 miles from Portland
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

        // User with no location data
        var userNoLocation = new ApplicationUser
        {
            Id = "user-nolocation",
            UserName = "nolocation@test.com",
            Email = "nolocation@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "NoLocation",
                LastName = "User",
                UserId = "user-nolocation",
                Latitude = null,
                Longitude = null
            }
        };

        context.Users.AddRange(userPortland, userSalem, userSeattle, userNoLocation);

        context.Books.AddRange(
            new Book
            {
                Title = "Mere Christianity",
                Author = "C.S. Lewis",
                Genre = BookGenre.Theology,
                OwnerId = "user-portland",
                Owner = userPortland,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true
            },
            new Book
            {
                Title = "The Screwtape Letters",
                Author = "C.S. Lewis",
                Genre = BookGenre.Theology,
                OwnerId = "user-salem",
                Owner = userSalem,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true
            },
            new Book
            {
                Title = "Radical",
                Author = "David Platt",
                Genre = BookGenre.ChristianLiving,
                OwnerId = "user-seattle",
                Owner = userSeattle,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true
            },
            new Book
            {
                Title = "No Location Book",
                Author = "Some Author",
                Genre = BookGenre.Other,
                OwnerId = "user-nolocation",
                Owner = userNoLocation,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true
            },
            new Book
            {
                Title = "Deleted Book",
                Author = "Some Author",
                Genre = BookGenre.Other,
                OwnerId = "user-portland",
                Owner = userPortland,
                IsAvailable = true,
                IsDeleted = true,
                IsVisible = false
            }
        );

        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------
    // Haversine distance tests (pure math, no DB needed)
    // -------------------------------------------------------

    [Fact]
    public void HaversineDistance_PortlandToSalem_IsApproximately47Miles()
    {
        var distance = HaversineTestHelper.CalculateDistanceMiles(
            45.5051, -122.6750,
            44.9429, -123.0351);

        // Straight-line distance is ~42.6 miles (not driving distance of ~47 miles)
        distance.Should().BeApproximately(42.6, precision: 2.0);
    }

    [Fact]
    public void HaversineDistance_PortlandToSeattle_IsApproximately174Miles()
    {
        var distance = HaversineTestHelper.CalculateDistanceMiles(
            45.5051, -122.6750,
            47.6062, -122.3321);

        // Straight-line distance is ~146 miles (not driving distance of ~174 miles)
        distance.Should().BeApproximately(146.0, precision: 3.0);
    }

    [Fact]
    public void HaversineDistance_SameLocation_IsZero()
    {
        var distance = HaversineTestHelper.CalculateDistanceMiles(
            45.5051, -122.6750,
            45.5051, -122.6750);

        distance.Should().BeApproximately(0.0, precision: 0.001);
    }

    // -------------------------------------------------------
    // SearchBooksNearLocationAsync tests
    // -------------------------------------------------------

    [Fact]
    public async Task SearchBooksNearLocation_WithLargeRadius_ReturnsAllBooksWithLocation()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act - search from Portland with 200 mile radius covers Portland, Salem, Seattle
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200);

        // Assert - 3 books with valid locations (excludes no-location and deleted)
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchBooksNearLocation_WithSmallRadius_ExcludesDistantBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act - 50 mile radius from Portland covers Portland (~0mi) and Salem (~47mi)
        // but NOT Seattle (~174mi)
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 50);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchBooksNearLocation_ResultsOrderedByDistance()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200);

        // Assert - closest book should be first
        results.Should().BeInAscendingOrder(r => r.DistanceMiles);
    }

    [Fact]
    public async Task SearchBooksNearLocation_ExcludesSoftDeletedBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200);

        // Assert
        results.Should().NotContain(r => r.Book.IsDeleted);
    }

    [Fact]
    public async Task SearchBooksNearLocation_ExcludesBooksWithNoOwnerLocation()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200);

        // Assert - "No Location Book" should not appear
        results.Should().NotContain(r => r.Book.Title == "No Location Book");
    }

    [Fact]
    public async Task SearchBooksNearLocation_WithQueryFilter_ReturnsOnlyMatchingBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200, query: "lewis");

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Book.Author == "C.S. Lewis");
    }

    [Fact]
    public async Task SearchBooksNearLocation_WithAvailableOnly_ExcludesUnavailableBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);

        // Mark the Portland book as unavailable
        var portlandBook = context.Books.First(b => b.OwnerId == "user-portland" && !b.IsDeleted);
        portlandBook.IsAvailable = false;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200, availableOnly: true);

        // Assert
        results.Should().NotContain(r => !r.Book.IsAvailable);
    }

    [Fact]
    public async Task SearchBooksNearLocation_ReturnsDistanceOnEachResult()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200);

        // Assert - every result should have a distance value
        results.Should().OnlyContain(r => r.DistanceMiles.HasValue);
        results.Should().OnlyContain(r => r.DistanceMiles >= 0);
    }

    [Fact]
    public async Task SearchBooksNearLocation_EmptyResults_WhenNoBooksInRadius()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act - search from New York, no books seeded there
        var results = await service.SearchBooksNearLocationAsync(
            40.7128, -74.0060, radiusMiles: 25);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchBooksNearLocation_SortByTitle_ReturnsAlphabeticalOrder()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200, sortBy: "title");

        // Assert
        results.Should().BeInAscendingOrder(r => r.Book.Title);
    }

    [Fact]
    public async Task SearchBooksNearLocation_SortByDistanceDescending_ReturnsFarthestFirst()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedBooksWithLocationsAsync(context);
        var service = CreateService(context);

        // Act
        var results = await service.SearchBooksNearLocationAsync(
            45.5051, -122.6750, radiusMiles: 200, sortBy: "distance", sortDirection: "desc");

        // Assert
        results.Should().BeInDescendingOrder(r => r.DistanceMiles);
    }
}