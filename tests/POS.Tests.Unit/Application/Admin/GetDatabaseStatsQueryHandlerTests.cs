using POS.Tests.Unit.Infrastructure;
using pos_system_api.Core.Application.Admin.Queries.GetDatabaseStats;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Shops.Entities;

namespace POS.Tests.Unit.Application.Admin;

public class GetDatabaseStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsAllZeros()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetDatabaseStatsQueryHandler(db);

        var stats = await handler.Handle(new GetDatabaseStatsQuery(), CancellationToken.None);

        stats.Drugs.Should().Be(0);
        stats.Shops.Should().Be(0);
        stats.Suppliers.Should().Be(0);
        stats.Inventory.Should().Be(0);
        stats.Users.Should().Be(0);
        stats.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ReturnsCountsOfSeededEntities()
    {
        using var db = TestDbContextFactory.Create();

        db.Shops.Add(new Shop(
            shopName: "Acme",
            legalName: "Acme LLC",
            licenseNumber: "L-1",
            address: new Address("1 St", "City", "ST", "00000", "Country"),
            contact: new Contact()));
        db.Users.Add(new User { Username = "u1", Email = "u1@x", PasswordHash = "x", FullName = "U One" });
        db.Users.Add(new User { Username = "u2", Email = "u2@x", PasswordHash = "x", FullName = "U Two" });
        await db.SaveChangesAsync();

        var handler = new GetDatabaseStatsQueryHandler(db);

        var stats = await handler.Handle(new GetDatabaseStatsQuery(), CancellationToken.None);

        stats.Shops.Should().Be(1);
        stats.Users.Should().Be(2);
        stats.Drugs.Should().Be(0, "no drugs were seeded");
    }
}
