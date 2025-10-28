using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ShopPackagingOverride entity.
/// </summary>
public class ShopPackagingOverrideConfiguration : IEntityTypeConfiguration<ShopPackagingOverride>
{
    public void Configure(EntityTypeBuilder<ShopPackagingOverride> builder)
    {
        builder.ToTable("ShopPackagingOverrides");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ShopId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.DrugId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.PackagingLevelId)
            .HasMaxLength(50);

        builder.Property(o => o.ParentPackagingLevelId)
            .HasMaxLength(50);

        builder.Property(o => o.ParentOverrideId)
            .HasMaxLength(50);

        builder.Property(o => o.CustomUnitName)
            .HasMaxLength(100);

        builder.Property(o => o.OverrideQuantityPerParent)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.SellingPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.MinimumSaleQuantity)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.IsSellable);
        builder.Property(o => o.IsDefaultSellUnit);
        builder.Property(o => o.CustomLevelOrder);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.LastUpdated);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => new { o.ShopId, o.DrugId });
        builder.HasIndex(o => new { o.ShopId, o.DrugId, o.PackagingLevelId })
            .IsUnique()
            .HasFilter("\"PackagingLevelId\" IS NOT NULL")
            .HasDatabaseName("IX_ShopPackagingOverride_ShopDrugLevel");

        builder.HasIndex(o => o.ParentOverrideId);

        builder.HasIndex(o => new { o.ShopId, o.DrugId, o.IsDefaultSellUnit })
            .HasDatabaseName("IX_ShopPackagingOverride_DefaultSellUnit");
    }
}
