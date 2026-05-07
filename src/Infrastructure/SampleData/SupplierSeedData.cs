using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Suppliers.Entities;
using System.Reflection;

namespace pos_system_api.Infrastructure.SampleData;

public static class SupplierSeedData
{
    public static List<Supplier> GetSuppliers()
    {
        var suppliers = new List<Supplier>();

        // Helper method to set protected Id property
        void SetSupplierId(Supplier supplier, string id)
        {
            var idProperty = typeof(Supplier).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            idProperty?.SetValue(supplier, id);
        }

        var supplier1 = new Supplier
        {
            SupplierName = "Global Pharma Inc.",
            SupplierType = SupplierType.Manufacturer,
            ContactNumber = "+1-800-555-1001",
            Email = "sales@globalpharma.com",
            Address = new Address
            {
                Street = "1000 Pharma Drive",
                City = "Boston",
                State = "MA",
                ZipCode = "02101",
                Country = "USA"
            },
            PaymentTerms = "Net 60",
            DeliveryLeadTime = 14,
            MinimumOrderValue = 5000,
            IsActive = true,
            Website = "https://globalpharma.com",
            TaxId = "TAX-MA-001",
            LicenseNumber = "MFG-2024-001",
            CreatedAt = DateTime.Parse("2025-10-29T17:31:48.299341Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        SetSupplierId(supplier1, "SUP-3F4F4A55");
        suppliers.Add(supplier1);

        var supplier2 = new Supplier
        {
            SupplierName = "MedDistro Solutions",
            SupplierType = SupplierType.Distributor,
            ContactNumber = "+1-888-555-2002",
            Email = "orders@meddistro.com",
            Address = new Address
            {
                Street = "500 Distribution Blvd",
                City = "Atlanta",
                State = "GA",
                ZipCode = "30301",
                Country = "USA"
            },
            PaymentTerms = "Net 45",
            DeliveryLeadTime = 7,
            MinimumOrderValue = 2000,
            IsActive = true,
            Website = "https://meddistro.com",
            TaxId = "TAX-GA-002",
            LicenseNumber = "DIST-2024-002",
            CreatedAt = DateTime.Parse("2025-10-29T17:31:48.299425Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        SetSupplierId(supplier2, "SUP-2507C9E8");
        suppliers.Add(supplier2);

        var supplier3 = new Supplier
        {
            SupplierName = "OldMed Suppliers",
            SupplierType = SupplierType.Distributor,
            ContactNumber = "+1-800-555-5005",
            Email = "info@oldmed.com",
            Address = new Address
            {
                Street = "999 Legacy Road",
                City = "Phoenix",
                State = "AZ",
                ZipCode = "85001",
                Country = "USA"
            },
            PaymentTerms = "Net 30",
            DeliveryLeadTime = 10,
            MinimumOrderValue = 1500,
            IsActive = false,
            Website = null,
            TaxId = null,
            LicenseNumber = null,
            CreatedAt = DateTime.Parse("2025-10-29T17:31:48.299433Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        SetSupplierId(supplier3, "SUP-ACCFB43C");
        suppliers.Add(supplier3);

        var supplier4 = new Supplier
        {
            SupplierName = "PharmaLink Agency",
            SupplierType = SupplierType.LocalAgent,
            ContactNumber = "+1-866-555-4004",
            Email = "contact@pharmalink.com",
            Address = new Address
            {
                Street = "300 Commerce Street",
                City = "Miami",
                State = "FL",
                ZipCode = "33101",
                Country = "USA"
            },
            PaymentTerms = "COD",
            DeliveryLeadTime = 3,
            MinimumOrderValue = 500,
            IsActive = true,
            Website = "https://pharmalink.com",
            TaxId = "TAX-FL-004",
            LicenseNumber = null,
            CreatedAt = DateTime.Parse("2025-10-29T17:31:48.299427Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        SetSupplierId(supplier4, "SUP-32380A98");
        suppliers.Add(supplier4);

        var supplier5 = new Supplier
        {
            SupplierName = "QuickMed Wholesalers",
            SupplierType = SupplierType.Wholesaler,
            ContactNumber = "+1-877-555-3003",
            Email = "support@quickmed.com",
            Address = new Address
            {
                Street = "200 Wholesale Way",
                City = "Houston",
                State = "TX",
                ZipCode = "77001",
                Country = "USA"
            },
            PaymentTerms = "Net 30",
            DeliveryLeadTime = 5,
            MinimumOrderValue = 1000,
            IsActive = true,
            Website = "https://quickmed.com",
            TaxId = "TAX-TX-003",
            LicenseNumber = "WHSL-2024-003",
            CreatedAt = DateTime.Parse("2025-10-29T17:31:48.299426Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        SetSupplierId(supplier5, "SUP-C24100F0");
        suppliers.Add(supplier5);

        return suppliers;
    }
}