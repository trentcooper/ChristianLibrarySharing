namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Response containing profile picture URLs
/// </summary>
public class ProfilePictureResponse
{
    /// <summary>
    /// URL to full-size profile picture
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// URL to thumbnail profile picture
    /// </summary>
    public string? ProfilePictureThumbnailUrl { get; set; }

    /// <summary>
    /// File size in bytes (for full-size image)
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime? UploadedAt { get; set; }

    /// <summary>
    /// Image dimensions (width x height)
    /// </summary>
    public string? Dimensions { get; set; }
}