using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace ChristianLibrary.Services;

/// <summary>
/// Service for book management operations
/// </summary>
public class BookService : IBookService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookService> _logger;

    public BookService(ApplicationDbContext context, ILogger<BookService> logger)
    {
        _context = context;
        _logger = logger;
        _logger.LogInformation("BookService initialized");
    }

    /// <summary>
    /// Gets a book by ID
    /// </summary>
    public async Task<Book?> GetBookByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving book with {BookId}", id);
        
        try
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);
            
            if (book == null)
            {
                _logger.LogWarning("Book with {BookId} not found", id);
                return null;
            }
            
            _logger.LogInformation(
                "Successfully retrieved book {BookId}: {Title} by {Author}",
                book.Id,
                book.Title,
                book.Author
            );
            
            return book;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving book {BookId}",
                id
            );
            throw;
        }
    }

    /// <summary>
    /// Gets all books from database
    /// </summary>
    public async Task<List<Book>> GetAllBooksAsync()
    {
        _logger.LogInformation("Retrieving all books from database");
        
        try
        {
            var books = await _context.Books
                .OrderBy(b => b.Title)
                .ToListAsync();
            
            _logger.LogInformation(
                "Successfully retrieved {BookCount} books from database",
                books.Count
            );
            
            return books;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all books");
            throw;
        }
    }

    /// <summary>
    /// Creates a new book
    /// </summary>
    public async Task<Book> CreateBookAsync(Book book)
    {
        _logger.LogInformation(
            "Creating new book: {Title} by {Author}",
            book.Title,
            book.Author
        );
        
        try
        {
            book.CreatedAt = DateTime.UtcNow;
            book.UpdatedAt = DateTime.UtcNow;
            
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Successfully created book {BookId}: {Title}",
                book.Id,
                book.Title
            );
            
            return book;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while creating book: {Title}",
                book.Title
            );
            throw;
        }
    }
}