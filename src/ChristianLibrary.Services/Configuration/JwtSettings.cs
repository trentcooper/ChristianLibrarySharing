namespace ChristianLibrary.Services.Configuration;

/// <summary>
/// Configuration settings for JWT token generation and validation
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key used to sign JWT tokens
    /// IMPORTANT: Use a strong, random key in production (minimum 32 characters)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (typically your application name or domain)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (typically your application or API endpoint)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// Default: 60 minutes (1 hour)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// Default: 7 days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}