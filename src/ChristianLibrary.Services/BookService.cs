using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
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
                ISBN = request.ISBN?.Trim(),
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
            book.ISBN = request.ISBN?.Trim();
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
}