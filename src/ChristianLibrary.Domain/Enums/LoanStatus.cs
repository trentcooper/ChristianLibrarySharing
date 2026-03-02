namespace ChristianLibrary.Domain.Enums
{
    /// <summary>
    /// Represents the status of an active loan
    /// </summary>
    public enum LoanStatus
    {
        /// <summary>
        /// Loan is active and book is with borrower
        /// </summary>
        Active = 1,

        /// <summary>
        /// Book has been returned and loan is complete
        /// </summary>
        Returned = 2,

        /// <summary>
        /// Loan is overdue past the due date
        /// </summary>
        Overdue = 3,

        /// <summary>
        /// Loan extension has been requested
        /// </summary>
        ExtensionRequested = 4,

        /// <summary>
        /// Loan was cancelled before book exchange
        /// </summary>
        Cancelled = 5
    }
}