using ChristianLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChristianLibrary.Data.Configurations
{
    /// <summary>
    /// Entity Type Configuration for Notification
    /// </summary>
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Table name
            builder.ToTable("Notifications");

            // Primary key
            builder.HasKey(n => n.Id);

            // Indexes
            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.Type);
            builder.HasIndex(n => n.IsRead);
            builder.HasIndex(n => n.Priority);
            builder.HasIndex(n => n.BookId);
            builder.HasIndex(n => n.BorrowRequestId);
            builder.HasIndex(n => n.LoanId);
            builder.HasIndex(n => n.MessageId);
            builder.HasIndex(n => n.ExpiresAt);
            builder.HasIndex(n => n.IsDeleted);
            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            // Properties
            builder.Property(n => n.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(n => n.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false);

            builder.Property(n => n.ActionUrl)
                .HasMaxLength(500);

            builder.Property(n => n.Priority)
                .HasDefaultValue(2); // Normal

            builder.Property(n => n.EmailSent)
                .HasDefaultValue(false);

            builder.Property(n => n.SmsSent)
                .HasDefaultValue(false);

            builder.Property(n => n.PushSent)
                .HasDefaultValue(false);

            // Audit fields from BaseEntity
            builder.Property(n => n.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(n => n.IsDeleted)
                .HasDefaultValue(false);
        }
    }
}