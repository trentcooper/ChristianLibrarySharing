using ChristianLibrary.Services.DTOs.Common;
using ChristianLibrary.Services.DTOs.Loans;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Service handling loan lifecycle operations
/// </summary>
public interface ILoanService
{
    /// <summary>
    /// Marks a book as returned, closing the active loan
    /// Maps to US-06.07: Mark book as returned
    /// </summary>
    Task<LoanResponse> MarkReturnedAsync(
        int loanId,
        string lenderId,
        MarkReturnedRequest request);

    /// <summary>
    /// Gets all loans where the authenticated user is the borrower
    /// Maps to US-06.08: View my active borrows (as borrower)
    /// </summary>
    Task<PagedResult<LoanSummary>> GetMyBorrowsAsync(
        string borrowerId,
        LoanQuery query);

    /// <summary>
    /// Gets all loans where the authenticated user is the lender
    /// Maps to US-06.09: View my active loans (as owner)
    /// </summary>
    Task<PagedResult<LoanSummary>> GetMyLoansAsync(
        string lenderId,
        LoanQuery query);
}