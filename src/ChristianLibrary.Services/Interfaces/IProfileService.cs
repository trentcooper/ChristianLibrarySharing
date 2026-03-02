using ChristianLibrary.Services.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Interface for user profile management services
/// </summary>
public interface IProfileService
{
    #region Location Management (US-03.02)
    
    /// <summary>
    /// Updates user's location/address
    /// </summary>
    Task<LocationResponse> UpdateLocationAsync(string userId, UpdateLocationRequest request);

    /// <summary>
    /// Clears user's location/address
    /// </summary>
    Task<bool> ClearLocationAsync(string userId);

    /// <summary>
    /// Gets user's current location
    /// </summary>
    Task<LocationResponse?> GetLocationAsync(string userId);

    #endregion

    #region Visibility Management (US-03.03)
    
    /// <summary>
    /// Updates user's profile visibility settings
    /// </summary>
    Task<VisibilityResponse> UpdateVisibilityAsync(string userId, UpdateVisibilityRequest request);

    /// <summary>
    /// Gets user's current visibility settings
    /// </summary>
    Task<VisibilityResponse?> GetVisibilityAsync(string userId);

    #endregion

    #region Profile Picture Management (US-03.04)
    
    /// <summary>
    /// Uploads and processes user's profile picture
    /// </summary>
    Task<ProfilePictureResponse> UploadProfilePictureAsync(string userId, IFormFile file);

    /// <summary>
    /// Deletes user's profile picture
    /// </summary>
    Task<bool> DeleteProfilePictureAsync(string userId);

    /// <summary>
    /// Gets user's current profile picture information
    /// </summary>
    Task<ProfilePictureResponse?> GetProfilePictureAsync(string userId);

    #endregion

    #region Notification Preferences (US-03.05)
    
    /// <summary>
    /// Updates user's notification preferences
    /// </summary>
    Task<NotificationPreferencesResponse> UpdateNotificationPreferencesAsync(
        string userId, 
        UpdateNotificationPreferencesRequest request);

    /// <summary>
    /// Gets user's current notification preferences
    /// </summary>
    Task<NotificationPreferencesResponse?> GetNotificationPreferencesAsync(string userId);

    #endregion
}