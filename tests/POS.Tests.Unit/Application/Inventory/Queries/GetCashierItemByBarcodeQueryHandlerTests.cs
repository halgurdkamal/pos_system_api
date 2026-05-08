using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Queries.GetCashierItemByBarcode;
using pos_system_api.Core.Domain.Categories.Entities;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace POS.Tests.Unit.Application.Inventory.Queries;

public class GetCashierItemByBarcodeQueryHandlerTests
{
    private const string ShopId = "SHOP-1";
    private const string PrefixedDrugId = "DRG-AAAA1111";
    private const string Barcode = "8901234567890";

    [Fact]
    public async Task Handle_LooksUpInventory_ByPrefixedDrugId_NotRawGuid()
    {
        // Drug.Id (from BaseEntity) is a fresh GUID; Drug.DrugId is the user-facing
        // "DRG-XXXXXXXX" form that ShopInventory.DrugId stores. The handler must use
        // DrugId for the inventory join, otherwise stocked drugs always 404.
        var drug = NewDrug();
        drug.Id.Should().NotBe(PrefixedDrugId, "test setup precondition: Id and DrugId must differ");

        var inventory = NewInventory(drugId: PrefixedDrugId, batchQty: 50);
        var inventoryRepo = new FakeInventoryRepository(inventory);

        var handler = new GetCashierItemByBarcodeQueryHandler(
            inventoryRepo,
            new FakeDrugRepository(drug),
            new FakeCategoryRepository(new Category("Antibiotics", categoryId: "CAT-1")));

        var result = await handler.Handle(new GetCashierItemByBarcodeQuery(ShopId, Barcode), CancellationToken.None);

        result.Should().NotBeNull();
        result!.DrugId.Should().Be(PrefixedDrugId, "the DTO must surface the user-facing ID, not the GUID PK");
        result.AvailableStock.Should().Be(50);
        inventoryRepo.LastDrugIdQueried.Should().Be(PrefixedDrugId, "the inventory join key must be the prefixed DrugId");
    }

    [Fact]
    public async Task Handle_FallsBackToRawGuid_WhenInventoryWasKeyedByDrugIdGuid()
    {
        // F-7 regression: PO /receive paths used to write ShopInventory.DrugId =
        // drug.Id (the GUID PK) instead of drug.DrugId (the prefixed friendly id).
        // The handler must try both forms or stocked drugs 404 at the till.
        var drug = NewDrug();
        var inventoryKeyedByGuid = NewInventory(drugId: drug.Id, batchQty: 25);

        var inventoryRepo = new FakeInventoryRepository(inventoryKeyedByGuid);
        var handler = new GetCashierItemByBarcodeQueryHandler(
            inventoryRepo,
            new FakeDrugRepository(drug),
            new FakeCategoryRepository(new Category("Antibiotics", categoryId: "CAT-1")));

        var result = await handler.Handle(new GetCashierItemByBarcodeQuery(ShopId, Barcode), CancellationToken.None);

        result.Should().NotBeNull();
        result!.AvailableStock.Should().Be(25);
        inventoryRepo.LastDrugIdQueried.Should().Be(drug.Id, "the fallback path queries by raw GUID");
    }

    [Fact]
    public async Task Handle_DrugNotFound_ReturnsNull()
    {
        var inventoryRepo = new FakeInventoryRepository();
        var handler = new GetCashierItemByBarcodeQueryHandler(
            inventoryRepo,
            new FakeDrugRepository(), // no drugs
            new FakeCategoryRepository());

        var result = await handler.Handle(new GetCashierItemByBarcodeQuery(ShopId, Barcode), CancellationToken.None);

        result.Should().BeNull();
        inventoryRepo.LastDrugIdQueried.Should().BeNull("inventory shouldn't be touched when the drug doesn't exist");
    }

