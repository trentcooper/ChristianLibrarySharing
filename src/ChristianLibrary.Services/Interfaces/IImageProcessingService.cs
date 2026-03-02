namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Service for image processing operations (resize, thumbnail generation, validation)
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Validates image file format and size
    /// </summary>
    /// <param name="file">Image file to validate</param>
    /// <param name="maxSizeBytes">Maximum allowed file size in bytes</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateImageAsync(Stream file, long maxSizeBytes);

    /// <summary>
    /// Resizes image to maximum dimensions while maintaining aspect ratio
    /// </summary>
    /// <param name="inputStream">Input image stream</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="maxWidth">Maximum width</param>
    /// <param name="maxHeight">Maximum height</param>
    /// <returns>Actual dimensions of resized image</returns>
    Task<(int Width, int Height)> ResizeImageAsync(
        Stream inputStream, 
        string outputPath, 
        int maxWidth, 
        int maxHeight);

    /// <summary>
    /// Creates square thumbnail with center crop
    /// </summary>
    /// <param name="inputStream">Input image stream</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="size">Thumbnail size (square)</param>
    Task CreateThumbnailAsync(Stream inputStream, string outputPath, int size);

    /// <summary>
    /// Gets image dimensions without loading entire file
    /// </summary>
    Task<(int Width, int Height)> GetImageDimensionsAsync(Stream imageStream);
}