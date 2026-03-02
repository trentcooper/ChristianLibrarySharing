using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Auth;

/// <summary>
/// Data transfer object for user login requests
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Remember me for extended session
    /// </summary>
    public bool RememberMe { get; set; } = false;
}