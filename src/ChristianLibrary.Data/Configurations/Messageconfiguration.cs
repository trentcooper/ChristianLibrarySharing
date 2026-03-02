using ChristianLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChristianLibrary.Data.Configurations
{
    /// <summary>
    /// Entity Type Configuration for Message
    /// </summary>
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            // Table name
            builder.ToTable("Messages");

            // Primary key
            builder.HasKey(m => m.Id);

            // Indexes
            builder.HasIndex(m => m.SenderId);
            builder.HasIndex(m => m.RecipientId);
            builder.HasIndex(m => m.IsRead);
            builder.HasIndex(m => m.BookId);
            builder.HasIndex(m => m.BorrowRequestId);
            builder.HasIndex(m => m.LoanId);
            builder.HasIndex(m => m.IsDeleted);
            builder.HasIndex(m => new { m.SenderId, m.RecipientId, m.CreatedAt });

            // Properties
            builder.Property(m => m.SenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(m => m.RecipientId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(m => m.Subject)
                .HasMaxLength(200);

            builder.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(5000);

            builder.Property(m => m.IsRead)
                .HasDefaultValue(false);

            builder.Property(m => m.SenderDeleted)
                .HasDefaultValue(false);

            builder.Property(m => m.RecipientDeleted)
                .HasDefaultValue(false);

            // Audit fields from BaseEntity
            builder.Property(m => m.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(m => m.IsDeleted)
                .HasDefaultValue(false);
        }
    }
}