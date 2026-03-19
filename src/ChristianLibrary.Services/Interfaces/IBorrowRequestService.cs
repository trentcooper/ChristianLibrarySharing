using ChristianLibrary.Services.DTOs.BorrowRequests;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Service handling borrow request operations
/// </summary>
public interface IBorrowRequestService
{
    /// <summary>
    /// Creates a new borrow request for a book
    /// </summary>
    Task<BorrowRequestResponse> CreateBorrowRequestAsync(
        CreateBorrowRequest request,
        string borrowerId);

    /// <summary>
    /// Gets all pending borrow requests received by the authenticated user (as lender)
    /// </summary>
    Task<List<BorrowRequestSummary>> GetIncomingRequestsAsync(string lenderId);

    /// <summary>
    /// Gets all borrow requests made by the authenticated user (as borrower)
    /// </summary>
    Task<List<BorrowRequestSummary>> GetOutgoingRequestsAsync(string borrowerId);

    /// <summary>
    /// Approves a borrow request and creates a loan
    /// </summary>
    Task<BorrowRequestResponse> ApproveRequestAsync(
        int borrowRequestId,
        string lenderId,
        string? responseMessage = null);

    /// <summary>
    /// Denies a borrow request
    /// </summary>
    Task<BorrowRequestResponse> DenyRequestAsync(
        int borrowRequestId,
        string lenderId,
        string? responseMessage = null);

    /// <summary>
    /// Cancels a borrow request (borrower only)
    /// </summary>
    Task<BorrowRequestResponse> CancelRequestAsync(
        int borrowRequestId,
        string borrowerId);
}