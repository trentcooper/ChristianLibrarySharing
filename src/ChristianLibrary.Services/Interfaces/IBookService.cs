using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Services.DTOs.Books;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Interface for book management services
/// </summary>
public interface IBookService
{
    /// <summary>
    /// Gets a book by ID
    /// </summary>
    Task<Book?> GetBookByIdAsync(int bookId);

    /// <summary>
    /// Gets all books
    /// </summary>
    Task<List<Book>> GetAllBooksAsync();

    /// <summary>
    /// Adds a new book manually to the authenticated user's catalog
    /// </summary>
    Task<BookResponse> AddBookAsync(CreateBookRequest request, string ownerId);
    
    /// <summary>
    /// Updates an existing book in the authenticated user's catalog
    /// </summary>
    Task<BookResponse> UpdateBookAsync(int bookId, UpdateBookRequest request, string ownerId);
    
    /// <summary>
    /// Soft deletes a book from the authenticated user's catalog
    /// </summary>
    Task<BookResponse> DeleteBookAsync(int bookId, string ownerId);
    
    /// <summary>
    /// Updates the availability status of a book
    /// </summary>
    Task<BookResponse> UpdateBookAvailabilityAsync(int bookId, bool isAvailable, string ownerId);
}