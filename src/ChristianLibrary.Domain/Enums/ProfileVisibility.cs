namespace ChristianLibrary.Domain.Enums;

/// <summary>
/// Defines visibility levels for user profiles
/// </summary>
public enum ProfileVisibility
{
    /// <summary>
    /// Profile visible to everyone
    /// </summary>
    Public = 1,

    /// <summary>
    /// Profile visible only to friends/connections
    /// </summary>
    FriendsOnly = 2,

    /// <summary>
    /// Profile hidden from everyone except the user
    /// </summary>
    Private = 3
}