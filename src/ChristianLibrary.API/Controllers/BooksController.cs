using Microsoft.AspNetCore.Mvc;
using ChristianLibrary.Services.Interfaces;
using ChristianLibrary.Domain.Entities;

namespace ChristianLibrary.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<ActionResult<List<Book>>> GetAll()
    {
        _logger.LogInformation("GET /api/books - Retrieving all books");
        
        var books = await _bookService.GetAllBooksAsync();
        return Ok(books);
    }

    /// <summary>
    /// Get a book by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Book>> GetById(int id)
    {
        _logger.LogInformation("GET /api/books/{BookId} - Retrieving book", id);
        
        var book = await _bookService.GetBookByIdAsync(id);
        
        if (book == null)
        {
            _logger.LogWarning("Book {BookId} not found, returning 404", id);
            return NotFound(new { message = $"Book with ID {id} not found" });
        }
        
        return Ok(book);
    }

    /// <summary>
    /// Create a new book
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Book>> Create([FromBody] Book book)
    {
        _logger.LogInformation(
            "POST /api/books - Creating book: {Title}",
            book.Title
        );
        
        if (string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author))
        {
            _logger.LogWarning("Invalid book data provided");
            return BadRequest(new { message = "Title and Author are required" });
        }
        
        var createdBook = await _bookService.CreateBookAsync(book);
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = createdBook.Id },
            createdBook
        );
    }
}
