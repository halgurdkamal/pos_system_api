using pos_system_api.Core.Application.Inventory.DTOs;
using MediatR;

namespace pos_system_api.Core.Application.Inventory.Commands.CreateStockAdjustment;

/// <summary>
/// Command to record a stock adjustment for audit trail
/// </summary>
public record CreateStockAdjustmentCommand(
    string ShopId,
    string DrugId,
    string? BatchNumber,
    string AdjustmentType,  // "Sale", "Return", "Damage", etc.
    int QuantityChanged,    // Positive = increase, Negative = decrease
    string Reason,
    string AdjustedBy,      // User ID
    string? Notes = null,
    string? ReferenceId = null,
    string? ReferenceType = null
) : IRequest<StockAdjustmentDto>;
