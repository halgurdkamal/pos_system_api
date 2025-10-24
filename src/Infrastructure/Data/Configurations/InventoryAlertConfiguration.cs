using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class InventoryAlertConfiguration : IEntityTypeConfiguration<InventoryAlert>
{
    public void Configure(EntityTypeBuilder<InventoryAlert> builder)
    {
        builder.ToTable("InventoryAlerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ShopId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.DrugId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.BatchNumber)
            .HasMaxLength(50);

        builder.Property(a => a.AlertType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.Severity)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.AcknowledgedBy)
            .HasMaxLength(100);

        builder.Property(a => a.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(a => a.ResolutionNotes)
            .HasMaxLength(1000);

        // Indexes for performance
        builder.HasIndex(a => a.ShopId);
        builder.HasIndex(a => a.DrugId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.AlertType);
        builder.HasIndex(a => a.Severity);
        builder.HasIndex(a => a.GeneratedAt);
        builder.HasIndex(a => new { a.ShopId, a.Status });
        builder.HasIndex(a => new { a.ShopId, a.AlertType, a.Status });
    }
}
