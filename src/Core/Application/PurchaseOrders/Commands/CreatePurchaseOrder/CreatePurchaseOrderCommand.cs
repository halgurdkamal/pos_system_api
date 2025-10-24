using MediatR;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Core.Application.PurchaseOrders.Commands.CreatePurchaseOrder;

public record CreatePurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public string ShopId { get; init; } = string.Empty;
    public string SupplierId { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public string Priority { get; init; } = "Normal";
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string PaymentTerms { get; init; } = "Net30";
    public string? CustomPaymentTerms { get; init; }
    public string? Notes { get; init; }
    public string? DeliveryAddress { get; init; }
    public string? ReferenceNumber { get; init; }
    public List<CreatePurchaseOrderItemDto> Items { get; init; } = new();
    public decimal? ShippingCost { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
}

public record CreatePurchaseOrderItemDto
{
    public string DrugId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal? DiscountPercentage { get; init; }
}

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly ILogger<CreatePurchaseOrderCommandHandler> _logger;

    public CreatePurchaseOrderCommandHandler(
        IPurchaseOrderRepository repository,
        ILogger<CreatePurchaseOrderCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating purchase order for shop {ShopId} from supplier {SupplierId}",
            request.ShopId, request.SupplierId);

        // Parse enums
        var priority = Enum.Parse<OrderPriority>(request.Priority, ignoreCase: true);
        var paymentTerms = Enum.Parse<PaymentTerms>(request.PaymentTerms, ignoreCase: true);

        // Create purchase order
        var purchaseOrder = new PurchaseOrder(
            request.ShopId,
            request.SupplierId,
            request.CreatedBy,
            priority,
            request.ExpectedDeliveryDate,
            paymentTerms,
            request.CustomPaymentTerms,
            request.Notes,
            request.DeliveryAddress,
            request.ReferenceNumber);

        // Add items
        foreach (var itemDto in request.Items)
        {
            purchaseOrder.AddItem(
                itemDto.DrugId,
                itemDto.Quantity,
                itemDto.UnitPrice,
                itemDto.DiscountPercentage);
        }

        // Set shipping and tax
        if (request.ShippingCost.HasValue || request.TaxAmount.HasValue)
        {
            purchaseOrder.SetShippingAndTax(
                request.ShippingCost ?? 0,
                request.TaxAmount ?? 0);
        }

        // Apply discount
        if (request.DiscountAmount.HasValue && request.DiscountAmount.Value > 0)
        {
            purchaseOrder.ApplyDiscount(request.DiscountAmount.Value);
        }

        await _repository.AddAsync(purchaseOrder, cancellationToken);

        _logger.LogInformation("Purchase order {OrderNumber} created successfully", purchaseOrder.OrderNumber);

        return MapToDto(purchaseOrder);
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po)
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

    private static PurchaseOrderItemDto MapItemToDto(PurchaseOrderItem item)
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
