using Microsoft.Extensions.Logging.Abstractions;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.Commands.CancelSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.CompleteSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.ConfirmSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.RefundSalesOrder;
using pos_system_api.Core.Domain.Sales.Entities;

namespace POS.Tests.Unit.Application.Sales;

public class SalesOrderCommandHandlerTests
{
    // ----- ConfirmSalesOrderCommand -----

    [Fact]
    public async Task Confirm_HappyPath_TransitionsToConfirmedAndSaves()
    {
        var so = DraftOrderWithItems();
        var repo = new FakeSalesOrderRepository(so);
        var handler = new ConfirmSalesOrderCommandHandler(repo, NullLogger<ConfirmSalesOrderCommandHandler>.Instance);

        var result = await handler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None);

        so.Status.Should().Be(SalesOrderStatus.Confirmed);
        repo.UpdateCount.Should().Be(1);
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Confirm_OrderNotFound_ThrowsKeyNotFound()
    {
        var repo = new FakeSalesOrderRepository();
        var handler = new ConfirmSalesOrderCommandHandler(repo, NullLogger<ConfirmSalesOrderCommandHandler>.Instance);

        var act = () => handler.Handle(new ConfirmSalesOrderCommand("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*missing*");
        repo.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Confirm_OrderWithoutItems_ThrowsInvalidOperation()
    {
        var so = new SalesOrder("shop-1", "cashier-1");
        var repo = new FakeSalesOrderRepository(so);
        var handler = new ConfirmSalesOrderCommandHandler(repo, NullLogger<ConfirmSalesOrderCommandHandler>.Instance);

        var act = () => handler.Handle(new ConfirmSalesOrderCommand(so.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        repo.UpdateCount.Should().Be(0);
    }

    // ----- CompleteSalesOrderCommand -----

    [Fact]
    public async Task Complete_HappyPath_TransitionsFromPaidToCompleted()
    {
        var so = PaidOrder();
        var repo = new FakeSalesOrderRepository(so);
        var handler = new CompleteSalesOrderCommandHandler(repo, NullLogger<CompleteSalesOrderCommandHandler>.Instance);

        var result = await handler.Handle(new CompleteSalesOrderCommand(so.Id), CancellationToken.None);

        so.Status.Should().Be(SalesOrderStatus.Completed);
        so.CompletedAt.Should().NotBeNull();
        repo.UpdateCount.Should().Be(1);
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Complete_NonPaidOrder_Throws()
    {
        var so = DraftOrderWithItems(); // Draft, not Paid
        var repo = new FakeSalesOrderRepository(so);
        var handler = new CompleteSalesOrderCommandHandler(repo, NullLogger<CompleteSalesOrderCommandHandler>.Instance);

        var act = () => handler.Handle(new CompleteSalesOrderCommand(so.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ----- CancelSalesOrderCommand -----

    [Fact]
    public async Task Cancel_HappyPath_TransitionsToCancelledAndStoresReason()
    {
        var so = DraftOrderWithItems();
        var repo = new FakeSalesOrderRepository(so);
        var handler = new CancelSalesOrderCommandHandler(repo, NullLogger<CancelSalesOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new CancelSalesOrderCommand(so.Id, "user-1", "Customer changed mind"),
            CancellationToken.None);

        so.Status.Should().Be(SalesOrderStatus.Cancelled);
        so.CancelledBy.Should().Be("user-1");
        so.CancellationReason.Should().Be("Customer changed mind");
        repo.UpdateCount.Should().Be(1);
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_OrderNotFound_Throws()
    {
        var repo = new FakeSalesOrderRepository();
        var handler = new CancelSalesOrderCommandHandler(repo, NullLogger<CancelSalesOrderCommandHandler>.Instance);

        var act = () => handler.Handle(
            new CancelSalesOrderCommand("missing", "user-1", "n/a"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ----- RefundSalesOrderCommand -----

    [Fact]
    public async Task Refund_PaidOrder_TransitionsToRefunded()
    {
        var so = PaidOrder();
        var repo = new FakeSalesOrderRepository(so);
        var handler = new RefundSalesOrderCommandHandler(repo, NullLogger<RefundSalesOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new RefundSalesOrderCommand(so.Id, "user-1", "Defective"),
            CancellationToken.None);

        so.Status.Should().Be(SalesOrderStatus.Refunded);
        so.CancellationReason.Should().Be("Refund: Defective");
        repo.UpdateCount.Should().Be(1);
        result.Status.Should().Be("Refunded");
    }

    [Fact]
    public async Task Refund_DraftOrder_Throws()
    {
        var so = DraftOrderWithItems(); // Draft, not Paid/Completed
        var repo = new FakeSalesOrderRepository(so);
        var handler = new RefundSalesOrderCommandHandler(repo, NullLogger<RefundSalesOrderCommandHandler>.Instance);

        var act = () => handler.Handle(
            new RefundSalesOrderCommand(so.Id, "user-1", "n/a"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Refund_OrderNotFound_Throws()
    {
        var repo = new FakeSalesOrderRepository();
        var handler = new RefundSalesOrderCommandHandler(repo, NullLogger<RefundSalesOrderCommandHandler>.Instance);

        var act = () => handler.Handle(
            new RefundSalesOrderCommand("missing", "user-1", "n/a"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ----- helpers -----

    private static SalesOrder DraftOrderWithItems()
    {
        var so = new SalesOrder("shop-1", "cashier-1", customerName: "Alice");
        so.AddItem("drug-1", quantity: 2, unitPrice: 10m);
        return so;
    }

    private static SalesOrder PaidOrder()
    {
        var so = DraftOrderWithItems();
        so.ProcessPayment(PaymentMethod.Cash, amountPaid: 20m);
        return so;
    }

    private sealed class FakeSalesOrderRepository : ISalesOrderRepository
    {
        private readonly Dictionary<string, SalesOrder> _store;
        public int UpdateCount { get; private set; }

        public FakeSalesOrderRepository(params SalesOrder[] seed)
        {
            _store = seed.ToDictionary(p => p.Id);
        }

        public Task<SalesOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue(id, out var so) ? so : null);

        public Task UpdateAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            _store[salesOrder.Id] = salesOrder;
            return Task.CompletedTask;
        }

        // Unused interface members — throw to surface accidental usage in tests.
        public Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<SalesOrder>> GetByCashierIdAsync(string cashierId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
}
