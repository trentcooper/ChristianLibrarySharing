using ChristianLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChristianLibrary.Data.Configurations
{
    /// <summary>
    /// Entity Type Configuration for BorrowRequest
    /// </summary>
    public class BorrowRequestConfiguration : IEntityTypeConfiguration<BorrowRequest>
    {
        public void Configure(EntityTypeBuilder<BorrowRequest> builder)
        {
            // Table name
            builder.ToTable("BorrowRequests");

            // Primary key
            builder.HasKey(br => br.Id);

            // Indexes
            builder.HasIndex(br => br.BookId);
            builder.HasIndex(br => br.BorrowerId);
            builder.HasIndex(br => br.LenderId);
            builder.HasIndex(br => br.Status);
            builder.HasIndex(br => br.RequestedStartDate);
            builder.HasIndex(br => br.ExpiresAt);
            builder.HasIndex(br => br.IsDeleted);

            // Properties
            builder.Property(br => br.BookId)
                .IsRequired();

            builder.Property(br => br.BorrowerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(br => br.LenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(br => br.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(br => br.RequestedStartDate)
                .IsRequired();

            builder.Property(br => br.RequestedEndDate)
                .IsRequired();

            builder.Property(br => br.Message)
                .HasMaxLength(1000);

            builder.Property(br => br.ResponseMessage)
                .HasMaxLength(1000);

            builder.Property(br => br.ExpiresAt)
                .IsRequired();

            // Audit fields from BaseEntity
            builder.Property(br => br.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(br => br.IsDeleted)
                .HasDefaultValue(false);

            // Relationships
            builder.HasOne(br => br.Loan)
                .WithOne(l => l.BorrowRequest)
                .HasForeignKey<Loan>(l => l.BorrowRequestId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}