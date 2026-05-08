using Microsoft.Extensions.Logging.Abstractions;
using POS.Tests.Unit.Application;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.Commands.ReceiveStock;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace POS.Tests.Unit.Application.PurchaseOrders;

public class ReceiveStockCommandHandlerTests
{
    [Fact]
    public async Task Receive_AddsBatchToExistingInventory_AndIncrementsTotalStock()
    {
        var po = SubmittedAndConfirmedOrder();
        var orderItemId = po.Items[0].Id;

        var existingInventory = new ShopInventory(
            shopId: "shop-1",
            drugId: "drug-1",
            reorderPoint: 50,
            storageLocation: "Shelf A",
            shopPricing: new ShopPricing(
                costPrice: 5m, sellingPrice: 12m, discount: 0m, currency: "USD", taxRate: 0m));

        var poRepo = new FakePurchaseOrderRepository(po);
        var invRepo = new FakeInventoryRepository(existingInventory);
        var handler = NewHandler(poRepo, invRepo);

        var result = await handler.Handle(new ReceiveStockCommand
        {
            OrderId = po.Id,
            ReceivedBy = "warehouse-clerk",
            Items = new List<ReceiveStockItemDto>
            {
                new()
                {
                    ItemId = orderItemId,
                    Quantity = 30,
                    BatchNumber = "BATCH-RX-001",
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                },
            },
        }, CancellationToken.None);

        existingInventory.Batches.Should().HaveCount(1);
        existingInventory.Batches[0].BatchNumber.Should().Be("BATCH-RX-001");
        existingInventory.Batches[0].QuantityOnHand.Should().Be(30);
        existingInventory.Batches[0].SupplierId.Should().Be(po.SupplierId);
        existingInventory.Batches[0].PurchasePrice.Should().Be(po.Items[0].UnitPrice);
        existingInventory.Batches[0].SellingPrice.Should().Be(12m, "selling price comes from existing ShopPricing");
        existingInventory.TotalStock.Should().Be(30);
        invRepo.UpdateCount.Should().Be(1);
        invRepo.AddCount.Should().Be(0, "inventory already existed; no new row created");
        result.Items.Should().HaveCount(1);
        result.Items[0].ReceivedQuantity.Should().Be(30);
    }

    [Fact]
    public async Task Receive_AutoCreatesShopInventory_OnFirstReceiptOfADrug()
    {
        var po = SubmittedAndConfirmedOrder();
        var orderItemId = po.Items[0].Id;

        var poRepo = new FakePurchaseOrderRepository(po);
        var invRepo = new FakeInventoryRepository(); // no inventory rows yet
        var handler = NewHandler(poRepo, invRepo);

        await handler.Handle(new ReceiveStockCommand
        {
            OrderId = po.Id,
            ReceivedBy = "clerk",
            Items = new List<ReceiveStockItemDto>
            {
                new()
                {
                    ItemId = orderItemId,
                    Quantity = 50,
                    BatchNumber = "BATCH-NEW-001",
                    ExpiryDate = DateTime.UtcNow.AddYears(2),
                },
            },
        }, CancellationToken.None);

        invRepo.AddCount.Should().Be(1, "first receipt should auto-create the inventory row");
        invRepo.UpdateCount.Should().Be(0,
            "F-1: a freshly AddAsync-ed entity is already tracked as Added; calling Update would re-mark it Modified " +
            "and EF would issue an UPDATE for a row that was never INSERTed. The AddBatch mutation rides along " +
            "with the pending INSERT on SaveChanges.");
        var inv = invRepo.GetSingle("shop-1", "drug-1");
        inv.Should().NotBeNull();
        inv!.Batches.Should().HaveCount(1);
        inv.TotalStock.Should().Be(50);
    }

