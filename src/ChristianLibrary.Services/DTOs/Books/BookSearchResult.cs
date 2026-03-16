using ChristianLibrary.Domain.Entities;

namespace ChristianLibrary.Services.DTOs.Books;

/// <summary>
/// Represents a book search result with optional distance information
/// </summary>
public class BookSearchResult
{
    public Book Book { get; set; } = null!;
    public double? DistanceMiles { get; set; }
}