using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Loans;

/// <summary>
/// Request payload for a borrower asking to extend a loan.
/// Maps to US-06.11
/// </summary>
public class RequestExtensionRequest
{
    /// <summary>
    /// The new due date the borrower is requesting.
    /// Must be after the loan's current DueDate.
    /// </summary>
    [Required]
    public DateTime RequestedDueDate { get; set; }

    /// <summary>
    /// Optional message from the borrower to the lender explaining the request.
    /// </summary>
    [MaxLength(1000)]
    public string? Message { get; set; }
}