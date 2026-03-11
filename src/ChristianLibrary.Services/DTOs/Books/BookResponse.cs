using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Books;

/// <summary>
/// Response DTO for book operations
/// </summary>
public class BookResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    // Book data
    public int? BookId { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public int? PageCount { get; set; }
    public string? Edition { get; set; }
    public string? Language { get; set; }
    public BookGenre? Genre { get; set; }
    public string? Description { get; set; }
    public BookCondition? Condition { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsVisible { get; set; }
    public string? OwnerNotes { get; set; }
    public string? CoverImageUrl { get; set; }
    public int? BorrowCount { get; set; }
    public decimal? AverageRating { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? OwnerId { get; set; }

    public static BookResponse CreateSuccess(string message, int bookId) =>
        new()
        {
            Success = true,
            Message = message,
            BookId = bookId
        };

    public static BookResponse CreateFailure(string message, List<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
}