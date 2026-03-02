using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Test endpoints for development and verification
/// ⚠️ WARNING: Remove this controller before deploying to production!
/// </summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(ApplicationDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get user profile by user ID (for testing schema changes)
    /// </summary>
    [HttpGet("userprofile/{userId}")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfile>> GetUserProfile(string userId)
    {
        _logger.LogInformation("TEST: Getting user profile for userId: {UserId}", userId);

        var profile = await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new { message = "User profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    [HttpGet("userprofile/me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfile>> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("TEST: Getting profile for current user: {UserId}", userId);

        var profile = await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound(new { message = "User profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Get all user profiles (for testing)
    /// </summary>
    [HttpGet("userprofile")]
    [ProducesResponseType(typeof(List<UserProfile>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserProfile>>> GetAllProfiles()
    {
        _logger.LogInformation("TEST: Getting all user profiles");

        var profiles = await _context.UserProfiles
            .Include(p => p.User)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return Ok(profiles);
    }

    /// <summary>
    /// Get all books (for testing)
    /// </summary>
    [HttpGet("books")]
    [ProducesResponseType(typeof(List<Book>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Book>>> GetAllBooks()
    {
        _logger.LogInformation("TEST: Getting all books");

        var books = await _context.Books
            .Include(b => b.Owner)
            .OrderBy(b => b.Title)
            .ToListAsync();

        return Ok(books);
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetDatabaseStats()
    {
        _logger.LogInformation("TEST: Getting database statistics");

        var stats = new
        {
            users = await _context.Users.CountAsync(),
            profiles = await _context.UserProfiles.CountAsync(),
            books = await _context.Books.CountAsync(),
            roles = await _context.Roles.CountAsync(),
            timestamp = DateTime.UtcNow
        };

        return Ok(stats);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            timestamp = DateTime.UtcNow,
            message = "⚠️ Test endpoints are active - remember to remove before production!"
        });
    }
}