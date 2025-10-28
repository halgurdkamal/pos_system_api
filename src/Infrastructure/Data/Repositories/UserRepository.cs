using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ShopMemberships)
                .ThenInclude(sm => sm.Shop)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ShopMemberships)
                .ThenInclude(sm => sm.Shop)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ShopMemberships)
                .ThenInclude(sm => sm.Shop)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ShopMemberships)
                .ThenInclude(sm => sm.Shop)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
    }

    /// <summary>
    /// Get user by identifier (username, email, or phone)
    /// Includes shop memberships and shop details for authorization
    /// </summary>
    public async Task<User?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ShopMemberships)
                .ThenInclude(sm => sm.Shop)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => 
                u.Username == identifier || 
                u.Email == identifier || 
                u.Phone == identifier, 
                cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username || u.Email == email, cancellationToken);
    }
}
