using ChristianLibrary.Services.DTOs.Profile;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChristianLibrary.API.Controllers;

/// <summary>
/// Controller for user profile management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    #region Location Management (US-03.02)

    /// <summary>
    /// Update current user's location/address
    /// </summary>
    [HttpPut("location")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LocationResponse>> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("PUT /api/profile/location - User {UserId} updating location", userId);

        try
        {
            var result = await _profileService.UpdateLocationAsync(userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update location for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clear current user's location/address
    /// </summary>
    [HttpDelete("location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ClearLocation()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("DELETE /api/profile/location - User {UserId} clearing location", userId);

        var result = await _profileService.ClearLocationAsync(userId);

        if (!result)
        {
            return NotFound(new { message = "User profile not found" });
        }

        return Ok(new { message = "Location cleared successfully" });
    }

    /// <summary>
    /// Get current user's location/address
    /// </summary>
    [HttpGet("location")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> GetLocation()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("GET /api/profile/location - User {UserId} retrieving location", userId);

        var result = await _profileService.GetLocationAsync(userId);

        if (result == null)
        {
            return NotFound(new { message = "No location data found" });
        }

        return Ok(result);
    }

    #endregion

    #region Visibility Management (US-03.03)

    /// <summary>
    /// Update current user's profile visibility settings
    /// </summary>
    [HttpPut("visibility")]
    [ProducesResponseType(typeof(VisibilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VisibilityResponse>> UpdateVisibility([FromBody] UpdateVisibilityRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("PUT /api/profile/visibility - User {UserId} updating visibility settings", userId);

        try
        {
            var result = await _profileService.UpdateVisibilityAsync(userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update visibility for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's visibility settings
    /// </summary>
    [HttpGet("visibility")]
    [ProducesResponseType(typeof(VisibilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisibilityResponse>> GetVisibility()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("GET /api/profile/visibility - User {UserId} retrieving visibility settings", userId);

        var result = await _profileService.GetVisibilityAsync(userId);

        if (result == null)
        {
            return NotFound(new { message = "User profile not found" });
        }

        return Ok(result);
    }

    #endregion

    #region Profile Picture Management (US-03.04)

    /// <summary>
    /// Upload current user's profile picture
    /// </summary>
    /// <remarks>
    /// Accepts JPEG, PNG, GIF, WebP formats. Maximum file size: 5MB.
    /// Image will be resized to 800x800 max and a 150x150 thumbnail will be created.
    /// </remarks>
    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProfilePictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfilePictureResponse>> UploadProfilePicture(IFormFile file)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        _logger.LogInformation("POST /api/profile/picture - User {UserId} uploading profile picture ({Size} bytes)", 
            userId, file.Length);

        try
        {
            var result = await _profileService.UploadProfilePictureAsync(userId, file);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete current user's profile picture
    /// </summary>
    [HttpDelete("picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProfilePicture()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("DELETE /api/profile/picture - User {UserId} deleting profile picture", userId);

        var result = await _profileService.DeleteProfilePictureAsync(userId);

        if (!result)
        {
            return NotFound(new { message = "User profile not found" });
        }

        return Ok(new { message = "Profile picture deleted successfully" });
    }

    /// <summary>
    /// Get current user's profile picture information
    /// </summary>
    [HttpGet("picture")]
    [ProducesResponseType(typeof(ProfilePictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfilePictureResponse>> GetProfilePicture()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("GET /api/profile/picture - User {UserId} retrieving profile picture info", userId);

        var result = await _profileService.GetProfilePictureAsync(userId);

        if (result == null)
        {
            return NotFound(new { message = "No profile picture found" });
        }

        return Ok(result);
    }

    #endregion

    #region Notification Preferences (US-03.05)

    /// <summary>
    /// Update current user's notification preferences
    /// </summary>
    [HttpPut("notifications")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("PUT /api/profile/notifications - User {UserId} updating notification preferences", userId);

        try
        {
            var result = await _profileService.UpdateNotificationPreferencesAsync(userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update notification preferences for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's notification preferences
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetNotificationPreferences()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("GET /api/profile/notifications - User {UserId} retrieving notification preferences", userId);

        var result = await _profileService.GetNotificationPreferencesAsync(userId);

        if (result == null)
        {
            return NotFound(new { message = "User profile not found" });
        }

        return Ok(result);
    }

    #endregion
}