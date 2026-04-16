using ChristianLibrary.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Loans;

/// <summary>
/// Request DTO for marking a book as returned
/// Maps to US-06.07: Mark book as returned
/// </summary>
public class MarkReturnedRequest
{
    /// <summary>
    /// Condition of the book at time of return
    /// Required for dispute resolution comparison against ConditionAtCheckout
    /// </summary>
    public BookCondition ConditionAtReturn { get; set; }

    /// <summary>
    /// Optional notes from borrower at time of return
    /// </summary>
    [MaxLength(1000)]
    public string? BorrowerNotes { get; set; }
}