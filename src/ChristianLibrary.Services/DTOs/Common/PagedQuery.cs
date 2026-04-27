using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Services.DTOs.Common;

/// <summary>
/// Abstract base class for paginated query parameters.
/// Cannot be instantiated directly — must be inherited by a concrete query class.
/// </summary>
public abstract class PagedQuery
{
    /// <summary>
    /// 1-based page number. Must be >= 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Must be between 1 and 100.
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;
}