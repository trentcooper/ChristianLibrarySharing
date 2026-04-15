using ChristianLibrary.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.BorrowRequests;

/// <summary>
/// Request DTO for marking a book as picked up/checked out
/// Maps to US-06.06: Mark book as picked up/borrowed
/// </summary>
public class MarkPickedUpRequest
{
    /// <summary>
    /// Condition of the book at time of checkout
    /// Required to establish baseline for dispute resolution
    /// </summary>
    public BookCondition ConditionAtCheckout { get; set; }

    /// <summary>
    /// Optional notes from lender at time of handoff
    /// </summary>
    [MaxLength(1000)]
    public string? LenderNotes { get; set; }
}