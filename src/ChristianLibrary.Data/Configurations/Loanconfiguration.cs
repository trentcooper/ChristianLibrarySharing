using ChristianLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChristianLibrary.Data.Configurations
{
    /// <summary>
    /// Entity Type Configuration for Loan
    /// </summary>
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            // Table name
            builder.ToTable("Loans");

            // Primary key
            builder.HasKey(l => l.Id);

            // Indexes
            builder.HasIndex(l => l.BookId);
            builder.HasIndex(l => l.BorrowerId);
            builder.HasIndex(l => l.LenderId);
            builder.HasIndex(l => l.BorrowRequestId);
            builder.HasIndex(l => l.Status);
            builder.HasIndex(l => l.StartDate);
            builder.HasIndex(l => l.DueDate);
            builder.HasIndex(l => l.ReturnedDate);
            builder.HasIndex(l => l.IsDeleted);

            // Properties
            builder.Property(l => l.BookId)
                .IsRequired();

            builder.Property(l => l.BorrowerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(l => l.LenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(l => l.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(l => l.StartDate)
                .IsRequired();

            builder.Property(l => l.DueDate)
                .IsRequired();

            builder.Property(l => l.ExtensionDays)
                .HasDefaultValue(0);

            builder.Property(l => l.ExtensionRequested)
                .HasDefaultValue(false);

            builder.Property(l => l.RemindersSent)
                .HasDefaultValue(0);

            builder.Property(l => l.LenderNotes)
                .HasMaxLength(1000);

            builder.Property(l => l.BorrowerNotes)
                .HasMaxLength(1000);
            
            builder.Property(l => l.ConditionAtCheckout)
                .HasConversion<int>();
            
            builder.Property(l => l.ConditionAtReturn)
                .HasConversion<int>();

            // Computed properties (ignored - calculated in code)
            builder.Ignore(l => l.IsOverdue);
            builder.Ignore(l => l.DaysUntilDue);

            // Audit fields from BaseEntity
            builder.Property(l => l.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(l => l.IsDeleted)
                .HasDefaultValue(false);
        }
    }
}