using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Auth;

/// <summary>
/// Request to initiate password reset process
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Email address of the account to reset
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}