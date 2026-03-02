using ChristianLibrary.Domain.Entities;

namespace ChristianLibrary.Domain.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for managing transactions and repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for UserProfile entities
        /// </summary>
        IRepository<UserProfile> UserProfiles { get; }

        /// <summary>
        /// Repository for Book entities
        /// </summary>
        IRepository<Book> Books { get; }

        /// <summary>
        /// Repository for BorrowRequest entities
        /// </summary>
        IRepository<BorrowRequest> BorrowRequests { get; }

        /// <summary>
        /// Repository for Loan entities
        /// </summary>
        IRepository<Loan> Loans { get; }

        /// <summary>
        /// Repository for Message entities
        /// </summary>
        IRepository<Message> Messages { get; }

        /// <summary>
        /// Repository for Notification entities
        /// </summary>
        IRepository<Notification> Notifications { get; }

        /// <summary>
        /// Save all changes to the database
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begin a database transaction
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}