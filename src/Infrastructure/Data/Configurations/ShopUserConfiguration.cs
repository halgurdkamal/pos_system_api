using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Shops.Entities;
using System.Text.Json;

namespace pos_system_api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ShopUser (many-to-many join table)
/// </summary>
public class ShopUserConfiguration : IEntityTypeConfiguration<ShopUser>
{
    public void Configure(EntityTypeBuilder<ShopUser> builder)
    {
        builder.ToTable("ShopUsers");

        builder.HasKey(su => su.Id);

        builder.Property(su => su.Id)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(su => su.UserId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(su => su.ShopId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(su => su.Role)
            .HasConversion<int>()
            .HasDefaultValue(ShopRole.Custom)
            .IsRequired();

        // Store Permissions as JSON array
        builder.Property(su => su.Permissions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Permission>>(v, (JsonSerializerOptions?)null) ?? new List<Permission>())
            .IsRequired();

        builder.Property(su => su.JoinedDate)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(su => su.InvitedBy)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(su => su.IsOwner)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(su => su.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(su => su.LastAccessDate)
            .IsRequired(false);

        builder.Property(su => su.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        // Relationships
        builder.HasOne(su => su.User)
            .WithMany(u => u.ShopMemberships)
            .HasForeignKey(su => su.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(su => su.Shop)
            .WithMany(s => s.Members)
            .HasForeignKey(su => su.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite unique index (one user can only have one membership per shop)
        builder.HasIndex(su => new { su.UserId, su.ShopId })
            .IsUnique();

        // Indexes for common queries
        builder.HasIndex(su => su.UserId);
        builder.HasIndex(su => su.ShopId);
        builder.HasIndex(su => su.Role);
        builder.HasIndex(su => su.IsOwner);
        builder.HasIndex(su => su.IsActive);
        builder.HasIndex(su => su.JoinedDate);
    }
}
