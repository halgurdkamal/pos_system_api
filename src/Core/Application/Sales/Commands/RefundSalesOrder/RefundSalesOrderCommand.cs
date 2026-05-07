using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Application.Sales.DTOs;

namespace pos_system_api.Core.Application.Sales.Commands.RefundSalesOrder;

public record RefundSalesOrderCommand(string OrderId, string RefundedBy, string Reason)
    : IRequest<SalesOrderDto>;

public class RefundSalesOrderCommandHandler
    : IRequestHandler<RefundSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly ISalesStockService _salesStockService;
    private readonly ILogger<RefundSalesOrderCommandHandler> _logger;

    public RefundSalesOrderCommandHandler(
        ISalesOrderRepository repository,
        ISalesStockService salesStockService,
        ILogger<RefundSalesOrderCommandHandler> logger)
    {
        _repository = repository;
        _salesStockService = salesStockService;
        _logger = logger;
    }

    public async Task<SalesOrderDto> Handle(
        RefundSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var salesOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (salesOrder == null)
        {
            throw new KeyNotFoundException($"Sales order {request.OrderId} not found");
        }

        // Refund is only valid from Paid or Completed (enforced by the domain
        // method). In both cases stock was previously deducted by ProcessPayment,
        // so we must restore it as the inverse.
        salesOrder.Refund(request.RefundedBy, request.Reason);
        await _repository.UpdateAsync(salesOrder, cancellationToken);

        await _salesStockService.RestoreForReversalAsync(salesOrder, cancellationToken);

        _logger.LogInformation(
            "Sales order {OrderNumber} refunded by {RefundedBy}: {Reason}",
            salesOrder.OrderNumber, request.RefundedBy, request.Reason);

        return SalesOrderMappers.ToDto(salesOrder);
    }
}
