namespace ChristianLibrary.Services.DTOs.Auth;

/// <summary>
/// Data transfer object for authentication responses
/// Used for registration, login, and other auth operations
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's ID (GUID)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// JWT token (will be populated in US-02.03)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Token expiration date (will be populated in US-02.03)
    /// </summary>
    public DateTime? TokenExpiration { get; set; }

    /// <summary>
    /// List of errors if operation failed
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// User's assigned roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static AuthResponse CreateSuccess(string message, string email, string userId, List<string>? roles = null)
    {
        return new AuthResponse
        {
            Success = true,
            Message = message,
            Email = email,
            UserId = userId,
            Roles = roles ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a failed response with a single error message
    /// </summary>
    public static AuthResponse CreateFailure(string message)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message,
            Errors = new List<string> { message }
        };
    }

    /// <summary>
    /// Creates a failed response with multiple error messages
    /// </summary>
    public static AuthResponse CreateFailure(string message, List<string> errors)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}