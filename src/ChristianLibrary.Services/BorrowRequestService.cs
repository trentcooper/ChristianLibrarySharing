using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services.DTOs.BorrowRequests;
using ChristianLibrary.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Service handling borrow request operations
/// </summary>
public class BorrowRequestService : IBorrowRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BorrowRequestService> _logger;

    public BorrowRequestService(
        ApplicationDbContext context,
        ILogger<BorrowRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // -------------------------------------------------------
    // US-06.02: Create Borrow Request
    // -------------------------------------------------------

    /// <summary>
    /// Creates a new borrow request validating business rules:
    /// - Book must exist and be available
    /// - Borrower cannot request their own book
    /// - No duplicate pending requests allowed
    /// - Dates must be valid (start in future, end after start)
    /// </summary>
    public async Task<BorrowRequestResponse> CreateBorrowRequestAsync(
        CreateBorrowRequest request,
        string borrowerId)
    {
        _logger.LogInformation(
            "CreateBorrowRequest - BookId={BookId}, BorrowerId={BorrowerId}",
            request.BookId, borrowerId);

        try
        {
            // Validate dates
            if (request.RequestedStartDate <= DateTime.UtcNow)
                return BorrowRequestResponse.CreateFailure(
                    "Requested start date must be in the future.");

            if (request.RequestedEndDate <= request.RequestedStartDate)
                return BorrowRequestResponse.CreateFailure(
                    "Requested end date must be after the start date.");

            // Validate book exists and is available
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == request.BookId && !b.IsDeleted);

            if (book == null)
                return BorrowRequestResponse.CreateFailure("Book not found.");

            if (!book.IsAvailable)
                return BorrowRequestResponse.CreateFailure(
                    "This book is not currently available for borrowing.");

            // Prevent borrowing your own book
            if (book.OwnerId == borrowerId)
                return BorrowRequestResponse.CreateFailure(
                    "You cannot request to borrow your own book.");

            // Check for duplicate pending request
            var existingRequest = await _context.BorrowRequests
                .AnyAsync(r =>
                    r.BookId == request.BookId &&
                    r.BorrowerId == borrowerId &&
                    r.Status == BorrowRequestStatus.Pending &&
                    !r.IsDeleted);

            if (existingRequest)
                return BorrowRequestResponse.CreateFailure(
                    "You already have a pending request for this book. " +
                    "Check your outgoing requests to view its status.");

            // Create the borrow request
            var borrowRequest = new BorrowRequest
            {
                BookId = request.BookId,
                BorrowerId = borrowerId,
                LenderId = book.OwnerId,
                Status = BorrowRequestStatus.Pending,
                RequestedStartDate = request.RequestedStartDate,
                RequestedEndDate = request.RequestedEndDate,
                Message = request.Message?.Trim(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.BorrowRequests.Add(borrowRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "BorrowRequest {RequestId} created for book {BookId} by borrower {BorrowerId}",
                borrowRequest.Id, request.BookId, borrowerId);

            return BorrowRequestResponse.CreateSuccess(
                "Borrow request sent successfully! The owner will be notified.",
                borrowRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error creating borrow request for book {BookId}", request.BookId);
            return BorrowRequestResponse.CreateFailure(
                "An unexpected error occurred while submitting your request.");
        }
    }

    // -------------------------------------------------------
    // US-06.03: View Incoming Requests
    // -------------------------------------------------------

    /// <summary>
    /// Returns all non-deleted borrow requests received by the lender
    /// ordered by most recent first
    /// </summary>
    public async Task<List<BorrowRequestSummary>> GetIncomingRequestsAsync(string lenderId)
    {
        _logger.LogInformation("Getting incoming requests for lender {LenderId}", lenderId);

        try
        {
            var requests = await _context.BorrowRequests
                .Include(r => r.Book)
                .Include(r => r.Borrower)
                .ThenInclude(u => u.Profile)
                .Where(r => r.LenderId == lenderId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(MapToSummary).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error getting incoming requests for lender {LenderId}", lenderId);
            return new List<BorrowRequestSummary>();
        }
    }

    // -------------------------------------------------------
    // US-06.02: View Outgoing Requests
    // -------------------------------------------------------

    /// <summary>
    /// Returns all non-deleted borrow requests made by the borrower
    /// ordered by most recent first
    /// </summary>
    public async Task<List<BorrowRequestSummary>> GetOutgoingRequestsAsync(string borrowerId)
    {
        _logger.LogInformation("Getting outgoing requests for borrower {BorrowerId}", borrowerId);

        try
        {
            var requests = await _context.BorrowRequests
                .Include(r => r.Book)
                .Include(r => r.Lender)
                .ThenInclude(u => u.Profile)
                .Where(r => r.BorrowerId == borrowerId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(MapToSummary).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error getting outgoing requests for borrower {BorrowerId}", borrowerId);
            return new List<BorrowRequestSummary>();
        }
    }

    // -------------------------------------------------------
    // US-06.04: Approve Request
    // -------------------------------------------------------

    /// <summary>
    /// Approves a borrow request and creates an active loan.
    /// Only the lender can approve their own requests.
    /// </summary>
    public async Task<BorrowRequestResponse> ApproveRequestAsync(
        int borrowRequestId,
        string lenderId,
        string? responseMessage = null)
    {
        _logger.LogInformation(
            "ApproveRequest - RequestId={RequestId}, LenderId={LenderId}",
            borrowRequestId, lenderId);

        try
        {
            var borrowRequest = await _context.BorrowRequests
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == borrowRequestId && !r.IsDeleted);

            if (borrowRequest == null)
                return BorrowRequestResponse.CreateFailure("Borrow request not found.");

            if (borrowRequest.LenderId != lenderId)
                return BorrowRequestResponse.CreateFailure(
                    "You do not have permission to approve this request.");

            if (borrowRequest.Status != BorrowRequestStatus.Pending)
                return BorrowRequestResponse.CreateFailure(
                    $"This request cannot be approved as it is currently {borrowRequest.Status}.");

            // Update request status
            borrowRequest.Status = BorrowRequestStatus.Approved;
            borrowRequest.ResponseMessage = responseMessage?.Trim();
            borrowRequest.RespondedAt = DateTime.UtcNow;
            borrowRequest.UpdatedAt = DateTime.UtcNow;

            // Create the loan
            var loan = new Loan
            {
                BookId = borrowRequest.BookId,
                BorrowerId = borrowRequest.BorrowerId,
                LenderId = borrowRequest.LenderId,
                BorrowRequestId = borrowRequest.Id,
                Status = LoanStatus.Active,
                StartDate = borrowRequest.RequestedStartDate,
                DueDate = borrowRequest.RequestedEndDate,
                CreatedAt = DateTime.UtcNow
            };

            // Mark book as unavailable
            borrowRequest.Book.IsAvailable = false;
            borrowRequest.Book.UpdatedAt = DateTime.UtcNow;

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "BorrowRequest {RequestId} approved, Loan {LoanId} created",
                borrowRequestId, loan.Id);

            return BorrowRequestResponse.CreateSuccess(
                "Request approved successfully. A loan has been created.",
                borrowRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error approving request {RequestId}", borrowRequestId);
            return BorrowRequestResponse.CreateFailure(
                "An unexpected error occurred while approving the request.");
        }
    }

    // -------------------------------------------------------
    // US-06.05: Deny Request
    // -------------------------------------------------------

    /// <summary>
    /// Denies a borrow request. Only the lender can deny their own requests.
    /// </summary>
    public async Task<BorrowRequestResponse> DenyRequestAsync(
        int borrowRequestId,
        string lenderId,
        string? responseMessage = null)
    {
        _logger.LogInformation(
            "DenyRequest - RequestId={RequestId}, LenderId={LenderId}",
            borrowRequestId, lenderId);

        try
        {
            var borrowRequest = await _context.BorrowRequests
                .FirstOrDefaultAsync(r => r.Id == borrowRequestId && !r.IsDeleted);

            if (borrowRequest == null)
                return BorrowRequestResponse.CreateFailure("Borrow request not found.");

            if (borrowRequest.LenderId != lenderId)
                return BorrowRequestResponse.CreateFailure(
                    "You do not have permission to deny this request.");

            if (borrowRequest.Status != BorrowRequestStatus.Pending)
                return BorrowRequestResponse.CreateFailure(
                    $"This request cannot be denied as it is currently {borrowRequest.Status}.");

            borrowRequest.Status = BorrowRequestStatus.Declined;
            borrowRequest.ResponseMessage = responseMessage?.Trim();
            borrowRequest.RespondedAt = DateTime.UtcNow;
            borrowRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "BorrowRequest {RequestId} denied by lender {LenderId}",
                borrowRequestId, lenderId);

            return BorrowRequestResponse.CreateSuccess(
                "Request denied successfully.", borrowRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error denying request {RequestId}", borrowRequestId);
            return BorrowRequestResponse.CreateFailure(
                "An unexpected error occurred while denying the request.");
        }
    }

    // -------------------------------------------------------
    // US-06.12: Cancel Request
    // -------------------------------------------------------

    /// <summary>
    /// Cancels a pending borrow request. Only the borrower can cancel their own requests.
    /// </summary>
    public async Task<BorrowRequestResponse> CancelRequestAsync(
        int borrowRequestId,
        string borrowerId)
    {
        _logger.LogInformation(
            "CancelRequest - RequestId={RequestId}, BorrowerId={BorrowerId}",
            borrowRequestId, borrowerId);

        try
        {
            var borrowRequest = await _context.BorrowRequests
                .FirstOrDefaultAsync(r => r.Id == borrowRequestId && !r.IsDeleted);

            if (borrowRequest == null)
                return BorrowRequestResponse.CreateFailure("Borrow request not found.");

            if (borrowRequest.BorrowerId != borrowerId)
                return BorrowRequestResponse.CreateFailure(
                    "You do not have permission to cancel this request.");

            if (borrowRequest.Status != BorrowRequestStatus.Pending)
                return BorrowRequestResponse.CreateFailure(
                    $"This request cannot be cancelled as it is currently {borrowRequest.Status}.");

            borrowRequest.Status = BorrowRequestStatus.Cancelled;
            borrowRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "BorrowRequest {RequestId} cancelled by borrower {BorrowerId}",
                borrowRequestId, borrowerId);

            return BorrowRequestResponse.CreateSuccess(
                "Request cancelled successfully.", borrowRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error cancelling request {RequestId}", borrowRequestId);
            return BorrowRequestResponse.CreateFailure(
                "An unexpected error occurred while cancelling the request.");
        }
    }

    // -------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------

    /// <summary>
    /// Maps a BorrowRequest entity to a BorrowRequestSummary DTO
    /// </summary>
    private static BorrowRequestSummary MapToSummary(BorrowRequest r) => new()
    {
        Id = r.Id,
        BookId = r.BookId,
        BookTitle = r.Book?.Title ?? string.Empty,
        BookAuthor = r.Book?.Author ?? string.Empty,
        BorrowerId = r.BorrowerId,
        BorrowerName = r.Borrower?.Profile?.FullName ?? "Unknown",
        LenderId = r.LenderId,
        LenderName = r.Lender?.Profile?.FullName ?? "Unknown",
        Status = r.Status,
        RequestedStartDate = r.RequestedStartDate,
        RequestedEndDate = r.RequestedEndDate,
        Message = r.Message,
        ResponseMessage = r.ResponseMessage,
        RespondedAt = r.RespondedAt,
        ExpiresAt = r.ExpiresAt,
        CreatedAt = r.CreatedAt
    };
}