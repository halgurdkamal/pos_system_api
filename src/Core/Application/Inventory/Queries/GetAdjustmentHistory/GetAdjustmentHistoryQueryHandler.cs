using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Queries.GetAdjustmentHistory;

public class GetAdjustmentHistoryQueryHandler : IRequestHandler<GetAdjustmentHistoryQuery, IEnumerable<StockAdjustmentDto>>
{
    private readonly IStockAdjustmentRepository _repository;
    private readonly ILogger<GetAdjustmentHistoryQueryHandler> _logger;

    public GetAdjustmentHistoryQueryHandler(
        IStockAdjustmentRepository repository,
        ILogger<GetAdjustmentHistoryQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockAdjustmentDto>> Handle(GetAdjustmentHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting adjustment history for shop {ShopId}", request.ShopId);

        IEnumerable<StockAdjustment> adjustments;

        // Parse adjustment type if provided
        AdjustmentType? adjustmentType = null;
        if (!string.IsNullOrEmpty(request.AdjustmentType))
        {
            if (Enum.TryParse<AdjustmentType>(request.AdjustmentType, true, out var parsedType))
            {
                adjustmentType = parsedType;
            }
        }

        // Get adjustments by drug or by shop
        if (!string.IsNullOrEmpty(request.DrugId))
        {
            adjustments = await _repository.GetByDrugAsync(
                request.ShopId,
                request.DrugId,
                request.StartDate,
                request.EndDate,
                cancellationToken);
        }
        else
        {
            adjustments = await _repository.GetByShopAsync(
                request.ShopId,
                request.StartDate,
                request.EndDate,
                adjustmentType,
                request.Limit,
                cancellationToken);
        }

        // Map to DTOs
        return adjustments.Select(a => new StockAdjustmentDto
        {
            Id = a.Id,
            ShopId = a.ShopId,
            DrugId = a.DrugId,
            BatchNumber = a.BatchNumber,
            AdjustmentType = a.AdjustmentType.ToString(),
            QuantityChanged = a.QuantityChanged,
            QuantityBefore = a.QuantityBefore,
            QuantityAfter = a.QuantityAfter,
            Reason = a.Reason,
            Notes = a.Notes,
            AdjustedBy = a.AdjustedBy,
            AdjustedAt = a.AdjustedAt,
            ReferenceId = a.ReferenceId,
            ReferenceType = a.ReferenceType
        }).ToList();
    }
}
