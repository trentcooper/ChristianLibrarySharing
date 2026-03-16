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

    /// <summary>
    /// Returns all books matching the search criteria passed in to the function
    /// </summary>
    public async Task<List<Book>> SearchBooksAsync(
        string query,
        string? genre = null,
        bool availableOnly = false,
        string? condition = null,
        string? churchAffiliation = null,
        string sortBy = "relevance",
        string sortDirection = "asc")
    {
        _logger.LogInformation(
            "Searching books - query='{Query}', genre='{Genre}', availableOnly={AvailableOnly}, condition='{Condition}', church='{Church}', sortBy='{SortBy}', sortDirection='{SortDirection}'",
            query, genre, availableOnly, condition, churchAffiliation, sortBy, sortDirection);

        try
        {
            var searchTerm = query.Trim().ToLower();

            var booksQuery = _context.Books
                .Include(b => b.Owner)
                .ThenInclude(u => u.Profile)
                .Where(b => !b.IsDeleted)
                .Where(b =>
                    b.Title.ToLower().Contains(searchTerm) ||
                    b.Author.ToLower().Contains(searchTerm) ||
                    (b.Isbn != null && b.Isbn.ToLower().Contains(searchTerm)));

            if (!string.IsNullOrWhiteSpace(genre))
            {
                if (Enum.TryParse<BookGenre>(genre, ignoreCase: true, out var genreEnum))
                    booksQuery = booksQuery.Where(b => b.Genre == genreEnum);
                else
                    _logger.LogWarning("SearchBooks received unrecognized genre '{Genre}'", genre);
            }

            if (!string.IsNullOrWhiteSpace(condition))
            {
                if (Enum.TryParse<BookCondition>(condition, ignoreCase: true, out var conditionEnum))
                    booksQuery = booksQuery.Where(b => b.Condition == conditionEnum);
                else
                    _logger.LogWarning("SearchBooks received unrecognized condition '{Condition}'", condition);
            }

            if (availableOnly)
                booksQuery = booksQuery.Where(b => b.IsAvailable);

            if (!string.IsNullOrWhiteSpace(churchAffiliation))
            {
                var churchTerm = churchAffiliation.Trim().ToLower();
                booksQuery = booksQuery.Where(b =>
                    b.Owner.Profile != null &&
                    b.Owner.Profile.ChurchName != null &&
                    b.Owner.Profile.ChurchName.ToLower().Contains(churchTerm));
            }

            // Fetch then apply in-memory sorting
            var books = await booksQuery.ToListAsync();
            return ApplySorting(books, sortBy, sortDirection, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching books with query '{Query}'", query);
            return new List<Book>();
        }
    }

    /// <summary>
    /// Searches for books near a geographic location within a given radius
    /// </summary>
    public async Task<List<BookSearchResult>> SearchBooksNearLocationAsync(
        double latitude,
        double longitude,
        double radiusMiles,
        string? query = null,
        string? genre = null,
        bool availableOnly = false,
        string sortBy = "distance",
        string sortDirection = "asc")
    {
        _logger.LogInformation(
            "Searching books near ({Lat},{Lon}) within {Radius} miles - query='{Query}', sortBy='{SortBy}'",
            latitude, longitude, radiusMiles, query, sortBy);

        try
        {
            var booksQuery = _context.Books
                .Include(b => b.Owner)
                .ThenInclude(u => u.Profile)
                .Where(b => !b.IsDeleted && b.IsVisible)
                .Where(b => b.Owner.Profile != null &&
                            b.Owner.Profile.Latitude != null &&
                            b.Owner.Profile.Longitude != null);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchTerm = query.Trim().ToLower();
                booksQuery = booksQuery.Where(b =>
                    b.Title.ToLower().Contains(searchTerm) ||
                    b.Author.ToLower().Contains(searchTerm) ||
                    (b.Isbn != null && b.Isbn.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                if (Enum.TryParse<BookGenre>(genre, ignoreCase: true, out var genreEnum))
                    booksQuery = booksQuery.Where(b => b.Genre == genreEnum);
                else
                    _logger.LogWarning("SearchBooksNearLocation received unrecognized genre '{Genre}'", genre);
            }

            if (availableOnly)
                booksQuery = booksQuery.Where(b => b.IsAvailable);

            var books = await booksQuery.ToListAsync();

            var results = books
                .Select(b =>
                {
                    var ownerLat = (double)b.Owner.Profile!.Latitude!;
                    var ownerLon = (double)b.Owner.Profile!.Longitude!;
                    var distance = CalculateDistanceMiles(latitude, longitude, ownerLat, ownerLon);
                    return new BookSearchResult { Book = b, DistanceMiles = distance };
                })
                .Where(r => r.DistanceMiles <= radiusMiles)
                .ToList();

            // Apply sorting
            var descending = sortDirection.ToLower() == "desc";
            return sortBy.ToLower() switch
            {
                "title" => descending
                    ? results.OrderByDescending(r => r.Book.Title).ToList()
                    : results.OrderBy(r => r.Book.Title).ToList(),

                "author" => descending
                    ? results.OrderByDescending(r => r.Book.Author).ToList()
                    : results.OrderBy(r => r.Book.Author).ToList(),

                "dateadded" => descending
                    ? results.OrderByDescending(r => r.Book.CreatedAt).ToList()
                    : results.OrderBy(r => r.Book.CreatedAt).ToList(),

                // Default: distance ascending
                _ => descending
                    ? results.OrderByDescending(r => r.DistanceMiles).ToList()
                    : results.OrderBy(r => r.DistanceMiles).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error searching books near ({Lat},{Lon})", latitude, longitude);
            return new List<BookSearchResult>();
        }
    }


    // -------------------------------------------------------
// Sorting Helpers
// -------------------------------------------------------

    /// <summary>
    /// Applies sort order to a list of books based on the specified sort field and direction.
    /// Falls back to relevance sorting if an unrecognized sort field is provided.
    /// </summary>
    /// <param name="books">The list of books to sort</param>
    /// <param name="sortBy">Sort field: title, author, dateadded, or relevance (default)</param>
    /// <param name="sortDirection">Sort direction: asc (default) or desc</param>
    /// <param name="query">Original search query used for relevance scoring</param>
    private static List<Book> ApplySorting(
        List<Book> books,
        string sortBy,
        string sortDirection,
        string? query = null)
    {
        var descending = sortDirection.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "title" => descending
                ? books.OrderByDescending(b => b.Title).ToList()
                : books.OrderBy(b => b.Title).ToList(),

            "author" => descending
                ? books.OrderByDescending(b => b.Author).ToList()
                : books.OrderBy(b => b.Author).ToList(),

            "dateadded" => descending
                ? books.OrderByDescending(b => b.CreatedAt).ToList()
                : books.OrderBy(b => b.CreatedAt).ToList(),

            // Default: relevance scoring
            _ => ApplyRelevanceSort(books, query, descending)
        };
    }

    /// <summary>
    /// Scores and sorts books by relevance to the search query using a simple
    /// priority system: exact title match (3) > partial title match (2) >
    /// author match (1) > other matches (0). Falls back to title sort
    /// when no query is provided.
    /// </summary>
    /// <param name="books">The list of books to score and sort</param>
    /// <param name="query">The search term to score against</param>
    /// <param name="descending">When true, lowest scores appear first</param>
    private static List<Book> ApplyRelevanceSort(
        List<Book> books,
        string? query,
        bool descending)
    {
        if (string.IsNullOrWhiteSpace(query))
            return books.OrderBy(b => b.Title).ToList();

        var term = query.Trim().ToLower();

        var scored = books.Select(b => new
        {
            Book = b,
            Score = b.Title.ToLower() == term ? 3 : // exact title match
                b.Title.ToLower().Contains(term) ? 2 : // partial title match
                b.Author.ToLower().Contains(term) ? 1 : // author match
                0 // isbn or other match
        });

        return descending
            ? scored.OrderBy(x => x.Score).Select(x => x.Book).ToList()
            : scored.OrderByDescending(x => x.Score).Select(x => x.Book).ToList();
    }

// -------------------------------------------------------
// Geographic Helpers
// -------------------------------------------------------

    /// <summary>
    /// Calculates the straight-line distance in miles between two geographic
    /// coordinates using the Haversine formula. This accounts for the curvature
    /// of the Earth and is accurate for the distances typical in community
    /// book sharing (up to ~100 miles).
    /// Note: Returns straight-line distance, not driving distance.
    /// </summary>
    /// <param name="lat1">Latitude of the first point in decimal degrees</param>
    /// <param name="lon1">Longitude of the first point in decimal degrees</param>
    /// <param name="lat2">Latitude of the second point in decimal degrees</param>
    /// <param name="lon2">Longitude of the second point in decimal degrees</param>
    /// <returns>Distance in miles between the two coordinates</returns>
    private static double CalculateDistanceMiles(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double earthRadiusMiles = 3958.8;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMiles * c;
    }

    /// <summary>
    /// Converts an angle in decimal degrees to radians.
    /// Used internally by the Haversine distance calculation.
    /// </summary>
    /// <param name="degrees">Angle in decimal degrees</param>
    /// <returns>Equivalent angle in radians</returns>
    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}