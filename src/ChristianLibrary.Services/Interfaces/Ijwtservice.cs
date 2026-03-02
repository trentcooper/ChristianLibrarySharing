using System.Security.Claims;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="email">User's email address</param>
    /// <param name="roles">User's roles</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(string userId, string email, IList<string> roles);

    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts the user ID from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if found, null otherwise</returns>
    string? GetUserIdFromToken(string token);
}