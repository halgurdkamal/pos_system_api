using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ShopInventory entity
/// </summary>
public class ShopInventoryConfiguration : IEntityTypeConfiguration<ShopInventory>
{
    public void Configure(EntityTypeBuilder<ShopInventory> builder)
    {
        builder.ToTable("ShopInventory");
        
        builder.HasKey(si => si.Id);
        
        builder.Property(si => si.Id)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(si => si.ShopId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(si => si.DrugId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(si => si.TotalStock)
            .IsRequired();
        
        builder.Property(si => si.ReorderPoint)
            .IsRequired();
        
        builder.Property(si => si.StorageLocation)
            .HasMaxLength(300);
        
        builder.Property(si => si.IsAvailable)
            .IsRequired();
        
        builder.Property(si => si.LastRestockDate);
        
        // Shop-specific packaging configuration
        builder.Property(si => si.ShopSpecificSellUnit)
            .HasMaxLength(50);
        
        builder.Property(si => si.MinimumSaleQuantity)
            .HasColumnType("decimal(18,2)");
        
        // Configure ShopPricing as owned entity
        builder.OwnsOne(si => si.ShopPricing, pricing =>
        {
            pricing.Property(p => p.CostPrice)
                .HasColumnName("Pricing_CostPrice")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            pricing.Property(p => p.SellingPrice)
                .HasColumnName("Pricing_SellingPrice")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            pricing.Property(p => p.Discount)
                .HasColumnName("Pricing_Discount")
                .HasColumnType("decimal(5,2)");
            
            pricing.Property(p => p.Currency)
                .HasColumnName("Pricing_Currency")
                .HasMaxLength(10);
            
            pricing.Property(p => p.TaxRate)
                .HasColumnName("Pricing_TaxRate")
                .HasColumnType("decimal(5,2)");
            
            pricing.Property(p => p.LastPriceUpdate)
                .HasColumnName("Pricing_LastPriceUpdate");
            
            // Configure PackagingLevelPrices as JSONB for PostgreSQL
            pricing.Property(p => p.PackagingLevelPrices)
                .HasColumnName("Pricing_PackagingLevelPrices")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, decimal>()
                );
        });
        
        // Configure Batches as owned collection (stored as JSONB for PostgreSQL in EF Core 6)
        builder.Property(si => si.Batches)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<pos_system_api.Core.Domain.Inventory.ValueObjects.Batch>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<pos_system_api.Core.Domain.Inventory.ValueObjects.Batch>()
            );
        
        // Base entity properties
        builder.Property(si => si.CreatedAt).IsRequired();
        builder.Property(si => si.CreatedBy).HasMaxLength(100);
        builder.Property(si => si.LastUpdated);
        builder.Property(si => si.UpdatedBy).HasMaxLength(100);
        
        // Foreign key relationships
        builder.HasOne<pos_system_api.Core.Domain.Shops.Entities.Shop>()
            .WithMany()
            .HasForeignKey(si => si.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne<pos_system_api.Core.Domain.Drugs.Entities.Drug>()
            .WithMany()
            .HasForeignKey(si => si.DrugId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(si => si.ShopId);
        builder.HasIndex(si => si.DrugId);
        builder.HasAlternateKey(si => new { si.ShopId, si.DrugId });
        builder.HasIndex(si => new { si.ShopId, si.DrugId }).IsUnique(); // Composite unique index
        builder.HasIndex(si => si.TotalStock);
        builder.HasIndex(si => si.IsAvailable);
        builder.HasIndex(si => si.LastRestockDate);

        builder.HasMany(si => si.PackagingOverrides)
            .WithOne()
            .HasForeignKey(po => new { po.ShopId, po.DrugId })
            .HasPrincipalKey(si => new { si.ShopId, si.DrugId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(si => si.PackagingOverrides);
    }
}
