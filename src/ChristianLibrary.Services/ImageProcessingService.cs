using ChristianLibrary.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ChristianLibrary.Services;

/// <summary>
/// Image processing service using ImageSharp library
/// Handles validation, resizing, and thumbnail generation
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedMimeTypes = 
    { 
        "image/jpeg", 
        "image/png", 
        "image/gif", 
        "image/webp" 
    };

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates image format and size
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateImageAsync(Stream file, long maxSizeBytes)
    {
        try
        {
            // Check file size
            if (file.Length > maxSizeBytes)
            {
                return (false, $"File size ({file.Length} bytes) exceeds maximum allowed size ({maxSizeBytes} bytes)");
            }

            if (file.Length == 0)
            {
                return (false, "File is empty");
            }

            // Try to load as image
            file.Position = 0;
            var imageInfo = await Image.IdentifyAsync(file);
            
            if (imageInfo == null)
            {
                return (false, "File is not a valid image");
            }

            _logger.LogInformation("Image validated: {Format}, {Width}x{Height}", 
                imageInfo.Metadata.DecodedImageFormat?.Name, 
                imageInfo.Width, 
                imageInfo.Height);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image validation failed");
            return (false, "Invalid image file");
        }
    }

    /// <summary>
    /// Resizes image to fit within max dimensions while maintaining aspect ratio
    /// </summary>
    public async Task<(int Width, int Height)> ResizeImageAsync(
        Stream inputStream, 
        string outputPath, 
        int maxWidth, 
        int maxHeight)
    {
        _logger.LogInformation("Resizing image to max {MaxWidth}x{MaxHeight}", maxWidth, maxHeight);

        inputStream.Position = 0;
        using var image = await Image.LoadAsync(inputStream);

        // Calculate new dimensions maintaining aspect ratio
        var ratioX = (double)maxWidth / image.Width;
        var ratioY = (double)maxHeight / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        // Only resize if image is larger than max dimensions
        if (ratio < 1.0)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Mode = ResizeMode.Max
            }));

            _logger.LogInformation("Image resized from {OldWidth}x{OldHeight} to {NewWidth}x{NewHeight}",
                image.Width, image.Height, newWidth, newHeight);
        }

        // Save as JPEG with 85% quality for good balance of size/quality
        await image.SaveAsJpegAsync(outputPath, new JpegEncoder { Quality = 85 });

        _logger.LogInformation("Resized image saved to {OutputPath}", outputPath);

        return (image.Width, image.Height);
    }

    /// <summary>
    /// Creates square thumbnail with center crop
    /// </summary>
    public async Task CreateThumbnailAsync(Stream inputStream, string outputPath, int size)
    {
        _logger.LogInformation("Creating {Size}x{Size} thumbnail", size, size);

        inputStream.Position = 0;
        using var image = await Image.LoadAsync(inputStream);

        // Create square thumbnail with center crop
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop
        }));

        // Save as JPEG
        await image.SaveAsJpegAsync(outputPath, new JpegEncoder { Quality = 85 });

        _logger.LogInformation("Thumbnail saved to {OutputPath}", outputPath);
    }

    /// <summary>
    /// Gets image dimensions without fully loading the image
    /// </summary>
    public async Task<(int Width, int Height)> GetImageDimensionsAsync(Stream imageStream)
    {
        imageStream.Position = 0;
        var imageInfo = await Image.IdentifyAsync(imageStream);
        
        if (imageInfo == null)
        {
            throw new InvalidOperationException("Unable to read image dimensions");
        }

        return (imageInfo.Width, imageInfo.Height);
    }
}