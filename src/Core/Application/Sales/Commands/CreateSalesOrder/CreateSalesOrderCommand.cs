using MediatR;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Core.Application.Sales.Commands.CreateSalesOrder;

public record CreateSalesOrderCommand : IRequest<SalesOrderDto>
{
    public string ShopId { get; init; } = string.Empty;
    public string CashierId { get; init; } = string.Empty;
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public bool IsPrescriptionRequired { get; init; }
    public string? PrescriptionNumber { get; init; }
    public string? Notes { get; init; }
    public List<CreateSalesOrderItemDto> Items { get; init; } = new();
    public decimal? TaxAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
}

public record CreateSalesOrderItemDto
{
    public string DrugId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal? UnitPrice { get; init; } // Optional - can be auto-populated from packaging level
    public string? PackagingLevel { get; init; } // e.g., "Box", "Strip" - for auto-pricing
    public decimal? DiscountPercentage { get; init; }
    public string? BatchNumber { get; init; }
}

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly IDrugRepository _drugRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<CreateSalesOrderCommandHandler> _logger;

    public CreateSalesOrderCommandHandler(
        ISalesOrderRepository repository,
        IDrugRepository drugRepository,
        IInventoryRepository inventoryRepository,
        ILogger<CreateSalesOrderCommandHandler> logger)
    {
        _repository = repository;
        _drugRepository = drugRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<SalesOrderDto> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating sales order for shop {ShopId} by cashier {CashierId}",
            request.ShopId, request.CashierId);

        // Create sales order
        var salesOrder = new SalesOrder(
            request.ShopId,
            request.CashierId,
            request.CustomerId,
            request.CustomerName,
            request.CustomerPhone,
            request.IsPrescriptionRequired,
            request.PrescriptionNumber,
            request.Notes);

        // Add items
        foreach (var itemDto in request.Items)
        {
            // Auto-populate price from packaging level if not provided
            decimal unitPrice = itemDto.UnitPrice ?? await GetUnitPriceFromPackagingLevel(request.ShopId, itemDto.DrugId, itemDto.PackagingLevel, cancellationToken);

            // Get base units consumed for stock tracking
            decimal baseUnitsConsumed = await CalculateBaseUnitsConsumed(itemDto.DrugId, itemDto.PackagingLevel, itemDto.Quantity, cancellationToken);

            salesOrder.AddItem(
                itemDto.DrugId,
                itemDto.Quantity,
                unitPrice,
                itemDto.DiscountPercentage,
                itemDto.BatchNumber,
                itemDto.PackagingLevel,
                baseUnitsConsumed);
        }

        // Set tax and discount
        if (request.TaxAmount.HasValue)
        {
            salesOrder.SetTax(request.TaxAmount.Value);
        }

        if (request.DiscountAmount.HasValue && request.DiscountAmount.Value > 0)
        {
            salesOrder.ApplyDiscount(request.DiscountAmount.Value);
        }

        await _repository.AddAsync(salesOrder, cancellationToken);

        _logger.LogInformation("Sales order {OrderNumber} created successfully", salesOrder.OrderNumber);

        return MapToDto(salesOrder);
    }

    private static SalesOrderDto MapToDto(SalesOrder so)
    {
        return new SalesOrderDto
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
            Items = so.Items.Select(MapItemToDto).ToList(),
            ProfitMargin = so.GetProfitMargin(),
            TotalItemsCount = so.GetTotalItemsCount()
        };
    }

    private static SalesOrderItemDto MapItemToDto(SalesOrderItem item)
    {
        return new SalesOrderItemDto
        {
            Id = item.Id,
            DrugId = item.DrugId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountPercentage = item.DiscountPercentage,
            DiscountAmount = item.DiscountAmount,
            TotalPrice = item.TotalPrice,
            BatchNumber = item.BatchNumber,
            PackagingLevelSold = item.PackagingLevelSold,
            BaseUnitsConsumed = item.BaseUnitsConsumed
        };
    }

    private async Task<decimal> GetUnitPriceFromPackagingLevel(string shopId, string drugId, string? packagingLevel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(packagingLevel))
        {
            throw new ArgumentException("Packaging level must be specified when unit price is not provided");
        }

        // Get shop-specific inventory for this drug
        var shopInventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);
        if (shopInventory == null)
        {
            throw new KeyNotFoundException($"Drug {drugId} not found in shop {shopId} inventory");
        }

        // Get price from shop's packaging-level pricing
        var price = shopInventory.ShopPricing.GetPackagingLevelPrice(packagingLevel);

        if (price <= 0)
        {
            throw new InvalidOperationException($"No selling price defined for packaging level '{packagingLevel}' of drug {drugId} in shop {shopId}");
        }

        return price;
    }

    private async Task<decimal> CalculateBaseUnitsConsumed(string drugId, string? packagingLevel, int quantity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(packagingLevel))
        {
            // If no packaging level specified, assume quantity is in base units
            return quantity;
        }

        var drug = await _drugRepository.GetByIdAsync(drugId, cancellationToken);
        if (drug == null)
        {
            throw new KeyNotFoundException($"Drug {drugId} not found");
        }

        var packagingInfo = drug.PackagingInfo;
        if (packagingInfo == null)
        {
            // If no packaging info, assume quantity is in base units
            return quantity;
        }

        var level = packagingInfo.PackagingLevels.FirstOrDefault(l => l.UnitName.Equals(packagingLevel, StringComparison.OrdinalIgnoreCase));
        if (level == null)
        {
            // If packaging level not found, assume quantity is in base units
            return quantity;
        }

        return level.CalculateBaseUnitsFromQuantity(quantity);
    }
}
