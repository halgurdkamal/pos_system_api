using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;

namespace pos_system_api.Core.Application.Sales.Commands.CancelSalesOrder;

public record CancelSalesOrderCommand(string OrderId, string CancelledBy, string Reason)
    : IRequest<SalesOrderDto>;

public class CancelSalesOrderCommandHandler
    : IRequestHandler<CancelSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly ILogger<CancelSalesOrderCommandHandler> _logger;

    public CancelSalesOrderCommandHandler(
        ISalesOrderRepository repository,
        ILogger<CancelSalesOrderCommandHandler> logger)
    {
        _repository = repository;
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

        salesOrder.Cancel(request.CancelledBy, request.Reason);
        await _repository.UpdateAsync(salesOrder, cancellationToken);

        _logger.LogInformation(
            "Sales order {OrderNumber} cancelled by {CancelledBy}: {Reason}",
            salesOrder.OrderNumber, request.CancelledBy, request.Reason);

        return SalesOrderMappers.ToDto(salesOrder);
    }
}
