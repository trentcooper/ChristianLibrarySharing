using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;


namespace ChristianLibrary.Domain.Entities
{
    public class Book : BaseEntity
    {
        // ── Bibliographic Information ──────────────────────────────────────

        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public int? PageCount { get; set; }
        public string? Edition { get; set; }
        public string Language { get; set; } = "English";

        // ── Classification ─────────────────────────────────────────────────

        public BookGenre Genre { get; set; } = BookGenre.Other;
        public string? Description { get; set; }

        // ── Physical State & Availability ──────────────────────────────────

        public BookCondition Condition { get; set; } = BookCondition.Good;
        public bool IsAvailable { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public string? OwnerNotes { get; set; }

        // ── Media ──────────────────────────────────────────────────────────

        public string? CoverImageUrl { get; set; }

        // ── Statistics ─────────────────────────────────────────────────────

        public int BorrowCount { get; set; } = 0;
        public decimal? AverageRating { get; set; }

        // ── Relationships ──────────────────────────────────────────────────

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    }
}