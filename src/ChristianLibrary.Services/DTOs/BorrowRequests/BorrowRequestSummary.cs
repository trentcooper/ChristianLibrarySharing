using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.BorrowRequests;

/// <summary>
/// Summary DTO for displaying borrow request information
/// </summary>
public class BorrowRequestSummary
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string BorrowerId { get; set; } = string.Empty;
    public string BorrowerName { get; set; } = string.Empty;
    public string LenderId { get; set; } = string.Empty;
    public string LenderName { get; set; } = string.Empty;
    public BorrowRequestStatus Status { get; set; }
    public DateTime RequestedStartDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public string? Message { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

