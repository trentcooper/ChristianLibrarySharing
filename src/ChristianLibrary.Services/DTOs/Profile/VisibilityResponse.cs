using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Response containing user profile visibility settings
/// </summary>
public class VisibilityResponse
{
    /// <summary>
    /// Overall profile visibility level
    /// </summary>
    public ProfileVisibility Visibility { get; set; }

    /// <summary>
    /// Human-readable visibility level
    /// </summary>
    public string VisibilityText => Visibility switch
    {
        ProfileVisibility.Public => "Public - Visible to everyone",
        ProfileVisibility.FriendsOnly => "Friends Only - Visible to connections only",
        ProfileVisibility.Private => "Private - Hidden from everyone",
        _ => "Unknown"
    };

    /// <summary>
    /// Whether to show full name publicly
    /// </summary>
    public bool ShowFullName { get; set; }

    /// <summary>
    /// Whether to show email publicly
    /// </summary>
    public bool ShowEmail { get; set; }

    /// <summary>
    /// Whether to show phone number publicly
    /// </summary>
    public bool ShowPhone { get; set; }

    /// <summary>
    /// Whether to show exact address publicly
    /// </summary>
    public bool ShowExactAddress { get; set; }

    /// <summary>
    /// Whether to show city/state only
    /// </summary>
    public bool ShowCityState { get; set; }

    /// <summary>
    /// Whether to show date of birth publicly
    /// </summary>
    public bool ShowDateOfBirth { get; set; }

    /// <summary>
    /// Summary of what's visible
    /// </summary>
    public string Summary
    {
        get
        {
            var visible = new List<string>();
            
            if (ShowFullName) visible.Add("Name");
            if (ShowEmail) visible.Add("Email");
            if (ShowPhone) visible.Add("Phone");
            if (ShowExactAddress) visible.Add("Full Address");
            else if (ShowCityState) visible.Add("City/State");
            if (ShowDateOfBirth) visible.Add("Date of Birth");

            return visible.Count > 0 
                ? $"Showing: {string.Join(", ", visible)}" 
                : "Minimal information visible";
        }
    }
}