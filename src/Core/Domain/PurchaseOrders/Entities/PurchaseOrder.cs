using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.PurchaseOrders.Entities;

/// <summary>
/// Purchase order status workflow
/// </summary>
public enum PurchaseOrderStatus
{
    Draft,          // Initial creation, being edited
    Submitted,      // Sent to supplier, awaiting confirmation
    Confirmed,      // Supplier confirmed the order
    PartiallyReceived, // Some items received
    Completed,      // All items received
    Cancelled       // Order cancelled
}

/// <summary>
/// Purchase order priority levels
/// </summary>
public enum OrderPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Purchase order payment terms
/// </summary>
public enum PaymentTerms
{
    Immediate,      // Pay on receipt
    Net15,          // Pay within 15 days
    Net30,          // Pay within 30 days
    Net45,          // Pay within 45 days
    Net60,          // Pay within 60 days
    Custom          // Custom terms
}

/// <summary>
/// Purchase order entity with comprehensive tracking for analytics
/// </summary>
public class PurchaseOrder : BaseEntity
{
    // Core Information
    public string OrderNumber { get; private set; }
    public string ShopId { get; private set; }
    public string SupplierId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public OrderPriority Priority { get; private set; }

    // Financial Information (for dashboard analytics)
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Payment & Terms
    public PaymentTerms PaymentTerms { get; private set; }
    public string? CustomPaymentTerms { get; private set; }
    public DateTime? PaymentDueDate { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime? PaidAt { get; private set; }

    // Dates (for tracking and analytics)
    public DateTime OrderDate { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // User Tracking (for audit and performance analytics)
    public new string CreatedBy { get; private set; }
    public string? SubmittedBy { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }

    // Additional Information
    public string? Notes { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? ReferenceNumber { get; private set; }

    // Items
    public List<PurchaseOrderItem> Items { get; private set; } = new();

    private PurchaseOrder() { } // EF Core

    public PurchaseOrder(
        string shopId,
        string supplierId,
        string createdBy,
        OrderPriority priority = OrderPriority.Normal,
        DateTime? expectedDeliveryDate = null,
        PaymentTerms paymentTerms = PaymentTerms.Net30,
        string? customPaymentTerms = null,
        string? notes = null,
        string? deliveryAddress = null,
        string? referenceNumber = null)
    {
        Id = Guid.NewGuid().ToString();
        OrderNumber = GenerateOrderNumber();
        ShopId = shopId;
        SupplierId = supplierId;
        Status = PurchaseOrderStatus.Draft;
        Priority = priority;
        CreatedBy = createdBy;
        OrderDate = DateTime.UtcNow;
        ExpectedDeliveryDate = expectedDeliveryDate;
        PaymentTerms = paymentTerms;
        CustomPaymentTerms = customPaymentTerms;
        Notes = notes;
        DeliveryAddress = deliveryAddress;
        ReferenceNumber = referenceNumber;

        SubTotal = 0;
        TaxAmount = 0;
        ShippingCost = 0;
        DiscountAmount = 0;
        TotalAmount = 0;
        IsPaid = false;
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"PO-{timestamp}-{random}";
    }

    public void AddItem(string drugId, int quantity, decimal unitPrice, decimal? discountPercentage = null)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Can only add items to draft orders");

        var item = new PurchaseOrderItem(Id, drugId, quantity, unitPrice, discountPercentage);
        Items.Add(item);
        RecalculateTotals();
    }

    public void UpdateItem(string itemId, int quantity, decimal unitPrice, decimal? discountPercentage = null)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Can only update items in draft orders");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new KeyNotFoundException($"Item {itemId} not found");

        item.Update(quantity, unitPrice, discountPercentage);
        RecalculateTotals();
    }

    public void RemoveItem(string itemId)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Can only remove items from draft orders");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotals();
        }
    }

    public void SetShippingAndTax(decimal shippingCost, decimal taxAmount)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Can only update shipping/tax in draft orders");

        ShippingCost = shippingCost;
        TaxAmount = taxAmount;
        RecalculateTotals();
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Can only apply discount to draft orders");

        DiscountAmount = discountAmount;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        TotalAmount = SubTotal + TaxAmount + ShippingCost - DiscountAmount;

        // Calculate payment due date based on terms
        if (PaymentTerms != PaymentTerms.Custom && SubmittedAt.HasValue)
        {
            PaymentDueDate = PaymentTerms switch
            {
                PaymentTerms.Immediate => SubmittedAt.Value,
                PaymentTerms.Net15 => SubmittedAt.Value.AddDays(15),
                PaymentTerms.Net30 => SubmittedAt.Value.AddDays(30),
                PaymentTerms.Net45 => SubmittedAt.Value.AddDays(45),
                PaymentTerms.Net60 => SubmittedAt.Value.AddDays(60),
                _ => null
            };
        }
    }

    public void Submit(string submittedBy)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot submit order in status {Status}");

        if (!Items.Any())
            throw new InvalidOperationException("Cannot submit order without items");

        Status = PurchaseOrderStatus.Submitted;
        SubmittedBy = submittedBy;
        SubmittedAt = DateTime.UtcNow;
        RecalculateTotals(); // Recalculate to set payment due date
    }

    public void Confirm(string confirmedBy)
    {
        if (Status != PurchaseOrderStatus.Submitted)
            throw new InvalidOperationException($"Can only confirm submitted orders");

        Status = PurchaseOrderStatus.Confirmed;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void MarkAsPartiallyReceived()
    {
        if (Status != PurchaseOrderStatus.Confirmed && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException($"Cannot mark as partially received in status {Status}");

        Status = PurchaseOrderStatus.PartiallyReceived;
    }

    public void Complete()
    {
        if (Status != PurchaseOrderStatus.Confirmed && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException($"Cannot complete order in status {Status}");

        // Check if all items are fully received
        if (Items.Any(i => i.ReceivedQuantity < i.OrderedQuantity))
            throw new InvalidOperationException("Cannot complete order with unreceived items");

        Status = PurchaseOrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancelledBy, string reason)
    {
        if (Status == PurchaseOrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed orders");

        Status = PurchaseOrderStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public void MarkAsPaid(DateTime? paidAt = null)
    {
        IsPaid = true;
        PaidAt = paidAt ?? DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes;
    }

    // Analytics helper methods
    public decimal GetCompletionPercentage()
    {
        if (!Items.Any()) return 0;

        var totalOrdered = Items.Sum(i => i.OrderedQuantity);
        var totalReceived = Items.Sum(i => i.ReceivedQuantity);

        return totalOrdered > 0 ? (decimal)totalReceived / totalOrdered * 100 : 0;
    }

    public int GetDaysToDeliver()
    {
        if (!CompletedAt.HasValue) return 0;
        return (CompletedAt.Value - OrderDate).Days;
    }

    public bool IsOverdue()
    {
        return PaymentDueDate.HasValue && !IsPaid && DateTime.UtcNow > PaymentDueDate.Value;
    }
}

/// <summary>
/// Purchase order line item with receiving tracking
/// </summary>
public class PurchaseOrderItem
{
    public string Id { get; private set; }
    public string PurchaseOrderId { get; private set; }
    public string DrugId { get; private set; }

    // Order details
    public int OrderedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice { get; private set; }

    // Receiving tracking (for performance analytics)
    public int ReceivedQuantity { get; private set; }
    public List<ReceiptRecord> Receipts { get; private set; } = new();

    private PurchaseOrderItem() { } // EF Core

    public PurchaseOrderItem(
        string purchaseOrderId,
        string drugId,
        int orderedQuantity,
        decimal unitPrice,
        decimal? discountPercentage = null)
    {
        Id = Guid.NewGuid().ToString();
        PurchaseOrderId = purchaseOrderId;
        DrugId = drugId;
        OrderedQuantity = orderedQuantity;
        UnitPrice = unitPrice;
        DiscountPercentage = discountPercentage ?? 0;
        ReceivedQuantity = 0;

        CalculateAmounts();
    }

    public void Update(int orderedQuantity, decimal unitPrice, decimal? discountPercentage = null)
    {
        OrderedQuantity = orderedQuantity;
        UnitPrice = unitPrice;
        DiscountPercentage = discountPercentage ?? 0;

        CalculateAmounts();
    }

    private void CalculateAmounts()
    {
        var subtotal = OrderedQuantity * UnitPrice;
        DiscountAmount = subtotal * (DiscountPercentage / 100);
        TotalPrice = subtotal - DiscountAmount;
    }

    public void ReceiveQuantity(int quantity, string batchNumber, DateTime expiryDate, string receivedBy)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (ReceivedQuantity + quantity > OrderedQuantity)
            throw new InvalidOperationException("Cannot receive more than ordered quantity");

        var receipt = new ReceiptRecord(
            Id,
            quantity,
            batchNumber,
            expiryDate,
            receivedBy,
            DateTime.UtcNow);

        Receipts.Add(receipt);
        ReceivedQuantity += quantity;
    }

    public bool IsFullyReceived() => ReceivedQuantity >= OrderedQuantity;
    public int GetPendingQuantity() => OrderedQuantity - ReceivedQuantity;
}

/// <summary>
/// Receipt record for tracking individual stock receipts
/// </summary>
public class ReceiptRecord
{
    public string Id { get; private set; }
    public string OrderItemId { get; private set; }
    public int Quantity { get; private set; }
    public string BatchNumber { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public string ReceivedBy { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    private ReceiptRecord() { } // EF Core

    public ReceiptRecord(
        string orderItemId,
        int quantity,
        string batchNumber,
        DateTime expiryDate,
        string receivedBy,
        DateTime receivedAt)
    {
        Id = Guid.NewGuid().ToString();
        OrderItemId = orderItemId;
        Quantity = quantity;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        ReceivedBy = receivedBy;
        ReceivedAt = receivedAt;
    }
}
