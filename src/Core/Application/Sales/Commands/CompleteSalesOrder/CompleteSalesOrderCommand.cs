using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;

namespace pos_system_api.Core.Application.Sales.Commands.CompleteSalesOrder;

public record CompleteSalesOrderCommand(string OrderId) : IRequest<SalesOrderDto>;

public class CompleteSalesOrderCommandHandler
    : IRequestHandler<CompleteSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly ILogger<CompleteSalesOrderCommandHandler> _logger;

    public CompleteSalesOrderCommandHandler(
        ISalesOrderRepository repository,
        ILogger<CompleteSalesOrderCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SalesOrderDto> Handle(
        CompleteSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var salesOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (salesOrder == null)
        {
            throw new KeyNotFoundException($"Sales order {request.OrderId} not found");
        }

        salesOrder.Complete();
        await _repository.UpdateAsync(salesOrder, cancellationToken);

        _logger.LogInformation("Sales order {OrderNumber} completed", salesOrder.OrderNumber);

        return SalesOrderMappers.ToDto(salesOrder);
    }
}
