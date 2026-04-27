using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services;
using ChristianLibrary.Services.DTOs.BorrowRequests;
using ChristianLibrary.Services.DTOs.Common;
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
    // Seed helpers — SeedAsync composes on SeedUsersAndBookAsync
    // -------------------------------------------------------

    private static async Task<(ApplicationUser lender, ApplicationUser borrower, Book book)>
        SeedUsersAndBookAsync(ApplicationDbContext context)
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

        return (lender, borrower, book);
    }
    
    /// <summary>
    /// Adds borrow requests against an existing book, one per supplied status.
    /// Use after SeedUsersAndBookAsync to compose a multi-request scenario.
    /// </summary>
    private static async Task SeedRequestsAsync(
        ApplicationDbContext context,
        int bookId,
        params BorrowRequestStatus[] statuses)
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < statuses.Length; i++)
        {
            context.BorrowRequests.Add(new BorrowRequest
            {
                BookId = bookId,
                BorrowerId = "borrower-1",
                LenderId = "lender-1",
                Status = statuses[i],
                RequestedStartDate = now.AddDays(1),
                RequestedEndDate = now.AddDays(30),
                ExpiresAt = now.AddDays(7),
                // Stagger CreatedAt so OrderByDescending(CreatedAt) is deterministic
                CreatedAt = now.AddSeconds(i)
            });
        }
        await context.SaveChangesAsync();
    }
    
    
    private static async Task<(ApplicationUser lender, ApplicationUser borrower, Book book)>
        SeedAsync(ApplicationDbContext context, BorrowRequestStatus status = BorrowRequestStatus.Pending)
    {
        var (lender, borrower, book) = await SeedUsersAndBookAsync(context);

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
    
    // -------------------------------------------------------
    // DeclineRequestAsync Tests (US-06.05)
    // -------------------------------------------------------

    [Fact]
    public async Task DeclineRequestAsync_Success_UpdatesStatusToDeclined()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.DeclineRequestAsync(
            request.Id, "lender-1", "Sorry, already lent out.");

        // Assert
        result.Success.Should().BeTrue();

        var updated = await context.BorrowRequests.FindAsync(request.Id);
        updated!.Status.Should().Be(BorrowRequestStatus.Declined);
        updated.ResponseMessage.Should().Be("Sorry, already lent out.");
        updated.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeclineRequestAsync_DoesNotCreateLoan()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        await service.DeclineRequestAsync(request.Id, "lender-1");

        // Assert
        var loans = await context.Loans.ToListAsync();
        loans.Should().BeEmpty();
    }

    [Fact]
    public async Task DeclineRequestAsync_DoesNotMarkBookUnavailable()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        await service.DeclineRequestAsync(request.Id, "lender-1");

        // Assert — book remains available; decline does not affect inventory
        var book = await context.Books.FindAsync(request.BookId);
        book!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task DeclineRequestAsync_WrongLender_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.DeclineRequestAsync(request.Id, "wrong-user");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");

        // Status should be unchanged
        var unchanged = await context.BorrowRequests.FindAsync(request.Id);
        unchanged!.Status.Should().Be(BorrowRequestStatus.Pending);
    }

    [Fact]
    public async Task DeclineRequestAsync_RequestNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.DeclineRequestAsync(999, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeclineRequestAsync_AlreadyDeclined_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Declined);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.DeclineRequestAsync(request.Id, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
    }

    // -------------------------------------------------------
    // CancelRequestAsync Tests (US-06.12)
    // -------------------------------------------------------

    [Fact]
    public async Task CancelRequestAsync_Success_UpdatesStatusToCancelled()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.CancelRequestAsync(request.Id, "borrower-1");

        // Assert
        result.Success.Should().BeTrue();

        var updated = await context.BorrowRequests.FindAsync(request.Id);
        updated!.Status.Should().Be(BorrowRequestStatus.Cancelled);
    }

    [Fact]
    public async Task CancelRequestAsync_WrongBorrower_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act — the lender trying to cancel is still the wrong user here
        var result = await service.CancelRequestAsync(request.Id, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");

        var unchanged = await context.BorrowRequests.FindAsync(request.Id);
        unchanged!.Status.Should().Be(BorrowRequestStatus.Pending);
    }

    [Fact]
    public async Task CancelRequestAsync_RequestNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.CancelRequestAsync(999, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CancelRequestAsync_AlreadyApproved_ReturnsFailure()
    {
        // Arrange — borrower cannot cancel after lender has approved
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Approved);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.CancelRequestAsync(request.Id, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CancelRequestAsync_AlreadyCancelled_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, BorrowRequestStatus.Cancelled);
        var service = CreateService(context);
        var request = context.BorrowRequests.First();

        // Act
        var result = await service.CancelRequestAsync(request.Id, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
    }
    
    // -------------------------------------------------------
    // CreateBorrowRequestAsync Tests (US-06.02)
    // -------------------------------------------------------

    [Fact]
    public async Task CreateBorrowRequestAsync_HappyPath_CreatesPendingRequest()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "I'd love to read this!"
        };
        var before = DateTime.UtcNow;

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeTrue();
        result.BorrowRequestId.Should().NotBeNull();

        var saved = await context.BorrowRequests.FindAsync(result.BorrowRequestId);
        saved.Should().NotBeNull();
        saved!.BookId.Should().Be(book.Id);
        saved.BorrowerId.Should().Be("borrower-1");
        saved.LenderId.Should().Be("lender-1");
        saved.Status.Should().Be(BorrowRequestStatus.Pending);
        saved.Message.Should().Be("I'd love to read this!");
        saved.ExpiresAt.Should().BeOnOrAfter(before.AddDays(7));
        saved.ExpiresAt.Should().BeOnOrBefore(DateTime.UtcNow.AddDays(7));
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_StartDateInPast_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(-1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("future");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_StartDateEqualsNow_ReturnsFailure()
    {
        // Arrange — boundary case: start date equal to the current moment
        // By the time the service checks, DateTime.UtcNow will have advanced past it
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow,
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("future");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_EndDateEqualsStartDate_ReturnsFailure()
    {
        // Arrange — boundary case: end date equal to start date is not a valid range
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var start = DateTime.UtcNow.AddDays(1);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = start,
            RequestedEndDate = start,
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("after");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_EndDateBeforeStartDate_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(10),
            RequestedEndDate = DateTime.UtcNow.AddDays(5),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("after");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_BookNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = 99999,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_BookSoftDeleted_ReturnsNotFound()
    {
        // Arrange — a soft-deleted book should be treated as if it doesn't exist
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        book.IsDeleted = true;
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_BookNotAvailable_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        book.IsAvailable = false;
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not currently available");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_BorrowerIsOwner_ReturnsFailure()
    {
        // Arrange — owner attempts to borrow their own book
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Test"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("your own book");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_ExistingPendingRequest_ReturnsFailure()
    {
        // Arrange — borrower already has a pending request for this book
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedAsync(context, BorrowRequestStatus.Pending);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Second attempt"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("pending request");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_PreviouslyDeclined_AllowsNewRequest()
    {
        // Arrange — only Pending requests should block new requests;
        // a previously declined request must not prevent a retry
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedAsync(context, BorrowRequestStatus.Declined);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "Trying again after decline"
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeTrue();

        var count = await context.BorrowRequests
            .CountAsync(r => r.BorrowerId == "borrower-1" && r.BookId == book.Id);
        count.Should().Be(2); // original declined + new pending
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_MessageWithWhitespace_IsTrimmed()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = "   I'd love to read this!   "
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeTrue();
        var saved = await context.BorrowRequests.FindAsync(result.BorrowRequestId);
        saved!.Message.Should().Be("I'd love to read this!");
    }

    [Fact]
    public async Task CreateBorrowRequestAsync_NullMessage_Succeeds()
    {
        // Arrange — message is optional; null should persist as null, not cause failure
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        var service = CreateService(context);
        var request = new CreateBorrowRequest
        {
            BookId = book.Id,
            RequestedStartDate = DateTime.UtcNow.AddDays(1),
            RequestedEndDate = DateTime.UtcNow.AddDays(14),
            Message = null
        };

        // Act
        var result = await service.CreateBorrowRequestAsync(request, "borrower-1");

        // Assert
        result.Success.Should().BeTrue();
        var saved = await context.BorrowRequests.FindAsync(result.BorrowRequestId);
        saved!.Message.Should().BeNull();
    }
    
    // -------------------------------------------------------
    // Summary Name Population — Guards Against Missing Includes
    // -------------------------------------------------------

    [Fact]
    public async Task GetIncomingRequestsAsync_PopulatesBorrowerAndLenderNames()
    {
        // Arrange — confirms both Borrower.Profile and Lender.Profile are included
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync("lender-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().HaveCount(1);
        var summary = result.Items.First();
        summary.BorrowerName.Should().Be("Borrower User");
        summary.LenderName.Should().Be("Lender User");
        summary.BorrowerName.Should().NotBe("Unknown");
        summary.LenderName.Should().NotBe("Unknown");
    }

    [Fact]
    public async Task GetOutgoingRequestsAsync_PopulatesBorrowerAndLenderNames()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetOutgoingRequestsAsync("borrower-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().HaveCount(1);
        var summary = result.Items.First();
        summary.BorrowerName.Should().Be("Borrower User");
        summary.LenderName.Should().Be("Lender User");
        summary.BorrowerName.Should().NotBe("Unknown");
        summary.LenderName.Should().NotBe("Unknown");
    }
    
    // -------------------------------------------------------
    // GetIncomingRequestsAsync — Filtering & Pagination (US-06.14)
    // -------------------------------------------------------

    [Fact]
    public async Task GetIncomingRequestsAsync_NoFilter_ReturnsAllStatuses()
    {
        // Arrange — one request per status; null filter should return all
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Approved,
            BorrowRequestStatus.Declined,
            BorrowRequestStatus.Cancelled);
        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync("lender-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetIncomingRequestsAsync_FilterByPending_ReturnsOnlyPending()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Approved,
            BorrowRequestStatus.Declined);
        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync(
            "lender-1",
            new BorrowRequestQuery { Status = BorrowRequestStatus.Pending });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(BorrowRequestStatus.Pending);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetIncomingRequestsAsync_FilterByDeclined_ExcludesNonMatching()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Approved);
        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync(
            "lender-1",
            new BorrowRequestQuery { Status = BorrowRequestStatus.Declined });

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetIncomingRequestsAsync_Pagination_FirstPageReturnsCorrectSlice()
    {
        // Arrange — 3 requests, pageSize 2, page 1 should return 2 items
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Pending);
        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync(
            "lender-1",
            new BorrowRequestQuery { Page = 1, PageSize = 2 });

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetIncomingRequestsAsync_Pagination_SecondPageReturnsRemainder()
    {
        // Arrange — 3 requests, pageSize 2, page 2 should return 1 item
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Pending);
        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync(
            "lender-1",
            new BorrowRequestQuery { Page = 2, PageSize = 2 });

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetIncomingRequestsAsync_SoftDeleted_IsExcluded()
    {
        // Arrange — soft-deleted requests should not appear or count
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Pending);

        // Soft-delete one of them
        var first = context.BorrowRequests.First();
        first.IsDeleted = true;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetIncomingRequestsAsync("lender-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetIncomingRequestsAsync_DoesNotReturnRequestsWhereCallerIsBorrower()
    {
        // Arrange — guards against a filter-direction bug
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id, BorrowRequestStatus.Pending);
        var service = CreateService(context);

        // Act — query as the borrower; should see no incoming
        var result = await service.GetIncomingRequestsAsync("borrower-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // -------------------------------------------------------
    // GetOutgoingRequestsAsync — Filtering & Pagination Parity (US-06.14)
    // -------------------------------------------------------

    [Fact]
    public async Task GetOutgoingRequestsAsync_NoFilter_ReturnsAllStatuses()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Approved,
            BorrowRequestStatus.Cancelled);
        var service = CreateService(context);

        // Act
        var result = await service.GetOutgoingRequestsAsync("borrower-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetOutgoingRequestsAsync_FilterByCancelled_ReturnsOnlyCancelled()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id,
            BorrowRequestStatus.Pending,
            BorrowRequestStatus.Cancelled);
        var service = CreateService(context);

        // Act
        var result = await service.GetOutgoingRequestsAsync(
            "borrower-1",
            new BorrowRequestQuery { Status = BorrowRequestStatus.Cancelled });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(BorrowRequestStatus.Cancelled);
    }

    [Fact]
    public async Task GetOutgoingRequestsAsync_DoesNotReturnRequestsWhereCallerIsLender()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book) = await SeedUsersAndBookAsync(context);
        await SeedRequestsAsync(context, book.Id, BorrowRequestStatus.Pending);
        var service = CreateService(context);

        // Act
        var result = await service.GetOutgoingRequestsAsync("lender-1", new BorrowRequestQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
    
    [Fact]
    public async Task PagedResult_WithInvalidPageSize_TotalPagesReturnsZero()
    {
        // Arrange — verifies PagedResult.TotalPages defends against PageSize=0
        // (would have produced int.MinValue under the old division-by-zero path)
        var result = new PagedResult<BorrowRequestSummary>
        {
            Items = new List<BorrowRequestSummary>(),
            TotalCount = 5,
            Page = 1,
            PageSize = 0
        };

        // Act + Assert
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task PagedResult_WithZeroTotalCount_TotalPagesReturnsZero()
    {
        // Arrange — empty result set should report 0 total pages
        var result = new PagedResult<BorrowRequestSummary>
        {
            Items = new List<BorrowRequestSummary>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        // Act + Assert
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }
}