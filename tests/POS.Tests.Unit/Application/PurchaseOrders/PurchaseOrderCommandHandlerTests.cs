using Microsoft.Extensions.Logging.Abstractions;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.Commands.CancelPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.ConfirmPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.MarkPurchaseOrderAsPaid;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace POS.Tests.Unit.Application.PurchaseOrders;

public class PurchaseOrderCommandHandlerTests
{
    // ----- ConfirmPurchaseOrderCommand -----

    [Fact]
    public async Task Confirm_HappyPath_TransitionsToConfirmedAndSaves()
    {
        var po = SubmittedOrder();
        var repo = new FakePurchaseOrderRepository(po);
        var handler = new ConfirmPurchaseOrderCommandHandler(repo, NullLogger<ConfirmPurchaseOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new ConfirmPurchaseOrderCommand(po.Id, "user-1"),
            CancellationToken.None);

        po.Status.Should().Be(PurchaseOrderStatus.Confirmed);
        po.ConfirmedBy.Should().Be("user-1");
        repo.UpdateCount.Should().Be(1);
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Confirm_OrderNotFound_ThrowsKeyNotFoundException()
    {
        var repo = new FakePurchaseOrderRepository();
        var handler = new ConfirmPurchaseOrderCommandHandler(repo, NullLogger<ConfirmPurchaseOrderCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ConfirmPurchaseOrderCommand("missing", "user-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*missing*");
        repo.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Confirm_OrderInWrongState_ThrowsInvalidOperation()
    {
        var po = DraftOrder(); // never submitted
        var repo = new FakePurchaseOrderRepository(po);
        var handler = new ConfirmPurchaseOrderCommandHandler(repo, NullLogger<ConfirmPurchaseOrderCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ConfirmPurchaseOrderCommand(po.Id, "user-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        repo.UpdateCount.Should().Be(0);
    }

    // ----- CancelPurchaseOrderCommand -----

    [Fact]
    public async Task Cancel_HappyPath_TransitionsToCancelledAndStoresReason()
    {
        var po = DraftOrder();
        var repo = new FakePurchaseOrderRepository(po);
        var handler = new CancelPurchaseOrderCommandHandler(repo, NullLogger<CancelPurchaseOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new CancelPurchaseOrderCommand(po.Id, "user-1", "Wrong supplier"),
            CancellationToken.None);

        po.Status.Should().Be(PurchaseOrderStatus.Cancelled);
        po.CancelledBy.Should().Be("user-1");
        po.CancellationReason.Should().Be("Wrong supplier");
        repo.UpdateCount.Should().Be(1);
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_OrderNotFound_Throws()
    {
        var repo = new FakePurchaseOrderRepository();
        var handler = new CancelPurchaseOrderCommandHandler(repo, NullLogger<CancelPurchaseOrderCommandHandler>.Instance);

        var act = () => handler.Handle(
            new CancelPurchaseOrderCommand("missing", "user-1", "n/a"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ----- MarkPurchaseOrderAsPaidCommand -----

    [Fact]
    public async Task MarkAsPaid_DefaultsToUtcNow_WhenPaidAtOmitted()
    {
        var po = DraftOrder();
        var repo = new FakePurchaseOrderRepository(po);
        var handler = new MarkPurchaseOrderAsPaidCommandHandler(repo, NullLogger<MarkPurchaseOrderAsPaidCommandHandler>.Instance);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(
            new MarkPurchaseOrderAsPaidCommand(po.Id),
            CancellationToken.None);
        var after = DateTime.UtcNow;

        po.IsPaid.Should().BeTrue();
        po.PaidAt.Should().NotBeNull();
        po.PaidAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.IsPaid.Should().BeTrue();
        repo.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task MarkAsPaid_UsesProvidedPaidAtTimestamp()
    {
        var po = DraftOrder();
        var repo = new FakePurchaseOrderRepository(po);
        var handler = new MarkPurchaseOrderAsPaidCommandHandler(repo, NullLogger<MarkPurchaseOrderAsPaidCommandHandler>.Instance);
        var when = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        await handler.Handle(new MarkPurchaseOrderAsPaidCommand(po.Id, when), CancellationToken.None);

        po.PaidAt.Should().Be(when);
    }

    [Fact]
    public async Task MarkAsPaid_OrderNotFound_Throws()
    {
        var repo = new FakePurchaseOrderRepository();
        var handler = new MarkPurchaseOrderAsPaidCommandHandler(repo, NullLogger<MarkPurchaseOrderAsPaidCommandHandler>.Instance);

        var act = () => handler.Handle(
            new MarkPurchaseOrderAsPaidCommand("missing"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ----- helpers -----

    private static PurchaseOrder DraftOrder()
    {
        var po = new PurchaseOrder("shop-1", "supplier-1", "creator");
        po.AddItem("drug-1", quantity: 5, unitPrice: 10m);
        return po;
    }

    private static PurchaseOrder SubmittedOrder()
    {
        var po = DraftOrder();
        po.Submit("submitter");
        return po;
    }

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

        // Unused interface members — throw to surface accidental usage in tests.
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
}
