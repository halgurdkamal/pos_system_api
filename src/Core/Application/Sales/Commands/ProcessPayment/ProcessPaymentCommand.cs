using MediatR;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Domain.Sales.Entities;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Core.Application.Sales.Commands.ProcessPayment;

public record ProcessPaymentCommand : IRequest<SalesOrderDto>
{
    public string OrderId { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public string? PaymentReference { get; init; }
}

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly ISalesStockService _salesStockService;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        ISalesOrderRepository repository,
        ISalesStockService salesStockService,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _repository = repository;
        _salesStockService = salesStockService;
        _logger = logger;
    }

    public async Task<SalesOrderDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var salesOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (salesOrder == null)
            throw new KeyNotFoundException($"Sales order {request.OrderId} not found");

        _logger.LogInformation("Processing payment for sales order {OrderNumber}", salesOrder.OrderNumber);

        // Parse payment method
        var paymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true);

        // Process payment — flips status Draft|Confirmed → Paid. After this point
        // the goods physically leave the shop, so we must deduct stock.
        salesOrder.ProcessPayment(paymentMethod, request.AmountPaid, request.PaymentReference);

        await _repository.UpdateAsync(salesOrder, cancellationToken);

        // Deduct from ShopInventory in the same logical transaction.
        // (No explicit DB transaction yet; if this fails after the order save, the
        // order is Paid but inventory wasn't decremented. Wrap in a unit-of-work
        // when transactional outboxes are added.)
        await _salesStockService.DeductForSaleAsync(salesOrder, cancellationToken);

        _logger.LogInformation("Payment processed for order {OrderNumber}: {PaymentMethod} - {AmountPaid:C}",
            salesOrder.OrderNumber, paymentMethod, request.AmountPaid);

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
            BatchNumber = item.BatchNumber
        };
    }
}
