namespace ChristianLibrary.Services.DTOs.Books;

/// <summary>
/// Rich book detail response respecting owner privacy settings
/// </summary>
public class BookDetailResponse
{
    // ── Book Information ───────────────────────────────────────
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public int? PageCount { get; set; }
    public string? Edition { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? OwnerNotes { get; set; }

    // ── Availability ───────────────────────────────────────────
    public bool IsAvailable { get; set; }

    // ── Owner Information (privacy filtered) ──────────────────
    public string? OwnerDisplayName { get; set; }
    public string? OwnerCity { get; set; }
    public string? OwnerState { get; set; }

    // ── Distance (optional — populated when caller provides location) ──
    public double? DistanceMiles { get; set; }

    // ── Similar Books ──────────────────────────────────────────
    public List<SimilarBookResponse> SimilarBooks { get; set; } = new();
}

/// <summary>
/// Slim book summary used in similar books list
/// </summary>
public class SimilarBookResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}