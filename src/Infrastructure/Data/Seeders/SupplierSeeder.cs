using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.Infrastructure.Data.Seeders;

/// <summary>
/// Seeder for creating sample drug suppliers
/// </summary>
public static class SupplierSeeder
{
    public static List<Supplier> GetSeedData()
    {
        var suppliers = new List<Supplier>();

        // Supplier 1: Global Pharma Inc. (Manufacturer)
        var supplier1 = new Supplier(
            supplierName: "Global Pharma Inc.",
            supplierType: SupplierType.Manufacturer,
            contactNumber: "+1-800-555-1001",
            email: "sales@globalpharma.com",
            address: new Address
            {
                Street = "1000 Pharma Drive",
                City = "Boston",
                State = "MA",
                ZipCode = "02101",
                Country = "USA"
            }
        )
        {
            PaymentTerms = "Net 60",
            DeliveryLeadTime = 14,
            MinimumOrderValue = 5000.00m,
            Website = "https://globalpharma.com",
            TaxId = "TAX-MA-001",
            LicenseNumber = "MFG-2024-001",
            IsActive = true
        };

        // Supplier 2: MedDistro Solutions (Distributor)
        var supplier2 = new Supplier(
            supplierName: "MedDistro Solutions",
            supplierType: SupplierType.Distributor,
            contactNumber: "+1-888-555-2002",
            email: "orders@meddistro.com",
            address: new Address
            {
                Street = "500 Distribution Blvd",
                City = "Atlanta",
                State = "GA",
                ZipCode = "30301",
                Country = "USA"
            }
        )
        {
            PaymentTerms = "Net 45",
            DeliveryLeadTime = 7,
            MinimumOrderValue = 2000.00m,
            Website = "https://meddistro.com",
            TaxId = "TAX-GA-002",
            LicenseNumber = "DIST-2024-002",
            IsActive = true
        };

        // Supplier 3: QuickMed Wholesalers (Wholesaler)
        var supplier3 = new Supplier(
            supplierName: "QuickMed Wholesalers",
            supplierType: SupplierType.Wholesaler,
            contactNumber: "+1-877-555-3003",
            email: "support@quickmed.com",
            address: new Address
            {
                Street = "200 Wholesale Way",
                City = "Houston",
                State = "TX",
                ZipCode = "77001",
                Country = "USA"
            }
        )
        {
            PaymentTerms = "Net 30",
            DeliveryLeadTime = 5,
            MinimumOrderValue = 1000.00m,
            Website = "https://quickmed.com",
            TaxId = "TAX-TX-003",
            LicenseNumber = "WHSL-2024-003",
            IsActive = true
        };

        // Supplier 4: PharmaLink Agency (Local Agent)
        var supplier4 = new Supplier(
            supplierName: "PharmaLink Agency",
            supplierType: SupplierType.LocalAgent,
            contactNumber: "+1-866-555-4004",
            email: "contact@pharmalink.com",
            address: new Address
            {
                Street = "300 Commerce Street",
                City = "Miami",
                State = "FL",
                ZipCode = "33101",
                Country = "USA"
            }
        )
        {
            PaymentTerms = "COD",
            DeliveryLeadTime = 3,
            MinimumOrderValue = 500.00m,
            Website = "https://pharmalink.com",
            TaxId = "TAX-FL-004",
            IsActive = true
        };

        // Supplier 5: Inactive Supplier (for filtering tests)
        var supplier5 = new Supplier(
            supplierName: "OldMed Suppliers",
            supplierType: SupplierType.Distributor,
            contactNumber: "+1-800-555-5005",
            email: "info@oldmed.com",
            address: new Address
            {
                Street = "999 Legacy Road",
                City = "Phoenix",
                State = "AZ",
                ZipCode = "85001",
                Country = "USA"
            }
        )
        {
            PaymentTerms = "Net 30",
            DeliveryLeadTime = 10,
            MinimumOrderValue = 1500.00m,
            IsActive = false
        };

        suppliers.Add(supplier1);
        suppliers.Add(supplier2);
        suppliers.Add(supplier3);
        suppliers.Add(supplier4);
        suppliers.Add(supplier5);

        return suppliers;
    }
}
