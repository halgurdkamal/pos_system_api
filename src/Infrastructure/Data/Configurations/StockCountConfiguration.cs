using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts");
        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id).IsRequired().HasMaxLength(50);
        builder.Property(sc => sc.ShopId).IsRequired().HasMaxLength(50);
        builder.Property(sc => sc.DrugId).IsRequired().HasMaxLength(50);
        builder.Property(sc => sc.Status).IsRequired().HasConversion<int>();
        builder.Property(sc => sc.CountedBy).IsRequired().HasMaxLength(50);
        builder.Property(sc => sc.VarianceReason).HasMaxLength(500);
        builder.Property(sc => sc.Notes).HasMaxLength(1000);
        builder.HasIndex(sc => sc.ShopId);
        builder.HasIndex(sc => sc.Status);
        builder.HasIndex(sc => new { sc.ShopId, sc.Status });
    }
}
