using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Request to update user profile visibility settings
/// </summary>
public class UpdateVisibilityRequest
{
    /// <summary>
    /// Overall profile visibility level
    /// </summary>
    public ProfileVisibility? Visibility { get; set; }

    /// <summary>
    /// Whether to show full name publicly
    /// </summary>
    public bool? ShowFullName { get; set; }

    /// <summary>
    /// Whether to show email publicly
    /// </summary>
    public bool? ShowEmail { get; set; }

    /// <summary>
    /// Whether to show phone number publicly
    /// </summary>
    public bool? ShowPhone { get; set; }

    /// <summary>
    /// Whether to show exact address publicly
    /// </summary>
    public bool? ShowExactAddress { get; set; }

    /// <summary>
    /// Whether to show city/state only
    /// </summary>
    public bool? ShowCityState { get; set; }

    /// <summary>
    /// Whether to show date of birth publicly
    /// </summary>
    public bool? ShowDateOfBirth { get; set; }
}