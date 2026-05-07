using System.Security.Claims;
using pos_system_api.Core.Application.Auth.Queries.GetCurrentUser;

namespace POS.Tests.Unit.Application.Auth;

public class GetCurrentUserQueryHandlerTests
{
    private readonly GetCurrentUserQueryHandler _handler = new();

    [Fact]
    public async Task Handle_WithoutNameIdentifierClaim_ReturnsNull()
    {
        var principal = BuildPrincipal();

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithBasicClaims_PopulatesUserDto()
    {
        var principal = BuildPrincipal(
            (ClaimTypes.NameIdentifier, "user-123"),
            (ClaimTypes.Name, "alice"),
            (ClaimTypes.Email, "alice@example.com"),
            ("fullName", "Alice Smith"),
            ("systemRole", "SuperAdmin"));

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-123");
        result.Username.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
        result.FullName.Should().Be("Alice Smith");
        result.SystemRole.Should().Be("SuperAdmin");
        result.Shops.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DefaultsSystemRoleToUser_WhenNotInClaims()
    {
        var principal = BuildPrincipal(
            (ClaimTypes.NameIdentifier, "u1"));

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result!.SystemRole.Should().Be("User");
    }

    [Fact]
    public async Task Handle_SingleShop_PopulatesRoleOwnershipAndPermissions()
    {
        var principal = BuildPrincipal(
            (ClaimTypes.NameIdentifier, "u1"),
            ("shopIds", "SHOP-A"),
            ("shop:SHOP-A:role", "Manager"),
            ("shop:SHOP-A:isOwner", "False"),
            ("shop:SHOP-A:permission", "ViewReports"),
            ("shop:SHOP-A:permission", "ManageInventory"));

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result!.Shops.Should().HaveCount(1);
        var shop = result.Shops[0];
        shop.ShopId.Should().Be("SHOP-A");
        shop.Role.Should().Be("Manager");
        shop.IsOwner.Should().BeFalse();
        shop.Permissions.Should().BeEquivalentTo(new[] { "ViewReports", "ManageInventory" });
    }

    [Fact]
    public async Task Handle_MultipleShops_BuildsOnePerCommaDelimitedShopId()
    {
        var principal = BuildPrincipal(
            (ClaimTypes.NameIdentifier, "u1"),
            ("shopIds", "SHOP-A,SHOP-B,SHOP-C"),
            ("shop:SHOP-A:role", "Owner"),
            ("shop:SHOP-A:isOwner", "True"),
            ("shop:SHOP-B:role", "Cashier"),
            ("shop:SHOP-C:role", "Pharmacist"));

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result!.Shops.Should().HaveCount(3);
        result.Shops.Select(s => s.ShopId).Should().BeEquivalentTo(new[] { "SHOP-A", "SHOP-B", "SHOP-C" });

        var ownedShop = result.Shops.Single(s => s.ShopId == "SHOP-A");
        ownedShop.IsOwner.Should().BeTrue();
        ownedShop.Role.Should().Be("Owner");

        var cashier = result.Shops.Single(s => s.ShopId == "SHOP-B");
        cashier.IsOwner.Should().BeFalse();
        cashier.Role.Should().Be("Cashier");
    }

    [Fact]
    public async Task Handle_ShopWithoutRoleClaim_DefaultsToCustom()
    {
        var principal = BuildPrincipal(
            (ClaimTypes.NameIdentifier, "u1"),
            ("shopIds", "SHOP-X"));

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result!.Shops[0].Role.Should().Be("Custom");
        result.Shops[0].IsOwner.Should().BeFalse();
        result.Shops[0].Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyShopIds_ReturnsNoShops()
    {
        var principal = BuildPrincipal(
            (ClaimTypes.NameIdentifier, "u1"),
            ("shopIds", ""));

        var result = await _handler.Handle(new GetCurrentUserQuery(principal), CancellationToken.None);

        result!.Shops.Should().BeEmpty();
    }

    private static ClaimsPrincipal BuildPrincipal(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.type, c.value)),
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
