using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services;
using ChristianLibrary.Services.DTOs.Loans;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChristianLibrary.UnitTests.Services;

public class LoanServiceTests
{
    // -------------------------------------------------------
    // Setup helpers — every test gets a fresh in-memory DB
    // -------------------------------------------------------

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static LoanService CreateService(ApplicationDbContext context)
    {
        var logger = new Mock<ILogger<LoanService>>().Object;
        return new LoanService(context, logger);
    }

    // -------------------------------------------------------
    // Seed helper — creates a complete loan scenario
    // -------------------------------------------------------

    private static async
        Task<(ApplicationUser lender, ApplicationUser borrower, Book book, BorrowRequest borrowRequest, Loan loan)>
        SeedAsync(ApplicationDbContext context, LoanStatus loanStatus = LoanStatus.Active)
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
            IsAvailable = false,
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
            Status = BorrowRequestStatus.Approved,
            RequestedStartDate = DateTime.UtcNow.AddDays(-14),
            RequestedEndDate = DateTime.UtcNow.AddDays(16),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };

        context.BorrowRequests.Add(borrowRequest);
        await context.SaveChangesAsync();

        var loan = new Loan
        {
            BookId = book.Id,
            BorrowerId = "borrower-1",
            LenderId = "lender-1",
            BorrowRequestId = borrowRequest.Id,
            Status = loanStatus,
            StartDate = DateTime.UtcNow.AddDays(-14),
            DueDate = DateTime.UtcNow.AddDays(16),
            ConditionAtCheckout = BookCondition.Good,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };

        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        return (lender, borrower, book, borrowRequest, loan);
    }

    // -------------------------------------------------------
    // MarkReturnedAsync Tests (US-06.07)
    // -------------------------------------------------------

    [Fact]
    public async Task MarkReturnedAsync_Success_ClosesLoan()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new MarkReturnedRequest
        {
            ConditionAtReturn = BookCondition.Good, BorrowerNotes = "Great book, thank you!"
        };

        // Act
        var result = await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        result.Success.Should().BeTrue();

        var updatedLoan = await context.Loans.FindAsync(loan.Id);
        updatedLoan!.Status.Should().Be(LoanStatus.Returned);
        updatedLoan.ReturnedDate.Should().NotBeNull();
        updatedLoan.ConditionAtReturn.Should().Be(BookCondition.Good);
        updatedLoan.BorrowerNotes.Should().Be("Great book, thank you!");
    }

    [Fact]
    public async Task MarkReturnedAsync_Success_MakesBookAvailable()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        var updatedBook = await context.Books.FindAsync(book.Id);
        updatedBook!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task MarkReturnedAsync_Success_UpdatesBookCondition()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Acceptable };

        // Act
        await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        var updatedBook = await context.Books.FindAsync(book.Id);
        updatedBook!.Condition.Should().Be(BookCondition.Acceptable);
    }

    [Fact]
    public async Task MarkReturnedAsync_Success_MarkesBorrowRequestCompleted()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, borrowRequest, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        var updatedRequest = await context.BorrowRequests.FindAsync(borrowRequest.Id);
        updatedRequest!.Status.Should().Be(BorrowRequestStatus.Completed);
    }

    [Fact]
    public async Task MarkReturnedAsync_OverdueLoan_CanStillBeReturned()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.Overdue);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        var result = await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        result.Success.Should().BeTrue();
        var updatedLoan = await context.Loans.FindAsync(loan.Id);
        updatedLoan!.Status.Should().Be(LoanStatus.Returned);
    }

    [Fact]
    public async Task MarkReturnedAsync_LoanNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        var result = await service.MarkReturnedAsync(999, "lender-1", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task MarkReturnedAsync_WrongLender_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        var result = await service.MarkReturnedAsync(loan.Id, "wrong-user", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task MarkReturnedAsync_AlreadyReturned_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.Returned);
        var service = CreateService(context);
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        var result = await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task MarkReturnedAsync_ReturnedDateIsActualReturnTime()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var before = DateTime.UtcNow;
        var request = new MarkReturnedRequest { ConditionAtReturn = BookCondition.Good };

        // Act
        await service.MarkReturnedAsync(loan.Id, "lender-1", request);

        // Assert
        var updatedLoan = await context.Loans.FindAsync(loan.Id);
        updatedLoan!.ReturnedDate.Should().BeOnOrAfter(before);
        updatedLoan.ReturnedDate.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    // -------------------------------------------------------
// GetMyBorrowsAsync Tests (US-06.08)
// -------------------------------------------------------

    [Fact]
    public async Task GetMyBorrowsAsync_ReturnsOnlyBorrowersLoans()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery());

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().BorrowerId.Should().Be("borrower-1");
    }

    [Fact]
    public async Task GetMyBorrowsAsync_ReturnsEmptyList_WhenNoBorrows()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("unknown-user", new LoanQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyBorrowsAsync_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.Returned);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery { Status = LoanStatus.Active });

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyBorrowsAsync_FilterByStatus_ReturnsMatchingLoans()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, LoanStatus.Returned);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery { Status = LoanStatus.Returned });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(LoanStatus.Returned);
    }

    [Fact]
    public async Task GetMyBorrowsAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (lender, borrower, _, borrowRequest, _) = await SeedAsync(context);

        // Add a second book and loan
        var book2 = new Book
        {
            Title = "The Screwtape Letters",
            Author = "C.S. Lewis",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            OwnerId = "lender-1",
            Owner = lender,
            IsAvailable = false,
            IsDeleted = false
        };
        context.Books.Add(book2);
        await context.SaveChangesAsync();

        context.Loans.Add(new Loan
        {
            BookId = book2.Id,
            BorrowerId = "borrower-1",
            LenderId = "lender-1",
            Status = LoanStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-7),
            DueDate = DateTime.UtcNow.AddDays(23),
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act — page 1 with pageSize 1
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery { Page = 1, PageSize = 1 });

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyBorrowsAsync_Pagination_SecondPageReturnsRemainingItems()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (lender, _, _, _, _) = await SeedAsync(context);

        var book2 = new Book
        {
            Title = "The Screwtape Letters",
            Author = "C.S. Lewis",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            OwnerId = "lender-1",
            Owner = lender,
            IsAvailable = false,
            IsDeleted = false
        };
        context.Books.Add(book2);
        await context.SaveChangesAsync();

        context.Loans.Add(new Loan
        {
            BookId = book2.Id,
            BorrowerId = "borrower-1",
            LenderId = "lender-1",
            Status = LoanStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-7),
            DueDate = DateTime.UtcNow.AddDays(23),
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act — page 2 with pageSize 1
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery { Page = 2, PageSize = 1 });

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

