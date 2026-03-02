namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Base entity class with common properties for all domain entities
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Date and time when the entity was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the entity was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// User ID who created the entity
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User ID who last updated the entity
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Date and time when the entity was deleted (soft delete)
        /// </summary>
        public DateTime? DeletedAt { get; set; }
    }
}