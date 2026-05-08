using Microsoft.Extensions.Logging.Abstractions;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;
using pos_system_api.Core.Domain.Sales.Entities;

namespace POS.Tests.Unit.Application.Inventory.Services;

public class SalesStockServiceTests
{
    private const string ShopId = "SHOP-1";
    private const string DrugId = "DRG-AAAA1111";

    [Fact]
    public async Task DeductForSale_UsesBaseUnitsConsumed_NotQuantity()
    {
        // 1 Box of 100 tablets: Quantity=1, BaseUnitsConsumed=100. The buggy
        // implementation deducted 1; we want 100.
        var inv = InventoryWith(batchQuantity: 150);
        var repo = new FakeInventoryRepository(inv);
        var uow = new FakeUnitOfWork();
        var service = new SalesStockService(repo, uow, NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");
        order.AddItem(DrugId, quantity: 1, unitPrice: 12m, packagingLevelSold: "Box", baseUnitsConsumed: 100);

        await service.DeductForSaleAsync(order);

        inv.TotalStock.Should().Be(50);
        repo.UpdateCount.Should().Be(1);
        uow.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task DeductForSale_FallsBackToQuantity_WhenBaseUnitsConsumedZero()
    {
        // Legacy SalesOrder rows pre-Nov-2025 migration have BaseUnitsConsumed=0.
        // Falling back to Quantity preserves what those orders were paid against.
        var inv = InventoryWith(batchQuantity: 50);
        var repo = new FakeInventoryRepository(inv);
        var service = new SalesStockService(repo, new FakeUnitOfWork(), NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");
        order.AddItem(DrugId, quantity: 3, unitPrice: 5m, baseUnitsConsumed: 0);

        await service.DeductForSaleAsync(order);

        inv.TotalStock.Should().Be(47);
    }

    [Fact]
    public async Task DeductForSale_RoundsBankerStyle_WhenBaseUnitsConsumedFractional()
    {
        // BaseUnitsConsumed=100.5 should round to 100 (banker's rounding to even).
        // CreateSalesOrderCommand rejects fractional values up-front; this is the
        // belt-and-braces guard for legacy data or direct domain construction.
        var inv = InventoryWith(batchQuantity: 200);
        var repo = new FakeInventoryRepository(inv);
        var service = new SalesStockService(repo, new FakeUnitOfWork(), NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");
        order.AddItem(DrugId, quantity: 1, unitPrice: 10m, baseUnitsConsumed: 100.5m);

        await service.DeductForSaleAsync(order);

        inv.TotalStock.Should().Be(100); // 200 - round(100.5, ToEven) = 200 - 100
    }

    [Fact]
    public async Task DeductForSale_LogsAndContinues_WhenInventoryMissing()
    {
        // Cashier sells a drug that hasn't been onboarded into ShopInventory yet:
        // payment must still succeed (we don't block at the till) but stock isn't
        // adjusted and a warning is logged.
        var repo = new FakeInventoryRepository(); // empty
        var uow = new FakeUnitOfWork();
        var service = new SalesStockService(repo, uow, NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");
        order.AddItem(DrugId, quantity: 1, unitPrice: 5m, baseUnitsConsumed: 10);

        await service.DeductForSaleAsync(order);

        repo.UpdateCount.Should().Be(0);
        uow.SaveChangesCount.Should().Be(1); // SaveChanges still called even when nothing was updated
    }

    [Fact]
    public async Task DeductForSale_NoItems_ShortCircuits_NoSaveChanges()
    {
        var repo = new FakeInventoryRepository();
        var uow = new FakeUnitOfWork();
        var service = new SalesStockService(repo, uow, NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");

        await service.DeductForSaleAsync(order);

        uow.SaveChangesCount.Should().Be(0);
    }

    [Fact]
    public async Task RestoreForReversal_UsesBaseUnitsConsumed_NotQuantity()
    {
        // Symmetrical to DeductForSale: refunding 1 Box of 100 must restore 100,
        // not 1.
        var inv = InventoryWith(batchQuantity: 50);
        var repo = new FakeInventoryRepository(inv);
        var service = new SalesStockService(repo, new FakeUnitOfWork(), NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");
        order.AddItem(DrugId, quantity: 1, unitPrice: 12m, batchNumber: "B1", packagingLevelSold: "Box", baseUnitsConsumed: 100);

        await service.RestoreForReversalAsync(order);

        inv.TotalStock.Should().Be(150);
    }

    [Fact]
    public async Task RestoreForReversal_FallsBackToQuantity_WhenBaseUnitsConsumedZero()
    {
        var inv = InventoryWith(batchQuantity: 10);
        var repo = new FakeInventoryRepository(inv);
        var service = new SalesStockService(repo, new FakeUnitOfWork(), NullLogger<SalesStockService>.Instance);

        var order = new SalesOrder(ShopId, "cashier-1");
        order.AddItem(DrugId, quantity: 4, unitPrice: 5m, batchNumber: "B1", baseUnitsConsumed: 0);

        await service.RestoreForReversalAsync(order);

        inv.TotalStock.Should().Be(14);
    }

    private static ShopInventory InventoryWith(int batchQuantity)
    {
        var inv = new ShopInventory(ShopId, DrugId, reorderPoint: 50, storageLocation: "Shelf A-1", new ShopPricing());
        inv.AddBatch(new Batch(
            batchNumber: "B1",
            supplierId: "sup-1",
            quantityOnHand: batchQuantity,
            receivedDate: DateTime.UtcNow.AddDays(-10),
            expiryDate: DateTime.UtcNow.AddYears(1),
            purchasePrice: 1.00m,
            sellingPrice: 2.00m,
            location: BatchLocation.ShopFloor,
            storageLocation: "Shelf A-1"));
        return inv;
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<(string shopId, string drugId), ShopInventory> _store;
        public int UpdateCount { get; private set; }

        public FakeInventoryRepository(params ShopInventory[] seed) =>
            _store = seed.ToDictionary(i => (i.ShopId, i.DrugId));

        public Task<IReadOnlyList<ShopInventory>> GetByShopAndDrugsAsync(
            string shopId, IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ShopInventory> rows = _store
                .Where(kv => kv.Key.shopId == shopId && drugIds.Contains(kv.Key.drugId))
                .Select(kv => kv.Value)
                .ToList();
            return Task.FromResult(rows);
        }

        public Task<ShopInventory> UpdateAsync(ShopInventory inventory, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            _store[(inventory.ShopId, inventory.DrugId)] = inventory;
            return Task.FromResult(inventory);
        }

        // Unused interface members — throw if accidentally called.
        public Task<ShopInventory?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ShopInventory?> GetByShopAndDrugAsync(string shopId, string drugId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IEnumerable<ShopInventory> Items, int TotalCount)> GetByShopAsync(string shopId, int page, int limit, bool? isAvailable = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetAllByShopAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetLowStockAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetExpiringBatchesAsync(string shopId, int days, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetOutOfStockAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> SearchByDrugNameAsync(string shopId, string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ShopInventory> AddAsync(ShopInventory inventory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string shopId, string drugId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalStockValueAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
