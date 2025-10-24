using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for ShopInventory entity
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShopInventory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.ShopInventory
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<ShopInventory?> GetByShopAndDrugAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopInventory
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ShopId == shopId && i.DrugId == drugId, cancellationToken);
    }

    public async Task<(IEnumerable<ShopInventory> Items, int TotalCount)> GetByShopAsync(
        string shopId,
        int page,
        int limit,
        bool? isAvailable = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId);

        // Filter by availability if specified
        if (isAvailable.HasValue)
        {
            if (isAvailable.Value)
            {
                query = query.Where(i => i.TotalStock > 0);
            }
            else
            {
                query = query.Where(i => i.TotalStock == 0);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var inventory = await query
            .OrderBy(i => i.DrugId)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (inventory, totalCount);
    }

    public async Task<IEnumerable<ShopInventory>> GetAllByShopAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopInventory>> GetLowStockAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId && i.TotalStock <= i.ReorderPoint)
            .OrderBy(i => i.TotalStock)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopInventory>> GetExpiringBatchesAsync(
        string shopId,
        int daysFromNow,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId)
            .ToListAsync(cancellationToken);

        // Filter in-memory to check batches expiry dates (batches are stored as jsonb)
        return inventory
            .Where(i => i.GetExpiringBatches(daysFromNow).Any())
            .OrderBy(i => i.DrugId)
            .ToList();
    }

    public async Task<IEnumerable<ShopInventory>> GetOutOfStockAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId && i.TotalStock == 0)
            .OrderBy(i => i.DrugId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopInventory>> SearchByDrugNameAsync(
        string shopId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        // Note: This query requires joining with Drugs table to search by name
        // Since we don't have navigation properties, we need to do a separate query
        var drugIds = await _context.Drugs
            .Where(d => d.BrandName.ToLower().Contains(searchTerm.ToLower()) ||
                       d.GenericName.ToLower().Contains(searchTerm.ToLower()))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        return await _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId && drugIds.Contains(i.DrugId))
            .OrderBy(i => i.DrugId)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopInventory> AddAsync(ShopInventory inventory, CancellationToken cancellationToken = default)
    {
        await _context.ShopInventory.AddAsync(inventory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return inventory;
    }

    public async Task<ShopInventory> UpdateAsync(ShopInventory inventory, CancellationToken cancellationToken = default)
    {
        _context.ShopInventory.Update(inventory);
        await _context.SaveChangesAsync(cancellationToken);
        return inventory;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.ShopInventory.FindAsync(new object[] { id }, cancellationToken);
        if (inventory != null)
        {
            _context.ShopInventory.Remove(inventory);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopInventory
            .AnyAsync(i => i.ShopId == shopId && i.DrugId == drugId, cancellationToken);
    }

    public async Task<decimal> GetTotalStockValueAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _context.ShopInventory
            .AsNoTracking()
            .Where(i => i.ShopId == shopId)
            .ToListAsync(cancellationToken);

        // Calculate total value from batches (in-memory since batches are jsonb)
        return inventory.Sum(i => i.Batches.Sum(b => b.QuantityOnHand * b.PurchasePrice));
    }
}
