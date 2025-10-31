namespace pos_system_api.Core.Application.PurchaseOrders.DTOs;

public record PurchaseOrderDto
{
    public string Id { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public string SupplierId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;

    // Financial
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }

    // Payment
    public string PaymentTerms { get; init; } = string.Empty;
    public string? CustomPaymentTerms { get; init; }
    public DateTime? PaymentDueDate { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? PaidAt { get; init; }

    // Dates
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }

    // Users
    public string CreatedBy { get; init; } = string.Empty;
    public string? SubmittedBy { get; init; }
    public string? ConfirmedBy { get; init; }
    public string? CancelledBy { get; init; }
    public string? CancellationReason { get; init; }

    // Additional
    public string? Notes { get; init; }
    public string? DeliveryAddress { get; init; }
    public string? ReferenceNumber { get; init; }

    // Items
    public List<PurchaseOrderItemDto> Items { get; init; } = new();

    // Analytics
    public decimal CompletionPercentage { get; init; }
    public int DaysToDeliver { get; init; }
    public bool IsOverdue { get; init; }
}

public record PurchaseOrderItemDto
{
    public string Id { get; init; } = string.Empty;
    public string DrugId { get; init; } = string.Empty;
    public int OrderedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalPrice { get; init; }
    public int ReceivedQuantity { get; init; }
    public int PendingQuantity { get; init; }
    public bool IsFullyReceived { get; init; }
    public List<ReceiptRecordDto> Receipts { get; init; } = new();
}

public record ReceiptRecordDto
{
    public string Id { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string BatchNumber { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
    public string ReceivedBy { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
}

public record PurchaseOrderSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public string SupplierId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public bool IsPaid { get; init; }
    public int ItemCount { get; init; }
    public decimal CompletionPercentage { get; init; }
}

// Dashboard analytics DTOs
public record PurchaseOrderDashboardDto
{
    public decimal TotalOrderValue { get; init; }
    public int TotalOrders { get; init; }
    public int DraftOrders { get; init; }
    public int PendingOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int OverduePayments { get; init; }
    public decimal OutstandingPayments { get; init; }
    public List<PurchaseOrderSummaryDto> RecentOrders { get; init; } = new();
    public Dictionary<string, decimal> SpendingBySupplier { get; init; } = new();
    public Dictionary<string, int> OrdersBySupplier { get; init; } = new();
}

public record SupplierPerformanceDto
{
    public string SupplierId { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public int TotalOrders { get; init; }
    public decimal TotalSpending { get; init; }
    public double AverageDeliveryDays { get; init; }
    public int CompletedOrders { get; init; }
    public int CancelledOrders { get; init; }
    public decimal OnTimeDeliveryRate { get; init; }
}
