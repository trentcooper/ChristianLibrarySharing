using ChristianLibrary.Services.DTOs.Auth;
using ChristianLibrary.Services.Interfaces;
using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Services;

/// <summary>
/// Service handling authentication and user management operations
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user with profile and sends email confirmation
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already registered", request.Email);
                return AuthResponse.CreateFailure("Email address is already registered");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = false, // Will be set to true after email confirmation
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning(
                    "Registration failed for {Email}: {Errors}",
                    request.Email,
                    string.Join(", ", errors)
                );
                return AuthResponse.CreateFailure("Registration failed", errors);
            }

            _logger.LogInformation("User created successfully: {Email} (ID: {UserId})", user.Email, user.Id);

            await _userManager.AddToRoleAsync(user, "Member");
            _logger.LogInformation("Assigned 'Member' role to user {Email}", user.Email);

            var profile = new UserProfile
            {
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Bio = request.Bio,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User profile created for user {Email}", user.Email);

            var roles = await _userManager.GetRolesAsync(user);

            // Generate email confirmation token
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // TODO: In production, send email with confirmation link
            // For now, we'll just log it so you can test
            _logger.LogWarning(
                "EMAIL CONFIRMATION TOKEN for {Email} (UserId: {UserId}): {Token} (This should be sent via email in production!)",
                request.Email,
                user.Id,
                confirmationToken
            );

            return AuthResponse.CreateSuccess(
                "Registration successful. Please check your email to confirm your account.",
                user.Email,
                user.Id,
                roles.ToList()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
            return AuthResponse.CreateFailure("An unexpected error occurred during registration");
        }
    }

    /// <summary>
    /// Authenticates user and generates JWT token
    /// </summary>
    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        _logger.LogInformation("Login attempt for email: {Email}", email);

        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Email} not found", email);
                return AuthResponse.CreateFailure("Invalid email or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: User {Email} account is inactive", email);
                return AuthResponse.CreateFailure("Your account has been deactivated. Please contact support.");
            }

            // OPTIONAL: Uncomment to require email confirmation before login
            // if (!user.EmailConfirmed)
            // {
            //     _logger.LogWarning("Login failed: User {Email} email not confirmed", email);
            //     return AuthResponse.CreateFailure("Please confirm your email address before logging in.");
            // }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login failed: User {Email} account is locked", email);
                return AuthResponse.CreateFailure("Account is locked due to multiple failed login attempts. Please try again later.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account locked after failed login attempt", email);
                    return AuthResponse.CreateFailure("Account locked due to multiple failed attempts. Please try again in 15 minutes.");
                }

                _logger.LogWarning("Login failed: Invalid password for {Email}", email);
                return AuthResponse.CreateFailure("Invalid email or password");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user.Id, user.Email!, roles);
            var tokenExpiration = DateTime.UtcNow.AddMinutes(60);

            _logger.LogInformation("User {Email} logged in successfully", email);

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Email = user.Email,
                UserId = user.Id,
                Token = token,
                TokenExpiration = tokenExpiration,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", email);
            return AuthResponse.CreateFailure("An unexpected error occurred during login");
        }
    }

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return AuthResponse.CreateSuccess(
                    "If an account with that email exists, a password reset link has been sent.",
                    request.Email,
                    string.Empty
                );
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            _logger.LogWarning(
                "PASSWORD RESET TOKEN for {Email}: {Token} (This should be sent via email in production!)",
                request.Email,
                resetToken
            );

            _logger.LogInformation("Password reset token generated for {Email}", request.Email);

            return AuthResponse.CreateSuccess(
                "If an account with that email exists, a password reset link has been sent.",
                request.Email,
                user.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for {Email}", request.Email);
            return AuthResponse.CreateFailure("An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Resets user's password using reset token
    /// </summary>
    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", request.Email);

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed: User {Email} not found", request.Email);
                return AuthResponse.CreateFailure("Invalid email or reset token");
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning(
                    "Password reset failed for {Email}: {Errors}",
                    request.Email,
                    string.Join(", ", errors)
                );
                return AuthResponse.CreateFailure("Password reset failed. The reset token may be invalid or expired.", errors);
            }

            _logger.LogInformation("Password successfully reset for {Email}", request.Email);

            await _userManager.ResetAccessFailedCountAsync(user);

            return AuthResponse.CreateSuccess(
                "Password has been reset successfully. You can now login with your new password.",
                user.Email!,
                user.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", request.Email);
            return AuthResponse.CreateFailure("An error occurred while resetting your password");
        }
    }

    /// <summary>
    /// Confirms user's email address
    /// </summary>
    public async Task<AuthResponse> ConfirmEmailAsync(string userId, string token)
    {
        _logger.LogInformation("Email confirmation attempt for userId: {UserId}", userId);

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation failed: User {UserId} not found", userId);
                return AuthResponse.CreateFailure("Invalid confirmation link");
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for user {Email}", user.Email);
                return AuthResponse.CreateSuccess(
                    "Email address has already been confirmed.",
                    user.Email!,
                    user.Id
                );
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning(
                    "Email confirmation failed for {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", errors)
                );
                return AuthResponse.CreateFailure("Email confirmation failed. The token may be invalid or expired.", errors);
            }

            _logger.LogInformation("Email confirmed successfully for {Email}", user.Email);

            return AuthResponse.CreateSuccess(
                "Email confirmed successfully! You can now login.",
                user.Email!,
                user.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for userId: {UserId}", userId);
            return AuthResponse.CreateFailure("An error occurred while confirming your email");
        }
    }

    /// <summary>
    /// Resends email confirmation token
    /// </summary>
    public async Task<AuthResponse> ResendConfirmationAsync(ResendConfirmationRequest request)
    {
        _logger.LogInformation("Resend confirmation requested for email: {Email}", request.Email);

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            // SECURITY: Always return success even if user doesn't exist
            if (user == null)
            {
                _logger.LogWarning("Resend confirmation requested for non-existent email: {Email}", request.Email);
                return AuthResponse.CreateSuccess(
                    "If an account with that email exists and is not confirmed, a confirmation email has been sent.",
                    request.Email,
                    string.Empty
                );
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Resend confirmation requested for already confirmed email: {Email}", request.Email);
                return AuthResponse.CreateSuccess(
                    "If an account with that email exists and is not confirmed, a confirmation email has been sent.",
                    request.Email,
                    user.Id
                );
            }

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            _logger.LogWarning(
                "EMAIL CONFIRMATION TOKEN for {Email} (UserId: {UserId}): {Token} (This should be sent via email in production!)",
                request.Email,
                user.Id,
                confirmationToken
            );

            return AuthResponse.CreateSuccess(
                "If an account with that email exists and is not confirmed, a confirmation email has been sent.",
                request.Email,
                user.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending confirmation email for {Email}", request.Email);
            return AuthResponse.CreateFailure("An error occurred while processing your request");
        }
    }
}