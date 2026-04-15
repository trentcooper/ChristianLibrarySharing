using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services;
using ChristianLibrary.Services.DTOs.BorrowRequests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChristianLibrary.UnitTests.Services;

public class BorrowRequestServiceTests
{
    // -------------------------------------------------------
    // Setup helpers — every test gets a fresh in-memory DB
    // and a real BorrowRequestService wired to it
    // -------------------------------------------------------

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static BorrowRequestService CreateService(ApplicationDbContext context)
    {
        var logger = new Mock<ILogger<BorrowRequestService>>().Object;
        return new BorrowRequestService(context, logger);
    }

    // -------------------------------------------------------
    // Seed helper — adds users, a book, and a borrow request
    // -------------------------------------------------------

    private static async Task<(ApplicationUser lender, ApplicationUser borrower, Book book)>
        SeedAsync(ApplicationDbContext context, BorrowRequestStatus status = BorrowRequestStatus.Pending)
    {
        var lender = new ApplicationUser
        {
            Id = "lender-1",
            UserName = "lender@test.com",
            Email = "lender@test.com",
            IsActive = true,
            Profile = new UserProfile { FirstName = "Lender", LastName = "User", UserId = "lender-1" }
        };

        var borrower = new ApplicationUser
        {
            Id = "borrower-1",
            UserName = "borrower@test.com",
            Email = "borrower@test.com",
            IsActive = true,
            Profile = new UserProfile { FirstName = "Borrower", LastName = "User", UserId = "borrower-1" }
        };

        var book = new Book
        {
            Title = "Mere Christianity",
            Author = "C.S. Lewis",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            OwnerId = "lender-1",
            Owner = lender,
            IsAvailable = true,
            IsDeleted = false
        };

        context.Users.AddRange(lender, borrower);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var borrowRequest = new BorrowRequest
        {
            BookId = book.Id,
            BorrowerId = "borrower-1",
            LenderId = "lender-1",
            Status = status,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(30),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        context.BorrowRequests.Add(borrowRequest);
        await context.SaveChangesAsync();

        return (lender, borrower, book);
    }

    // -------------------------------------------------------
    // ApproveRequestAsync Tests (refactored — no loan created)
    // -------------------------------------------------------

    [Fact]
    public async Task ApproveRequestAsync_Success_UpdatesStatusOnly()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.ApproveRequestAsync(request.Id, "lender-1", "Enjoy the read!");

        // Assert
        result.Success.Should().BeTrue();

        var updated = await context.BorrowRequests.FindAsync(request.Id);
        updated!.Status.Should().Be(BorrowRequestStatus.Approved);
        updated.ResponseMessage.Should().Be("Enjoy the read!");
    }

    [Fact]
    public async Task ApproveRequestAsync_DoesNotCreateLoan()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        await service.ApproveRequestAsync(request.Id, "lender-1");

        // Assert — no loan should be created at approval time
        var loans = await context.Loans.ToListAsync();
        loans.Should().BeEmpty();
    }

    [Fact]
    public async Task ApproveRequestAsync_DoesNotMarkBookUnavailable()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        await service.ApproveRequestAsync(request.Id, "lender-1");

        // Assert — book stays available until physical pickup
        var book = await context.Books.FindAsync(request.BookId);
        book!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveRequestAsync_WrongLender_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.ApproveRequestAsync(request.Id, "wrong-user");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task ApproveRequestAsync_RequestNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.ApproveRequestAsync(999, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ApproveRequestAsync_AlreadyApproved_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.ApproveRequestAsync(request.Id, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
    }

    // -------------------------------------------------------
    // MarkPickedUpAsync Tests (US-06.06)
    // -------------------------------------------------------

    [Fact]
    public async Task MarkPickedUpAsync_Success_CreatesLoan()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();
        var pickupRequest = new MarkPickedUpRequest
        {
            ConditionAtCheckout = BookCondition.Good,
            LenderNotes = "Book is in great shape!"
        };

        // Act
        var result = await service.MarkPickedUpAsync(request.Id, "lender-1", pickupRequest);

        // Assert
        result.Success.Should().BeTrue();

        var loan = await context.Loans.FirstOrDefaultAsync();
        loan.Should().NotBeNull();
        loan!.BookId.Should().Be(request.BookId);
        loan.BorrowerId.Should().Be("borrower-1");
        loan.LenderId.Should().Be("lender-1");
        loan.BorrowRequestId.Should().Be(request.Id);
        loan.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public async Task MarkPickedUpAsync_Success_MarksBookUnavailable()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();
        var pickupRequest = new MarkPickedUpRequest
        {
            ConditionAtCheckout = BookCondition.Good
        };

        // Act
        await service.MarkPickedUpAsync(request.Id, "lender-1", pickupRequest);

        // Assert
        var book = await context.Books.FindAsync(request.BookId);
        book!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task MarkPickedUpAsync_Success_RecordsConditionAtCheckout()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();
        var pickupRequest = new MarkPickedUpRequest
        {
            ConditionAtCheckout = BookCondition.Acceptable,
            LenderNotes = "Some wear on cover"
        };

        // Act
        await service.MarkPickedUpAsync(request.Id, "lender-1", pickupRequest);

        // Assert
        var loan = await context.Loans.FirstOrDefaultAsync();
        loan!.ConditionAtCheckout.Should().Be(BookCondition.Acceptable);
        loan.LenderNotes.Should().Be("Some wear on cover");
    }

    [Fact]
    public async Task MarkPickedUpAsync_Success_UpdatesBookCondition()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();
        var pickupRequest = new MarkPickedUpRequest
        {
            ConditionAtCheckout = BookCondition.Acceptable
        };

        // Act
        await service.MarkPickedUpAsync(request.Id, "lender-1", pickupRequest);

        // Assert — book condition reflects lender's assessment at checkout
        var book = await context.Books.FindAsync(request.BookId);
        book!.Condition.Should().Be(BookCondition.Acceptable);
    }

    [Fact]
    public async Task MarkPickedUpAsync_Success_StartDateIsActualPickupTime()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();
        var before = DateTime.UtcNow;

        // Act
        await service.MarkPickedUpAsync(request.Id, "lender-1",
            new MarkPickedUpRequest { ConditionAtCheckout = BookCondition.Good });

        // Assert — StartDate is actual pickup time, not requested start date
        var loan = await context.Loans.FirstOrDefaultAsync();
        loan!.StartDate.Should().BeOnOrAfter(before);
        loan.StartDate.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task MarkPickedUpAsync_RequestNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);

        // Act
        var result = await service.MarkPickedUpAsync(999, "lender-1",
            new MarkPickedUpRequest { ConditionAtCheckout = BookCondition.Good });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task MarkPickedUpAsync_WrongLender_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.MarkPickedUpAsync(request.Id, "wrong-user",
            new MarkPickedUpRequest { ConditionAtCheckout = BookCondition.Good });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task MarkPickedUpAsync_RequestNotApproved_ReturnsFailure()
    {
        // Arrange — request is still Pending, not Approved
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Pending);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.MarkPickedUpAsync(request.Id, "lender-1",
            new MarkPickedUpRequest { ConditionAtCheckout = BookCondition.Good });

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MarkPickedUpAsync_RequestNotApproved_DoesNotCreateLoan()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Pending);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        await service.MarkPickedUpAsync(request.Id, "lender-1",
            new MarkPickedUpRequest { ConditionAtCheckout = BookCondition.Good });

        // Assert
        var loans = await context.Loans.ToListAsync();
        loans.Should().BeEmpty();
    }
}