namespace ChristianLibrary.Services.DTOs.BorrowRequests;

/// <summary>
/// Response DTO for borrow request operations
/// </summary>
public class BorrowRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? BorrowRequestId { get; set; }

    public static BorrowRequestResponse CreateSuccess(string message, int borrowRequestId) =>
        new() { Success = true, Message = message, BorrowRequestId = borrowRequestId };

    public static BorrowRequestResponse CreateFailure(string message) =>
        new() { Success = false, Message = message };
}