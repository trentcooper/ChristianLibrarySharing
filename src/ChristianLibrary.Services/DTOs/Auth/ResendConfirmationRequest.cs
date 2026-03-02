using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Auth;

/// <summary>
/// Request to resend email confirmation
/// </summary>
public class ResendConfirmationRequest
{
    /// <summary>
    /// Email address of the account
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}