using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;

namespace pos_system_api.Core.Application.PurchaseOrders.Commands.ConfirmPurchaseOrder;

public record ConfirmPurchaseOrderCommand(string OrderId, string ConfirmedBy)
    : IRequest<PurchaseOrderDto>;

public class ConfirmPurchaseOrderCommandHandler
    : IRequestHandler<ConfirmPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmPurchaseOrderCommandHandler> _logger;

    public ConfirmPurchaseOrderCommandHandler(
        IPurchaseOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmPurchaseOrderCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PurchaseOrderDto> Handle(
        ConfirmPurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var purchaseOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (purchaseOrder == null)
        {
            throw new KeyNotFoundException($"Purchase order {request.OrderId} not found");
        }

        purchaseOrder.Confirm(request.ConfirmedBy);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);

        _logger.LogInformation(
            "Purchase order {OrderNumber} confirmed by {ConfirmedBy}",
            purchaseOrder.OrderNumber, request.ConfirmedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PurchaseOrderMappers.ToDto(purchaseOrder);
    }
}
