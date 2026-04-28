namespace ChristianLibrary.Services.Configuration;

/// <summary>
/// Configuration values for loan workflow behavior.
/// Bound from the "LoanSettings" section of appsettings.json.
/// </summary>
public class LoanSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "LoanSettings";

    /// <summary>
    /// Maximum number of days a borrower may extend a loan in a single
    /// extension request. Computed as (RequestedExtensionDate - current DueDate).
    /// </summary>
    public int MaxExtensionDays { get; set; } = 21;
}