using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Supplier entity
/// </summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.SupplierName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.SupplierType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.ContactNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.PaymentTerms)
            .HasMaxLength(100);

        builder.Property(s => s.DeliveryLeadTime)
            .IsRequired();

        builder.Property(s => s.MinimumOrderValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.Website)
            .HasMaxLength(300);

        builder.Property(s => s.TaxId)
            .HasMaxLength(100);

        builder.Property(s => s.LicenseNumber)
            .HasMaxLength(100);

        // Configure Address as owned entity
        builder.OwnsOne(s => s.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Address_Street").HasMaxLength(300);
            address.Property(a => a.City).HasColumnName("Address_City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("Address_State").HasMaxLength(100);
            address.Property(a => a.ZipCode).HasColumnName("Address_ZipCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Address_Country").HasMaxLength(100);
        });

        // Base entity properties
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.LastUpdated);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        // Indexes
        builder.HasIndex(s => s.SupplierName);
        builder.HasIndex(s => s.Email);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.SupplierType);
    }
}
