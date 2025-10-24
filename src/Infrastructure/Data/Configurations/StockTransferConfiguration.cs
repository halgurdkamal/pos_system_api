using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers");

        builder.HasKey(st => st.Id);

        builder.Property(st => st.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.FromShopId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.ToShopId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.DrugId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.BatchNumber)
            .HasMaxLength(100);

        builder.Property(st => st.Quantity)
            .IsRequired();

        builder.Property(st => st.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(st => st.InitiatedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.InitiatedAt)
            .IsRequired();

        builder.Property(st => st.ApprovedBy)
            .HasMaxLength(50);

        builder.Property(st => st.ReceivedBy)
            .HasMaxLength(50);

        builder.Property(st => st.CancelledBy)
            .HasMaxLength(50);

        builder.Property(st => st.CancellationReason)
            .HasMaxLength(500);

        builder.Property(st => st.Notes)
            .HasMaxLength(1000);

        // Indexes for common queries
        builder.HasIndex(st => st.FromShopId);
        builder.HasIndex(st => st.ToShopId);
        builder.HasIndex(st => st.Status);
        builder.HasIndex(st => st.InitiatedAt);
        builder.HasIndex(st => new { st.FromShopId, st.Status });
        builder.HasIndex(st => new { st.ToShopId, st.Status });
    }
}
