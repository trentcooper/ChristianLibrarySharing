using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChristianLibrary.Data.Repositories
{
    /// <summary>
    /// Unit of Work implementation for managing transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy-loaded repositories
        private IRepository<UserProfile>? _userProfiles;
        private IRepository<Book>? _books;
        private IRepository<BorrowRequest>? _borrowRequests;
        private IRepository<Loan>? _loans;
        private IRepository<Message>? _messages;
        private IRepository<Notification>? _notifications;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<UserProfile> UserProfiles
        {
            get
            {
                _userProfiles ??= new Repository<UserProfile>(_context);
                return _userProfiles;
            }
        }

        public IRepository<Book> Books
        {
            get
            {
                _books ??= new Repository<Book>(_context);
                return _books;
            }
        }

        public IRepository<BorrowRequest> BorrowRequests
        {
            get
            {
                _borrowRequests ??= new Repository<BorrowRequest>(_context);
                return _borrowRequests;
            }
        }

        public IRepository<Loan> Loans
        {
            get
            {
                _loans ??= new Repository<Loan>(_context);
                return _loans;
            }
        }

        public IRepository<Message> Messages
        {
            get
            {
                _messages ??= new Repository<Message>(_context);
                return _messages;
            }
        }

        public IRepository<Notification> Notifications
        {
            get
            {
                _notifications ??= new Repository<Notification>(_context);
                return _notifications;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
        }
    }
}