using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Queries.GetPackagingPricing;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace POS.Tests.Unit.Application.Inventory;

public class GetPackagingPricingQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNull()
    {
        var repo = new FakeInventoryRepository();
        var handler = new GetPackagingPricingQueryHandler(repo);

        var result = await handler.Handle(
            new GetPackagingPricingQuery("shop-1", "drug-1"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HappyPath_ProjectsPricingFields()
    {
        var pricing = new ShopPricing(
            costPrice: 5m,
            sellingPrice: 10m,
            discount: 20m, // 20% discount → final = 10 * 0.8 = 8
            currency: "USD",
            taxRate: 5m);
        pricing.SetPackagingLevelPrice("Strip", 3m);
        pricing.SetPackagingLevelPrice("Tablet", 0.5m);

        var inventory = new ShopInventory(
            shopId: "shop-1",
            drugId: "drug-1",
            reorderPoint: 50,
            storageLocation: "Shelf A",
            shopPricing: pricing);

        var repo = new FakeInventoryRepository(inventory);
        var handler = new GetPackagingPricingQueryHandler(repo);

        var result = await handler.Handle(
            new GetPackagingPricingQuery("shop-1", "drug-1"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.CostPrice.Should().Be(5m);
        result.SellingPrice.Should().Be(10m);
        result.Discount.Should().Be(20m);
        result.Currency.Should().Be("USD");
        result.TaxRate.Should().Be(5m);
        result.ProfitMargin.Should().Be(8m - 5m, "final price after 20% discount minus cost");
        result.PackagingLevelPrices.Should().BeEquivalentTo(new Dictionary<string, decimal>
        {
            ["Strip"] = 3m,
            ["Tablet"] = 0.5m,
        });
    }

    [Fact]
    public async Task Handle_ReturnsCopyOfPackagingLevelPrices_NotReference()
    {
        var pricing = new ShopPricing(1, 2, 0, "USD", 0);
        pricing.SetPackagingLevelPrice("Box", 100m);

        var inventory = new ShopInventory("s", "d", 10, "loc", pricing);
        var repo = new FakeInventoryRepository(inventory);
        var handler = new GetPackagingPricingQueryHandler(repo);

        var result = await handler.Handle(
            new GetPackagingPricingQuery("s", "d"),
            CancellationToken.None);

        result!.PackagingLevelPrices["Box"] = 999m;

        // Original entity must not be mutated by changes to the returned DTO dictionary.
        pricing.PackagingLevelPrices["Box"].Should().Be(100m);
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<(string shopId, string drugId), ShopInventory> _store;

        public FakeInventoryRepository(params ShopInventory[] seed)
        {
            _store = seed.ToDictionary(i => (i.ShopId, i.DrugId));
        }

        public Task<ShopInventory?> GetByShopAndDrugAsync(
            string shopId, string drugId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue((shopId, drugId), out var inv) ? inv : null);

        public Task<ShopInventory?> GetByShopAndDrugForUpdateAsync(
            string shopId, string drugId, CancellationToken cancellationToken = default) =>
            GetByShopAndDrugAsync(shopId, drugId, cancellationToken);

        // Unused interface members — throw to surface accidental usage.
        public Task<ShopInventory?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ShopInventory>> GetByShopAndDrugsAsync(string shopId, IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ShopInventory>> GetByShopAndDrugsForUpdateAsync(string shopId, IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IEnumerable<ShopInventory> Items, int TotalCount)> GetByShopAsync(string shopId, int page, int limit, bool? isAvailable = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetAllByShopAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetLowStockAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetExpiringBatchesAsync(string shopId, int days, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetOutOfStockAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> SearchByDrugNameAsync(string shopId, string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ShopInventory> AddAsync(ShopInventory inventory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ShopInventory> UpdateAsync(ShopInventory inventory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string shopId, string drugId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalStockValueAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
