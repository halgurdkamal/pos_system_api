using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Shop entity
/// </summary>
public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("Shops");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(s => s.ShopName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(s => s.LegalName)
            .IsRequired()
            .HasMaxLength(300);
        
        builder.Property(s => s.LicenseNumber)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.TaxId)
            .HasMaxLength(100);
        
        builder.Property(s => s.VatRegistrationNumber)
            .HasMaxLength(100);
        
        builder.Property(s => s.PharmacyRegistrationNumber)
            .HasMaxLength(100);
        
        builder.Property(s => s.LogoUrl)
            .HasMaxLength(500);
        
        builder.Property(s => s.BrandColorPrimary)
            .HasMaxLength(10);
        
        builder.Property(s => s.BrandColorSecondary)
            .HasMaxLength(10);
        
        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(s => s.DefaultTaxRate)
            .HasColumnType("decimal(5,2)");
        
        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(s => s.RegistrationDate)
            .IsRequired();
        
        // Configure Address as owned entity
        builder.OwnsOne(s => s.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Address_Street").HasMaxLength(300);
            address.Property(a => a.City).HasColumnName("Address_City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("Address_State").HasMaxLength(100);
            address.Property(a => a.ZipCode).HasColumnName("Address_ZipCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Address_Country").HasMaxLength(100);
        });
        
        // Configure Contact as owned entity
        builder.OwnsOne(s => s.Contact, contact =>
        {
            contact.Property(c => c.Phone).HasColumnName("Contact_Phone").HasMaxLength(50);
            contact.Property(c => c.Email).HasColumnName("Contact_Email").HasMaxLength(200);
            contact.Property(c => c.Website).HasColumnName("Contact_Website").HasMaxLength(300);
        });
        
        // Configure Receipt Configuration as owned entity
        builder.OwnsOne(s => s.ReceiptConfig, receipt =>
        {
            receipt.Property(r => r.ReceiptShopName).HasColumnName("Receipt_ShopName").HasMaxLength(200).IsRequired();
            receipt.Property(r => r.ReceiptHeaderText).HasColumnName("Receipt_HeaderText").HasMaxLength(500);
            receipt.Property(r => r.ReceiptFooterText).HasColumnName("Receipt_FooterText").HasMaxLength(500);
            receipt.Property(r => r.ReturnPolicyText).HasColumnName("Receipt_ReturnPolicy").HasMaxLength(1000);
            receipt.Property(r => r.PharmacistName).HasColumnName("Receipt_PharmacistName").HasMaxLength(200);
            receipt.Property(r => r.ShowLogoOnReceipt).HasColumnName("Receipt_ShowLogo");
            receipt.Property(r => r.ShowTaxBreakdown).HasColumnName("Receipt_ShowTaxBreakdown");
            receipt.Property(r => r.ShowBarcode).HasColumnName("Receipt_ShowBarcode");
            receipt.Property(r => r.ShowQrCode).HasColumnName("Receipt_ShowQrCode");
            receipt.Property(r => r.ShowPharmacyLicense).HasColumnName("Receipt_ShowPharmacyLicense");
            receipt.Property(r => r.ShowVatNumber).HasColumnName("Receipt_ShowVatNumber");
            receipt.Property(r => r.ReceiptWidth).HasColumnName("Receipt_Width");
            receipt.Property(r => r.ReceiptLanguage).HasColumnName("Receipt_Language").HasMaxLength(10);
            receipt.Property(r => r.PrintDuplicateReceipt).HasColumnName("Receipt_PrintDuplicate");
            receipt.Property(r => r.PharmacyWarningText).HasColumnName("Receipt_PharmacyWarning").HasMaxLength(500);
            receipt.Property(r => r.ControlledSubstanceWarning).HasColumnName("Receipt_ControlledSubstanceWarning").HasMaxLength(500);
        });
        
        // Configure Hardware Configuration as owned entity
        builder.OwnsOne(s => s.HardwareConfig, hardware =>
        {
            hardware.Property(h => h.ReceiptPrinterName).HasColumnName("Hardware_ReceiptPrinterName").HasMaxLength(200);
            hardware.Property(h => h.ReceiptPrinterConnectionType).HasColumnName("Hardware_ReceiptPrinterConnection").HasMaxLength(50);
            hardware.Property(h => h.ReceiptPrinterIpAddress).HasColumnName("Hardware_ReceiptPrinterIp").HasMaxLength(50);
            hardware.Property(h => h.ReceiptPrinterPort).HasColumnName("Hardware_ReceiptPrinterPort");
            hardware.Property(h => h.BarcodePrinterName).HasColumnName("Hardware_BarcodePrinterName").HasMaxLength(200);
            hardware.Property(h => h.BarcodePrinterConnectionType).HasColumnName("Hardware_BarcodePrinterConnection").HasMaxLength(50);
            hardware.Property(h => h.BarcodePrinterIpAddress).HasColumnName("Hardware_BarcodePrinterIp").HasMaxLength(50);
            hardware.Property(h => h.BarcodeLabelSize).HasColumnName("Hardware_BarcodeLabelSize").HasConversion<int>();
            hardware.Property(h => h.BarcodeScannerModel).HasColumnName("Hardware_BarcodeScannerModel").HasMaxLength(200);
            hardware.Property(h => h.BarcodeScannerConnectionType).HasColumnName("Hardware_BarcodeScannerConnection").HasMaxLength(50);
            hardware.Property(h => h.AutoSubmitOnScan).HasColumnName("Hardware_AutoSubmitOnScan");
            hardware.Property(h => h.CashDrawerModel).HasColumnName("Hardware_CashDrawerModel").HasMaxLength(200);
            hardware.Property(h => h.CashDrawerEnabled).HasColumnName("Hardware_CashDrawerEnabled");
            hardware.Property(h => h.CashDrawerOpenCommand).HasColumnName("Hardware_CashDrawerOpenCommand").HasMaxLength(100);
            hardware.Property(h => h.PaymentTerminalModel).HasColumnName("Hardware_PaymentTerminalModel").HasMaxLength(200);
            hardware.Property(h => h.PaymentTerminalConnectionType).HasColumnName("Hardware_PaymentTerminalConnection").HasMaxLength(50);
            hardware.Property(h => h.PaymentTerminalIpAddress).HasColumnName("Hardware_PaymentTerminalIp").HasMaxLength(50);
            hardware.Property(h => h.IntegratedPayments).HasColumnName("Hardware_IntegratedPayments");
            hardware.Property(h => h.PosTerminalId).HasColumnName("Hardware_PosTerminalId").HasMaxLength(50);
            hardware.Property(h => h.PosTerminalName).HasColumnName("Hardware_PosTerminalName").HasMaxLength(200);
            hardware.Property(h => h.CustomerDisplayEnabled).HasColumnName("Hardware_CustomerDisplayEnabled");
            hardware.Property(h => h.CustomerDisplayType).HasColumnName("Hardware_CustomerDisplayType").HasMaxLength(100);
        });
        
        // Store JSON collections
        builder.Property(s => s.OperatingHours)
            .HasColumnType("jsonb");
        
        builder.Property(s => s.ShopImageUrls)
            .HasColumnType("jsonb");
        
        builder.Property(s => s.AcceptedInsuranceProviders)
            .HasColumnType("jsonb");
        
        // Many-to-many relationship with Users via ShopUser
        builder.HasMany(s => s.Members)
            .WithOne(su => su.Shop)
            .HasForeignKey(su => su.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Base entity properties
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.LastUpdated);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);
        
        // Indexes
        builder.HasIndex(s => s.LicenseNumber).IsUnique();
        builder.HasIndex(s => s.ShopName);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.RegistrationDate);
    }
}
