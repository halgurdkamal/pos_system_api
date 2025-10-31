using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Domain.Categories.Entities;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Infrastructure.Data;
using pos_system_api.Infrastructure.SampleData;

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
        var hasCategories = await _context.Categories.AnyAsync();
        var hasDrugs = await _context.Drugs.AnyAsync();
        var hasShops = await _context.Shops.AnyAsync();
        var hasSuppliers = await _context.Suppliers.AnyAsync();
        var hasInventory = await _context.ShopInventory.AnyAsync();

        // Only seed if database is empty
        if (hasCategories || hasDrugs || hasShops || hasSuppliers || hasInventory)
        {
            Console.WriteLine("Database already contains data. Skipping seeding.");
            return;
        }

        Console.WriteLine("Seeding database with sample data...");

        // 1. Seed Categories first
        var categories = CategorySeedData.GetCategories();
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
        Console.WriteLine($"✓ Seeded {categories.Count} categories");

        // 2. Seed Drugs
        var drugs = GenerateSampleDrugs(categories);
        await _context.Drugs.AddRangeAsync(drugs);
        await _context.SaveChangesAsync();
        Console.WriteLine($"✓ Seeded {drugs.Count} drugs");

        // 3. Seed Shops
        var shops = ShopSeeder.GetSeedData();
        await _context.Shops.AddRangeAsync(shops);
        await _context.SaveChangesAsync();
        Console.WriteLine($"✓ Seeded {shops.Count} shops");

        // 4. Seed Suppliers
        var suppliers = SupplierSeeder.GetSeedData();
        await _context.Suppliers.AddRangeAsync(suppliers);
        await _context.SaveChangesAsync();
        Console.WriteLine($"✓ Seeded {suppliers.Count} suppliers");

        // 5. Seed Inventory for active shops
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

        Console.WriteLine("Database seeding completed successfully!");
    }

    private List<Drug> GenerateSampleDrugs(List<Category> categories)
    {
        var drugs = new List<Drug>();
        var categoryIds = categories.Select(c => c.CategoryId).ToList();

        // Create sample drugs for each category
        foreach (var categoryId in categoryIds.Take(5)) // Limit to first 5 categories for seeding
        {
            for (int i = 0; i < 3; i++) // 3 drugs per category
            {
                var drug = SampleDrugProvider.GenerateDrug();
                drug.CategoryId = categoryId;
                drug.DrugId = $"DRG-{categoryId.Split('-')[1]}-{i + 1:D3}";
                drug.CreatedAt = DateTime.UtcNow;
                drug.CreatedBy = "system";
                drugs.Add(drug);
            }
        }

        return drugs;
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