// -------------------------------------------------------
// GetMyLoansAsync Tests (US-06.09)
// -------------------------------------------------------

    [Fact]
    public async Task GetMyLoansAsync_ReturnsOnlyLendersLoans()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyLoansAsync("lender-1", new LoanQuery());

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().LenderId.Should().Be("lender-1");
    }

    [Fact]
    public async Task GetMyLoansAsync_ReturnsEmptyList_WhenNoLoans()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyLoansAsync("unknown-user", new LoanQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyLoansAsync_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context, LoanStatus.Active);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyLoansAsync("lender-1", new LoanQuery { Status = LoanStatus.Returned });

        // Assert
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLoansAsync_NoStatusFilter_ReturnsAllStatuses()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (lender, borrower, _, _, _) = await SeedAsync(context, LoanStatus.Active);

        // Add a returned loan
        var book2 = new Book
        {
            Title = "The Screwtape Letters",
            Author = "C.S. Lewis",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            OwnerId = "lender-1",
            Owner = lender,
            IsAvailable = true,
            IsDeleted = false
        };
        context.Books.Add(book2);
        await context.SaveChangesAsync();

        context.Loans.Add(new Loan
        {
            BookId = book2.Id,
            BorrowerId = "borrower-1",
            LenderId = "lender-1",
            Status = LoanStatus.Returned,
            StartDate = DateTime.UtcNow.AddDays(-30),
            DueDate = DateTime.UtcNow.AddDays(-1),
            ReturnedDate = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act — no status filter
        var result = await service.GetMyLoansAsync("lender-1", new LoanQuery());

        // Assert — both Active and Returned loans returned
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}