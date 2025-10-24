using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");

        builder.HasKey(so => so.Id);

        builder.Property(so => so.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(so => so.OrderNumber)
            .IsUnique();

        builder.Property(so => so.ShopId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(so => so.CashierId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(so => so.CustomerId)
            .HasMaxLength(100);

        builder.Property(so => so.CustomerName)
            .HasMaxLength(200);

        builder.Property(so => so.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(so => so.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Financial columns with precision
        builder.Property(so => so.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(so => so.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(so => so.DiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(so => so.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(so => so.AmountPaid)
            .HasColumnType("decimal(18,2)");

        builder.Property(so => so.ChangeGiven)
            .HasColumnType("decimal(18,2)");

        builder.Property(so => so.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(so => so.PaymentReference)
            .HasMaxLength(200);

        builder.Property(so => so.CancelledBy)
            .HasMaxLength(100);

        builder.Property(so => so.CancellationReason)
            .HasMaxLength(500);

        builder.Property(so => so.Notes)
            .HasMaxLength(2000);

        builder.Property(so => so.PrescriptionNumber)
            .HasMaxLength(100);

        // Indexes for common queries
        builder.HasIndex(so => so.ShopId);
        builder.HasIndex(so => so.CashierId);
        builder.HasIndex(so => so.CustomerId);
        builder.HasIndex(so => so.Status);
        builder.HasIndex(so => so.OrderDate);
        builder.HasIndex(so => new { so.ShopId, so.Status });
        builder.HasIndex(so => new { so.ShopId, so.OrderDate });
        builder.HasIndex(so => new { so.ShopId, so.CashierId, so.OrderDate });

        // Relationships
        builder.HasMany(so => so.Items)
            .WithOne()
            .HasForeignKey(i => i.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
{
    public void Configure(EntityTypeBuilder<SalesOrderItem> builder)
    {
        builder.ToTable("SalesOrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.SalesOrderId)
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

        builder.Property(i => i.BatchNumber)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(i => i.SalesOrderId);
        builder.HasIndex(i => i.DrugId);
        builder.HasIndex(i => new { i.SalesOrderId, i.DrugId });
    }
}
