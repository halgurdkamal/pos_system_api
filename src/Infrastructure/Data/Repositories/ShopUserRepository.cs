using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for ShopUser entity
/// </summary>
public class ShopUserRepository : IShopUserRepository
{
    private readonly ApplicationDbContext _context;

    public ShopUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShopUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.ShopUsers
            .Include(su => su.User)
            .Include(su => su.Shop)
            .FirstOrDefaultAsync(su => su.Id == id, cancellationToken);
    }

    public async Task<ShopUser?> GetByUserAndShopAsync(string userId, string shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopUsers
            .Include(su => su.User)
            .Include(su => su.Shop)
            .FirstOrDefaultAsync(su => su.UserId == userId && su.ShopId == shopId, cancellationToken);
    }

    public async Task<List<ShopUser>> GetShopMembersAsync(string shopId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.ShopUsers
            .Include(su => su.User)
            .Where(su => su.ShopId == shopId);

        if (activeOnly)
        {
            query = query.Where(su => su.IsActive);
        }

        return await query
            .OrderByDescending(su => su.IsOwner)
            .ThenBy(su => su.Role)
            .ThenBy(su => su.JoinedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ShopUser>> GetUserShopsAsync(string userId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.ShopUsers
            .Include(su => su.Shop)
            .Where(su => su.UserId == userId);

        if (activeOnly)
        {
            query = query.Where(su => su.IsActive);
        }

        return await query
            .OrderByDescending(su => su.IsOwner)
            .ThenBy(su => su.JoinedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ShopUser>> GetShopOwnersAsync(string shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopUsers
            .Include(su => su.User)
            .Where(su => su.ShopId == shopId && su.IsOwner && su.IsActive)
            .OrderBy(su => su.JoinedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(string userId, string shopId, Permission permission, CancellationToken cancellationToken = default)
    {
        var shopUser = await GetByUserAndShopAsync(userId, shopId, cancellationToken);
        return shopUser?.HasPermission(permission) ?? false;
    }

    public async Task<bool> IsShopOwnerAsync(string userId, string shopId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopUsers
            .AnyAsync(su => su.UserId == userId && su.ShopId == shopId && su.IsOwner && su.IsActive, cancellationToken);
    }

    public async Task<ShopUser> CreateAsync(ShopUser shopUser, CancellationToken cancellationToken = default)
    {
        shopUser.CreatedAt = DateTime.UtcNow;
        shopUser.JoinedDate = DateTime.UtcNow;

        _context.ShopUsers.Add(shopUser);
        await _context.SaveChangesAsync(cancellationToken);

        return shopUser;
    }

    public async Task<ShopUser> UpdateAsync(ShopUser shopUser, CancellationToken cancellationToken = default)
    {
        shopUser.LastUpdated = DateTime.UtcNow;

        _context.ShopUsers.Update(shopUser);
        await _context.SaveChangesAsync(cancellationToken);

        return shopUser;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var shopUser = await GetByIdAsync(id, cancellationToken);
        if (shopUser != null)
        {
            _context.ShopUsers.Remove(shopUser);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeactivateAsync(string id, CancellationToken cancellationToken = default)
    {
        var shopUser = await GetByIdAsync(id, cancellationToken);
        if (shopUser != null)
        {
            shopUser.IsActive = false;
            shopUser.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
