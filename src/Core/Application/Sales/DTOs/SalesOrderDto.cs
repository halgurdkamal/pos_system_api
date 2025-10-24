namespace pos_system_api.Core.Application.Sales.DTOs;

public record SalesOrderDto
{
    public string Id { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string Status { get; init; } = string.Empty;
    
    // Financial
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal ChangeGiven { get; init; }
    
    // Payment
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    public DateTime? PaidAt { get; init; }
    
    // Dates
    public DateTime OrderDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    
    // Users
    public string CashierId { get; init; } = string.Empty;
    public string? CancelledBy { get; init; }
    public string? CancellationReason { get; init; }
    
    // Additional
    public string? Notes { get; init; }
    public bool IsPrescriptionRequired { get; init; }
    public string? PrescriptionNumber { get; init; }
    
    // Items
    public List<SalesOrderItemDto> Items { get; init; } = new();
    
    // Analytics
    public decimal ProfitMargin { get; init; }
    public int TotalItemsCount { get; init; }
}

public record SalesOrderItemDto
{
    public string Id { get; init; } = string.Empty;
    public string DrugId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalPrice { get; init; }
    public string? BatchNumber { get; init; }
}

public record SalesOrderSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OrderDate { get; init; }
    public string CashierId { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public string? PaymentMethod { get; init; }
}

// Dashboard analytics DTOs
public record SalesOrderDashboardDto
{
    public decimal TotalSales { get; init; }
    public int TotalOrders { get; init; }
    public int TodaysOrders { get; init; }
    public decimal TodaysSales { get; init; }
    public int CompletedOrders { get; init; }
    public int CancelledOrders { get; init; }
    public decimal AverageOrderValue { get; init; }
    public List<SalesOrderSummaryDto> RecentOrders { get; init; } = new();
    public Dictionary<string, decimal> SalesByCashier { get; init; } = new();
    public Dictionary<string, decimal> SalesByPaymentMethod { get; init; } = new();
    public Dictionary<string, int> TopSellingDrugs { get; init; } = new();
}

public record CashierPerformanceDto
{
    public string CashierId { get; init; } = string.Empty;
    public string CashierName { get; init; } = string.Empty;
    public int TotalOrders { get; init; }
    public decimal TotalSales { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int TodaysOrders { get; init; }
    public decimal TodaysSales { get; init; }
}
