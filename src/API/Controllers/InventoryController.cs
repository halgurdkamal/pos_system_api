using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.Commands.AddStock;
using pos_system_api.Core.Application.Inventory.Commands.ReduceStock;
using pos_system_api.Core.Application.Inventory.Commands.UpdatePricing;
using pos_system_api.Core.Application.Inventory.Commands.UpdateReorderPoint;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Queries.GetExpiringBatches;
using pos_system_api.Core.Application.Inventory.Queries.GetLowStock;
using pos_system_api.Core.Application.Inventory.Queries.GetShopInventory;
using pos_system_api.Core.Application.Inventory.Queries.GetTotalStockValue;
using pos_system_api.Core.Application.Inventory.Queries.GetCashierItems;
using pos_system_api.Core.Application.Inventory.Queries.GetCashierItemByBarcode;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for shop inventory operations (Multi-tenant inventory management)
/// </summary>
[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
// [Authorize(Policy = "ShopAccess")] // All inventory endpoints require shop-specific access
public class InventoryController : BaseApiController
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Add stock to shop inventory (creates a new batch)
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="dto">Stock details including batch information</param>
    /// <returns>Updated inventory details</returns>
    [HttpPost("shops/{shopId}/stock")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryDto>> AddStock(string shopId, [FromBody] AddStockDto dto)
    {
        try
        {
            var command = new AddStockCommand(
                shopId,
                dto.DrugId,
                dto.BatchNumber,
                dto.SupplierId,
                dto.Quantity,
                dto.ExpiryDate,
                dto.PurchasePrice,
                dto.SellingPrice,
                dto.StorageLocation
            );
            var result = await _mediator.Send(command);
            return CreatedAtAction(
                nameof(GetShopInventory), 
                new { shopId = result.ShopId }, 
                result
            );
        }
        catch (Exception ex)
        {
            return BadRequestWithDetails(ex);
        }
    }

    /// <summary>
    /// Reduce stock from shop inventory (FIFO - First In First Out)
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="dto">Quantity to reduce</param>
    /// <returns>Updated inventory details</returns>
    [HttpPut("shops/{shopId}/drugs/{drugId}/reduce")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> ReduceStock(
        string shopId, 
        string drugId, 
        [FromBody] ReduceStockDto dto)
    {
        try
        {
            var command = new ReduceStockCommand(shopId, drugId, dto.Quantity);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFoundWithDetails(ex);
            return BadRequestWithDetails(ex);
        }
    }

    /// <summary>
    /// Update shop-specific pricing for an inventory item
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="dto">Updated pricing details</param>
    /// <returns>Updated inventory details</returns>
    [HttpPut("shops/{shopId}/drugs/{drugId}/pricing")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> UpdatePricing(
        string shopId, 
        string drugId, 
        [FromBody] UpdatePricingDto dto)
    {
        try
        {
            var command = new UpdatePricingCommand(
                shopId, 
                drugId, 
                dto.CostPrice, 
                dto.SellingPrice, 
                dto.TaxRate
            );
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundWithDetails(ex);
        }
    }

    /// <summary>
    /// Update reorder point for an inventory item
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="dto">Updated reorder point</param>
    /// <returns>Updated inventory details</returns>
    [HttpPut("shops/{shopId}/drugs/{drugId}/reorder-point")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryDto>> UpdateReorderPoint(
        string shopId, 
        string drugId, 
        [FromBody] UpdateReorderPointDto dto)
    {
        try
        {
            var command = new UpdateReorderPointCommand(shopId, drugId, dto.ReorderPoint);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundWithDetails(ex);
        }
        catch (ArgumentException ex)
        {
            return BadRequestWithDetails(ex);
        }
    }

    /// <summary>
    /// Get paginated inventory for a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <param name="isAvailable">Filter by availability (optional)</param>
    /// <returns>Paginated list of inventory items</returns>
    [HttpGet("shops/{shopId}")]
    [ProducesResponseType(typeof(PagedResult<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InventoryDto>>> GetShopInventory(
        string shopId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] bool? isAvailable = null)
    {
        var query = new GetShopInventoryQuery(shopId, page, limit, isAvailable);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get low stock items for a shop (below reorder point)
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <returns>List of low stock items</returns>
    [HttpGet("shops/{shopId}/low-stock")]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetLowStock(string shopId)
    {
        var query = new GetLowStockQuery(shopId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get inventory items with expiring batches
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="days">Number of days to check for expiration (default: 30)</param>
    /// <returns>List of items with expiring batches</returns>
    [HttpGet("shops/{shopId}/expiring")]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetExpiringBatches(
        string shopId,
        [FromQuery] int days = 30)
    {
        var query = new GetExpiringBatchesQuery(shopId, days);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get total stock value for a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <returns>Total stock value in the shop's currency</returns>
    [HttpGet("shops/{shopId}/value")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTotalStockValue(string shopId)
    {
        var query = new GetTotalStockValueQuery(shopId);
        var result = await _mediator.Send(query);
        return Ok(new { shopId, totalValue = result, currency = "USD" });
    }

    /// <summary>
    /// Move stock from storage to shop floor (for display/customer access)
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="dto">Move details (quantity and optional batch number)</param>
    /// <returns>Updated inventory with location breakdown</returns>
    [HttpPost("shops/{shopId}/drugs/{drugId}/move-to-floor")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> MoveToShopFloor(
        string shopId,
        string drugId,
        [FromBody] MoveStockDto dto)
    {
        try
        {
            var command = new pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor.MoveToShopFloorCommand(
                shopId,
                drugId,
                dto.Quantity,
                dto.BatchNumber
            );
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Move stock from shop floor back to storage
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="dto">Move details (quantity and optional batch number)</param>
    /// <returns>Updated inventory with location breakdown</returns>
    [HttpPost("shops/{shopId}/drugs/{drugId}/move-to-storage")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> MoveToStorage(
        string shopId,
        string drugId,
        [FromBody] MoveStockDto dto)
    {
        try
        {
            var command = new pos_system_api.Core.Application.Inventory.Commands.MoveToStorage.MoveToStorageCommand(
                shopId,
                drugId,
                dto.Quantity,
                dto.BatchNumber
            );
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available items for cashier POS - includes drug info, images, stock, and pricing
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="searchTerm">Search by name, barcode, or category (optional)</param>
    /// <param name="category">Filter by category (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 50)</param>
    /// <returns>Cashier-friendly list of available items with all necessary info</returns>
    [HttpGet("shops/{shopId}/pos-items")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(PagedResult<CashierItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CashierItemDto>>> GetPosItems(
        string shopId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        var query = new GetCashierItemsQuery(shopId, searchTerm, category, page, limit);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get single item for cashier by barcode scan
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="barcode">Drug barcode</param>
    /// <returns>Item details with stock and pricing</returns>
    [HttpGet("shops/{shopId}/pos-items/by-barcode/{barcode}")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(CashierItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashierItemDto>> GetPosItemByBarcode(string shopId, string barcode)
    {
        var query = new GetCashierItemByBarcodeQuery(shopId, barcode);
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound(new { error = $"Item with barcode '{barcode}' not found or out of stock in this shop" });
        
        return Ok(result);
    }
}
