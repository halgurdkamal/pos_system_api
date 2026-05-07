using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Sales.Commands.CancelSalesOrder;

public record CancelSalesOrderCommand(string OrderId, string CancelledBy, string Reason)
    : IRequest<SalesOrderDto>;

public class CancelSalesOrderCommandHandler
    : IRequestHandler<CancelSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly ISalesStockService _salesStockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSalesOrderCommandHandler> _logger;

    public CancelSalesOrderCommandHandler(
        ISalesOrderRepository repository,
        ISalesStockService salesStockService,
        IUnitOfWork unitOfWork,
        ILogger<CancelSalesOrderCommandHandler> logger)
    {
        _repository = repository;
        _salesStockService = salesStockService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SalesOrderDto> Handle(
        CancelSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var salesOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (salesOrder == null)
        {
            throw new KeyNotFoundException($"Sales order {request.OrderId} not found");
        }

        // Capture status BEFORE Cancel(), so we know whether stock was previously
        // deducted by ProcessPayment. Cancel is valid from Draft, Confirmed, or
        // Paid; only the Paid case requires a stock restore.
        var previouslyDeducted = salesOrder.Status == SalesOrderStatus.Paid;

        salesOrder.Cancel(request.CancelledBy, request.Reason);
        await _repository.UpdateAsync(salesOrder, cancellationToken);

        if (previouslyDeducted)
        {
            await _salesStockService.RestoreForReversalAsync(salesOrder, cancellationToken);
        }

        _logger.LogInformation(
            "Sales order {OrderNumber} cancelled by {CancelledBy}: {Reason} (stock restored: {StockRestored})",
            salesOrder.OrderNumber, request.CancelledBy, request.Reason, previouslyDeducted);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SalesOrderMappers.ToDto(salesOrder);
    }
}
