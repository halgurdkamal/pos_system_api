using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.SystemRole)
            .HasConversion<int>()
            .HasDefaultValue(pos_system_api.Core.Domain.Auth.Enums.SystemRole.User)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(u => u.IsEmailVerified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.LockedUntil)
            .IsRequired(false);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(u => u.RefreshTokenExpiryTime)
            .IsRequired(false);

        builder.Property(u => u.Phone)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        // Many-to-many relationship with Shops via ShopUser
        builder.HasMany(u => u.ShopMemberships)
            .WithOne(su => su.User)
            .HasForeignKey(su => su.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(u => u.SystemRole);
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.LastLoginAt);
    }
}
