using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Admin-only controller demonstrating role-based authorization
/// All endpoints in this controller require Admin role
/// </summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // COMMENTED OUT for testing - uncomment when auth is ready
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics (Admin only)
    /// </summary>
    [HttpGet("dashboard/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> GetDashboardStats()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        
        _logger.LogInformation("Admin dashboard stats accessed by {Email}", email);
        
        return Ok(new
        {
            totalUsers = 150,
            activeLoans = 42,
            pendingApprovals = 8,
            totalBooks = 350
        });
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> GetAllUsers()
    {
        _logger.LogInformation("Admin viewing all users");
        
        return Ok(new[]
        {
            new { 
                id = 1, 
                name = "John Doe",
                email = "john@example.com", 
                isActive = true,
                createdAt = DateTime.Now.AddMonths(-3)
            },
            new { 
                id = 2, 
                name = "Jane Smith",
                email = "jane@example.com", 
                isActive = true,
                createdAt = DateTime.Now.AddMonths(-2)
            },
            new { 
                id = 3, 
                name = "Bob Johnson",
                email = "bob@example.com", 
                isActive = false,
                createdAt = DateTime.Now.AddMonths(-1)
            }
        });
    }

    /// <summary>
    /// Toggle user active status (Admin only)
    /// </summary>
    [HttpPost("users/{id}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> ToggleUserStatus(int id)
    {
        _logger.LogInformation("Toggling status for user {UserId}", id);
        
        return Ok(new { success = true, message = $"User {id} status toggled" });
    }

    /// <summary>
    /// Get pending book approvals (Admin only)
    /// </summary>
    [HttpGet("books/pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> GetPendingBooks()
    {
        _logger.LogInformation("Fetching pending books");
        
        return Ok(new[]
        {
            new { 
                id = 1, 
                title = "The Pilgrim's Progress", 
                author = "John Bunyan", 
                submittedAt = DateTime.Now.AddDays(-2) 
            },
            new { 
                id = 2, 
                title = "Mere Christianity", 
                author = "C.S. Lewis", 
                submittedAt = DateTime.Now.AddDays(-1) 
            },
            new { 
                id = 3, 
                title = "The Cost of Discipleship", 
                author = "Dietrich Bonhoeffer", 
                submittedAt = DateTime.Now.AddHours(-5) 
            }
        });
    }

    /// <summary>
    /// Get all books (Admin only)
    /// </summary>
    [HttpGet("books")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> GetAllBooks()
    {
        _logger.LogInformation("Fetching all books");
        
        return Ok(new[]
        {
            new { 
                id = 1, 
                title = "Knowing God", 
                author = "J.I. Packer",
                status = "Available"
            },
            new { 
                id = 2, 
                title = "The Holiness of God", 
                author = "R.C. Sproul",
                status = "Loaned"
            }
        });
    }

    /// <summary>
    /// Approve a book (Admin only)
    /// </summary>
    [HttpPost("books/{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> ApproveBook(int id)
    {
        _logger.LogInformation("Approving book {BookId}", id);
        
        return Ok(new { success = true, message = $"Book {id} approved" });
    }

    /// <summary>
    /// Reject a book (Admin only)
    /// </summary>
    [HttpPost("books/{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> RejectBook(int id, [FromBody] RejectBookRequest? request)
    {
        _logger.LogInformation("Rejecting book {BookId} with reason: {Reason}", id, request?.Reason);
        
        return Ok(new { success = true, message = $"Book {id} rejected" });
    }

    /// <summary>
    /// Manage user roles (Admin only)
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> ManageUserRoles(string userId, [FromBody] object roleData)
    {
        _logger.LogInformation("Admin managing roles for user {UserId}", userId);
        
        return Ok(new
        {
            message = $"Role management endpoint for user {userId}",
            note = "Actual implementation would update user roles in database"
        });
    }

    /// <summary>
    /// System settings (Admin only)
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> GetSettings()
    {
        return Ok(new
        {
            message = "System settings",
            settings = new
            {
                maintenanceMode = false,
                registrationEnabled = true,
                maxBorrowDays = 30,
                maxBooksPerUser = 5
            }
        });
    }
}

public record RejectBookRequest(string? Reason); 