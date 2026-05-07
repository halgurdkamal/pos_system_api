using MediatR;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReceiveStockCommandHandler> _logger;

    public ReceiveStockCommandHandler(
        IPurchaseOrderRepository poRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReceiveStockCommandHandler> logger)
    {
        _poRepository = poRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
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

            // Record receipt on the PO line.
            orderItem.ReceiveQuantity(
                receiptDto.Quantity,
                receiptDto.BatchNumber,
                receiptDto.ExpiryDate,
                request.ReceivedBy);

            // Push the matching batch into ShopInventory so stock counts reflect
            // what we actually have on hand. Symmetric partner to the deduction
            // that happens on Sales /payment (see SalesStockService).
            await UpdateInventoryAsync(
                purchaseOrder,
                orderItem,
                receiptDto,
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(purchaseOrder);
    }

    private async Task UpdateInventoryAsync(
        pos_system_api.Core.Domain.PurchaseOrders.Entities.PurchaseOrder purchaseOrder,
        PurchaseOrderItem orderItem,
        ReceiveStockItemDto receipt,
        CancellationToken cancellationToken)
    {
        var shopId = purchaseOrder.ShopId;
        var drugId = orderItem.DrugId;

        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);

        // First time receiving this drug into this shop — create a starter
        // inventory row so the batch has somewhere to live. The shop can fine-tune
        // pricing / reorder point / location later via the existing inventory
        // management endpoints.
        if (inventory == null)
        {
            inventory = new ShopInventory(
                shopId: shopId,
                drugId: drugId,
                reorderPoint: 50,
                storageLocation: "Receiving",
                shopPricing: new ShopPricing(
                    costPrice: orderItem.UnitPrice,
                    sellingPrice: orderItem.UnitPrice, // safe default; shop can override
                    discount: 0m,
                    currency: "USD",
                    taxRate: 0m));
            await _inventoryRepository.AddAsync(inventory, cancellationToken);

            _logger.LogInformation(
                "Auto-created ShopInventory for first receipt of drug {DrugId} in shop {ShopId}",
                drugId, shopId);
        }

        var batch = new Batch(
            batchNumber: receipt.BatchNumber,
            supplierId: purchaseOrder.SupplierId,
            quantityOnHand: receipt.Quantity,
            receivedDate: DateTime.UtcNow,
            expiryDate: receipt.ExpiryDate,
            purchasePrice: orderItem.UnitPrice,
            sellingPrice: inventory.ShopPricing?.SellingPrice ?? orderItem.UnitPrice,
            location: BatchLocation.Storage,
            storageLocation: inventory.StorageLocation);

        inventory.AddBatch(batch);
        await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        _logger.LogInformation(
            "Added batch {BatchNumber} ({Quantity} units of {DrugId}, expires {ExpiryDate:yyyy-MM-dd}) " +
            "to shop {ShopId}; inventory total is now {TotalStock}",
            receipt.BatchNumber, receipt.Quantity, drugId, receipt.ExpiryDate, shopId, inventory.TotalStock);
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
