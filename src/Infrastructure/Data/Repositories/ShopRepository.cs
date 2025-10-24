using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Shop entity
/// </summary>
public class ShopRepository : IShopRepository
{
    private readonly ApplicationDbContext _context;

    public ShopRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Shop?> GetByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.LicenseNumber == licenseNumber, cancellationToken);
    }

    public async Task<(IEnumerable<Shop> Shops, int TotalCount)> GetAllAsync(
        int page, 
        int limit, 
        ShopStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shops.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var shops = await query
            .OrderByDescending(s => s.RegistrationDate)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (shops, totalCount);
    }

    public async Task<IEnumerable<Shop>> SearchByNameAsync(
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .Where(s => s.ShopName.ToLower().Contains(searchTerm.ToLower()) ||
                       s.LegalName.ToLower().Contains(searchTerm.ToLower()))
            .OrderBy(s => s.ShopName)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<Shop> AddAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        await _context.Shops.AddAsync(shop, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return shop;
    }

    public async Task<Shop> UpdateAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        _context.Shops.Update(shop);
        await _context.SaveChangesAsync(cancellationToken);
        return shop;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var shop = await _context.Shops.FindAsync(new object[] { id }, cancellationToken);
        if (shop != null)
        {
            _context.Shops.Remove(shop);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> LicenseNumberExistsAsync(
        string licenseNumber, 
        string? excludeShopId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shops.Where(s => s.LicenseNumber == licenseNumber);

        if (!string.IsNullOrEmpty(excludeShopId))
        {
            query = query.Where(s => s.Id != excludeShopId);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
