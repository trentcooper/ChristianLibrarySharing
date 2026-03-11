using ChristianLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChristianLibrary.Data.Configurations
{
    /// <summary>
    /// Entity Type Configuration for Book
    /// </summary>
    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            // Table name
            builder.ToTable("Books");

            // Primary key
            builder.HasKey(b => b.Id);

            // Indexes
            builder.HasIndex(b => b.Title);
            builder.HasIndex(b => b.Author);
            builder.HasIndex(b => b.ISBN);
            builder.HasIndex(b => b.Genre);
            builder.HasIndex(b => b.OwnerId);
            builder.HasIndex(b => b.IsAvailable);
            builder.HasIndex(b => b.IsVisible);
            builder.HasIndex(b => b.IsDeleted);
            builder.HasIndex(b => new { b.Title, b.Author });

            // Properties
            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(b => b.Author)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(b => b.ISBN)
                .HasMaxLength(20);

            builder.Property(b => b.Publisher)
                .HasMaxLength(200);

            builder.Property(b => b.Genre)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(b => b.Description)
                .HasMaxLength(2000);

            builder.Property(b => b.CoverImageUrl)
                .HasMaxLength(500);

            builder.Property(b => b.Language)
                .HasMaxLength(50)
                .HasDefaultValue("English");

            builder.Property(b => b.OwnerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(b => b.OwnerNotes)
                .HasMaxLength(1000);

            builder.Property(b => b.Condition)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(b => b.IsAvailable)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(b => b.IsVisible)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(b => b.BorrowCount)
                .HasDefaultValue(0);

            builder.Property(b => b.AverageRating)
                .HasPrecision(3, 2);

            // Audit fields from BaseEntity
            builder.Property(b => b.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(b => b.IsDeleted)
                .HasDefaultValue(false);

            // Relationships
            builder.HasMany(b => b.BorrowRequests)
                .WithOne(br => br.Book)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.Loans)
                .WithOne(l => l.Book)
                .HasForeignKey(l => l.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}