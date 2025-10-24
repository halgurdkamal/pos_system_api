using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sa => sa.ShopId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sa => sa.DrugId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sa => sa.BatchNumber)
            .HasMaxLength(100);

        builder.Property(sa => sa.AdjustmentType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(sa => sa.QuantityChanged)
            .IsRequired();

        builder.Property(sa => sa.QuantityBefore)
            .IsRequired();

        builder.Property(sa => sa.QuantityAfter)
            .IsRequired();

        builder.Property(sa => sa.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sa => sa.Notes)
            .HasMaxLength(1000);

        builder.Property(sa => sa.AdjustedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sa => sa.AdjustedAt)
            .IsRequired();

        builder.Property(sa => sa.ReferenceId)
            .HasMaxLength(50);

        builder.Property(sa => sa.ReferenceType)
            .HasMaxLength(50);

        // Indexes for common queries
        builder.HasIndex(sa => sa.ShopId);
        builder.HasIndex(sa => sa.DrugId);
        builder.HasIndex(sa => sa.AdjustedAt);
        builder.HasIndex(sa => new { sa.ShopId, sa.AdjustedAt });
        builder.HasIndex(sa => sa.AdjustmentType);
    }
}
