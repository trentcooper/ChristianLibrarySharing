using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services;
using ChristianLibrary.Services.Configuration;
using ChristianLibrary.Services.DTOs.Loans;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    private static LoanService CreateService(
        ApplicationDbContext context,
        int maxExtensionDays = 21)
    {
        var logger = new Mock<ILogger<LoanService>>().Object;
        var loanSettings = Options.Create(new LoanSettings { MaxExtensionDays = maxExtensionDays });
        return new LoanService(context, logger, loanSettings);
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
    
    // -------------------------------------------------------
    // GetMyBorrowsAsync — Additional Coverage
    // -------------------------------------------------------

    [Fact]
    public async Task GetMyBorrowsAsync_SoftDeletedLoan_IsExcluded()
    {
        // Arrange — a loan marked IsDeleted should never surface
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        loan.IsDeleted = true;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyBorrowsAsync_OrdersByStartDateDescending()
    {
        // Arrange — three loans with staggered start dates
        await using var context = CreateInMemoryContext();
        var (lender, _, _, _, _) = await SeedAsync(context); // creates loan with StartDate = now - 14

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
        var book3 = new Book
        {
            Title = "Knowing God",
            Author = "J.I. Packer",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            OwnerId = "lender-1",
            Owner = lender,
            IsAvailable = false,
            IsDeleted = false
        };
        context.Books.AddRange(book2, book3);
        await context.SaveChangesAsync();

        context.Loans.AddRange(
            new Loan
            {
                BookId = book2.Id,
                BorrowerId = "borrower-1",
                LenderId = "lender-1",
                Status = LoanStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-1), // most recent
                DueDate = DateTime.UtcNow.AddDays(29),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Loan
            {
                BookId = book3.Id,
                BorrowerId = "borrower-1",
                LenderId = "lender-1",
                Status = LoanStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-7), // middle
                DueDate = DateTime.UtcNow.AddDays(23),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery { PageSize = 10 });

        // Assert — newest first
        result.Items.Should().HaveCount(3);
        result.Items.Should().BeInDescendingOrder(l => l.StartDate);
    }

    [Fact]
    public async Task GetMyBorrowsAsync_DoesNotReturnLoansWhereCallerIsLender()
    {
        // Arrange — a borrow query from the lender's perspective should return nothing,
        // even though the lender has loans they own
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("lender-1", new LoanQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyBorrowsAsync_LoanSummary_PopulatesBookAndLenderFields()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, book, _, loan) = await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyBorrowsAsync("borrower-1", new LoanQuery());

        // Assert — the mapping surfaces everything the borrower UI needs
        result.Items.Should().HaveCount(1);
        var summary = result.Items.First();
        summary.BookId.Should().Be(book.Id);
        summary.BookTitle.Should().Be("Mere Christianity");
        summary.BookAuthor.Should().Be("C.S. Lewis");
        summary.LenderId.Should().Be("lender-1");
        summary.LenderName.Should().Be("Lender User");
        summary.Status.Should().Be(LoanStatus.Active);
        summary.ConditionAtCheckout.Should().Be(BookCondition.Good);
        // IsOverdue is a computed property — seeded with DueDate 16 days out, so false
        summary.IsOverdue.Should().BeFalse();
        summary.DaysUntilDue.Should().BeGreaterThan(0);
    }

    // -------------------------------------------------------
    // GetMyLoansAsync — Additional Coverage
    // -------------------------------------------------------

    [Fact]
    public async Task GetMyLoansAsync_DoesNotReturnLoansWhereCallerIsBorrower()
    {
        // Arrange — a loans query from the borrower's perspective should return nothing
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMyLoansAsync("borrower-1", new LoanQuery());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyLoansAsync_OrdersByStartDateDescending()
    {
        // Arrange — three loans with staggered start dates
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
        var book3 = new Book
        {
            Title = "Knowing God",
            Author = "J.I. Packer",
            Genre = BookGenre.Theology,
            Condition = BookCondition.Good,
            OwnerId = "lender-1",
            Owner = lender,
            IsAvailable = false,
            IsDeleted = false
        };
        context.Books.AddRange(book2, book3);
        await context.SaveChangesAsync();

        context.Loans.AddRange(
            new Loan
            {
                BookId = book2.Id,
                BorrowerId = "borrower-1",
                LenderId = "lender-1",
                Status = LoanStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(29),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Loan
            {
                BookId = book3.Id,
                BorrowerId = "borrower-1",
                LenderId = "lender-1",
                Status = LoanStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-7),
                DueDate = DateTime.UtcNow.AddDays(23),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetMyLoansAsync("lender-1", new LoanQuery { PageSize = 10 });

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Should().BeInDescendingOrder(l => l.StartDate);
    }
    
    // -------------------------------------------------------
    // RequestExtensionAsync Tests (US-06.11)
    // -------------------------------------------------------

    [Fact]
    public async Task RequestExtensionAsync_Success_TransitionsLoanToExtensionRequested()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var newDueDate = loan.DueDate.AddDays(7);
        var request = new RequestExtensionRequest
        {
            RequestedDueDate = newDueDate,
            Message = "Need a bit more time, thanks!"
        };

        // Act
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", request);

        // Assert
        result.Success.Should().BeTrue();

        var updated = await context.Loans.FindAsync(loan.Id);
        updated!.Status.Should().Be(LoanStatus.ExtensionRequested);
        updated.ExtensionRequested.Should().BeTrue();
        updated.RequestedExtensionDate.Should().Be(newDueDate);
        updated.ExtensionRequestMessage.Should().Be("Need a bit more time, thanks!");
        // DueDate not changed yet — only on approval
        updated.DueDate.Should().Be(loan.DueDate);
    }

    [Fact]
    public async Task RequestExtensionAsync_OverdueLoan_CanRequestExtension()
    {
        // Arrange — borrower can request extension even when loan has gone overdue
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.Overdue);
        var service = CreateService(context);
        var request = new RequestExtensionRequest
        {
            RequestedDueDate = loan.DueDate.AddDays(7)
        };

        // Act
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", request);

        // Assert
        result.Success.Should().BeTrue();
        var updated = await context.Loans.FindAsync(loan.Id);
        updated!.Status.Should().Be(LoanStatus.ExtensionRequested);
    }

    [Fact]
    public async Task RequestExtensionAsync_LoanNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);
        var request = new RequestExtensionRequest { RequestedDueDate = DateTime.UtcNow.AddDays(7) };

        // Act
        var result = await service.RequestExtensionAsync(999, "borrower-1", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task RequestExtensionAsync_WrongBorrower_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new RequestExtensionRequest { RequestedDueDate = loan.DueDate.AddDays(7) };

        // Act — lender attempts to request an extension on the borrower's behalf
        var result = await service.RequestExtensionAsync(loan.Id, "lender-1", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task RequestExtensionAsync_LoanReturned_ReturnsFailure()
    {
        // Arrange — terminal status: Returned loans cannot be extended
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.Returned);
        var service = CreateService(context);
        var request = new RequestExtensionRequest { RequestedDueDate = loan.DueDate.AddDays(7) };

        // Act
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", request);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RequestExtensionAsync_RequestedDateNotAfterCurrentDueDate_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var request = new RequestExtensionRequest { RequestedDueDate = loan.DueDate };

        // Act
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("after the current due date");
    }

    [Fact]
    public async Task RequestExtensionAsync_ExceedsMaxExtensionDays_ReturnsFailure()
    {
        // Arrange — settings cap at 7 days, request 14
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context, maxExtensionDays: 7);
        var request = new RequestExtensionRequest { RequestedDueDate = loan.DueDate.AddDays(14) };

        // Act
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("7 days");
    }

    [Fact]
    public async Task RequestExtensionAsync_PendingExtensionAlreadyExists_ReturnsFailure()
    {
        // Arrange — first request transitions to ExtensionRequested, second cannot proceed
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.ExtensionRequested);
        var service = CreateService(context);
        var request = new RequestExtensionRequest { RequestedDueDate = loan.DueDate.AddDays(7) };

        // Act
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already pending");
    }

    [Fact]
    public async Task RequestExtensionAsync_AfterPriorApproval_AllowsAnotherExtensionRequest()
    {
        // Arrange — confirms multiple extensions over the loan's lifetime are allowed
        // (this is one of the design decisions captured in US-06.11 acceptance criteria)
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);
        var firstRequest = new RequestExtensionRequest { RequestedDueDate = loan.DueDate.AddDays(7) };

        // Act 1 — request, then approve
        await service.RequestExtensionAsync(loan.Id, "borrower-1", firstRequest);
        await service.ApproveExtensionAsync(loan.Id, "lender-1");

        // Act 2 — request a second extension
        var refreshed = await context.Loans.FindAsync(loan.Id);
        var secondRequest = new RequestExtensionRequest { RequestedDueDate = refreshed!.DueDate.AddDays(7) };
        var result = await service.RequestExtensionAsync(loan.Id, "borrower-1", secondRequest);

        // Assert
        result.Success.Should().BeTrue();
        var final = await context.Loans.FindAsync(loan.Id);
        final!.Status.Should().Be(LoanStatus.ExtensionRequested);
        final.ExtensionDays.Should().Be(7); // accumulated from the first approval; second not yet approved
    }

    // -------------------------------------------------------
    // ApproveExtensionAsync Tests (US-06.11)
    // -------------------------------------------------------

    [Fact]
    public async Task ApproveExtensionAsync_Success_UpdatesDueDateAndAccumulatesDays()
    {
        // Arrange — set up a loan with a pending extension
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var originalDueDate = loan.DueDate;
        var newDueDate = loan.DueDate.AddDays(10);
        loan.Status = LoanStatus.ExtensionRequested;
        loan.ExtensionRequested = true;
        loan.RequestedExtensionDate = newDueDate;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ApproveExtensionAsync(loan.Id, "lender-1");

        // Assert
        result.Success.Should().BeTrue();
        var updated = await context.Loans.FindAsync(loan.Id);
        updated!.DueDate.Should().Be(newDueDate);
        updated.Status.Should().Be(LoanStatus.Active);
        updated.ExtensionRequested.Should().BeFalse();
        updated.ExtensionDays.Should().Be(10);
        // Audit breadcrumb retained
        updated.RequestedExtensionDate.Should().Be(newDueDate);
    }

    [Fact]
    public async Task ApproveExtensionAsync_LoanNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.ApproveExtensionAsync(999, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ApproveExtensionAsync_WrongLender_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.ExtensionRequested);
        loan.RequestedExtensionDate = loan.DueDate.AddDays(7);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        // Act
        var result = await service.ApproveExtensionAsync(loan.Id, "wrong-user");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task ApproveExtensionAsync_NoPendingExtension_ReturnsFailure()
    {
        // Arrange — loan is Active, never had an extension requested
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.ApproveExtensionAsync(loan.Id, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no pending extension request");
    }

    // -------------------------------------------------------
    // DeclineExtensionAsync Tests (US-06.11)
    // -------------------------------------------------------

    [Fact]
    public async Task DeclineExtensionAsync_Success_RestoresActiveStatusWhenNotOverdue()
    {
        // Arrange — pending extension on a loan whose current DueDate is in the future
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        loan.Status = LoanStatus.ExtensionRequested;
        loan.ExtensionRequested = true;
        loan.RequestedExtensionDate = loan.DueDate.AddDays(7);
        loan.ExtensionRequestMessage = "Please?";
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var originalDueDate = loan.DueDate;

        // Act
        var result = await service.DeclineExtensionAsync(loan.Id, "lender-1");

        // Assert
        result.Success.Should().BeTrue();
        var updated = await context.Loans.FindAsync(loan.Id);
        updated!.Status.Should().Be(LoanStatus.Active);
        updated.ExtensionRequested.Should().BeFalse();
        updated.DueDate.Should().Be(originalDueDate);
        // Audit breadcrumb retained
        updated.RequestedExtensionDate.Should().NotBeNull();
        updated.ExtensionRequestMessage.Should().Be("Please?");
    }

    [Fact]
    public async Task DeclineExtensionAsync_OverdueLoan_RestoresOverdueStatus()
    {
        // Arrange — DueDate already in the past at decline time
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        loan.DueDate = DateTime.UtcNow.AddDays(-1);
        loan.Status = LoanStatus.ExtensionRequested;
        loan.ExtensionRequested = true;
        loan.RequestedExtensionDate = DateTime.UtcNow.AddDays(7);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeclineExtensionAsync(loan.Id, "lender-1");

        // Assert
        result.Success.Should().BeTrue();
        var updated = await context.Loans.FindAsync(loan.Id);
        updated!.Status.Should().Be(LoanStatus.Overdue);
    }

    [Fact]
    public async Task DeclineExtensionAsync_WrongLender_ReturnsFailureWithPermission()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context, LoanStatus.ExtensionRequested);
        var service = CreateService(context);

        // Act
        var result = await service.DeclineExtensionAsync(loan.Id, "wrong-user");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task DeclineExtensionAsync_NoPendingExtension_ReturnsFailure()
    {
        // Arrange — loan is Active, no extension requested
        await using var context = CreateInMemoryContext();
        var (_, _, _, _, loan) = await SeedAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.DeclineExtensionAsync(loan.Id, "lender-1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no pending extension request");
    }
}