    [Fact]
    public async Task Handle_InventoryEmpty_ReturnsNull()
    {
        var drug = NewDrug();
        var handler = new GetCashierItemByBarcodeQueryHandler(
            new FakeInventoryRepository(), // no inventory rows
            new FakeDrugRepository(drug),
            new FakeCategoryRepository());

        var result = await handler.Handle(new GetCashierItemByBarcodeQuery(ShopId, Barcode), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InventoryHasZeroStock_ReturnsNull()
    {
        var drug = NewDrug();
        // Inventory row exists but TotalStock=0 (no batches).
        var emptyInventory = new ShopInventory(ShopId, PrefixedDrugId, 50, "Shelf A", new ShopPricing());
        var handler = new GetCashierItemByBarcodeQueryHandler(
            new FakeInventoryRepository(emptyInventory),
            new FakeDrugRepository(drug),
            new FakeCategoryRepository());

        var result = await handler.Handle(new GetCashierItemByBarcodeQuery(ShopId, Barcode), CancellationToken.None);

        result.Should().BeNull();
    }

    private static Drug NewDrug() => new Drug
    {
        DrugId = PrefixedDrugId,
        Barcode = Barcode,
        BrandName = "Amoxil 500",
        GenericName = "Amoxicillin",
        CategoryId = "CAT-1",
        CategoryName = "Antibiotics",
    };

    private static ShopInventory NewInventory(string drugId, int batchQty)
    {
        var inv = new ShopInventory(ShopId, drugId, reorderPoint: 50, storageLocation: "Shelf A", new ShopPricing(
            costPrice: 5m,
            sellingPrice: 12m,
            discount: 0,
            currency: "USD",
            taxRate: 0));
        inv.AddBatch(new Batch(
            batchNumber: "B1",
            supplierId: "sup-1",
            quantityOnHand: batchQty,
            receivedDate: DateTime.UtcNow.AddDays(-5),
            expiryDate: DateTime.UtcNow.AddYears(1),
            purchasePrice: 5m,
            sellingPrice: 12m,
            location: BatchLocation.ShopFloor,
            storageLocation: "Shelf A"));
        return inv;
    }

    private sealed class FakeDrugRepository : IDrugRepository
    {
        private readonly Dictionary<string, Drug> _byBarcode;
        public FakeDrugRepository(params Drug[] seed) =>
            _byBarcode = seed.ToDictionary(d => d.Barcode);

        public Task<Drug?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byBarcode.TryGetValue(barcode, out var d) ? d : null);

        public Task<Drug?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Drug>> GetByIdsAsync(IReadOnlyCollection<string> drugIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<pos_system_api.Core.Application.Common.Models.PagedResult<Drug>> GetAllAsync(int page, int limit, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Drug> CreateAsync(Drug drug, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Drug> UpdateAsync(Drug drug, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(string drugId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly Dictionary<string, Category> _store;
        public FakeCategoryRepository(params Category[] seed) =>
            _store = seed.ToDictionary(c => c.CategoryId);

        public Task<Category?> GetByIdAsync(string categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue(categoryId, out var c) ? c : null);

        public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Category>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(string categoryId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string categoryId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetDrugCountAsync(string categoryId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly Dictionary<(string shopId, string drugId), ShopInventory> _store;
        public string? LastDrugIdQueried { get; private set; }

        public FakeInventoryRepository(params ShopInventory[] seed) =>
            _store = seed.ToDictionary(i => (i.ShopId, i.DrugId));

        public Task<ShopInventory?> GetByShopAndDrugAsync(
            string shopId, string drugId, CancellationToken cancellationToken = default)
        {
            LastDrugIdQueried = drugId;
            return Task.FromResult(_store.TryGetValue((shopId, drugId), out var inv) ? inv : null);
        }

        public Task<ShopInventory?> GetByShopAndDrugForUpdateAsync(
            string shopId, string drugId, CancellationToken cancellationToken = default) =>
            GetByShopAndDrugAsync(shopId, drugId, cancellationToken);

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
