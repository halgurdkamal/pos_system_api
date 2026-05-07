using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace POS.Tests.Unit.Domain.Inventory;

public class ShopInventoryTests
{
    private static ShopInventory NewInventory() =>
        new("shop-1", "drug-1", reorderPoint: 50, storageLocation: "Shelf A-1", new ShopPricing());

    private static Batch ActiveBatch(
        string batchNumber,
        int quantity,
        DateTime? receivedDate = null,
        DateTime? expiryDate = null,
        BatchLocation location = BatchLocation.ShopFloor) =>
        new(
            batchNumber: batchNumber,
            supplierId: "sup-1",
            quantityOnHand: quantity,
            receivedDate: receivedDate ?? DateTime.UtcNow.AddDays(-10),
            expiryDate: expiryDate ?? DateTime.UtcNow.AddYears(1),
            purchasePrice: 1.00m,
            sellingPrice: 2.00m,
            location: location,
            storageLocation: "Shelf A-1");

    [Fact]
    public void Constructor_GeneratesIdWithInvPrefix()
    {
        var inv = NewInventory();

        inv.Id.Should().StartWith("INV-");
    }

    [Fact]
    public void AddBatch_IncreasesTotalStock()
    {
        var inv = NewInventory();

        inv.AddBatch(ActiveBatch("B1", 100));

        inv.TotalStock.Should().Be(100);
        inv.IsAvailable.Should().BeTrue();
        inv.LastRestockDate.Should().NotBeNull();
    }

    [Fact]
    public void AddBatch_MultipleBatches_SumsQuantities()
    {
        var inv = NewInventory();

        inv.AddBatch(ActiveBatch("B1", 100));
        inv.AddBatch(ActiveBatch("B2", 50));

        inv.TotalStock.Should().Be(150);
    }

    [Fact]
    public void ReduceStock_FifoOrderByReceivedDate()
    {
        var inv = NewInventory();
        var older = ActiveBatch("B-OLD", 30, receivedDate: DateTime.UtcNow.AddDays(-30));
        var newer = ActiveBatch("B-NEW", 100, receivedDate: DateTime.UtcNow.AddDays(-1));
        inv.AddBatch(older);
        inv.AddBatch(newer);

        inv.ReduceStock(20);

        // Oldest batch consumed first; older had 30, lose 20, leaves 10. Newer untouched.
        older.QuantityOnHand.Should().Be(10);
        newer.QuantityOnHand.Should().Be(100);
        inv.TotalStock.Should().Be(110);
    }

    [Fact]
    public void ReduceStock_SpansMultipleBatches_WhenFirstIsExhausted()
    {
        var inv = NewInventory();
        var older = ActiveBatch("B-OLD", 30, receivedDate: DateTime.UtcNow.AddDays(-30));
        var newer = ActiveBatch("B-NEW", 100, receivedDate: DateTime.UtcNow.AddDays(-1));
        inv.AddBatch(older);
        inv.AddBatch(newer);

        inv.ReduceStock(50);

        older.QuantityOnHand.Should().Be(0);
        newer.QuantityOnHand.Should().Be(80);
        inv.TotalStock.Should().Be(80);
    }

    [Fact]
    public void ReduceStock_IgnoresExpiredBatches()
    {
        var inv = NewInventory();
        var expired = ActiveBatch("B-OLD", 100, receivedDate: DateTime.UtcNow.AddDays(-30));
        expired.Status = BatchStatus.Expired;
        var active = ActiveBatch("B-NEW", 50, receivedDate: DateTime.UtcNow.AddDays(-1));
        inv.AddBatch(expired);
        inv.AddBatch(active);

        inv.ReduceStock(10);

        expired.QuantityOnHand.Should().Be(100, "expired batches must not be sold");
        active.QuantityOnHand.Should().Be(40);
    }

    [Fact]
    public void RecalculateTotalStock_ExcludesNonActiveBatches()
    {
        var inv = NewInventory();
        var active = ActiveBatch("B1", 50);
        var expired = ActiveBatch("B2", 30);
        expired.Status = BatchStatus.Expired;
        inv.AddBatch(active);
        inv.AddBatch(expired);

        inv.RecalculateTotalStock();

        inv.TotalStock.Should().Be(50);
    }

    [Fact]
    public void GetShopFloorStock_ReturnsOnlyShopFloorActive()
    {
        var inv = NewInventory();
        inv.AddBatch(ActiveBatch("B1", 40, location: BatchLocation.ShopFloor));
        inv.AddBatch(ActiveBatch("B2", 100, location: BatchLocation.Storage));
        inv.AddBatch(ActiveBatch("B3", 25, location: BatchLocation.Quarantine));

        inv.GetShopFloorStock().Should().Be(40);
        inv.GetStorageStock().Should().Be(100);
        inv.GetQuarantinedStock().Should().Be(25);
    }

    [Fact]
    public void RestockShopFloor_MovesQuantityFromStorage()
    {
        var inv = NewInventory();
        inv.AddBatch(ActiveBatch("B1", 100, location: BatchLocation.Storage));

        inv.RestockShopFloor(30);

        inv.GetStorageStock().Should().Be(70);
        inv.GetShopFloorStock().Should().Be(30);
        inv.TotalStock.Should().Be(100, "moving stock between locations doesn't change the total");
    }

    [Fact]
    public void RestockShopFloor_ThrowsWhenInsufficientStorage()
    {
        var inv = NewInventory();
        inv.AddBatch(ActiveBatch("B1", 10, location: BatchLocation.Storage));

        var act = () => inv.RestockShopFloor(50);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock in storage*");
    }

    [Fact]
    public void IsLowStock_TrueAtOrBelowReorderPoint()
    {
        var inv = NewInventory(); // reorderPoint = 50
        inv.AddBatch(ActiveBatch("B1", 50));

        inv.IsLowStock().Should().BeTrue();

        inv.AddBatch(ActiveBatch("B2", 1));
        inv.IsLowStock().Should().BeFalse();
    }

    [Fact]
    public void MarkExpiredBatches_TransitionsActiveExpiredBatches()
    {
        var inv = NewInventory();
        var expiredButActive = ActiveBatch("B1", 50, expiryDate: DateTime.UtcNow.AddDays(-1));
        var stillFresh = ActiveBatch("B2", 50, expiryDate: DateTime.UtcNow.AddDays(30));
        inv.AddBatch(expiredButActive);
        inv.AddBatch(stillFresh);

        inv.MarkExpiredBatches();

        expiredButActive.Status.Should().Be(BatchStatus.Expired);
        stillFresh.Status.Should().Be(BatchStatus.Active);
        inv.TotalStock.Should().Be(50, "expired batches no longer count toward total");
    }
}
