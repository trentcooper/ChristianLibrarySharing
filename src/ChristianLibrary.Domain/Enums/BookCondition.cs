namespace ChristianLibrary.Domain.Enums
{
/// <summary>
/// Represents the physical condition of a book
/// </summary>
public enum BookCondition
{
    /// <summary>
    /// Like new, no visible wear
    /// </summary>
    LikeNew = 1,

    /// <summary>
    /// Very good condition, minor wear
    /// </summary>
    VeryGood = 2,

    /// <summary>
    /// Good condition, some wear but fully readable
    /// </summary>
    Good = 3,

    /// <summary>
    /// Acceptable condition, noticeable wear
    /// </summary>
    Acceptable = 4,

    /// <summary>
    /// Poor condition, significant wear but still usable
    /// </summary>
    Poor = 5
}
}