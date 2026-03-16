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

public class BookDetailTests
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
    // Seed helpers
    // -------------------------------------------------------

    private static async Task<(ApplicationUser owner, Book book)> SeedBookWithOwnerAsync(
        ApplicationDbContext context,
        bool showFullName = true,
        bool showCityState = true)
    {
        var owner = new ApplicationUser
        {
            Id = "owner-1",
            UserName = "owner@test.com",
            Email = "owner@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "John",
                LastName = "Bunyan",
                UserId = "owner-1",
                City = "Portland",
                State = "OR",
                Latitude = 45.5051m,
                Longitude = -122.6750m,
                ShowFullName = showFullName,
                ShowCityState = showCityState
            }
        };

        var book = new Book
        {
            Title = "Mere Christianity",
            Author = "C.S. Lewis",
            Isbn = "9780060652920",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            Description = "A classic apologetics book",
            Language = "English",
            OwnerId = "owner-1",
            Owner = owner,
            IsAvailable = true,
            IsDeleted = false,
            IsVisible = true
        };

        context.Users.Add(owner);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        return (owner, book);
    }

    // -------------------------------------------------------
    // GetBookDetailAsync Tests
    // -------------------------------------------------------

    [Fact]
    public async Task GetBookDetailAsync_ReturnsBookDetail_WhenBookExists()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Mere Christianity");
        result.Author.Should().Be("C.S. Lewis");
        result.Genre.Should().Be("Theology");
        result.Condition.Should().Be("Good");
        result.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetBookDetailAsync_ReturnsNull_WhenBookNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBookDetailAsync_ReturnsNull_WhenBookIsSoftDeleted()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var owner = new ApplicationUser
        {
            Id = "owner-1",
            UserName = "owner@test.com",
            Email = "owner@test.com",
            IsActive = true,
            Profile = new UserProfile
            {
                FirstName = "John",
                LastName = "Bunyan",
                UserId = "owner-1"
            }
        };

        var deletedBook = new Book
        {
            Title = "Deleted Book",
            Author = "Some Author",
            Genre = BookGenre.Other,
            Condition = BookCondition.Good,
            Language = "English",
            OwnerId = "owner-1",
            Owner = owner,
            IsDeleted = true,
            IsVisible = false
        };

        context.Users.Add(owner);
        context.Books.Add(deletedBook);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(deletedBook.Id);

        // Assert
        result.Should().BeNull();
    }

    // -------------------------------------------------------
    // Privacy Settings Tests
    // -------------------------------------------------------

    [Fact]
    public async Task GetBookDetailAsync_ShowsFullName_WhenShowFullNameIsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context, showFullName: true);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.OwnerDisplayName.Should().Be("John Bunyan");
    }

    [Fact]
    public async Task GetBookDetailAsync_HidesFullName_WhenShowFullNameIsFalse()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context, showFullName: false);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.OwnerDisplayName.Should().Be("Community Member");
    }

    [Fact]
    public async Task GetBookDetailAsync_ShowsCityState_WhenShowCityStateIsTrue()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context, showCityState: true);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.OwnerCity.Should().Be("Portland");
        result.OwnerState.Should().Be("OR");
    }

    [Fact]
    public async Task GetBookDetailAsync_HidesCityState_WhenShowCityStateIsFalse()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context, showCityState: false);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.OwnerCity.Should().BeNull();
        result.OwnerState.Should().BeNull();
    }

    // -------------------------------------------------------
    // Distance Tests
    // -------------------------------------------------------

    [Fact]
    public async Task GetBookDetailAsync_ReturnsDistance_WhenCallerLocationProvided()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context);
        var service = CreateService(context);

        // Act — caller is in Salem, OR (~42 miles from Portland)
        var result = await service.GetBookDetailAsync(
            book.Id,
            callerLatitude: 44.9429,
            callerLongitude: -123.0351);

        // Assert
        result!.DistanceMiles.Should().NotBeNull();
        result.DistanceMiles.Should().BeApproximately(42.6, precision: 3.0);
    }

    [Fact]
    public async Task GetBookDetailAsync_ReturnsNullDistance_WhenCallerLocationNotProvided()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.DistanceMiles.Should().BeNull();
    }

    // -------------------------------------------------------
    // Similar Books Tests
    // -------------------------------------------------------

    [Fact]
    public async Task GetBookDetailAsync_ReturnsSimilarBooks_BySameGenre()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (owner, book) = await SeedBookWithOwnerAsync(context);

        // Add another Theology book by different author
        context.Books.Add(new Book
        {
            Title = "The Cost of Discipleship",
            Author = "Dietrich Bonhoeffer",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            Language = "English",
            OwnerId = "owner-1",
            Owner = owner,
            IsAvailable = true,
            IsDeleted = false,
            IsVisible = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.SimilarBooks.Should().HaveCount(1);
        result.SimilarBooks.First().Title.Should().Be("The Cost of Discipleship");
    }

    [Fact]
    public async Task GetBookDetailAsync_ReturnsSimilarBooks_BySameAuthor()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (owner, book) = await SeedBookWithOwnerAsync(context);

        // Add another C.S. Lewis book in different genre
        context.Books.Add(new Book
        {
            Title = "The Screwtape Letters",
            Author = "C.S. Lewis",
            Genre = BookGenre.ChristianLiving,
            Condition = BookCondition.Good,
            Language = "English",
            OwnerId = "owner-1",
            Owner = owner,
            IsAvailable = true,
            IsDeleted = false,
            IsVisible = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.SimilarBooks.Should().HaveCount(1);
        result.SimilarBooks.First().Author.Should().Be("C.S. Lewis");
    }

    [Fact]
    public async Task GetBookDetailAsync_SimilarBooks_ExcludesCurrentBook()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, book) = await SeedBookWithOwnerAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.SimilarBooks.Should().NotContain(b => b.Id == book.Id);
    }

    [Fact]
    public async Task GetBookDetailAsync_SimilarBooks_ExcludesDeletedBooks()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (owner, book) = await SeedBookWithOwnerAsync(context);

        context.Books.Add(new Book
        {
            Title = "Deleted Theology Book",
            Author = "Some Author",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            Language = "English",
            OwnerId = "owner-1",
            Owner = owner,
            IsAvailable = true,
            IsDeleted = true,
            IsVisible = false
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.SimilarBooks.Should().NotContain(b => b.Title == "Deleted Theology Book");
    }

    [Fact]
    public async Task GetBookDetailAsync_SimilarBooks_LimitedToFive()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (owner, book) = await SeedBookWithOwnerAsync(context);

        // Add 7 similar books
        for (int i = 1; i <= 7; i++)
        {
            context.Books.Add(new Book
            {
                Title = $"Similar Book {i}",
                Author = "Various",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                Language = "English",
                OwnerId = "owner-1",
                Owner = owner,
                IsAvailable = true,
                IsDeleted = false,
                IsVisible = true
            });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetBookDetailAsync(book.Id);

        // Assert
        result!.SimilarBooks.Should().HaveCount(5);
    }
}