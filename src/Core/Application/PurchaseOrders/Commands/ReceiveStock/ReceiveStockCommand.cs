using MediatR;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Core.Application.PurchaseOrders.Commands.ReceiveStock;

public record ReceiveStockCommand : IRequest<PurchaseOrderDto>
{
    public string OrderId { get; init; } = string.Empty;
    public string ReceivedBy { get; init; } = string.Empty;
    public List<ReceiveStockItemDto> Items { get; init; } = new();
}

public record ReceiveStockItemDto
{
    public string ItemId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string BatchNumber { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
}

public class ReceiveStockCommandHandler : IRequestHandler<ReceiveStockCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _poRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<ReceiveStockCommandHandler> _logger;

    public ReceiveStockCommandHandler(
        IPurchaseOrderRepository poRepository,
        IInventoryRepository inventoryRepository,
        ILogger<ReceiveStockCommandHandler> logger)
    {
        _poRepository = poRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<PurchaseOrderDto> Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _poRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (purchaseOrder == null)
            throw new KeyNotFoundException($"Purchase order {request.OrderId} not found");

        _logger.LogInformation("Receiving stock for purchase order {OrderNumber}", purchaseOrder.OrderNumber);

        // Process each item
        foreach (var receiptDto in request.Items)
        {
            var orderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == receiptDto.ItemId);
            if (orderItem == null)
            {
                _logger.LogWarning("Order item {ItemId} not found in order {OrderNumber}",
                    receiptDto.ItemId, purchaseOrder.OrderNumber);
                continue;
            }

            // Record receipt
            orderItem.ReceiveQuantity(
                receiptDto.Quantity,
                receiptDto.BatchNumber,
                receiptDto.ExpiryDate,
                request.ReceivedBy);

            // Update inventory
            await UpdateInventoryAsync(
                purchaseOrder.ShopId,
                orderItem.DrugId,
                receiptDto.Quantity,
                receiptDto.BatchNumber,
                receiptDto.ExpiryDate,
                request.ReceivedBy,
                cancellationToken);

            _logger.LogInformation("Received {Quantity} units of drug {DrugId} for order {OrderNumber}",
                receiptDto.Quantity, orderItem.DrugId, purchaseOrder.OrderNumber);
        }

        // Update order status
        if (purchaseOrder.Items.All(i => i.IsFullyReceived()))
        {
            purchaseOrder.Complete();
            _logger.LogInformation("Purchase order {OrderNumber} completed - all items received",
                purchaseOrder.OrderNumber);
        }
        else if (purchaseOrder.Items.Any(i => i.ReceivedQuantity > 0))
        {
            purchaseOrder.MarkAsPartiallyReceived();
            _logger.LogInformation("Purchase order {OrderNumber} partially received",
                purchaseOrder.OrderNumber);
        }

        await _poRepository.UpdateAsync(purchaseOrder, cancellationToken);

        return MapToDto(purchaseOrder);
    }

    private async Task UpdateInventoryAsync(
        string shopId,
        string drugId,
        int quantity,
        string batchNumber,
        DateTime expiryDate,
        string receivedBy,
        CancellationToken cancellationToken)
    {
        // Get existing inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);

        if (inventory == null)
        {
            _logger.LogWarning("Inventory not found for shop {ShopId} and drug {DrugId}. " +
                "Please create inventory record first.", shopId, drugId);
            return;
        }

        // Log the receipt - actual inventory batch management
        // should be done through dedicated StockAdjustment commands
        _logger.LogInformation("Received stock for drug {DrugId} in shop {ShopId}: " +
            "{Quantity} units in batch {BatchNumber}, expiry {ExpiryDate:yyyy-MM-dd}",
            drugId, shopId, quantity, batchNumber, expiryDate);

        // TODO: Integrate with StockAdjustment system to properly track
        // batches, expiry dates, and update inventory totals
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
