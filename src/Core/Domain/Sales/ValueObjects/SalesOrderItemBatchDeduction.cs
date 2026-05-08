namespace pos_system_api.Core.Domain.Sales.ValueObjects;

/// <summary>
/// One chunk of a FIFO deduction made when a sale was paid: the batch the units came
/// from and how many base units. A single SalesOrderItem may have several of these
/// when its quantity spans multiple batches. Persisted on SalesOrderItem so a later
/// refund or cancel can restore each chunk to its original batch (preserves
/// expiry/recall traceability).
/// </summary>
public class SalesOrderItemBatchDeduction
{
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public SalesOrderItemBatchDeduction() { }

    public SalesOrderItemBatchDeduction(string batchNumber, int quantity)
    {
        BatchNumber = batchNumber;
        Quantity = quantity;
    }
}
