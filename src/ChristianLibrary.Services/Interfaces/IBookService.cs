using ChristianLibrary.Domain.Entities;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Interface for book management services
/// </summary>
public interface IBookService
{
    /// <summary>
    /// Gets a book by ID
    /// </summary>
    Task<Book?> GetBookByIdAsync(int id);
    
    /// <summary>
    /// Gets all books
    /// </summary>
    Task<List<Book>> GetAllBooksAsync();
    
    /// <summary>
    /// Creates a new book
    /// </summary>
    Task<Book> CreateBookAsync(Book book);
}