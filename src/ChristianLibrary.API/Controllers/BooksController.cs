using ChristianLibrary.Services.DTOs.Books;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Controller for book catalog operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IIsbnLookupService _isbnLookupService;
    private readonly ILogger<BooksController> _logger;
    

    public BooksController(
        IBookService bookService,
        IIsbnLookupService isbnLookupService,
        ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _isbnLookupService = isbnLookupService;
        _logger = logger;
    }

    /// <summary>
    /// Get all books
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("GET /api/books - Retrieving all books");
        var books = await _bookService.GetAllBooksAsync();
        return Ok(books);
    }

    /// <summary>
    /// Get a book by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("GET /api/books/{BookId} - Retrieving book", id);

        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null)
        {
            _logger.LogWarning("Book {BookId} not found", id);
            return NotFound(new { message = $"Book with ID {id} not found" });
        }

        return Ok(book);
    }

    /// <summary>
    /// Adds a new book manually to the authenticated user's catalog
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddBook([FromBody] CreateBookRequest request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(ownerId))
        {
            _logger.LogWarning("AddBook failed: Unable to resolve user identity from token");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(BookResponse.CreateFailure("Title is required"));

        if (string.IsNullOrWhiteSpace(request.Author))
            return BadRequest(BookResponse.CreateFailure("Author is required"));

        var response = await _bookService.AddBookAsync(request, ownerId);

        if (!response.Success)
        {
            _logger.LogWarning("AddBook failed for user {OwnerId}: {Message}", ownerId, response.Message);
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetById), new { id = response.BookId }, response);
    }
    
    /// <summary>
    /// Looks up book details by ISBN from Open Library
    /// </summary>
    [HttpGet("isbn/{isbn}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IsbnLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IsbnLookupResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IsbnLookupResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LookupByIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return BadRequest(IsbnLookupResponse.CreateError("ISBN is required"));

        _logger.LogInformation("GET /api/books/isbn/{Isbn} - Looking up book", isbn);

        var result = await _isbnLookupService.LookupByIsbnAsync(isbn);

        if (!result.Found)
            return NotFound(result);

        return Ok(result);
    }
    
    /// <summary>
    /// Updates an existing book in the authenticated user's catalog
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookRequest request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(ownerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(BookResponse.CreateFailure("Title is required"));

        if (string.IsNullOrWhiteSpace(request.Author))
            return BadRequest(BookResponse.CreateFailure("Author is required"));

        var response = await _bookService.UpdateBookAsync(id, request, ownerId);

        if (!response.Success)
        {
            _logger.LogWarning(
                "UpdateBook failed for book {BookId} by user {OwnerId}: {Message}",
                id, ownerId, response.Message);

            // Distinguish between not found and permission denied
            if (response.Message.Contains("not found"))
                return NotFound(response);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }
    
    /// <summary>
    /// Soft deletes a book from the authenticated user's catalog
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(ownerId))
            return Unauthorized();

        var response = await _bookService.DeleteBookAsync(id, ownerId);

        if (!response.Success)
        {
            _logger.LogWarning(
                "DeleteBook failed for book {BookId} by user {OwnerId}: {Message}",
                id, ownerId, response.Message);

            if (response.Message.Contains("not found"))
                return NotFound(response);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }
    
    /// <summary>
    /// Updates the availability status of a book
    /// </summary>
    [HttpPatch("{id}/availability")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAvailability(int id, [FromBody] bool isAvailable)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(ownerId))
            return Unauthorized();

        var response = await _bookService.UpdateBookAvailabilityAsync(id, isAvailable, ownerId);

        if (!response.Success)
        {
            _logger.LogWarning(
                "UpdateAvailability failed for book {BookId} by user {OwnerId}: {Message}",
                id, ownerId, response.Message);

            if (response.Message.Contains("not found"))
                return NotFound(response);

            if (response.Message.Contains("permission"))
                return StatusCode(StatusCodes.Status403Forbidden, response);

            return BadRequest(response);
        }

        return Ok(response);
    }
}