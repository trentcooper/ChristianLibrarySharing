using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChristianLibrary.Data.Configurations
{
    /// <summary>
    /// Entity Type Configuration for UserProfile
    /// </summary>
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // Table name
            builder.ToTable("UserProfiles");

            // Primary key
            builder.HasKey(p => p.Id);

            // Indexes
            builder.HasIndex(p => p.UserId).IsUnique();
            builder.HasIndex(p => p.City);
            builder.HasIndex(p => p.State);
            builder.HasIndex(p => p.ChurchName);
            builder.HasIndex(p => new { p.Latitude, p.Longitude });

            // Properties
            builder.Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(p => p.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Bio)
                .HasMaxLength(1000);

            builder.Property(p => p.Latitude)
                .HasPrecision(10, 7);

            builder.Property(p => p.Longitude)
                .HasPrecision(10, 7);

            // FIXED: NotificationFrequency is now an enum, not a string!
            builder.Property(p => p.NotificationFrequency)
                .HasDefaultValue(NotificationFrequency.Immediate);

            builder.Property(p => p.ShowFullName)
                .HasDefaultValue(true);

            builder.Property(p => p.ShowEmail)
                .HasDefaultValue(false);

            builder.Property(p => p.ShowPhone)
                .HasDefaultValue(false);

            builder.Property(p => p.ShowExactAddress)
                .HasDefaultValue(false);

            builder.Property(p => p.ShowCityState)
                .HasDefaultValue(true);

            builder.Property(p => p.EmailNotifications)
                .HasDefaultValue(true);

            builder.Property(p => p.SmsNotifications)
                .HasDefaultValue(false);

            builder.Property(p => p.PushNotifications)
                .HasDefaultValue(true);

            // Computed column (ignored - calculated in code)
            builder.Ignore(p => p.FullName);

            // Audit fields from BaseEntity
            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.IsDeleted)
                .HasDefaultValue(false);
        }
    }
}