using ChristianLibrary.Services.DTOs.Books;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Interface for ISBN lookup operations against external book APIs
/// </summary>
public interface IIsbnLookupService
{
    /// <summary>
    /// Looks up book details by ISBN using Open Library
    /// </summary>
    Task<IsbnLookupResponse> LookupByIsbnAsync(string isbn);
}