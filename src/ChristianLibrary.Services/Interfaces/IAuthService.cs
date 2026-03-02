using ChristianLibrary.Services.DTOs.Auth;

namespace ChristianLibrary.Services.Interfaces;

/// <summary>
/// Interface for authentication and user management services
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user and generates JWT token
    /// </summary>
    Task<AuthResponse> LoginAsync(string email, string password);

    /// <summary>
    /// Initiates password reset process by generating reset token
    /// </summary>
    Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Resets user's password using reset token
    /// </summary>
    Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Confirms user's email address
    /// </summary>
    Task<AuthResponse> ConfirmEmailAsync(string userId, string token);

    /// <summary>
    /// Resends email confirmation token
    /// </summary>
    Task<AuthResponse> ResendConfirmationAsync(ResendConfirmationRequest request);
}