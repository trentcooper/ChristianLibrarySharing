using ChristianLibrary.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Services.DTOs.Common;
using ChristianLibrary.Services.DTOs.Loans;
using ChristianLibrary.Services.Interfaces;
using ChristianLibrary.Services.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Service handling loan lifecycle operations
/// </summary>
public class LoanService : ILoanService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoanService> _logger;
    private readonly LoanSettings _loanSettings;

    public LoanService(
        ApplicationDbContext context,
        ILogger<LoanService> logger,
        IOptions<LoanSettings> loanSettings)
    {
        _context = context;
        _logger = logger;
        _loanSettings = loanSettings.Value;
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

            // Status must be Active or Overdue. ExtensionRequested is deliberately
            // excluded — the workflow forces the lender to approve or decline a
            // pending extension before the loan can be closed. See US-06.11.
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
    // US-06.11: Request Loan Extension
    // -------------------------------------------------------

    /// <summary>
    /// Borrower requests an extension on an Active or Overdue loan they currently hold.
    /// Validates: caller is the borrower, loan is Active or Overdue, no pending extension exists,
    /// requested date is after current DueDate, and within MaxExtensionDays of the current DueDate.
    /// </summary>
    public async Task<LoanResponse> RequestExtensionAsync(
        int loanId,
        string borrowerId,
        RequestExtensionRequest request)
    {
        _logger.LogInformation(
            "RequestExtension - LoanId={LoanId}, BorrowerId={BorrowerId}, RequestedDueDate={RequestedDueDate}",
            loanId, borrowerId, request.RequestedDueDate);

        try
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.Id == loanId && !l.IsDeleted);

            if (loan == null)
                return LoanResponse.CreateFailure("Loan not found.");

            if (loan.BorrowerId != borrowerId)
                return LoanResponse.CreateFailure(
                    "You do not have permission to request an extension on this loan.");
            
            if (loan.Status == LoanStatus.ExtensionRequested)
                return LoanResponse.CreateFailure(
                    "An extension request is already pending on this loan.");

            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return LoanResponse.CreateFailure(
                    $"This loan cannot be extended as it is currently {loan.Status}.");

            if (request.RequestedDueDate <= loan.DueDate)
                return LoanResponse.CreateFailure(
                    "Requested due date must be after the current due date.");

            var extensionDays = (request.RequestedDueDate - loan.DueDate).Days;
            if (extensionDays > _loanSettings.MaxExtensionDays)
                return LoanResponse.CreateFailure(
                    $"Extension cannot exceed {_loanSettings.MaxExtensionDays} days from the current due date.");

            // Capture the request on the loan
            loan.Status = LoanStatus.ExtensionRequested;
            loan.ExtensionRequested = true;
            loan.RequestedExtensionDate = request.RequestedDueDate;
            loan.ExtensionRequestMessage = request.Message?.Trim();
            loan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Loan {LoanId} extension requested by borrower {BorrowerId} until {RequestedDueDate}",
                loanId, borrowerId, request.RequestedDueDate);

            return LoanResponse.CreateSuccess(
                "Extension request submitted. The lender will be notified.",
                loan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error requesting extension for loan {LoanId}", loanId);
            return LoanResponse.CreateFailure(
                "An unexpected error occurred while submitting the extension request.");
        }
    }

    /// <summary>
    /// Lender approves a pending extension request, updating the loan's DueDate
    /// and resetting status to Active.
    /// </summary>
    public async Task<LoanResponse> ApproveExtensionAsync(int loanId, string lenderId)
    {
        _logger.LogInformation(
            "ApproveExtension - LoanId={LoanId}, LenderId={LenderId}", loanId, lenderId);

        try
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.Id == loanId && !l.IsDeleted);

            if (loan == null)
                return LoanResponse.CreateFailure("Loan not found.");

            if (loan.LenderId != lenderId)
                return LoanResponse.CreateFailure(
                    "You do not have permission to approve this extension request.");

            if (loan.Status != LoanStatus.ExtensionRequested || loan.RequestedExtensionDate == null)
                return LoanResponse.CreateFailure("There is no pending extension request on this loan.");

            // Apply the extension
            var addedDays = (loan.RequestedExtensionDate.Value - loan.DueDate).Days;
            loan.DueDate = loan.RequestedExtensionDate.Value;
            loan.ExtensionDays += addedDays;
            loan.Status = LoanStatus.Active;
            loan.ExtensionRequested = false;
            loan.UpdatedAt = DateTime.UtcNow;
            // RequestedExtensionDate and ExtensionRequestMessage retained as audit breadcrumb

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Loan {LoanId} extension approved by lender {LenderId}; new DueDate={DueDate}, ExtensionDays={ExtensionDays}",
                loanId, lenderId, loan.DueDate, loan.ExtensionDays);

            return LoanResponse.CreateSuccess(
                "Extension approved. The borrower has been notified.",
                loan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error approving extension for loan {LoanId}", loanId);
            return LoanResponse.CreateFailure(
                "An unexpected error occurred while approving the extension request.");
        }
    }

    /// <summary>
    /// Lender declines a pending extension request, returning the loan to its prior state
    /// (Active or Overdue) based on the current DueDate.
    /// </summary>
    public async Task<LoanResponse> DeclineExtensionAsync(int loanId, string lenderId)
    {
        _logger.LogInformation(
            "DeclineExtension - LoanId={LoanId}, LenderId={LenderId}", loanId, lenderId);

        try
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.Id == loanId && !l.IsDeleted);

            if (loan == null)
                return LoanResponse.CreateFailure("Loan not found.");

            if (loan.LenderId != lenderId)
                return LoanResponse.CreateFailure(
                    "You do not have permission to decline this extension request.");

            if (loan.Status != LoanStatus.ExtensionRequested)
                return LoanResponse.CreateFailure("There is no pending extension request on this loan.");

            // Restore prior status based on current DueDate
            loan.Status = DateTime.UtcNow > loan.DueDate ? LoanStatus.Overdue : LoanStatus.Active;
            loan.ExtensionRequested = false;
            loan.UpdatedAt = DateTime.UtcNow;
            // RequestedExtensionDate and ExtensionRequestMessage retained as audit breadcrumb

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Loan {LoanId} extension declined by lender {LenderId}; status restored to {Status}",
                loanId, lenderId, loan.Status);

            return LoanResponse.CreateSuccess(
                "Extension request declined.",
                loan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error declining extension for loan {LoanId}", loanId);
            return LoanResponse.CreateFailure(
                "An unexpected error occurred while declining the extension request.");
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
            BorrowerNotes = l.BorrowerNotes,
            RequestedExtensionDate = l.RequestedExtensionDate,
            ExtensionRequestMessage = l.ExtensionRequestMessage
        };
}