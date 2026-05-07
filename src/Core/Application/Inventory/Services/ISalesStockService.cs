using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Inventory.Services;

/// <summary>
/// Applies stock movements that pair with sales-order state transitions.
///
/// Lifecycle (matches SalesOrder state machine):
///   ProcessPayment  : Draft|Confirmed → Paid     ⇒ DeductForSaleAsync
///   Refund          : Paid|Completed → Refunded  ⇒ RestoreForReversalAsync
///   Cancel          : Paid             → Cancelled ⇒ RestoreForReversalAsync
///                     (Cancel from Draft|Confirmed needs no stock change.)
///
/// Centralising these calls behind one service keeps the inventory write
/// rule in a single place and makes it trivial to swap for a no-op or a
/// transactional outbox later.
/// </summary>
public interface ISalesStockService
{
    /// <summary>
    /// Decrement <see cref="ShopInventory"/> for every line on the order using
    /// FIFO batch consumption. Skips items whose drug has no inventory row in
    /// the shop (the absence is logged; the order is not blocked).
    /// </summary>
    Task DeductForSaleAsync(SalesOrder order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment <see cref="ShopInventory"/> for every line on the order, preferring
    /// the original batch the sale was made from (recorded on
    /// <see cref="SalesOrderItem.BatchNumber"/>) when it's still active.
    /// </summary>
    Task RestoreForReversalAsync(SalesOrder order, CancellationToken cancellationToken = default);
}
