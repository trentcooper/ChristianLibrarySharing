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
}