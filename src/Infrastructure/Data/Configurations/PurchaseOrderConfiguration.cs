using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(po => po.Id);

        builder.Property(po => po.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(po => po.OrderNumber)
            .IsUnique();

        builder.Property(po => po.ShopId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(po => po.SupplierId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(po => po.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(po => po.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Financial columns with precision
        builder.Property(po => po.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.ShippingCost)
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.DiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.PaymentTerms)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(po => po.CustomPaymentTerms)
            .HasMaxLength(200);

        builder.Property(po => po.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(po => po.SubmittedBy)
            .HasMaxLength(100);

        builder.Property(po => po.ConfirmedBy)
            .HasMaxLength(100);

        builder.Property(po => po.CancelledBy)
            .HasMaxLength(100);

        builder.Property(po => po.CancellationReason)
            .HasMaxLength(500);

        builder.Property(po => po.Notes)
            .HasMaxLength(2000);

        builder.Property(po => po.DeliveryAddress)
            .HasMaxLength(500);

        builder.Property(po => po.ReferenceNumber)
            .HasMaxLength(100);

        // Indexes for common queries
        builder.HasIndex(po => po.ShopId);
        builder.HasIndex(po => po.SupplierId);
        builder.HasIndex(po => po.Status);
        builder.HasIndex(po => po.OrderDate);
        builder.HasIndex(po => new { po.ShopId, po.Status });
        builder.HasIndex(po => new { po.ShopId, po.OrderDate });

        // Relationships
        builder.HasMany(po => po.Items)
            .WithOne()
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PurchaseOrderId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.DrugId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.DiscountPercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(i => i.DiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TotalPrice)
            .HasColumnType("decimal(18,2)");

        // Indexes
        builder.HasIndex(i => i.PurchaseOrderId);
        builder.HasIndex(i => i.DrugId);

        // Relationships
        builder.HasMany(i => i.Receipts)
            .WithOne()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReceiptRecordConfiguration : IEntityTypeConfiguration<ReceiptRecord>
{
    public void Configure(EntityTypeBuilder<ReceiptRecord> builder)
    {
        builder.ToTable("ReceiptRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderItemId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.BatchNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.ReceivedBy)
            .IsRequired()
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(r => r.OrderItemId);
        builder.HasIndex(r => r.BatchNumber);
        builder.HasIndex(r => r.ReceivedAt);
    }
}
