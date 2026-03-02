namespace ChristianLibrary.Domain.Enums
{
    /// <summary>
    /// Represents the type/category of a notification
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// General system notification
        /// </summary>
        System = 1,

        /// <summary>
        /// New borrow request received
        /// </summary>
        BorrowRequest = 2,

        /// <summary>
        /// Borrow request approved
        /// </summary>
        RequestApproved = 3,

        /// <summary>
        /// Borrow request declined
        /// </summary>
        RequestDeclined = 4,

        /// <summary>
        /// Loan due date reminder
        /// </summary>
        DueDateReminder = 5,

        /// <summary>
        /// Loan is overdue
        /// </summary>
        Overdue = 6,

        /// <summary>
        /// Book has been returned
        /// </summary>
        BookReturned = 7,

        /// <summary>
        /// New message received
        /// </summary>
        Message = 8,

        /// <summary>
        /// Extension request received
        /// </summary>
        ExtensionRequest = 9
    }
}