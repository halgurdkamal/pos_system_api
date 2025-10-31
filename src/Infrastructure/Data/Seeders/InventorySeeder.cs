using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Infrastructure.Data.Seeders;

/// <summary>
/// Seeder for creating sample shop inventory with batches
/// Requires existing Shops, Drugs, and Suppliers
/// </summary>
public static class InventorySeeder
{
    public static List<ShopInventory> GetSeedData(string shopId, List<string> drugIds, List<string> supplierIds)
    {
        var inventories = new List<ShopInventory>();

        if (drugIds.Count < 3 || supplierIds.Count < 2)
        {
            return inventories; // Not enough data to seed
        }

        // Inventory 1: High stock item with multiple batches
        var inventory1 = new ShopInventory(
            shopId: shopId,
            drugId: drugIds[0],
            reorderPoint: 50,
            storageLocation: "Shelf A1",
            shopPricing: new ShopPricing
            {
                CostPrice = 15.50m,
                SellingPrice = 24.99m,
                Currency = "USD",
                TaxRate = 0.08m
            }
        );

        // Add multiple batches (FIFO testing)
        inventory1.AddBatch(new Batch
        {
            BatchNumber = "BATCH-001-2024",
            SupplierId = supplierIds[0],
            QuantityOnHand = 150,
            ReceivedDate = DateTime.UtcNow.AddDays(-60),
            ExpiryDate = DateTime.UtcNow.AddMonths(18),
            PurchasePrice = 15.00m,
            SellingPrice = 24.99m,
            Status = BatchStatus.Active
        });

        inventory1.AddBatch(new Batch
        {
            BatchNumber = "BATCH-002-2024",
            SupplierId = supplierIds[0],
            QuantityOnHand = 100,
            ReceivedDate = DateTime.UtcNow.AddDays(-30),
            ExpiryDate = DateTime.UtcNow.AddMonths(24),
            PurchasePrice = 15.50m,
            SellingPrice = 24.99m,
            Status = BatchStatus.Active
        });

        // Inventory 2: Low stock item (below reorder point)
        var inventory2 = new ShopInventory(
            shopId: shopId,
            drugId: drugIds[1],
            reorderPoint: 100,
            storageLocation: "Shelf B2",
            shopPricing: new ShopPricing
            {
                CostPrice = 8.75m,
                SellingPrice = 14.99m,
                Currency = "USD",
                TaxRate = 0.08m
            }
        );

        inventory2.AddBatch(new Batch
        {
            BatchNumber = "BATCH-003-2024",
            SupplierId = supplierIds[1],
            QuantityOnHand = 45, // Below reorder point
            ReceivedDate = DateTime.UtcNow.AddDays(-45),
            ExpiryDate = DateTime.UtcNow.AddMonths(12),
            PurchasePrice = 8.75m,
            SellingPrice = 14.99m,
            Status = BatchStatus.Active
        });

        // Inventory 3: Item with expiring batch
        var inventory3 = new ShopInventory(
            shopId: shopId,
            drugId: drugIds[2],
            reorderPoint: 30,
            storageLocation: "Shelf C3",
            shopPricing: new ShopPricing
            {
                CostPrice = 22.00m,
                SellingPrice = 35.99m,
                Currency = "USD",
                TaxRate = 0.08m
            }
        );

        // Batch expiring soon (for expiry alerts)
        inventory3.AddBatch(new Batch
        {
            BatchNumber = "BATCH-004-2024",
            SupplierId = supplierIds[0],
            QuantityOnHand = 80,
            ReceivedDate = DateTime.UtcNow.AddDays(-150),
            ExpiryDate = DateTime.UtcNow.AddDays(25), // Expires in 25 days
            PurchasePrice = 22.00m,
            SellingPrice = 35.99m,
            Status = BatchStatus.Active
        });

        // Fresh batch
        inventory3.AddBatch(new Batch
        {
            BatchNumber = "BATCH-005-2024",
            SupplierId = supplierIds[1],
            QuantityOnHand = 60,
            ReceivedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddMonths(20),
            PurchasePrice = 21.50m,
            SellingPrice = 35.99m,
            Status = BatchStatus.Active
        });

        inventories.Add(inventory1);
        inventories.Add(inventory2);
        inventories.Add(inventory3);

        return inventories;
    }

    /// <summary>
    /// Create inventory for multiple shops
    /// </summary>
    public static List<ShopInventory> GetSeedDataForAllShops(
        List<string> shopIds,
        List<string> drugIds,
        List<string> supplierIds)
    {
        var allInventories = new List<ShopInventory>();

        foreach (var shopId in shopIds)
        {
            var shopInventories = GetSeedData(shopId, drugIds, supplierIds);
            allInventories.AddRange(shopInventories);
        }

        return allInventories;
    }
}
