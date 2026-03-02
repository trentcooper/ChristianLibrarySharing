using ChristianLibrary.Services.DTOs.Auth;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Handles authentication and user management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("POST /api/auth/register - Registration request for {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(AuthResponse.CreateFailure("Validation failed", errors));
        }

        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Authenticates a user and returns JWT token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("POST /api/auth/login - Login request for {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(AuthResponse.CreateFailure("Validation failed", errors));
        }

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Confirms user's email address
    /// </summary>
    /// <param name="userId">User ID from confirmation link</param>
    /// <param name="token">Email confirmation token</param>
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ConfirmEmail(
        [FromQuery] string userId, 
        [FromQuery] string token)
    {
        _logger.LogInformation("GET /api/auth/confirm-email - Request for userId: {UserId}", userId);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return BadRequest(AuthResponse.CreateFailure("Invalid confirmation link"));
        }

        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Resends email confirmation
    /// </summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        _logger.LogInformation("POST /api/auth/resend-confirmation - Request for {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(AuthResponse.CreateFailure("Validation failed", errors));
        }

        var result = await _authService.ResendConfirmationAsync(request);

        // Always return 200 OK to prevent email enumeration
        return Ok(result);
    }

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        _logger.LogInformation("POST /api/auth/forgot-password - Request for {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(AuthResponse.CreateFailure("Validation failed", errors));
        }

        var result = await _authService.ForgotPasswordAsync(request);

        return Ok(result);
    }

    /// <summary>
    /// Resets user's password with reset token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        _logger.LogInformation("POST /api/auth/reset-password - Request for {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(AuthResponse.CreateFailure("Validation failed", errors));
        }

        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Test endpoint - Requires authentication (any role)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email?.Split('@')[0] ?? "User";

        return Ok(new
        {
            id = userId,
            email,
            name,
            roles,
            message = "JWT authentication is working!"
        });
    }

    /// <summary>
    /// Test endpoint - Admin only
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> AdminOnly()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        
        return Ok(new
        {
            message = "Success! You have Admin access.",
            email,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint - Member only
    /// </summary>
    [HttpGet("member-only")]
    [Authorize(Roles = "Member")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> MemberOnly()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        
        return Ok(new
        {
            message = "Success! You have Member access.",
            email,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint - Admin or Member
    /// </summary>
    [HttpGet("admin-or-member")]
    [Authorize(Roles = "Admin,Member")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<object> AdminOrMember()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        
        return Ok(new
        {
            message = "Success! You have either Admin or Member access.",
            email,
            roles,
            timestamp = DateTime.UtcNow
        });
    }
}