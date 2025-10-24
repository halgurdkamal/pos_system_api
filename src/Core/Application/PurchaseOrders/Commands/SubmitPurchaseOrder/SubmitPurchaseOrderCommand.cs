using MediatR;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Core.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;

public record SubmitPurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public string OrderId { get; init; } = string.Empty;
    public string SubmittedBy { get; init; } = string.Empty;
}

public class SubmitPurchaseOrderCommandHandler : IRequestHandler<SubmitPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly ILogger<SubmitPurchaseOrderCommandHandler> _logger;

    public SubmitPurchaseOrderCommandHandler(
        IPurchaseOrderRepository repository,
        ILogger<SubmitPurchaseOrderCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PurchaseOrderDto> Handle(SubmitPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (purchaseOrder == null)
            throw new KeyNotFoundException($"Purchase order {request.OrderId} not found");

        _logger.LogInformation("Submitting purchase order {OrderNumber}", purchaseOrder.OrderNumber);

        purchaseOrder.Submit(request.SubmittedBy);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);

        _logger.LogInformation("Purchase order {OrderNumber} submitted successfully", purchaseOrder.OrderNumber);

        return MapToDto(purchaseOrder);
    }

    private static PurchaseOrderDto MapToDto(pos_system_api.Core.Domain.PurchaseOrders.Entities.PurchaseOrder po)
    {
        return new PurchaseOrderDto
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
            Items = po.Items.Select(MapItemToDto).ToList(),
            CompletionPercentage = po.GetCompletionPercentage(),
            DaysToDeliver = po.GetDaysToDeliver(),
            IsOverdue = po.IsOverdue()
        };
    }

    private static PurchaseOrderItemDto MapItemToDto(pos_system_api.Core.Domain.PurchaseOrders.Entities.PurchaseOrderItem item)
    {
        return new PurchaseOrderItemDto
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
                ReceivedAt = r.ReceivedAt
            }).ToList()
        };
    }
}
