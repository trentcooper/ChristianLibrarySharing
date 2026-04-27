using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services.DTOs.Common;

namespace ChristianLibrary.Services.DTOs.BorrowRequests;

/// <summary>
/// Query parameters for filtering and paginating borrow request lists
/// (incoming and outgoing). Mirrors LoanQuery for consistency.
/// </summary>
public class BorrowRequestQuery : PagedQuery
{
    public BorrowRequestStatus? Status { get; set; }
}