using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents a book in a user's personal library catalog
    /// </summary>
    public class Book
    {
        public int Id { get; set; }

        // ── Bibliographic Information ──────────────────────────────────────

        /// <summary>Full title of the book</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Author(s) of the book</summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>ISBN-10 or ISBN-13</summary>
        public string? ISBN { get; set; }

        /// <summary>Publishing company or organization</summary>
        public string? Publisher { get; set; }

        /// <summary>Year the book was published</summary>
        public int? PublicationYear { get; set; }

        /// <summary>Total number of pages</summary>
        public int? PageCount { get; set; }

        /// <summary>Edition of the book (e.g. "2nd Edition")</summary>
        public string? Edition { get; set; }

        /// <summary>Language the book is written in</summary>
        public string Language { get; set; } = "English";

        // ── Classification ─────────────────────────────────────────────────

        /// <summary>Genre or category of the book</summary>
        public BookGenre Genre { get; set; } = BookGenre.Other;

        /// <summary>Short description or synopsis</summary>
        public string? Description { get; set; }

        // ── Physical State & Availability ──────────────────────────────────

        /// <summary>Physical condition of the book</summary>
        public BookCondition Condition { get; set; } = BookCondition.Good;

        /// <summary>Whether the book is currently available to borrow</summary>
        public bool IsAvailable { get; set; } = true;
        
        // ── Physical State & Availability ──────────────────────────────────
        public bool IsVisible { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        /// <summary>Optional notes from the owner visible to potential borrowers</summary>
        public string? OwnerNotes { get; set; }

        // ── Media ──────────────────────────────────────────────────────────

        /// <summary>URL to the book cover image</summary>
        public string? CoverImageUrl { get; set; }
        
    

        // ── Statistics ─────────────────────────────────────────────────────
        public int BorrowCount { get; set; } = 0;
        public decimal? AverageRating { get; set; }
        

        // ── Audit Fields ───────────────────────────────────────────────────

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ── Relationships ──────────────────────────────────────────────────

        /// <summary>Foreign key to ApplicationUser (Identity GUID)</summary>
        public string OwnerId { get; set; } = string.Empty;

        /// <summary>The user who owns this book</summary>
        public ApplicationUser Owner { get; set; } = null!;

        /// <summary>All loan history for this book</summary>
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        
        public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    }
}