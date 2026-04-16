namespace ChristianLibrary.Services.DTOs.Loans;

/// <summary>
/// Response DTO for loan operations
/// </summary>
public class LoanResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LoanId { get; set; }

    public static LoanResponse CreateSuccess(string message, int loanId) =>
        new() { Success = true, Message = message, LoanId = loanId };

    public static LoanResponse CreateFailure(string message) =>
        new() { Success = false, Message = message };
}