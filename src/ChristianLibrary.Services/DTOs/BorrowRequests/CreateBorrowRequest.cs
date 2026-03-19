namespace ChristianLibrary.Services.DTOs.BorrowRequests;

/// <summary>
/// Request DTO for creating a new borrow request
/// </summary>
public class CreateBorrowRequest
{
    public int BookId { get; set; }
    public DateTime RequestedStartDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public string? Message { get; set; }
}