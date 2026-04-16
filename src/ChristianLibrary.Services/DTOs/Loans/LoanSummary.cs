using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Loans;

/// <summary>
/// Summary DTO for loan list views
/// </summary>
public class LoanSummary
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string BorrowerId { get; set; } = string.Empty;
    public string BorrowerName { get; set; } = string.Empty;
    public string LenderId { get; set; } = string.Empty;
    public string LenderName { get; set; } = string.Empty;
    public LoanStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
    public BookCondition? ConditionAtCheckout { get; set; }
    public BookCondition? ConditionAtReturn { get; set; }
    public string? LenderNotes { get; set; }
    public string? BorrowerNotes { get; set; }
}