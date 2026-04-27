using ChristianLibrary.Services.DTOs.BorrowRequests;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChristianLibrary.Services.DTOs.Common;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Controller for borrow request operations
/// </summary>
[ApiController]
[Route("api/borrow-requests")]
[Authorize]
public class BorrowRequestsController : ControllerBase
{
    private readonly IBorrowRequestService _borrowRequestService;
    private readonly ILogger<BorrowRequestsController> _logger;

    public BorrowRequestsController(
        IBorrowRequestService borrowRequestService,
        ILogger<BorrowRequestsController> logger)
    {
        _borrowRequestService = borrowRequestService;
        _logger = logger;
    }

    // -------------------------------------------------------
    // US-06.02: Request to borrow a book
    // -------------------------------------------------------

    /// <summary>
    /// Creates a new borrow request for a book
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBorrowRequest([FromBody] CreateBorrowRequest request)
    {
        var borrowerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(borrowerId))
            return Unauthorized();

        _logger.LogInformation(
            "POST /api/borrow-requests - BookId={BookId}, BorrowerId={BorrowerId}",
            request.BookId, borrowerId);

        var response = await _borrowRequestService.CreateBorrowRequestAsync(request, borrowerId);

        if (!response.Success)
        {
            _logger.LogWarning(
                "CreateBorrowRequest failed for book {BookId} by {BorrowerId}: {Message}",
                request.BookId, borrowerId, response.Message);
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetOutgoingRequests),
            new { },
            response);
    }

    // -------------------------------------------------------
    // US-06.03: View incoming borrow requests (as lender)
    // -------------------------------------------------------

    /// <summary>
    /// Returns a paginated list of borrow requests received by the authenticated user as a lender,
    /// optionally filtered by status
    /// </summary>
    [HttpGet("incoming")]
    [ProducesResponseType(typeof(PagedResult<BorrowRequestSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIncomingRequests([FromQuery] BorrowRequestQuery query)
    {
        var lenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(lenderId))
            return Unauthorized();

        _logger.LogInformation(
            "GET /api/borrow-requests/incoming - LenderId={LenderId}, Page={Page}, PageSize={PageSize}, Status={Status}",
            lenderId, query.Page, query.PageSize, query.Status);

        var result = await _borrowRequestService.GetIncomingRequestsAsync(lenderId, query);

        return Ok(result);
    }

    // -------------------------------------------------------
    // US-06.02: View Outgoing Requests (borrower's own requests)
    // -------------------------------------------------------

    /// <summary>
    /// Returns a paginated list of borrow requests made by the authenticated user as a borrower,
    /// optionally filtered by status
    /// </summary>
    [HttpGet("outgoing")]
    [ProducesResponseType(typeof(PagedResult<BorrowRequestSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOutgoingRequests([FromQuery] BorrowRequestQuery query)
    {
        var borrowerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(borrowerId))
            return Unauthorized();

        _logger.LogInformation(
            "GET /api/borrow-requests/outgoing - BorrowerId={BorrowerId}, Page={Page}, PageSize={PageSize}, Status={Status}",
            borrowerId, query.Page, query.PageSize, query.Status);

        var result = await _borrowRequestService.GetOutgoingRequestsAsync(borrowerId, query);

        return Ok(result);
    }

    // -------------------------------------------------------
    // US-06.04: Approve borrow request
    // -------------------------------------------------------

    /// <summary>
    /// Approves a pending borrow request
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] string? responseMessage = null)
    {
        var lenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(lenderId))
            return Unauthorized();

        _logger.LogInformation(
            "POST /api/borrow-requests/{RequestId}/approve - LenderId={LenderId}",
            id, lenderId);

        var response = await _borrowRequestService.ApproveRequestAsync(id, lenderId, responseMessage);

        if (!response.Success)
        {
            _logger.LogWarning(
                "ApproveRequest failed for request {RequestId} by lender {LenderId}: {Message}",
                id, lenderId, response.Message);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }

    // -------------------------------------------------------
    // US-06.05: Decline borrow request
    // -------------------------------------------------------

    /// <summary>
    /// Declines a pending borrow request
    /// </summary>
    [HttpPost("{id}/decline")]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeclineRequest(int id, [FromBody] string? responseMessage = null)
    {
        var lenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(lenderId))
            return Unauthorized();

        _logger.LogInformation(
            "POST /api/borrow-requests/{RequestId}/decline - LenderId={LenderId}",
            id, lenderId);

        var response = await _borrowRequestService.DeclineRequestAsync(id, lenderId, responseMessage);

        if (!response.Success)
        {
            _logger.LogWarning(
                "DeclineRequest failed for request {RequestId} by lender {LenderId}: {Message}",
                id, lenderId, response.Message);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }

    // -------------------------------------------------------
    // US-06.06: Mark book as picked up/borrowed
    // -------------------------------------------------------

    /// <summary>
    /// Marks a book as picked up by the borrower, creating an active loan
    /// </summary>
    [HttpPost("{id}/pickup")]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkPickedUp(int id, [FromBody] MarkPickedUpRequest request)
    {
        var lenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(lenderId))
            return Unauthorized();

        _logger.LogInformation(
            "POST /api/borrow-requests/{RequestId}/pickup - LenderId={LenderId}",
            id, lenderId);

        var response = await _borrowRequestService.MarkPickedUpAsync(id, lenderId, request);

        if (!response.Success)
        {
            _logger.LogWarning(
                "MarkPickedUp failed for request {RequestId} by lender {LenderId}: {Message}",
                id, lenderId, response.Message);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }

    // -------------------------------------------------------
    // US-06.12: Cancel borrow request
    // -------------------------------------------------------

    /// <summary>
    /// Cancels a pending borrow request (borrower only)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var borrowerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(borrowerId))
            return Unauthorized();

        _logger.LogInformation(
            "POST /api/borrow-requests/{RequestId}/cancel - BorrowerId={BorrowerId}",
            id, borrowerId);

        var response = await _borrowRequestService.CancelRequestAsync(id, borrowerId);

        if (!response.Success)
        {
            _logger.LogWarning(
                "CancelRequest failed for request {RequestId} by borrower {BorrowerId}: {Message}",
                id, borrowerId, response.Message);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }
}