using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetExpiringBatches;

/// <summary>
/// Query to get inventory items with expiring batches
/// </summary>
public record GetExpiringBatchesQuery(
    string ShopId,
    int Days = 30
) : IRequest<IEnumerable<InventoryDto>>;
