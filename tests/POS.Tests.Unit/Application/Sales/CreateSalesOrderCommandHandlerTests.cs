using Microsoft.Extensions.Logging.Abstractions;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Sales.Commands.CreateSalesOrder;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;
using pos_system_api.Core.Domain.Sales.Entities;

namespace POS.Tests.Unit.Application.Sales;

public class CreateSalesOrderCommandHandlerTests
{
    private const string ShopId = "SHOP-1";
    private const string DrugId = "DRG-AAAA1111";

    [Fact]
    public async Task Create_Rejects_FractionalBaseUnitsConsumed()
    {
        // A "Half-Bottle" packaging level with baseUnitQuantity=0.5 selling 3 units
        // would compute BaseUnitsConsumed=1.5. Stock is tracked in whole base units
        // (Batch.QuantityOnHand : int), so a fractional value would force silent
        // rounding at deduction time. The handler must reject up-front.
        var drug = DrugWithFractionalLevel(unitName: "Half-Bottle", baseUnitQuantity: 0.5m);
        var handler = NewHandler(drug, inventory: null);

        var cmd = new CreateSalesOrderCommand
        {
            ShopId = ShopId,
            CashierId = "cashier-1",
            Items =
            {
                new CreateSalesOrderItemDto
                {
                    DrugId = DrugId,
                    Quantity = 3,
                    UnitPrice = 5m,
                    PackagingLevel = "Half-Bottle",
                }
            }
        };

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*fractional*");
    }

    [Fact]
    public async Task Create_Allows_WholeBaseUnitsConsumed()
    {
        // Sanity check the validation isn't over-eager: standard Box=100 must pass.
        var drug = DrugWithFractionalLevel(unitName: "Box", baseUnitQuantity: 100m);
        var handler = NewHandler(drug, inventory: null);

        var cmd = new CreateSalesOrderCommand
        {
            ShopId = ShopId,
            CashierId = "cashier-1",
            Items =
            {
                new CreateSalesOrderItemDto
                {
                    DrugId = DrugId,
                    Quantity = 2,
                    UnitPrice = 12m,
                    PackagingLevel = "Box",
                }
            }
        };

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Items.Should().ContainSingle()
            .Which.BaseUnitsConsumed.Should().Be(200m);
    }

    private static Drug DrugWithFractionalLevel(string unitName, decimal baseUnitQuantity)
    {
        var info = new PackagingInfo(UnitType.Count, "tablet", "Tablet");
        info.AddPackagingLevel(new PackagingLevel(
            packagingLevelId: "PKG-LV-1",
            levelNumber: 1,
            unitName: unitName,
            baseUnitQuantity: baseUnitQuantity,
            isSellable: true,
            isDefault: true));

        return new Drug
        {
            DrugId = DrugId,
            Barcode = "X",
            BrandName = "Test",
            GenericName = "Test",
            CategoryId = "CAT-1",
            CategoryName = "Cat",
            PackagingInfo = info,
        };
    }

    private static CreateSalesOrderCommandHandler NewHandler(Drug drug, ShopInventory? inventory) =>
        new(
            new FakeSalesOrderRepository(),
            new FakeDrugRepository(drug),
            new FakeInventoryRepository(inventory),
            new FakeUnitOfWork(),
            NullLogger<CreateSalesOrderCommandHandler>.Instance);

    private sealed class FakeSalesOrderRepository : ISalesOrderRepository
    {
        public Task AddAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SalesOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetByCashierIdAsync(string cashierId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(List<SalesOrder> Orders, int TotalCount)> GetPagedAsync(string? shopId = null, string? cashierId = null, string? customerId = null, SalesOrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, PaymentMethod? paymentMethod = null, string? searchTerm = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalSalesAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetOrderCountAsync(string shopId, SalesOrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetTodaysOrdersAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetRecentOrdersAsync(string shopId, int count = 10, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, decimal>> GetCashierSalesAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, int>> GetCashierOrderCountAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, decimal>> GetSalesByPaymentMethodAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Dictionary<string, int>> GetTopSellingDrugsAsync(string shopId, int topCount = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeDrugRepository : IDrugRepository
    {
        private readonly Drug? _drug;
        public FakeDrugRepository(Drug? drug) => _drug = drug;

        public Task<Drug?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_drug != null && _drug.DrugId == id ? _drug : null);

        public Task<Drug?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Drug>> GetByIdsAsync(IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<Drug>> GetAllAsync(int page, int limit, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Drug> CreateAsync(Drug drug, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Drug> UpdateAsync(Drug drug, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(string drugId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly ShopInventory? _inv;
        public FakeInventoryRepository(ShopInventory? inv) => _inv = inv;

        public Task<ShopInventory?> GetByShopAndDrugAsync(string shopId, string drugId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_inv);

        public Task<ShopInventory?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ShopInventory>> GetByShopAndDrugsAsync(string shopId, IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
