using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;

namespace pos_system_api.Core.Application.Sales.Commands.ConfirmSalesOrder;

public record ConfirmSalesOrderCommand(string OrderId) : IRequest<SalesOrderDto>;

public class ConfirmSalesOrderCommandHandler
    : IRequestHandler<ConfirmSalesOrderCommand, SalesOrderDto>
{
    private readonly ISalesOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmSalesOrderCommandHandler> _logger;

    public ConfirmSalesOrderCommandHandler(
        ISalesOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmSalesOrderCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SalesOrderDto> Handle(
        ConfirmSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var salesOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (salesOrder == null)
        {
            throw new KeyNotFoundException($"Sales order {request.OrderId} not found");
        }

        salesOrder.Confirm();
        await _repository.UpdateAsync(salesOrder, cancellationToken);

        _logger.LogInformation("Sales order {OrderNumber} confirmed", salesOrder.OrderNumber);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SalesOrderMappers.ToDto(salesOrder);
    }
}
