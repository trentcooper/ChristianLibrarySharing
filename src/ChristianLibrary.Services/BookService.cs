using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ChristianLibrary.Services.DTOs.Books;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Service handling book catalog operations
/// </summary>
public class BookService : IBookService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BookService> _logger;

    public BookService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<BookService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets a book by ID
    /// </summary>
    public async Task<Book?> GetBookByIdAsync(int bookId)
    {
        _logger.LogInformation("Getting book by ID: {BookId}", bookId);
        return await _context.Books.FindAsync(bookId);
    }

    /// <summary>
    /// Gets all books
    /// </summary>
    public async Task<List<Book>> GetAllBooksAsync()
    {
        _logger.LogInformation("Getting all books");
        return await _context.Books.ToListAsync();
    }

    /// <summary>
    /// Adds a new book manually to the authenticated user's catalog
    /// </summary>
    public async Task<BookResponse> AddBookAsync(CreateBookRequest request, string ownerId)
    {
        _logger.LogInformation("Adding book for user {OwnerId}: {Title}", ownerId, request.Title);

        try
        {
            var owner = await _userManager.FindByIdAsync(ownerId);
            if (owner == null)
            {
                _logger.LogWarning("AddBook failed: User {OwnerId} not found", ownerId);
                return BookResponse.CreateFailure("User not found");
            }

            if (!owner.IsActive)
            {
                _logger.LogWarning("AddBook failed: User {OwnerId} account is inactive", ownerId);
                return BookResponse.CreateFailure("Your account is inactive. Please contact support.");
            }

            var book = new Book
            {
                Title = request.Title.Trim(),
                Author = request.Author.Trim(),
                Isbn = request.ISBN?.Trim(),
                Publisher = request.Publisher?.Trim(),
                PublicationYear = request.PublicationYear,
                PageCount = request.PageCount,
                Edition = request.Edition?.Trim(),
                Language = request.Language.Trim(),
                Genre = request.Genre,
                Description = request.Description?.Trim(),
                Condition = request.Condition,
                OwnerNotes = request.OwnerNotes?.Trim(),
                IsAvailable = true,
                IsVisible = true,
                IsDeleted = false,
                BorrowCount = 0,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Book added successfully: {Title} (ID: {BookId}) for user {OwnerId}",
                book.Title, book.Id, ownerId);

            return BookResponse.CreateSuccess("Book added to your catalog successfully", book.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding book for user {OwnerId}", ownerId);
            return BookResponse.CreateFailure("An unexpected error occurred while adding the book");
        }
    }

    /// <summary>
    /// Updates an existing book — only the owner can edit their book
    /// </summary>
    public async Task<BookResponse> UpdateBookAsync(int bookId, UpdateBookRequest request, string ownerId)
    {
        _logger.LogInformation(
            "Update book {BookId} requested by user {OwnerId}", bookId, ownerId);

        try
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
            {
                _logger.LogWarning("UpdateBook failed: Book {BookId} not found", bookId);
                return BookResponse.CreateFailure($"Book with ID {bookId} not found");
            }

            if (book.IsDeleted)
            {
                _logger.LogWarning("UpdateBook failed: Book {BookId} is deleted", bookId);
                return BookResponse.CreateFailure($"Book with ID {bookId} not found");
            }

            if (book.OwnerId != ownerId)
            {
                _logger.LogWarning(
                    "UpdateBook failed: User {OwnerId} does not own book {BookId}",
                    ownerId, bookId);
                return BookResponse.CreateFailure("You do not have permission to edit this book");
            }

            // Apply updates
            book.Title = request.Title.Trim();
            book.Author = request.Author.Trim();
            book.Isbn = request.ISBN?.Trim();
            book.Publisher = request.Publisher?.Trim();
            book.PublicationYear = request.PublicationYear;
            book.PageCount = request.PageCount;
            book.Edition = request.Edition?.Trim();
            book.Language = request.Language.Trim();
            book.Genre = request.Genre;
            book.Description = request.Description?.Trim();
            book.Condition = request.Condition;
            book.OwnerNotes = request.OwnerNotes?.Trim();
            book.CoverImageUrl = request.CoverImageUrl?.Trim();
            book.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Book {BookId} updated successfully by user {OwnerId}", bookId, ownerId);

            return BookResponse.CreateSuccess("Book updated successfully", book.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating book {BookId} for user {OwnerId}",
                bookId, ownerId);
            return BookResponse.CreateFailure("An unexpected error occurred while updating the book");
        }
    }
    
    /// <summary>
    /// Soft deletes a book — only the owner can delete their book
    /// </summary>
    public async Task<BookResponse> DeleteBookAsync(int bookId, string ownerId)
    {
        _logger.LogInformation(
            "Delete book {BookId} requested by user {OwnerId}", bookId, ownerId);

        try
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null || book.IsDeleted)
            {
                _logger.LogWarning("DeleteBook failed: Book {BookId} not found", bookId);
                return BookResponse.CreateFailure($"Book with ID {bookId} not found");
            }

            if (book.OwnerId != ownerId)
            {
                _logger.LogWarning(
                    "DeleteBook failed: User {OwnerId} does not own book {BookId}",
                    ownerId, bookId);
                return BookResponse.CreateFailure("You do not have permission to delete this book");
            }

            if (!book.IsAvailable)
            {
                _logger.LogWarning(
                    "DeleteBook failed: Book {BookId} is currently on loan", bookId);
                return BookResponse.CreateFailure(
                    "This book cannot be deleted while it is currently on loan");
            }

            // Soft delete - preserve data for loan history integrity
            book.IsDeleted = true;
            book.IsAvailable = false;
            book.IsVisible = false;
            book.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Book {BookId} soft deleted by user {OwnerId}", bookId, ownerId);

            return BookResponse.CreateSuccess("Book removed from your catalog successfully", bookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting book {BookId} for user {OwnerId}",
                bookId, ownerId);
            return BookResponse.CreateFailure("An unexpected error occurred while deleting the book");
        }
    }
    
    /// <summary>
    /// Updates the availability status of a book — only the owner can change availability
    /// </summary>
    public async Task<BookResponse> UpdateBookAvailabilityAsync(int bookId, bool isAvailable, string ownerId)
    {
        _logger.LogInformation(
            "Availability update for book {BookId} to {IsAvailable} by user {OwnerId}",
            bookId, isAvailable, ownerId);

        try
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null || book.IsDeleted)
            {
                _logger.LogWarning(
                    "UpdateAvailability failed: Book {BookId} not found", bookId);
                return BookResponse.CreateFailure($"Book with ID {bookId} not found");
            }

            if (book.OwnerId != ownerId)
            {
                _logger.LogWarning(
                    "UpdateAvailability failed: User {OwnerId} does not own book {BookId}",
                    ownerId, bookId);
                return BookResponse.CreateFailure(
                    "You do not have permission to update this book");
            }

            book.IsAvailable = isAvailable;
            book.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var statusMessage = isAvailable ? "available" : "unavailable";
            _logger.LogInformation(
                "Book {BookId} marked as {Status} by user {OwnerId}",
                bookId, statusMessage, ownerId);

            return BookResponse.CreateSuccess(
                $"Book marked as {statusMessage} successfully", bookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error updating availability for book {BookId}", bookId);
            return BookResponse.CreateFailure(
                "An unexpected error occurred while updating book availability");
        }
    }
    
    /// <summary>
    /// Returns all books belonging to the authenticated user
    /// </summary>
    public async Task<List<Book>> GetMyBooksAsync(string ownerId)
    {
        _logger.LogInformation("Getting book collection for user {OwnerId}", ownerId);

        try
        {
            return await _context.Books
                .Where(b => b.OwnerId == ownerId && !b.IsDeleted)
                .OrderBy(b => b.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving books for user {OwnerId}", ownerId);
            return new List<Book>();
        }
    }
    
    public async Task<List<Book>> SearchBooksAsync(string query, string? genre = null, bool availableOnly = false)
    {
        _logger.LogInformation(
            "Searching books - query='{Query}', genre='{Genre}', availableOnly={AvailableOnly}",
            query, genre, availableOnly);

        try
        {
            var searchTerm = query.Trim().ToLower();

            var booksQuery = _context.Books
                .Where(b => !b.IsDeleted)
                .Where(b =>
                    b.Title.ToLower().Contains(searchTerm) ||
                    b.Author.ToLower().Contains(searchTerm) ||
                    (b.Isbn != null && b.Isbn.ToLower().Contains(searchTerm)));

            // Genre filter - parse string to enum if provided
            if (!string.IsNullOrWhiteSpace(genre))
            {
                if (Enum.TryParse<BookGenre>(genre, ignoreCase: true, out var genreEnum))
                    booksQuery = booksQuery.Where(b => b.Genre == genreEnum);
                else
                    _logger.LogWarning("SearchBooks received unrecognized genre value '{Genre}'", genre);
            }

            if (availableOnly)
                booksQuery = booksQuery.Where(b => b.IsAvailable);

            return await booksQuery
                .OrderBy(b => b.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching books with query '{Query}'", query);
            return new List<Book>();
        }
    }
}