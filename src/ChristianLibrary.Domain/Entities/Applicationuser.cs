using Microsoft.AspNetCore.Identity;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents a user in the Christian Library system
    /// Extends ASP.NET Core Identity's IdentityUser
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Date and time when the user registered
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the user last logged in
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Whether the user account is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Navigation property to user profile
        /// </summary>
        public virtual UserProfile? Profile { get; set; }

        /// <summary>
        /// Navigation property to books owned by this user
        /// </summary>
        public virtual ICollection<Book> OwnedBooks { get; set; } = new List<Book>();

        /// <summary>
        /// Navigation property to borrow requests made by this user
        /// </summary>
        public virtual ICollection<BorrowRequest> BorrowRequestsMade { get; set; } = new List<BorrowRequest>();

        /// <summary>
        /// Navigation property to borrow requests received by this user (as book owner)
        /// </summary>
        public virtual ICollection<BorrowRequest> BorrowRequestsReceived { get; set; } = new List<BorrowRequest>();

        /// <summary>
        /// Navigation property to loans where this user is the borrower
        /// </summary>
        public virtual ICollection<Loan> LoansAsBorrower { get; set; } = new List<Loan>();

        /// <summary>
        /// Navigation property to loans where this user is the lender
        /// </summary>
        public virtual ICollection<Loan> LoansAsLender { get; set; } = new List<Loan>();

        /// <summary>
        /// Navigation property to messages sent by this user
        /// </summary>
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

        /// <summary>
        /// Navigation property to messages received by this user
        /// </summary>
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

        /// <summary>
        /// Navigation property to notifications for this user
        /// </summary>
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}