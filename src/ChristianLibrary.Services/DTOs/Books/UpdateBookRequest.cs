using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Books;

/// <summary>
/// Request DTO for updating an existing book in the catalog
/// </summary>
public class UpdateBookRequest
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public int? PageCount { get; set; }
    public string? Edition { get; set; }
    public string Language { get; set; } = "English";
    public BookGenre Genre { get; set; } = BookGenre.Other;
    public string? Description { get; set; }
    public BookCondition Condition { get; set; } = BookCondition.Good;
    public string? OwnerNotes { get; set; }
    public string? CoverImageUrl { get; set; }
}