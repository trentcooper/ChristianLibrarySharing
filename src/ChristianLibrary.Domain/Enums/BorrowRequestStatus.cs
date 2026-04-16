namespace ChristianLibrary.Domain.Enums
{
    /// <summary>
    /// Represents the status of a borrow request
    /// </summary>
    public enum BorrowRequestStatus
    {
        /// <summary>
        /// Request has been submitted and is awaiting response
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Request has been approved by the book owner
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Request has been declined by the book owner
        /// </summary>
        Declined = 3,

        /// <summary>
        /// Request was cancelled by the borrower
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// Request has expired without response
        /// </summary>
        Expired = 5,
        
        /// <summary>
        /// Borrow request has completed the full lifecycle - book has been returned
        /// </summary>
        Completed = 6
    }
}