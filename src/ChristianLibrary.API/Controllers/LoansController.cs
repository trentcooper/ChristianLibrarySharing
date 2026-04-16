using ChristianLibrary.Services.DTOs.Loans;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChristianLibrary.Services.DTOs.Common;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Controller for loan lifecycle operations
/// </summary>
[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly ILogger<LoansController> _logger;

    public LoansController(
        ILoanService loanService,
        ILogger<LoansController> logger)
    {
        _loanService = loanService;
        _logger = logger;
    }

    // -------------------------------------------------------
    // US-06.07: Mark book as returned
    // -------------------------------------------------------

    /// <summary>
    /// Marks a book as returned, closing the active loan
    /// </summary>
    [HttpPost("{id}/return")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkReturned(int id, [FromBody] MarkReturnedRequest request)
    {
        var lenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(lenderId))
            return Unauthorized();

        _logger.LogInformation(
            "POST /api/loans/{LoanId}/return - LenderId={LenderId}",
            id, lenderId);

        var response = await _loanService.MarkReturnedAsync(id, lenderId, request);

        if (!response.Success)
        {
            _logger.LogWarning(
                "MarkReturned failed for loan {LoanId} by lender {LenderId}: {Message}",
                id, lenderId, response.Message);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }
    
    // -------------------------------------------------------
    // US-06.08: View my borrows (as borrower)
    // -------------------------------------------------------

    /// <summary>
    /// Returns all loans where the authenticated user is the borrower
    /// </summary>
    [HttpGet("my-borrows")]
    [ProducesResponseType(typeof(PagedResult<LoanSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBorrows([FromQuery] LoanQuery query)
    {
        var borrowerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(borrowerId))
            return Unauthorized();

        _logger.LogInformation(
            "GET /api/loans/my-borrows - BorrowerId={BorrowerId}", borrowerId);

        var result = await _loanService.GetMyBorrowsAsync(borrowerId, query);

        return Ok(result);
    }

    // -------------------------------------------------------
    // US-06.09: View my loans (as lender/owner)
    // -------------------------------------------------------

    /// <summary>
    /// Returns all loans where the authenticated user is the lender
    /// </summary>
    [HttpGet("my-loans")]
    [ProducesResponseType(typeof(PagedResult<LoanSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyLoans([FromQuery] LoanQuery query)
    {
        var lenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(lenderId))
            return Unauthorized();

        _logger.LogInformation(
            "GET /api/loans/my-loans - LenderId={LenderId}", lenderId);

        var result = await _loanService.GetMyLoansAsync(lenderId, query);

        return Ok(result);
    }
}