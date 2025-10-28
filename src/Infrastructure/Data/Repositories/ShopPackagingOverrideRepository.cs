using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class ShopPackagingOverrideRepository : IShopPackagingOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public ShopPackagingOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShopPackagingOverride>> GetByShopAndDrugAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopPackagingOverrides
            .AsNoTracking()
            .Where(o => o.ShopId == shopId && o.DrugId == drugId)
            .OrderBy(o => o.PackagingLevelId == null)
            .ThenBy(o => o.CustomLevelOrder)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopPackagingOverride?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShopPackagingOverrides
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<ShopPackagingOverride> AddAsync(
        ShopPackagingOverride overrideEntity,
        CancellationToken cancellationToken = default)
    {
        overrideEntity.CreatedAt = DateTime.UtcNow;
        _context.ShopPackagingOverrides.Add(overrideEntity);
        await _context.SaveChangesAsync(cancellationToken);
        return overrideEntity;
    }

    public async Task<ShopPackagingOverride> UpdateAsync(
        ShopPackagingOverride overrideEntity,
        CancellationToken cancellationToken = default)
    {
        overrideEntity.LastUpdated = DateTime.UtcNow;
        _context.ShopPackagingOverrides.Update(overrideEntity);
        await _context.SaveChangesAsync(cancellationToken);
        return overrideEntity;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ShopPackagingOverrides
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity != null)
        {
            _context.ShopPackagingOverrides.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