    [Fact]
    public async Task Receive_PartialReceipt_KeepsOrderInPartiallyReceivedState()
    {
        var po = SubmittedAndConfirmedOrder(); // ordered qty = 100
        var orderItemId = po.Items[0].Id;
        var existingInventory = new ShopInventory(
            "shop-1", "drug-1", 50, "Shelf A",
            new ShopPricing(5m, 12m, 0m, "USD", 0m));

        var poRepo = new FakePurchaseOrderRepository(po);
        var invRepo = new FakeInventoryRepository(existingInventory);
        var handler = NewHandler(poRepo, invRepo);

        await handler.Handle(new ReceiveStockCommand
        {
            OrderId = po.Id,
            ReceivedBy = "clerk",
            Items = new List<ReceiveStockItemDto>
            {
                new()
                {
                    ItemId = orderItemId,
                    Quantity = 40, // only 40 of 100 ordered
                    BatchNumber = "BATCH-PARTIAL",
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                },
            },
        }, CancellationToken.None);

        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        existingInventory.TotalStock.Should().Be(40);
    }

    // ----- helpers -----

    private static PurchaseOrder SubmittedAndConfirmedOrder()
    {
        var po = new PurchaseOrder("shop-1", "supplier-1", "creator");
        po.AddItem("drug-1", quantity: 100, unitPrice: 5m);
        po.Submit("submitter");
        po.Confirm("confirmer");
        return po;
    }

    private static ReceiveStockCommandHandler NewHandler(
        IPurchaseOrderRepository poRepo,
        IInventoryRepository invRepo) =>
        new(poRepo, invRepo, new FakeUnitOfWork(), NullLogger<ReceiveStockCommandHandler>.Instance);

    private sealed class FakePurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly Dictionary<string, PurchaseOrder> _store;
        public int UpdateCount { get; private set; }

        public FakePurchaseOrderRepository(params PurchaseOrder[] seed)
        {
            _store = seed.ToDictionary(p => p.Id);
        }

        public Task<PurchaseOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue(id, out var po) ? po : null);

        public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            _store[purchaseOrder.Id] = purchaseOrder;
            return Task.CompletedTask;
        }

        public Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PurchaseOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PurchaseOrder>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(List<PurchaseOrder> Orders, int TotalCount)> GetPagedAsync(string? shopId = null, string? supplierId = null, PurchaseOrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, OrderPriority? priority = null, bool? isPaid = null, string? searchTerm = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalOrderValueAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetOrderCountAsync(string shopId, PurchaseOrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PurchaseOrder>> GetPendingOrdersAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PurchaseOrder>> GetOverduePaymentsAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PurchaseOrder>> GetRecentOrdersAsync(string shopId, int count = 10, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, decimal>> GetSupplierSpendingAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, int>> GetSupplierOrderCountAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, double>> GetSupplierAverageDeliveryTimeAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<(string shopId, string drugId), ShopInventory> _store;
        public int AddCount { get; private set; }
        public int UpdateCount { get; private set; }

        public FakeInventoryRepository(params ShopInventory[] seed)
        {
            _store = seed.ToDictionary(i => (i.ShopId, i.DrugId));
        }

        public ShopInventory? GetSingle(string shopId, string drugId) =>
            _store.TryGetValue((shopId, drugId), out var inv) ? inv : null;

        public Task<ShopInventory?> GetByShopAndDrugAsync(
            string shopId, string drugId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue((shopId, drugId), out var inv) ? inv : null);

        public Task<ShopInventory> AddAsync(ShopInventory inventory, CancellationToken cancellationToken = default)
        {
            AddCount++;
            _store[(inventory.ShopId, inventory.DrugId)] = inventory;
            return Task.FromResult(inventory);
        }

        public Task<ShopInventory> UpdateAsync(ShopInventory inventory, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            _store[(inventory.ShopId, inventory.DrugId)] = inventory;
            return Task.FromResult(inventory);
        }

        // Unused interface members — throw if accidentally called.
        public Task<ShopInventory?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ShopInventory>> GetByShopAndDrugsAsync(string shopId, IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IEnumerable<ShopInventory> Items, int TotalCount)> GetByShopAsync(string shopId, int page, int limit, bool? isAvailable = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetAllByShopAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetLowStockAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetExpiringBatchesAsync(string shopId, int days, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> GetOutOfStockAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ShopInventory>> SearchByDrugNameAsync(string shopId, string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string shopId, string drugId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalStockValueAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
