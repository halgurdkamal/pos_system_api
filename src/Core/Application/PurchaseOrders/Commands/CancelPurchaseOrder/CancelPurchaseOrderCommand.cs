using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;

namespace pos_system_api.Core.Application.PurchaseOrders.Commands.CancelPurchaseOrder;

public record CancelPurchaseOrderCommand(string OrderId, string CancelledBy, string Reason)
    : IRequest<PurchaseOrderDto>;

public class CancelPurchaseOrderCommandHandler
    : IRequestHandler<CancelPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelPurchaseOrderCommandHandler> _logger;

    public CancelPurchaseOrderCommandHandler(
        IPurchaseOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CancelPurchaseOrderCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PurchaseOrderDto> Handle(
        CancelPurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var purchaseOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (purchaseOrder == null)
        {
            throw new KeyNotFoundException($"Purchase order {request.OrderId} not found");
        }

        purchaseOrder.Cancel(request.CancelledBy, request.Reason);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);

        _logger.LogInformation(
            "Purchase order {OrderNumber} cancelled by {CancelledBy}: {Reason}",
            purchaseOrder.OrderNumber, request.CancelledBy, request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PurchaseOrderMappers.ToDto(purchaseOrder);
    }
}
