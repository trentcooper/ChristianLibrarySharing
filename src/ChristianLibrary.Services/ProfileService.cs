using ChristianLibrary.Data.Context;
using ChristianLibrary.Services.DTOs.Profile;
using ChristianLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Service for user profile management operations
/// </summary>
public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileService> _logger;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly string _uploadsPath;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private const int MaxImageWidth = 800;
    private const int MaxImageHeight = 800;
    private const int ThumbnailSize = 150;

    public ProfileService(
        ApplicationDbContext context,
        ILogger<ProfileService> logger,
        IImageProcessingService imageProcessingService,
        string uploadsPath = "uploads/profile-pictures")
    {
        _context = context;
        _logger = logger;
        _imageProcessingService = imageProcessingService;
        _uploadsPath = uploadsPath;

        // Ensure uploads directory exists
        Directory.CreateDirectory(_uploadsPath);
    }

    #region Location Management (US-03.02)

    /// <summary>
    /// Updates user's location/address information
    /// </summary>
    public async Task<LocationResponse> UpdateLocationAsync(string userId, UpdateLocationRequest request)
    {
        _logger.LogInformation("Updating location for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogError("Profile not found for user {UserId}", userId);
            throw new InvalidOperationException("User profile not found");
        }

        // Update address fields
        profile.Street = request.Street;
        profile.City = request.City;
        profile.State = request.State;
        profile.ZipCode = request.ZipCode;
        profile.Country = request.Country;

        // Update coordinates if provided
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            profile.Latitude = request.Latitude;
            profile.Longitude = request.Longitude;
            _logger.LogInformation("Coordinates manually provided for user {UserId}", userId);
        }
        else if (!string.IsNullOrEmpty(request.City) && !string.IsNullOrEmpty(request.State))
        {
            // TODO: Future enhancement - geocode the address to get lat/long
            // For now, coordinates will remain null unless manually provided
            _logger.LogInformation("Address provided without coordinates - geocoding not yet implemented");
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Location updated successfully for user {UserId}", userId);

        return new LocationResponse
        {
            Street = profile.Street,
            City = profile.City,
            State = profile.State,
            ZipCode = profile.ZipCode,
            Country = profile.Country,
            Latitude = profile.Latitude,
            Longitude = profile.Longitude
        };
    }

    /// <summary>
    /// Clears user's location/address information
    /// </summary>
    public async Task<bool> ClearLocationAsync(string userId)
    {
        _logger.LogInformation("Clearing location for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogError("Profile not found for user {UserId}", userId);
            return false;
        }

        // Clear all location fields
        profile.Street = null;
        profile.City = null;
        profile.State = null;
        profile.ZipCode = null;
        profile.Country = null;
        profile.Latitude = null;
        profile.Longitude = null;

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Location cleared successfully for user {UserId}", userId);

        return true;
    }

    /// <summary>
    /// Gets user's current location information
    /// </summary>
    public async Task<LocationResponse?> GetLocationAsync(string userId)
    {
        _logger.LogInformation("Retrieving location for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogWarning("Profile not found for user {UserId}", userId);
            return null;
        }

        // Return null if no location data exists
        if (string.IsNullOrEmpty(profile.City) && 
            string.IsNullOrEmpty(profile.State) &&
            string.IsNullOrEmpty(profile.Street))
        {
            _logger.LogInformation("No location data for user {UserId}", userId);
            return null;
        }

        return new LocationResponse
        {
            Street = profile.Street,
            City = profile.City,
            State = profile.State,
            ZipCode = profile.ZipCode,
            Country = profile.Country,
            Latitude = profile.Latitude,
            Longitude = profile.Longitude
        };
    }

    #endregion

    #region Visibility Management (US-03.03)

    /// <summary>
    /// Updates user's profile visibility settings
    /// </summary>
    public async Task<VisibilityResponse> UpdateVisibilityAsync(string userId, UpdateVisibilityRequest request)
    {
        _logger.LogInformation("Updating visibility settings for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogError("Profile not found for user {UserId}", userId);
            throw new InvalidOperationException("User profile not found");
        }

        // Update only the fields that are provided (null = don't change)
        if (request.Visibility.HasValue)
        {
            profile.Visibility = request.Visibility.Value;
            _logger.LogInformation("Profile visibility set to {Visibility} for user {UserId}", 
                request.Visibility.Value, userId);
        }

        if (request.ShowFullName.HasValue)
            profile.ShowFullName = request.ShowFullName.Value;

        if (request.ShowEmail.HasValue)
            profile.ShowEmail = request.ShowEmail.Value;

        if (request.ShowPhone.HasValue)
            profile.ShowPhone = request.ShowPhone.Value;

        if (request.ShowExactAddress.HasValue)
            profile.ShowExactAddress = request.ShowExactAddress.Value;

        if (request.ShowCityState.HasValue)
            profile.ShowCityState = request.ShowCityState.Value;

        if (request.ShowDateOfBirth.HasValue)
            profile.ShowDateOfBirth = request.ShowDateOfBirth.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Visibility settings updated successfully for user {UserId}", userId);

        return new VisibilityResponse
        {
            Visibility = profile.Visibility,
            ShowFullName = profile.ShowFullName,
            ShowEmail = profile.ShowEmail,
            ShowPhone = profile.ShowPhone,
            ShowExactAddress = profile.ShowExactAddress,
            ShowCityState = profile.ShowCityState,
            ShowDateOfBirth = profile.ShowDateOfBirth
        };
    }

    /// <summary>
    /// Gets user's current visibility settings
    /// </summary>
    public async Task<VisibilityResponse?> GetVisibilityAsync(string userId)
    {
        _logger.LogInformation("Retrieving visibility settings for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogWarning("Profile not found for user {UserId}", userId);
            return null;
        }

        return new VisibilityResponse
        {
            Visibility = profile.Visibility,
            ShowFullName = profile.ShowFullName,
            ShowEmail = profile.ShowEmail,
            ShowPhone = profile.ShowPhone,
            ShowExactAddress = profile.ShowExactAddress,
            ShowCityState = profile.ShowCityState,
            ShowDateOfBirth = profile.ShowDateOfBirth
        };
    }

    #endregion

    #region Profile Picture Management (US-03.04)

    /// <summary>
    /// Uploads and processes user's profile picture
    /// </summary>
    public async Task<ProfilePictureResponse> UploadProfilePictureAsync(string userId, IFormFile file)
    {
        _logger.LogInformation("Uploading profile picture for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogError("Profile not found for user {UserId}", userId);
            throw new InvalidOperationException("User profile not found");
        }

        // Validate file
        using var fileStream = file.OpenReadStream();
        var (isValid, errorMessage) = await _imageProcessingService.ValidateImageAsync(fileStream, MaxFileSizeBytes);
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid image upload attempt for user {UserId}: {Error}", userId, errorMessage);
            throw new InvalidOperationException(errorMessage ?? "Invalid image file");
        }

        // Delete old profile picture if exists
        await DeleteOldProfilePictureFilesAsync(profile);

        // Generate unique filename
        var fileExtension = ".jpg"; // Always save as JPEG after processing
        var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
        var thumbnailFileName = $"{userId}_{Guid.NewGuid()}_thumb{fileExtension}";

        // CLOUD STORAGE NOTE:
        // When migrating to cloud storage (Azure Blob, AWS S3, etc.):
        // 1. Replace file paths with cloud URLs
        // 2. Upload to cloud storage instead of local file system
        // 3. Update _uploadsPath to be cloud container/bucket name
        // 4. Modify DeleteOldProfilePictureFilesAsync to delete from cloud
        // Example for Azure Blob:
        //   var blobClient = _blobContainerClient.GetBlobClient(fileName);
        //   await blobClient.UploadAsync(fileStream);
        //   profile.ProfilePictureUrl = blobClient.Uri.ToString();

        var fullImagePath = Path.Combine(_uploadsPath, fileName);
        var thumbnailPath = Path.Combine(_uploadsPath, thumbnailFileName);

        // Process and save images
        fileStream.Position = 0;
        var (width, height) = await _imageProcessingService.ResizeImageAsync(
            fileStream, 
            fullImagePath, 
            MaxImageWidth, 
            MaxImageHeight);

        fileStream.Position = 0;
        await _imageProcessingService.CreateThumbnailAsync(
            fileStream, 
            thumbnailPath, 
            ThumbnailSize);

        // Update profile with new picture paths
        // CLOUD STORAGE NOTE: These would be full URLs instead of relative paths
        profile.ProfilePictureUrl = $"/uploads/profile-pictures/{fileName}";
        profile.ProfilePictureThumbnailUrl = $"/uploads/profile-pictures/{thumbnailFileName}";
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile picture uploaded successfully for user {UserId}", userId);

        var fileInfo = new FileInfo(fullImagePath);

        return new ProfilePictureResponse
        {
            ProfilePictureUrl = profile.ProfilePictureUrl,
            ProfilePictureThumbnailUrl = profile.ProfilePictureThumbnailUrl,
            FileSizeBytes = fileInfo.Length,
            UploadedAt = DateTime.UtcNow,
            Dimensions = $"{width}x{height}"
        };
    }

    /// <summary>
    /// Deletes user's profile picture
    /// </summary>
    public async Task<bool> DeleteProfilePictureAsync(string userId)
    {
        _logger.LogInformation("Deleting profile picture for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogWarning("Profile not found for user {UserId}", userId);
            return false;
        }

        // Delete physical files
        await DeleteOldProfilePictureFilesAsync(profile);

        // Clear database references
        profile.ProfilePictureUrl = null;
        profile.ProfilePictureThumbnailUrl = null;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile picture deleted successfully for user {UserId}", userId);

        return true;
    }

    /// <summary>
    /// Gets user's current profile picture information
    /// </summary>
    public async Task<ProfilePictureResponse?> GetProfilePictureAsync(string userId)
    {
        _logger.LogInformation("Retrieving profile picture info for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogWarning("Profile not found for user {UserId}", userId);
            return null;
        }

        if (string.IsNullOrEmpty(profile.ProfilePictureUrl))
        {
            _logger.LogInformation("No profile picture for user {UserId}", userId);
            return null;
        }

        // Try to get file size if file exists locally
        // CLOUD STORAGE NOTE: For cloud storage, retrieve metadata from cloud service
        long? fileSize = null;
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", profile.ProfilePictureUrl.TrimStart('/'));
        if (File.Exists(localPath))
        {
            var fileInfo = new FileInfo(localPath);
            fileSize = fileInfo.Length;
        }

        return new ProfilePictureResponse
        {
            ProfilePictureUrl = profile.ProfilePictureUrl,
            ProfilePictureThumbnailUrl = profile.ProfilePictureThumbnailUrl,
            FileSizeBytes = fileSize,
            UploadedAt = profile.UpdatedAt
        };
    }

    /// <summary>
    /// Helper method to delete old profile picture files
    /// </summary>
    private async Task DeleteOldProfilePictureFilesAsync(Domain.Entities.UserProfile profile)
    {
        // CLOUD STORAGE NOTE:
        // When using cloud storage, replace file deletion with cloud API calls:
        // Example for Azure Blob:
        //   var blobClient = _blobContainerClient.GetBlobClient(extractedFileName);
        //   await blobClient.DeleteIfExistsAsync();

        if (!string.IsNullOrEmpty(profile.ProfilePictureUrl))
        {
            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", profile.ProfilePictureUrl.TrimStart('/'));
            if (File.Exists(oldFilePath))
            {
                try
                {
                    File.Delete(oldFilePath);
                    _logger.LogInformation("Deleted old profile picture: {FilePath}", oldFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old profile picture: {FilePath}", oldFilePath);
                }
            }
        }

        if (!string.IsNullOrEmpty(profile.ProfilePictureThumbnailUrl))
        {
            var oldThumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", profile.ProfilePictureThumbnailUrl.TrimStart('/'));
            if (File.Exists(oldThumbnailPath))
            {
                try
                {
                    File.Delete(oldThumbnailPath);
                    _logger.LogInformation("Deleted old thumbnail: {FilePath}", oldThumbnailPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old thumbnail: {FilePath}", oldThumbnailPath);
                }
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Notification Preferences (US-03.05)

    /// <summary>
    /// Updates user's notification preferences
    /// </summary>
    public async Task<NotificationPreferencesResponse> UpdateNotificationPreferencesAsync(
        string userId, 
        UpdateNotificationPreferencesRequest request)
    {
        _logger.LogInformation("Updating notification preferences for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogError("Profile not found for user {UserId}", userId);
            throw new InvalidOperationException("User profile not found");
        }

        // Update general notification settings
        if (request.EmailNotifications.HasValue)
            profile.EmailNotifications = request.EmailNotifications.Value;

        if (request.SmsNotifications.HasValue)
            profile.SmsNotifications = request.SmsNotifications.Value;

        if (request.PushNotifications.HasValue)
            profile.PushNotifications = request.PushNotifications.Value;

        // Update notification frequency
        if (request.NotificationFrequency.HasValue)
        {
            profile.NotificationFrequency = request.NotificationFrequency.Value;
            _logger.LogInformation("Notification frequency set to {Frequency} for user {UserId}", 
                request.NotificationFrequency.Value, userId);
        }

        // Update specific event notifications
        if (request.NotifyOnBorrowRequest.HasValue)
            profile.NotifyOnBorrowRequest = request.NotifyOnBorrowRequest.Value;

        if (request.NotifyOnRequestApproval.HasValue)
            profile.NotifyOnRequestApproval = request.NotifyOnRequestApproval.Value;

        if (request.NotifyOnRequestDenial.HasValue)
            profile.NotifyOnRequestDenial = request.NotifyOnRequestDenial.Value;

        if (request.NotifyOnDueDate.HasValue)
            profile.NotifyOnDueDate = request.NotifyOnDueDate.Value;

        if (request.NotifyOnReturn.HasValue)
            profile.NotifyOnReturn = request.NotifyOnReturn.Value;

        if (request.NotifyOnNewMessage.HasValue)
            profile.NotifyOnNewMessage = request.NotifyOnNewMessage.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification preferences updated successfully for user {UserId}", userId);

        return new NotificationPreferencesResponse
        {
            EmailNotifications = profile.EmailNotifications,
            SmsNotifications = profile.SmsNotifications,
            PushNotifications = profile.PushNotifications,
            NotificationFrequency = profile.NotificationFrequency,
            NotifyOnBorrowRequest = profile.NotifyOnBorrowRequest,
            NotifyOnRequestApproval = profile.NotifyOnRequestApproval,
            NotifyOnRequestDenial = profile.NotifyOnRequestDenial,
            NotifyOnDueDate = profile.NotifyOnDueDate,
            NotifyOnReturn = profile.NotifyOnReturn,
            NotifyOnNewMessage = profile.NotifyOnNewMessage
        };
    }

    /// <summary>
    /// Gets user's current notification preferences
    /// </summary>
    public async Task<NotificationPreferencesResponse?> GetNotificationPreferencesAsync(string userId)
    {
        _logger.LogInformation("Retrieving notification preferences for user {UserId}", userId);

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            _logger.LogWarning("Profile not found for user {UserId}", userId);
            return null;
        }

        return new NotificationPreferencesResponse
        {
            EmailNotifications = profile.EmailNotifications,
            SmsNotifications = profile.SmsNotifications,
            PushNotifications = profile.PushNotifications,
            NotificationFrequency = profile.NotificationFrequency,
            NotifyOnBorrowRequest = profile.NotifyOnBorrowRequest,
            NotifyOnRequestApproval = profile.NotifyOnRequestApproval,
            NotifyOnRequestDenial = profile.NotifyOnRequestDenial,
            NotifyOnDueDate = profile.NotifyOnDueDate,
            NotifyOnReturn = profile.NotifyOnReturn,
            NotifyOnNewMessage = profile.NotifyOnNewMessage
        };
    }

    #endregion
}