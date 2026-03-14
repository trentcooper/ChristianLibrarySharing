namespace ChristianLibrary.Services.DTOs.Books;

/// <summary>
/// Response DTO for Isbn lookup results from Open Library
/// </summary>
public class IsbnLookupResponse
{
    public bool Found { get; set; }
    public string Message { get; set; } = string.Empty;

    // Bibliographic data returned from Open Library
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public int? PageCount { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Isbn { get; set; }
    public string? Language { get; set; }

    public static IsbnLookupResponse CreateFound(
        string title,
        string? author,
        string? publisher,
        int? publicationYear,
        int? pageCount,
        string? description,
        string? coverImageUrl,
        string? isbn,
        string? language) =>
        new()
        {
            Found = true,
            Message = "Book found",
            Title = title,
            Author = author,
            Publisher = publisher,
            PublicationYear = publicationYear,
            PageCount = pageCount,
            Description = description,
            CoverImageUrl = coverImageUrl,
            Isbn = isbn,
            Language = language
        };

    public static IsbnLookupResponse CreateNotFound(string isbn) =>
        new()
        {
            Found = false,
            Message = $"No book found for Isbn {isbn}"
        };

    public static IsbnLookupResponse CreateError(string message) =>
        new()
        {
            Found = false,
            Message = message
        };
}