using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Sales.DTOs;

/// <summary>
/// Centralised projections from <see cref="SalesOrder"/> aggregates to DTOs.
/// New handlers should use this helper instead of duplicating MapToDto blocks.
/// </summary>
internal static class SalesOrderMappers
{
    public static SalesOrderDto ToDto(SalesOrder so) => new()
    {
        Id = so.Id,
        OrderNumber = so.OrderNumber,
        ShopId = so.ShopId,
        CustomerId = so.CustomerId,
        CustomerName = so.CustomerName,
        CustomerPhone = so.CustomerPhone,
        Status = so.Status.ToString(),
        SubTotal = so.SubTotal,
        TaxAmount = so.TaxAmount,
        DiscountAmount = so.DiscountAmount,
        TotalAmount = so.TotalAmount,
        AmountPaid = so.AmountPaid,
        ChangeGiven = so.ChangeGiven,
        PaymentMethod = so.PaymentMethod?.ToString(),
        PaymentReference = so.PaymentReference,
        PaidAt = so.PaidAt,
        OrderDate = so.OrderDate,
        CompletedAt = so.CompletedAt,
        CancelledAt = so.CancelledAt,
        CashierId = so.CashierId,
        CancelledBy = so.CancelledBy,
        CancellationReason = so.CancellationReason,
        Notes = so.Notes,
        IsPrescriptionRequired = so.IsPrescriptionRequired,
        PrescriptionNumber = so.PrescriptionNumber,
        Items = so.Items.Select(ToItemDto).ToList(),
        ProfitMargin = so.GetProfitMargin(),
        TotalItemsCount = so.GetTotalItemsCount(),
    };

    public static SalesOrderItemDto ToItemDto(SalesOrderItem item) => new()
    {
        Id = item.Id,
        DrugId = item.DrugId,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        DiscountPercentage = item.DiscountPercentage,
        DiscountAmount = item.DiscountAmount,
        TotalPrice = item.TotalPrice,
        BatchNumber = item.BatchNumber,
    };

    public static SalesOrderSummaryDto ToSummaryDto(SalesOrder so) => new()
    {
        Id = so.Id,
        OrderNumber = so.OrderNumber,
        ShopId = so.ShopId,
        CustomerName = so.CustomerName,
        Status = so.Status.ToString(),
        TotalAmount = so.TotalAmount,
        OrderDate = so.OrderDate,
        CashierId = so.CashierId,
        ItemCount = so.Items.Count,
        PaymentMethod = so.PaymentMethod?.ToString(),
    };
}
