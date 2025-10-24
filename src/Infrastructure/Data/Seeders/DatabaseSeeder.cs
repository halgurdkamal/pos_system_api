using Microsoft.EntityFrameworkCore;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Infrastructure.Data.Seeders;

/// <summary>
/// Main database seeder service that coordinates all seeding operations
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;

    public DatabaseSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seed all entities in the correct order (respecting foreign key constraints)
    /// </summary>
    public async Task SeedAllAsync()
    {
        // Check if database has data
        var hasShops = await _context.Shops.AnyAsync();
        var hasSuppliers = await _context.Suppliers.AnyAsync();
        var hasInventory = await _context.ShopInventory.AnyAsync();

        // Only seed shops/suppliers/inventory if database is empty
        if (hasShops || hasSuppliers || hasInventory)
        {
            Console.WriteLine("Database already contains data. Skipping shop/supplier/inventory seeding.");
            // Note: Users can still be seeded independently
        }
        else
        {
            Console.WriteLine("Seeding database with sample data...");

            // 1. Seed Shops
            var shops = ShopSeeder.GetSeedData();
            await _context.Shops.AddRangeAsync(shops);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✓ Seeded {shops.Count} shops");

            // 2. Seed Suppliers
            var suppliers = SupplierSeeder.GetSeedData();
            await _context.Suppliers.AddRangeAsync(suppliers);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✓ Seeded {suppliers.Count} suppliers");

            // 3. Get existing drugs (should already exist from initial migration)
            var drugs = await _context.Drugs.Take(10).ToListAsync();
            
            if (drugs.Count >= 3)
            {
                // 4. Seed Inventory for active shops
                var activeShops = shops.Where(s => s.Status == Core.Domain.Shops.Entities.ShopStatus.Active).ToList();
                var drugIds = drugs.Select(d => d.Id).ToList();
                var supplierIds = suppliers.Where(s => s.IsActive).Select(s => s.Id).ToList();

                var inventories = InventorySeeder.GetSeedDataForAllShops(
                    activeShops.Select(s => s.Id).ToList(),
                    drugIds,
                    supplierIds
                );

                await _context.ShopInventory.AddRangeAsync(inventories);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✓ Seeded {inventories.Count} inventory items");
            }
            else
            {
                Console.WriteLine("⚠ Not enough drugs in database. Skipping inventory seeding.");
            }

            Console.WriteLine("Database seeding completed successfully!");
        }
    }

    /// <summary>
    /// Clear all seeded data (useful for testing)
    /// </summary>
    public async Task ClearAllAsync()
    {
        Console.WriteLine("Clearing all seeded data...");

        // Order matters: delete in reverse order of foreign key dependencies
        _context.ShopInventory.RemoveRange(await _context.ShopInventory.ToListAsync());
        _context.Suppliers.RemoveRange(await _context.Suppliers.ToListAsync());
        _context.Shops.RemoveRange(await _context.Shops.ToListAsync());

        await _context.SaveChangesAsync();
        Console.WriteLine("All seeded data cleared.");
    }
}
