using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Infrastructure.Auth;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds initial user accounts for testing
/// </summary>
public class UserSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _passwordHasher;

    public UserSeeder(ApplicationDbContext context, PasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        // Check if users already exist
        if (await _context.Users.AnyAsync())
        {
            Console.WriteLine("Users already exist. Skipping user seeding.");
            return;
        }

        Console.WriteLine("Seeding users...");

        // Get shops for shop-specific users
        var shops = await _context.Shops.Take(3).ToListAsync();

        if (shops.Count == 0)
        {
            Console.WriteLine("No shops found. Please seed shops first.");
            return;
        }

        var users = new List<User>();
        var shopUsers = new List<ShopUser>();

        // Create SuperAdmin user (access to all shops)
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@possystem.com",
            PasswordHash = _passwordHasher.HashPassword("Admin@123"),
            FullName = "System Administrator",
            SystemRole = SystemRole.SuperAdmin,
            IsActive = true,
            IsEmailVerified = true,
            Phone = "+1234567890",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
        users.Add(adminUser);

        // Create ShopOwner for each shop
        foreach (var shop in shops)
        {
            var shopOwner = new User
            {
                Username = $"owner_{shop.Id.ToLower().Replace("shop-", "")}",
                Email = $"owner.{shop.Id.ToLower().Replace("shop-", "")}@possystem.com",
                PasswordHash = _passwordHasher.HashPassword("Owner@123"),
                FullName = $"{shop.ShopName} Owner",
                SystemRole = SystemRole.User,
                IsActive = true,
                IsEmailVerified = true,
                Phone = shop.Contact.Phone,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            users.Add(shopOwner);

            // Create ShopUser entry (owner membership)
            var shopUserOwner = new ShopUser
            {
                UserId = shopOwner.Id,
                ShopId = shop.Id,
                Role = ShopRole.Owner,
                IsOwner = true,
                IsActive = true,
                JoinedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            shopUserOwner.SetRole(ShopRole.Owner); // Set default Owner permissions
            shopUsers.Add(shopUserOwner);

            // Create Staff for each shop
            var staff = new User
            {
                Username = $"staff_{shop.Id.ToLower().Replace("shop-", "")}",
                Email = $"staff.{shop.Id.ToLower().Replace("shop-", "")}@possystem.com",
                PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                FullName = $"{shop.ShopName} Staff",
                SystemRole = SystemRole.User,
                IsActive = true,
                IsEmailVerified = true,
                Phone = shop.Contact.Phone,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            users.Add(staff);

            // Create ShopUser entry (cashier membership)
            var shopUserStaff = new ShopUser
            {
                UserId = staff.Id,
                ShopId = shop.Id,
                Role = ShopRole.Cashier,
                IsOwner = false,
                IsActive = true,
                JoinedDate = DateTime.UtcNow,
                InvitedBy = shopOwner.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            shopUserStaff.SetRole(ShopRole.Cashier); // Set default Cashier permissions
            shopUsers.Add(shopUserStaff);
        }

        // Save all users and shop memberships
        await _context.Users.AddRangeAsync(users);
        await _context.ShopUsers.AddRangeAsync(shopUsers);
        await _context.SaveChangesAsync();

        Console.WriteLine($"Seeded {users.Count} users successfully:");
        Console.WriteLine($"  - 1 Admin: admin / Admin@123");
        foreach (var shop in shops)
        {
            Console.WriteLine($"  - Shop {shop.Id}:");
            Console.WriteLine($"      Owner: owner_{shop.Id.ToLower().Replace("shop-", "")} / Owner@123");
            Console.WriteLine($"      Staff: staff_{shop.Id.ToLower().Replace("shop-", "")} / Staff@123");
        }
    }
}
