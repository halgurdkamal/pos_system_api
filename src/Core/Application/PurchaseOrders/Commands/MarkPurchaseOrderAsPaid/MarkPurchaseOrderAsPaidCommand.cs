using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;

namespace pos_system_api.Core.Application.PurchaseOrders.Commands.MarkPurchaseOrderAsPaid;

public record MarkPurchaseOrderAsPaidCommand(string OrderId, DateTime? PaidAt = null)
    : IRequest<PurchaseOrderDto>;

public class MarkPurchaseOrderAsPaidCommandHandler
    : IRequestHandler<MarkPurchaseOrderAsPaidCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly ILogger<MarkPurchaseOrderAsPaidCommandHandler> _logger;

    public MarkPurchaseOrderAsPaidCommandHandler(
        IPurchaseOrderRepository repository,
        ILogger<MarkPurchaseOrderAsPaidCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PurchaseOrderDto> Handle(
        MarkPurchaseOrderAsPaidCommand request,
        CancellationToken cancellationToken)
    {
        var purchaseOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (purchaseOrder == null)
        {
            throw new KeyNotFoundException($"Purchase order {request.OrderId} not found");
        }

        purchaseOrder.MarkAsPaid(request.PaidAt);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);

        _logger.LogInformation(
            "Purchase order {OrderNumber} marked as paid",
            purchaseOrder.OrderNumber);

        return PurchaseOrderMappers.ToDto(purchaseOrder);
    }
}
