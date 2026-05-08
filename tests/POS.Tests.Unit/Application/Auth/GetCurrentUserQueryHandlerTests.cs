using System.Security.Claims;
using pos_system_api.Core.Application.Auth.Queries.GetCurrentUser;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Shops.Entities;

namespace POS.Tests.Unit.Application.Auth;

public class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithoutNameIdentifierClaim_ReturnsNull()
    {
        var handler = new GetCurrentUserQueryHandler(new FakeUserRepository());
        var principal = BuildPrincipal();

        var result = await handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserNotInDb_ReturnsNull()
    {
        var handler = new GetCurrentUserQueryHandler(new FakeUserRepository());
        var principal = BuildPrincipal((ClaimTypes.NameIdentifier, "missing"));

        var result = await handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_LoadsUserFromDb_PopulatesAllFields()
    {
        var user = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            FullName = "Alice Smith",
            SystemRole = SystemRole.SuperAdmin,
            IsActive = true,
            IsEmailVerified = true,
            LastLoginAt = new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc),
            Phone = "+10000000000",
            ProfileImageUrl = "https://cdn.example.com/a.png",
        };
        var handler = new GetCurrentUserQueryHandler(new FakeUserRepository(user));
        var principal = BuildPrincipal((ClaimTypes.NameIdentifier, user.Id));

        var result = await handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
        result.FullName.Should().Be("Alice Smith");
        result.SystemRole.Should().Be("SuperAdmin");
        result.IsActive.Should().BeTrue();
        result.IsEmailVerified.Should().BeTrue();
        result.LastLoginAt.Should().Be(new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc),
            "Q-12: /me must reflect the persisted LastLoginAt, not return null");
        result.Phone.Should().Be("+10000000000");
        result.ProfileImageUrl.Should().Be("https://cdn.example.com/a.png");
        result.Shops.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithActiveShopMembership_PopulatesShopsCollection()
    {
        var user = new User
        {
            Username = "owner",
            Email = "o@x.com",
            FullName = "Owner",
            SystemRole = SystemRole.User,
            ShopMemberships = new List<ShopUser>
            {
                new ShopUser
                {
                    ShopId = "SHOP-A",
                    Role = ShopRole.Owner,
                    IsOwner = true,
                    IsActive = true,
                    JoinedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Permissions = new List<Permission> { Permission.ProcessSales, Permission.ViewReports },
                    Shop = new Shop { ShopName = "Main Branch" },
                },
                new ShopUser
                {
                    ShopId = "SHOP-B",
                    Role = ShopRole.Cashier,
                    IsOwner = false,
                    IsActive = false, // inactive — should be filtered out
                    Shop = new Shop { ShopName = "Old Branch" },
                },
            },
        };
        var handler = new GetCurrentUserQueryHandler(new FakeUserRepository(user));
        var principal = BuildPrincipal((ClaimTypes.NameIdentifier, user.Id));

        var result = await handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result!.Shops.Should().HaveCount(1, "inactive memberships are filtered");
        var shop = result.Shops[0];
        shop.ShopId.Should().Be("SHOP-A");
        shop.ShopName.Should().Be("Main Branch");
        shop.Role.Should().Be("Owner");
        shop.IsOwner.Should().BeTrue();
        shop.Permissions.Should().BeEquivalentTo(new[] { "ProcessSales", "ViewReports" });
    }

    private static ClaimsPrincipal BuildPrincipal(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.type, c.value)),
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _byId;

        public FakeUserRepository(params User[] seed) =>
            _byId = seed.ToDictionary(u => u.Id);

        public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(userId, out var u) ? u : null);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateLoginInfoAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
