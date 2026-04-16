using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Common;

/// <summary>
/// Abstract base class for paginated query parameters.
/// Cannot be instantiated directly — must be inherited by a concrete query class.
/// </summary>
public abstract class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}