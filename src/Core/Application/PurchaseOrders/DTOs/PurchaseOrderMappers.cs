using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace pos_system_api.Core.Application.PurchaseOrders.DTOs;

/// <summary>
/// Centralised projections from <see cref="PurchaseOrder"/> aggregates to DTOs.
/// Existing handlers inline their own mapping; new handlers should use this helper.
/// </summary>
internal static class PurchaseOrderMappers
{
    public static PurchaseOrderDto ToDto(PurchaseOrder po) => new()
    {
        Id = po.Id,
        OrderNumber = po.OrderNumber,
        ShopId = po.ShopId,
        SupplierId = po.SupplierId,
        Status = po.Status.ToString(),
        Priority = po.Priority.ToString(),
        SubTotal = po.SubTotal,
        TaxAmount = po.TaxAmount,
        ShippingCost = po.ShippingCost,
        DiscountAmount = po.DiscountAmount,
        TotalAmount = po.TotalAmount,
        PaymentTerms = po.PaymentTerms.ToString(),
        CustomPaymentTerms = po.CustomPaymentTerms,
        PaymentDueDate = po.PaymentDueDate,
        IsPaid = po.IsPaid,
        PaidAt = po.PaidAt,
        OrderDate = po.OrderDate,
        ExpectedDeliveryDate = po.ExpectedDeliveryDate,
        SubmittedAt = po.SubmittedAt,
        ConfirmedAt = po.ConfirmedAt,
        CompletedAt = po.CompletedAt,
        CancelledAt = po.CancelledAt,
        CreatedBy = po.CreatedBy,
        SubmittedBy = po.SubmittedBy,
        ConfirmedBy = po.ConfirmedBy,
        CancelledBy = po.CancelledBy,
        CancellationReason = po.CancellationReason,
        Notes = po.Notes,
        DeliveryAddress = po.DeliveryAddress,
        ReferenceNumber = po.ReferenceNumber,
        Items = po.Items.Select(ToItemDto).ToList(),
        CompletionPercentage = po.GetCompletionPercentage(),
        DaysToDeliver = po.GetDaysToDeliver(),
        IsOverdue = po.IsOverdue(),
    };

    public static PurchaseOrderItemDto ToItemDto(PurchaseOrderItem item) => new()
    {
        Id = item.Id,
        DrugId = item.DrugId,
        OrderedQuantity = item.OrderedQuantity,
        UnitPrice = item.UnitPrice,
        DiscountPercentage = item.DiscountPercentage,
        DiscountAmount = item.DiscountAmount,
        TotalPrice = item.TotalPrice,
        ReceivedQuantity = item.ReceivedQuantity,
        PendingQuantity = item.GetPendingQuantity(),
        IsFullyReceived = item.IsFullyReceived(),
        Receipts = item.Receipts.Select(r => new ReceiptRecordDto
        {
            Id = r.Id,
            Quantity = r.Quantity,
            BatchNumber = r.BatchNumber,
            ExpiryDate = r.ExpiryDate,
            ReceivedBy = r.ReceivedBy,
            ReceivedAt = r.ReceivedAt,
        }).ToList(),
    };

    public static PurchaseOrderSummaryDto ToSummaryDto(PurchaseOrder po) => new()
    {
        Id = po.Id,
        OrderNumber = po.OrderNumber,
        ShopId = po.ShopId,
        SupplierId = po.SupplierId,
        Status = po.Status.ToString(),
        Priority = po.Priority.ToString(),
        TotalAmount = po.TotalAmount,
        OrderDate = po.OrderDate,
        ExpectedDeliveryDate = po.ExpectedDeliveryDate,
        IsPaid = po.IsPaid,
        ItemCount = po.Items.Count,
        CompletionPercentage = po.GetCompletionPercentage(),
    };
}
