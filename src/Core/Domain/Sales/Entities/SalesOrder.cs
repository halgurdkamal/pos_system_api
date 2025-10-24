using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Sales.Entities;

/// <summary>
/// Sales order status
/// </summary>
public enum SalesOrderStatus
{
    Draft,          // Order being created
    Confirmed,      // Order confirmed, ready for payment
    Paid,           // Payment completed
    Completed,      // Order fulfilled
    Cancelled,      // Order cancelled
    Refunded        // Order refunded
}

/// <summary>
/// Payment method types
/// </summary>
public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    MobileMoney,
    BankTransfer,
    Mixed            // Multiple payment methods
}

/// <summary>
/// Sales order entity for cashier/POS transactions
/// </summary>
public class SalesOrder : BaseEntity
{
    // Core Information
    public string OrderNumber { get; private set; }
    public string ShopId { get; private set; }
    public string? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    
    // Financial Information (for dashboard analytics)
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal ChangeGiven { get; private set; }
    
    // Payment Information
    public PaymentMethod? PaymentMethod { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTime? PaidAt { get; private set; }
    
    // Dates (for tracking and analytics)
    public DateTime OrderDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    
    // User Tracking (for audit and performance analytics)
    public string CashierId { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    
    // Additional Information
    public string? Notes { get; private set; }
    public bool IsPrescriptionRequired { get; private set; }
    public string? PrescriptionNumber { get; private set; }
    
    // Items
    public List<SalesOrderItem> Items { get; private set; } = new();

    private SalesOrder() { } // EF Core

    public SalesOrder(
        string shopId,
        string cashierId,
        string? customerId = null,
        string? customerName = null,
        string? customerPhone = null,
        bool isPrescriptionRequired = false,
        string? prescriptionNumber = null,
        string? notes = null)
    {
        Id = Guid.NewGuid().ToString();
        OrderNumber = GenerateOrderNumber();
        ShopId = shopId;
        CashierId = cashierId;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        Status = SalesOrderStatus.Draft;
        OrderDate = DateTime.UtcNow;
        IsPrescriptionRequired = isPrescriptionRequired;
        PrescriptionNumber = prescriptionNumber;
        Notes = notes;
        
        SubTotal = 0;
        TaxAmount = 0;
        DiscountAmount = 0;
        TotalAmount = 0;
        AmountPaid = 0;
        ChangeGiven = 0;
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"SO-{timestamp}-{random}";
    }

    public void AddItem(string drugId, int quantity, decimal unitPrice, decimal? discountPercentage = null, string? batchNumber = null)
    {
        if (Status != SalesOrderStatus.Draft && Status != SalesOrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot add items to order in status {Status}");

        var item = new SalesOrderItem(Id, drugId, quantity, unitPrice, discountPercentage, batchNumber);
        Items.Add(item);
        RecalculateTotals();
    }

    public void UpdateItem(string itemId, int quantity, decimal unitPrice, decimal? discountPercentage = null)
    {
        if (Status != SalesOrderStatus.Draft && Status != SalesOrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot update items in order status {Status}");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new KeyNotFoundException($"Item {itemId} not found");

        item.Update(quantity, unitPrice, discountPercentage);
        RecalculateTotals();
    }

    public void RemoveItem(string itemId)
    {
        if (Status != SalesOrderStatus.Draft && Status != SalesOrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot remove items from order status {Status}");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotals();
        }
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        if (Status != SalesOrderStatus.Draft && Status != SalesOrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot apply discount in status {Status}");

        DiscountAmount = discountAmount;
        RecalculateTotals();
    }

    public void SetTax(decimal taxAmount)
    {
        if (Status != SalesOrderStatus.Draft && Status != SalesOrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot set tax in status {Status}");

        TaxAmount = taxAmount;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        TotalAmount = SubTotal + TaxAmount - DiscountAmount;
        
        if (TotalAmount < 0)
            TotalAmount = 0;
    }

    public void Confirm()
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException($"Can only confirm draft orders");

        if (!Items.Any())
            throw new InvalidOperationException("Cannot confirm order without items");

        Status = SalesOrderStatus.Confirmed;
    }

    public void ProcessPayment(PaymentMethod paymentMethod, decimal amountPaid, string? paymentReference = null)
    {
        if (Status != SalesOrderStatus.Confirmed && Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot process payment for order in status {Status}");

        if (amountPaid < TotalAmount)
            throw new InvalidOperationException($"Amount paid ({amountPaid:C}) is less than total amount ({TotalAmount:C})");

        PaymentMethod = paymentMethod;
        AmountPaid = amountPaid;
        ChangeGiven = amountPaid - TotalAmount;
        PaymentReference = paymentReference;
        PaidAt = DateTime.UtcNow;
        Status = SalesOrderStatus.Paid;
    }

    public void Complete()
    {
        if (Status != SalesOrderStatus.Paid)
            throw new InvalidOperationException($"Can only complete paid orders");

        Status = SalesOrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancelledBy, string reason)
    {
        if (Status == SalesOrderStatus.Completed || Status == SalesOrderStatus.Refunded)
            throw new InvalidOperationException($"Cannot cancel order in status {Status}");

        Status = SalesOrderStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public void Refund(string refundedBy, string reason)
    {
        if (Status != SalesOrderStatus.Completed && Status != SalesOrderStatus.Paid)
            throw new InvalidOperationException($"Can only refund completed or paid orders");

        Status = SalesOrderStatus.Refunded;
        CancelledBy = refundedBy;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = $"Refund: {reason}";
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes;
    }

    public void UpdateCustomer(string? customerId, string? customerName, string? customerPhone)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
    }

    // Analytics helper methods
    public decimal GetProfitMargin()
    {
        if (SubTotal == 0) return 0;
        var cost = Items.Sum(i => i.Quantity * (i.UnitPrice * 0.7m)); // Assuming 30% markup
        return ((SubTotal - cost) / SubTotal) * 100;
    }

    public int GetTotalItemsCount()
    {
        return Items.Sum(i => i.Quantity);
    }
}

/// <summary>
/// Sales order line item
/// </summary>
public class SalesOrderItem
{
    public string Id { get; private set; }
    public string SalesOrderId { get; private set; }
    public string DrugId { get; private set; }
    
    // Order details
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice { get; private set; }
    
    // Batch tracking
    public string? BatchNumber { get; private set; }

    private SalesOrderItem() { } // EF Core

    public SalesOrderItem(
        string salesOrderId,
        string drugId,
        int quantity,
        decimal unitPrice,
        decimal? discountPercentage = null,
        string? batchNumber = null)
    {
        Id = Guid.NewGuid().ToString();
        SalesOrderId = salesOrderId;
        DrugId = drugId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercentage = discountPercentage ?? 0;
        BatchNumber = batchNumber;
        
        CalculateAmounts();
    }

    public void Update(int quantity, decimal unitPrice, decimal? discountPercentage = null)
    {
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercentage = discountPercentage ?? 0;
        
        CalculateAmounts();
    }

    private void CalculateAmounts()
    {
        var subtotal = Quantity * UnitPrice;
        DiscountAmount = subtotal * (DiscountPercentage / 100);
        TotalPrice = subtotal - DiscountAmount;
    }
}
