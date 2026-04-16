using ChristianLibrary.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Services.DTOs.Common;
using ChristianLibrary.Services.DTOs.Loans;
using ChristianLibrary.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Service handling loan lifecycle operations
/// </summary>
public class LoanService : ILoanService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoanService> _logger;

    public LoanService(ApplicationDbContext context, ILogger<LoanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // -------------------------------------------------------
    // US-06.07: Mark Book as Returned
    // -------------------------------------------------------

    public async Task<LoanResponse> MarkReturnedAsync(int loanId, string lenderId, MarkReturnedRequest request)
    {
        _logger.LogInformation("MarkReturned - LoanId={LoanId}, LenderId={LenderId}", loanId, lenderId);

        try
        {
            var loan = await _context.Loans.Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == loanId && !l.IsDeleted);

            if (loan == null) return LoanResponse.CreateFailure("Loan not found.");

            if (loan.LenderId != lenderId)
                return LoanResponse.CreateFailure("You do not have permission to mark this loan as returned.");

            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return LoanResponse.CreateFailure(
                    $"This loan cannot be marked as returned as it is currently {loan.Status}.");

            // Close the loan
            loan.Status = LoanStatus.Returned;
            loan.ReturnedDate = DateTime.UtcNow;
            loan.ConditionAtReturn = request.ConditionAtReturn;
            loan.BorrowerNotes = request.BorrowerNotes?.Trim();
            loan.UpdatedAt = DateTime.UtcNow;

            // Mark the original borrow request as completed
            if (loan.BorrowRequestId.HasValue)
            {
                var borrowRequest =
                    await _context.BorrowRequests.FirstOrDefaultAsync(r => r.Id == loan.BorrowRequestId.Value);

                if (borrowRequest != null)
                {
                    borrowRequest.Status = BorrowRequestStatus.Completed;
                    borrowRequest.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Make book available again and update condition
            loan.Book.IsAvailable = true;
            loan.Book.Condition = request.ConditionAtReturn;
            loan.Book.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Loan {LoanId} marked as returned, Book {BookId} now available", loanId,
                loan.BookId);

            return LoanResponse.CreateSuccess("Book marked as returned. Thank you for using Christian Library Sharing!",
                loan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error marking loan {LoanId} as returned", loanId);
            return LoanResponse.CreateFailure("An unexpected error occurred while marking the book as returned.");
        }
    }

    // -------------------------------------------------------
    // US-06.08: View My Borrows (as borrower)
    // -------------------------------------------------------

    public async Task<PagedResult<LoanSummary>> GetMyBorrowsAsync(string borrowerId, LoanQuery query)
    {
        _logger.LogInformation(
            "GetMyBorrows - BorrowerId={BorrowerId}, Page={Page}, PageSize={PageSize}, Status={Status}", borrowerId,
            query.Page, query.PageSize, query.Status);

        try
        {
            var dbQuery = _context.Loans.Include(l => l.Book)
                .Include(l => l.Lender)
                .ThenInclude(u => u.Profile)
                .Where(l => l.BorrowerId == borrowerId && !l.IsDeleted);

            if (query.Status.HasValue) dbQuery = dbQuery.Where(l => l.Status == query.Status.Value);

            var totalCount = await dbQuery.CountAsync();

            var loans = await dbQuery.OrderByDescending(l => l.StartDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<LoanSummary>
            {
                Items = loans.Select(MapToSummary).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting borrows for borrower {BorrowerId}", borrowerId);
            return new PagedResult<LoanSummary>();
        }
    }

    // -------------------------------------------------------
    // US-06.09: View My Loans (as lender/owner)
    // -------------------------------------------------------

    public async Task<PagedResult<LoanSummary>> GetMyLoansAsync(string lenderId, LoanQuery query)
    {
        _logger.LogInformation("GetMyLoans - LenderId={LenderId}, Page={Page}, PageSize={PageSize}, Status={Status}",
            lenderId, query.Page, query.PageSize, query.Status);

        try
        {
            var dbQuery = _context.Loans.Include(l => l.Book)
                .Include(l => l.Borrower)
                .ThenInclude(u => u.Profile)
                .Where(l => l.LenderId == lenderId && !l.IsDeleted);

            if (query.Status.HasValue) dbQuery = dbQuery.Where(l => l.Status == query.Status.Value);

            var totalCount = await dbQuery.CountAsync();

            var loans = await dbQuery.OrderByDescending(l => l.StartDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<LoanSummary>
            {
                Items = loans.Select(MapToSummary).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting loans for lender {LenderId}", lenderId);
            return new PagedResult<LoanSummary>();
        }
    }

    // -------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------

    private static LoanSummary MapToSummary(Loan l) =>
        new()
        {
            Id = l.Id,
            BookId = l.BookId,
            BookTitle = l.Book?.Title ?? string.Empty,
            BookAuthor = l.Book?.Author ?? string.Empty,
            BorrowerId = l.BorrowerId,
            BorrowerName = l.Borrower?.Profile?.FullName ?? "Unknown",
            LenderId = l.LenderId,
            LenderName = l.Lender?.Profile?.FullName ?? "Unknown",
            Status = l.Status,
            StartDate = l.StartDate,
            DueDate = l.DueDate,
            ReturnedDate = l.ReturnedDate,
            IsOverdue = l.IsOverdue,
            DaysUntilDue = l.DaysUntilDue,
            ConditionAtCheckout = l.ConditionAtCheckout,
            ConditionAtReturn = l.ConditionAtReturn,
            LenderNotes = l.LenderNotes,
            BorrowerNotes = l.BorrowerNotes
        };
}