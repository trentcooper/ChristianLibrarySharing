using System.Linq.Expressions;

namespace ChristianLibrary.Domain.Interfaces
{
    /// <summary>
    /// Generic repository interface for common data operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Get entity by ID
        /// </summary>
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Find entities matching a predicate
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get first entity matching predicate or null
        /// </summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if any entity matches predicate
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get count of entities matching predicate
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new entity
        /// </summary>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple entities
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing entity
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Update multiple entities
        /// </summary>
        void UpdateRange(IEnumerable<T> entities);

        /// <summary>
        /// Remove an entity
        /// </summary>
        void Remove(T entity);

        /// <summary>
        /// Remove multiple entities
        /// </summary>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// Get entities with pagination
        /// </summary>
        Task<IEnumerable<T>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get entities with includes (eager loading)
        /// </summary>
        Task<IEnumerable<T>> GetWithIncludesAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);
    }
}