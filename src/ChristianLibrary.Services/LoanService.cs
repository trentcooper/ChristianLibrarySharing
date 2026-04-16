using ChristianLibrary.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ChristianLibrary.Data.Context;
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

    public LoanService(
        ApplicationDbContext context,
        ILogger<LoanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // -------------------------------------------------------
    // US-06.07: Mark Book as Returned
    // -------------------------------------------------------

    public async Task<LoanResponse> MarkReturnedAsync(
        int loanId,
        string lenderId,
        MarkReturnedRequest request)
    {
        _logger.LogInformation(
            "MarkReturned - LoanId={LoanId}, LenderId={LenderId}",
            loanId, lenderId);

        try
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == loanId && !l.IsDeleted);

            if (loan == null)
                return LoanResponse.CreateFailure("Loan not found.");

            if (loan.LenderId != lenderId)
                return LoanResponse.CreateFailure(
                    "You do not have permission to mark this loan as returned.");

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
                var borrowRequest = await _context.BorrowRequests
                    .FirstOrDefaultAsync(r => r.Id == loan.BorrowRequestId.Value);

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

            _logger.LogInformation(
                "Loan {LoanId} marked as returned, Book {BookId} now available",
                loanId, loan.BookId);

            return LoanResponse.CreateSuccess(
                "Book marked as returned. Thank you for using Christian Library Sharing!",
                loan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error marking loan {LoanId} as returned", loanId);
            return LoanResponse.CreateFailure(
                "An unexpected error occurred while marking the book as returned.");
        }
    }
}