using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.Commands.AddStock;
using pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor;
using pos_system_api.Core.Application.Inventory.Commands.MoveToStorage;
using pos_system_api.Core.Application.Inventory.Commands.PackagingOverrides;
using pos_system_api.Core.Application.Inventory.Commands.ReduceStock;
using pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPricing;
using pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPrices;
using pos_system_api.Core.Application.Inventory.Commands.UpdatePricing;
using pos_system_api.Core.Application.Inventory.Commands.UpdateReorderPoint;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Queries.GetCashierItemByBarcode;
using pos_system_api.Core.Application.Inventory.Queries.GetCashierItems;
using pos_system_api.Core.Application.Inventory.Queries.GetEffectivePackaging;
using pos_system_api.Core.Application.Inventory.Queries.GetExpiringBatches;
using pos_system_api.Core.Application.Inventory.Queries.GetLowStock;
using pos_system_api.Core.Application.Inventory.Queries.GetPackagingPricing;
using pos_system_api.Core.Application.Inventory.Queries.GetShopInventory;
using pos_system_api.Core.Application.Inventory.Queries.GetTotalStockValue;
using pos_system_api.Core.Application.Inventory.Services;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for shop inventory operations (Multi-tenant inventory management).
/// </summary>
[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
[Authorize(Policy = "ShopAccess")]
public class InventoryController : BaseApiController
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get merged packaging configuration for a drug in a specific shop.</summary>
    [HttpGet("shops/{shopId}/drugs/{drugId}/packaging")]
    [ProducesResponseType(typeof(EffectivePackagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EffectivePackagingDto>> GetPackaging(
        string shopId, string drugId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEffectivePackagingQuery(shopId, drugId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a packaging override (global override or custom level) for a shop.</summary>
    [HttpPost("shops/{shopId}/drugs/{drugId}/packaging-overrides")]
    [ProducesResponseType(typeof(EffectivePackagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EffectivePackagingDto>> CreatePackagingOverride(
        string shopId,
        string drugId,
        [FromBody] PackagingOverrideInputDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreatePackagingOverrideCommand(shopId, drugId, dto), cancellationToken);
        return Ok(result);
    }

    /// <summary>Update packaging override or global linked level for a shop.</summary>
    [HttpPut("shops/{shopId}/drugs/{drugId}/packaging-levels/{levelId}")]
    [ProducesResponseType(typeof(EffectivePackagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EffectivePackagingDto>> UpdatePackagingLevel(
        string shopId,
        string drugId,
        string levelId,
        [FromBody] PackagingOverrideInputDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdatePackagingLevelCommand(shopId, drugId, levelId, dto), cancellationToken);
        return Ok(result);
    }

    /// <summary>Add stock to shop inventory (creates a new batch).</summary>
    [HttpPost("shops/{shopId}/stock")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryDto>> AddStock(
        string shopId,
        [FromBody] AddStockDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddStockCommand(
            shopId,
            dto.DrugId,
            dto.BatchNumber,
            dto.SupplierId,
            dto.Quantity,
            dto.ExpiryDate,
            dto.PurchasePrice,
            dto.SellingPrice,
            dto.StorageLocation), cancellationToken);

        return CreatedAtAction(nameof(GetShopInventory), new { shopId = result.ShopId }, result);
    }

    /// <summary>Reduce stock from shop inventory (FIFO - First In First Out).</summary>
    [HttpPut("shops/{shopId}/drugs/{drugId}/reduce")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> ReduceStock(
        string shopId,
        string drugId,
        [FromBody] ReduceStockDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReduceStockCommand(shopId, drugId, dto.Quantity), cancellationToken);
        return Ok(result);
    }

    /// <summary>Update shop-specific pricing for an inventory item.</summary>
    [HttpPut("shops/{shopId}/drugs/{drugId}/pricing")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> UpdatePricing(
        string shopId,
        string drugId,
        [FromBody] UpdatePricingDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdatePricingCommand(shopId, drugId, dto.CostPrice, dto.SellingPrice, dto.TaxRate),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Update reorder point for an inventory item.</summary>
    [HttpPut("shops/{shopId}/drugs/{drugId}/reorder-point")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryDto>> UpdateReorderPoint(
        string shopId,
        string drugId,
        [FromBody] UpdateReorderPointDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateReorderPointCommand(shopId, drugId, dto.ReorderPoint), cancellationToken);
        return Ok(result);
    }

    /// <summary>Update packaging-level pricing for an inventory item.</summary>
    [HttpPut("shops/{shopId}/drugs/{drugId}/packaging-pricing")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> UpdatePackagingPricing(
        string shopId,
        string drugId,
        [FromBody] Dictionary<string, decimal> packagingPrices,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdatePackagingPricingCommand(shopId, drugId, packagingPrices), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get packaging-level pricing for an inventory item.</summary>
    [HttpGet("shops/{shopId}/drugs/{drugId}/packaging-pricing")]
    [ProducesResponseType(typeof(ShopPricingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopPricingDto>> GetPackagingPricing(
        string shopId, string drugId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPackagingPricingQuery(shopId, drugId), cancellationToken);
        return result == null
            ? NotFound(new { error = $"Inventory not found for Shop '{shopId}' and Drug '{drugId}'." })
            : Ok(result);
    }

    /// <summary>
    /// Update packaging level prices from the active batch. Auto-calculates
    /// prices for null/zero packaging levels; preserves shop-defined custom prices.
    /// </summary>
    [HttpPost("shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch")]
    [ProducesResponseType(typeof(PackagingPricingUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackagingPricingUpdateResult>> UpdatePackagingPricesFromBatch(
        string shopId, string drugId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdatePackagingPricesFromBatchCommand(shopId, drugId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get paginated inventory for a shop.</summary>
    [HttpGet("shops/{shopId}")]
    [ProducesResponseType(typeof(PagedResult<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InventoryDto>>> GetShopInventory(
        string shopId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] bool? isAvailable = null)
    {
        var result = await _mediator.Send(
            new GetShopInventoryQuery(shopId, page, limit, isAvailable), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get low stock items for a shop (below reorder point).</summary>
    [HttpGet("shops/{shopId}/low-stock")]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetLowStock(
        string shopId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLowStockQuery(shopId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get inventory items with batches expiring within the given window.</summary>
    [HttpGet("shops/{shopId}/expiring")]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetExpiringBatches(
        string shopId,
        CancellationToken cancellationToken,
        [FromQuery] int days = 30)
    {
        var result = await _mediator.Send(
            new GetExpiringBatchesQuery(shopId, days), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get total stock value for a shop, denominated in the shop's currency.</summary>
    [HttpGet("shops/{shopId}/value")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTotalStockValue(
        string shopId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTotalStockValueQuery(shopId), cancellationToken);
        return Ok(new { shopId, totalValue = result.TotalValue, currency = result.Currency });
    }

    /// <summary>Move stock from storage to shop floor (for display/customer access).</summary>
    [HttpPost("shops/{shopId}/drugs/{drugId}/move-to-floor")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> MoveToShopFloor(
        string shopId,
        string drugId,
        [FromBody] MoveStockDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MoveToShopFloorCommand(shopId, drugId, dto.Quantity, dto.BatchNumber),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Move stock from shop floor back to storage.</summary>
    [HttpPost("shops/{shopId}/drugs/{drugId}/move-to-storage")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> MoveToStorage(
        string shopId,
        string drugId,
        [FromBody] MoveStockDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MoveToStorageCommand(shopId, drugId, dto.Quantity, dto.BatchNumber),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get available items for cashier POS — drug info, images, stock, and pricing.
    /// </summary>
    [HttpGet("shops/{shopId}/pos-items")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(PagedResult<ShopPosItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShopPosItemDto>>> GetPosItems(
        string shopId,
        CancellationToken cancellationToken,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        var result = await _mediator.Send(
            new GetCashierItemsQuery(shopId, searchTerm, category, page, limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get single item for cashier by barcode scan.</summary>
    [HttpGet("shops/{shopId}/pos-items/by-barcode/{barcode}")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(CashierItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashierItemDto>> GetPosItemByBarcode(
        string shopId, string barcode, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCashierItemByBarcodeQuery(shopId, barcode), cancellationToken);
        return result == null
            ? NotFound(new { error = $"Item with barcode '{barcode}' not found or out of stock in this shop" })
            : Ok(result);
    }
}
