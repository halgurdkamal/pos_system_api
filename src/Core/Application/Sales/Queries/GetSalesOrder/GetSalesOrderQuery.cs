using MediatR;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Sales.Queries.GetSalesOrder;

public record GetSalesOrderQuery(string OrderId) : IRequest<SalesOrderDto?>;

public class GetSalesOrderQueryHandler : IRequestHandler<GetSalesOrderQuery, SalesOrderDto?>
{
    private readonly ISalesOrderRepository _repository;

    public GetSalesOrderQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<SalesOrderDto?> Handle(GetSalesOrderQuery request, CancellationToken cancellationToken)
    {
        var salesOrder = await _repository.GetByIdAsync(request.OrderId, cancellationToken);
        return salesOrder == null ? null : MapToDto(salesOrder);
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
