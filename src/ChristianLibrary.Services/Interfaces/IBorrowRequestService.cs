using ChristianLibrary.Services.DTOs.BorrowRequests;
using ChristianLibrary.Services.DTOs.Common;

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
    /// Gets a paginated list of borrow requests received by the authenticated user (as lender),
    /// optionally filtered by status
    /// </summary>
    Task<PagedResult<BorrowRequestSummary>> GetIncomingRequestsAsync(
        string lenderId,
        BorrowRequestQuery query);

    /// <summary>
    /// Gets a paginated list of borrow requests made by the authenticated user (as borrower),
    /// optionally filtered by status
    /// </summary>
    Task<PagedResult<BorrowRequestSummary>> GetOutgoingRequestsAsync(
        string borrowerId,
        BorrowRequestQuery query);

    /// <summary>
    /// Approves a borrow request and creates a loan
    /// </summary>
    Task<BorrowRequestResponse> ApproveRequestAsync(
        int borrowRequestId,
        string lenderId,
        string? responseMessage = null);

    /// <summary>
    /// Marks a book as picked up by the borrower, creating an active loan
    /// Maps to US-06.06: Mark book as picked up/borrowed
    /// </summary>
    Task<BorrowRequestResponse> MarkPickedUpAsync(
        int borrowRequestId,
        string lenderId,
        MarkPickedUpRequest request);

    /// <summary>
    /// Declines a borrow request
    /// </summary>
    Task<BorrowRequestResponse> DeclineRequestAsync(
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