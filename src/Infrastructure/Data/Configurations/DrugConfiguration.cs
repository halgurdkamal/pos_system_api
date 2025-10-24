using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Drugs.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        // Table name
        builder.ToTable("Drugs");

        // Primary key
        builder.HasKey(d => d.Id);

        // Properties
        builder.Property(d => d.DrugId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(d => d.DrugId)
            .IsUnique();

        builder.Property(d => d.Barcode)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(d => d.Barcode)
            .IsUnique();

        builder.Property(d => d.BarcodeType)
            .HasMaxLength(50);

        builder.Property(d => d.BrandName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.GenericName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Manufacturer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.OriginCountry)
            .HasMaxLength(100);

        builder.Property(d => d.Category)
            .HasMaxLength(100);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        // Collection properties (stored as JSON in PostgreSQL)
        builder.Property(d => d.ImageUrls)
            .HasColumnType("jsonb");

        builder.Property(d => d.SideEffects)
            .HasColumnType("jsonb");

        builder.Property(d => d.InteractionNotes)
            .HasColumnType("jsonb");

        builder.Property(d => d.Tags)
            .HasColumnType("jsonb");

        builder.Property(d => d.RelatedDrugs)
            .HasColumnType("jsonb");

        // Value Objects - Configure as owned entities

        // Formulation
        builder.OwnsOne(d => d.Formulation, formulation =>
        {
            formulation.Property(f => f.Form)
                .HasMaxLength(50)
                .HasColumnName("FormulationForm");

            formulation.Property(f => f.Strength)
                .HasMaxLength(50)
                .HasColumnName("FormulationStrength");

            formulation.Property(f => f.RouteOfAdministration)
                .HasMaxLength(50)
                .HasColumnName("RouteOfAdministration");
        });

        // BasePricing (suggested retail price - shops can override)
        builder.OwnsOne(d => d.BasePricing, pricing =>
        {
            pricing.Property(p => p.SuggestedRetailPrice)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("BasePricing_SuggestedRetailPrice");

            pricing.Property(p => p.Currency)
                .HasMaxLength(10)
                .HasColumnName("BasePricing_Currency");

            pricing.Property(p => p.SuggestedTaxRate)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("BasePricing_SuggestedTaxRate");

            pricing.Property(p => p.LastPriceUpdate)
                .HasColumnName("BasePricing_LastPriceUpdate");
        });

        // Regulatory
        builder.OwnsOne(d => d.Regulatory, regulatory =>
        {
            regulatory.Property(r => r.IsPrescriptionRequired)
                .HasColumnName("IsPrescriptionRequired");

            regulatory.Property(r => r.IsHighRisk)
                .HasColumnName("IsHighRisk");

            regulatory.Property(r => r.DrugAuthorityNumber)
                .HasMaxLength(100)
                .HasColumnName("DrugAuthorityNumber");

            regulatory.Property(r => r.ApprovalDate)
                .HasColumnName("ApprovalDate");

            regulatory.Property(r => r.ControlSchedule)
                .HasMaxLength(50)
                .HasColumnName("ControlSchedule");
        });

        // NOTE: Inventory and SupplierInfo are now in ShopInventory entity (multi-tenant model)
        
        // Audit fields from BaseEntity
        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasMaxLength(100);

        builder.Property(d => d.LastUpdated);

        builder.Property(d => d.UpdatedBy)
            .HasMaxLength(100);
    }
}
