using ChristianLibrary.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ChristianLibrary.API.Configuration;

/// <summary>
/// Centralized configuration for ASP.NET Core Identity
/// Handles user identity, password policies, lockout settings, and token configuration
/// </summary>
public static class IdentityConfig
{
    /// <summary>
    /// Configures ASP.NET Core Identity services with security best practices
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings - Strong password requirements
            ConfigurePasswordOptions(options.Password);

            // Lockout settings - Protect against brute force attacks
            ConfigureLockoutOptions(options.Lockout);

            // User settings - Validation rules
            ConfigureUserOptions(options.User);

            // Sign-in settings
            ConfigureSignInOptions(options.SignIn);

            // Token settings
            ConfigureTokenOptions(options.Tokens);
        })
        .AddEntityFrameworkStores<Data.Context.ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Configure password complexity requirements
    /// </summary>
    private static void ConfigurePasswordOptions(PasswordOptions options)
    {
        // Require at least one digit (0-9)
        options.RequireDigit = true;

        // Require at least one lowercase letter (a-z)
        options.RequireLowercase = true;

        // Require at least one uppercase letter (A-Z)
        options.RequireUppercase = true;

        // Require at least one non-alphanumeric character (!@#$%^&* etc.)
        options.RequireNonAlphanumeric = true;

        // Minimum password length
        options.RequiredLength = 8;

        // Require at least this many unique characters in password
        options.RequiredUniqueChars = 4;
    }

    /// <summary>
    /// Configure account lockout settings to prevent brute force attacks
    /// </summary>
    private static void ConfigureLockoutOptions(LockoutOptions options)
    {
        // Enable account lockout functionality
        options.AllowedForNewUsers = true;

        // Lock account for 15 minutes after too many failed attempts
        options.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // Lock account after 5 failed login attempts
        options.MaxFailedAccessAttempts = 5;
    }

    /// <summary>
    /// Configure user account validation rules
    /// </summary>
    private static void ConfigureUserOptions(UserOptions options)
    {
        // Require unique email addresses (one email per account)
        options.RequireUniqueEmail = true;

        // Allowed characters in username
        // Default allows letters, digits, and some special characters
        options.AllowedUserNameCharacters = 
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    }

    /// <summary>
    /// Configure sign-in requirements
    /// </summary>
    private static void ConfigureSignInOptions(SignInOptions options)
    {
        // Require confirmed email before allowing login
        // Set to false for development, true for production
        options.RequireConfirmedEmail = false; // TODO: Change to true in production

        // Require confirmed phone number before allowing login
        options.RequireConfirmedPhoneNumber = false;

        // Require confirmed account (email or phone) before allowing login
        options.RequireConfirmedAccount = false; // TODO: Change to true in production
    }

    /// <summary>
    /// Configure token providers and lifespans
    /// </summary>
    private static void ConfigureTokenOptions(TokenOptions options)
    {
        // Email confirmation token lifespan
        options.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
        
        // Password reset token lifespan
        options.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;

        // Change email token provider
        options.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
    }

    /// <summary>
    /// Configure data protection token lifespan (for email confirmation, password reset, etc.)
    /// Call this after AddIdentityConfiguration
    /// </summary>
    public static IServiceCollection ConfigureIdentityTokenLifespan(
        this IServiceCollection services, 
        TimeSpan? tokenLifespan = null)
    {
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            // Default: 24 hours for email confirmation and password reset tokens
            options.TokenLifespan = tokenLifespan ?? TimeSpan.FromHours(24);
        });

        return services;
    }
}