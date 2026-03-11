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
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookService bookService, ILogger<BooksController> logger)
    {
        _bookService = bookService;
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
}