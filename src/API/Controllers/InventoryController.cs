using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.Commands.AddStock;
using pos_system_api.Core.Application.Inventory.Commands.ReduceStock;
using pos_system_api.Core.Application.Inventory.Commands.UpdatePricing;
using pos_system_api.Core.Application.Inventory.Commands.UpdateReorderPoint;
using pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPricing;
using pos_system_api.Core.Application.Inventory.Commands.PackagingOverrides;
using pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor;
using pos_system_api.Core.Application.Inventory.Commands.MoveToStorage;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;
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
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IStockAdjustmentRepository _stockAdjustmentRepository;
    private readonly IShopPackagingOverrideRepository _packagingOverrideRepository;
    private readonly IEffectivePackagingService _effectivePackagingService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository,
        ISupplierRepository supplierRepository,
        IStockAdjustmentRepository stockAdjustmentRepository,
        IShopPackagingOverrideRepository packagingOverrideRepository,
        IEffectivePackagingService effectivePackagingService,
        ICategoryRepository categoryRepository,
        ILoggerFactory loggerFactory,
        ILogger<InventoryController> logger)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
        _supplierRepository = supplierRepository;
        _stockAdjustmentRepository = stockAdjustmentRepository;
        _packagingOverrideRepository = packagingOverrideRepository;
        _effectivePackagingService = effectivePackagingService;
        _categoryRepository = categoryRepository;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get merged packaging configuration for a drug in a specific shop
    /// </summary>
    [HttpGet("shops/{shopId}/drugs/{drugId}/packaging")]
    [ProducesResponseType(typeof(EffectivePackagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EffectivePackagingDto>> GetPackaging(string shopId, string drugId)
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var result = await _effectivePackagingService.GetEffectivePackagingAsync(shopId, drugId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a packaging override (global override or custom level) for a shop
    /// </summary>
    [HttpPost("shops/{shopId}/drugs/{drugId}/packaging-overrides")]
    [ProducesResponseType(typeof(EffectivePackagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EffectivePackagingDto>> CreatePackagingOverride(
        string shopId,
        string drugId,
        [FromBody] PackagingOverrideInputDto dto)
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreatePackagingOverrideHandler();
            var command = new CreatePackagingOverrideCommand(shopId, drugId, dto);
            var result = await handler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequestWithDetails(ex);
        }
    }

    /// <summary>
    /// Update packaging override or global linked level for a shop
    /// </summary>
    [HttpPut("shops/{shopId}/drugs/{drugId}/packaging-levels/{levelId}")]
    [ProducesResponseType(typeof(EffectivePackagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EffectivePackagingDto>> UpdatePackagingLevel(
        string shopId,
        string drugId,
        string levelId,
        [FromBody] PackagingOverrideInputDto dto)
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateUpdatePackagingLevelHandler();
            var command = new UpdatePackagingLevelCommand(shopId, drugId, levelId, dto);
            var result = await handler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequestWithDetails(ex);
        }
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
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateAddStockHandler();
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
            var result = await handler.Handle(command, cancellationToken);
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
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateReduceStockHandler();
            var command = new ReduceStockCommand(shopId, drugId, dto.Quantity);
            var result = await handler.Handle(command, cancellationToken);
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
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateUpdatePricingHandler();
            var command = new UpdatePricingCommand(
                shopId,
                drugId,

                dto.CostPrice,

                dto.SellingPrice,

                dto.TaxRate
            );
            var result = await handler.Handle(command, cancellationToken);
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
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateUpdateReorderPointHandler();
            var command = new UpdateReorderPointCommand(shopId, drugId, dto.ReorderPoint);
            var result = await handler.Handle(command, cancellationToken);
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
    /// Update packaging-level pricing for an inventory item
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="packagingPrices">Dictionary of packaging level names to prices</param>
    /// <returns>Updated inventory details</returns>
    [HttpPut("shops/{shopId}/drugs/{drugId}/packaging-pricing")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryDto>> UpdatePackagingPricing(
        string shopId,
        string drugId,
        [FromBody] Dictionary<string, decimal> packagingPrices)
    {
        try
        {
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateUpdatePackagingPricingHandler();
            var command = new UpdatePackagingPricingCommand(shopId, drugId, packagingPrices);
            var result = await handler.Handle(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundWithDetails(ex);
        }
    }

    /// <summary>
    /// Get packaging-level pricing for an inventory item
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <returns>Shop pricing details including packaging level prices</returns>
    [HttpGet("shops/{shopId}/drugs/{drugId}/packaging-pricing")]
    [ProducesResponseType(typeof(ShopPricingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopPricingDto>> GetPackagingPricing(string shopId, string drugId)
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);

        if (inventory == null)
        {
            return NotFound(new { error = $"Inventory not found for Shop '{shopId}' and Drug '{drugId}'." });
        }

        var pricing = inventory.ShopPricing;
        var finalPrice = pricing.GetFinalPrice();

        var dto = new ShopPricingDto
        {
            CostPrice = pricing.CostPrice,
            SellingPrice = pricing.SellingPrice,
            Discount = pricing.Discount,
            Currency = pricing.Currency,
            TaxRate = pricing.TaxRate,
            ProfitMargin = finalPrice - pricing.CostPrice,
            ProfitMarginPercentage = pricing.GetProfitMargin(),
            LastPriceUpdate = pricing.LastPriceUpdate,
            PackagingLevelPrices = new Dictionary<string, decimal>(pricing.PackagingLevelPrices)
        };

        return Ok(dto);
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
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var handler = CreateGetShopInventoryQueryHandler();
        var query = new GetShopInventoryQuery(shopId, page, limit, isAvailable);
        var result = await handler.Handle(query, cancellationToken);
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
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var handler = CreateGetLowStockQueryHandler();
        var query = new GetLowStockQuery(shopId);
        var result = await handler.Handle(query, cancellationToken);
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
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var handler = CreateGetExpiringBatchesQueryHandler();
        var query = new GetExpiringBatchesQuery(shopId, days);
        var result = await handler.Handle(query, cancellationToken);
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
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var handler = CreateGetTotalStockValueQueryHandler();
        var query = new GetTotalStockValueQuery(shopId);
        var result = await handler.Handle(query, cancellationToken);
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
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateMoveToShopFloorHandler();
            var command = new pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor.MoveToShopFloorCommand(
                shopId,
                drugId,
                dto.Quantity,
                dto.BatchNumber
            );
            var result = await handler.Handle(command, cancellationToken);
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
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
            var handler = CreateMoveToStorageHandler();
            var command = new pos_system_api.Core.Application.Inventory.Commands.MoveToStorage.MoveToStorageCommand(
                shopId,
                drugId,
                dto.Quantity,
                dto.BatchNumber
            );
            var result = await handler.Handle(command, cancellationToken);
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
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var handler = CreateGetCashierItemsQueryHandler();
        var query = new GetCashierItemsQuery(shopId, searchTerm, category, page, limit);
        var result = await handler.Handle(query, cancellationToken);
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
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var handler = CreateGetCashierItemByBarcodeQueryHandler();
        var query = new GetCashierItemByBarcodeQuery(shopId, barcode);
        var result = await handler.Handle(query, cancellationToken);


        if (result == null)
            return NotFound(new { error = $"Item with barcode '{barcode}' not found or out of stock in this shop" });


        return Ok(result);
    }

    private AddStockCommandHandler CreateAddStockHandler() =>
        new(_inventoryRepository, _drugRepository, _supplierRepository);

    private ReduceStockCommandHandler CreateReduceStockHandler() =>
        new(_inventoryRepository, _stockAdjustmentRepository, _loggerFactory.CreateLogger<ReduceStockCommandHandler>());

    private UpdatePricingCommandHandler CreateUpdatePricingHandler() =>
        new(_inventoryRepository);

    private UpdateReorderPointCommandHandler CreateUpdateReorderPointHandler() =>
        new(_inventoryRepository);

    private UpdatePackagingPricingCommandHandler CreateUpdatePackagingPricingHandler() =>
        new(_inventoryRepository, _loggerFactory.CreateLogger<UpdatePackagingPricingCommandHandler>());

    private CreatePackagingOverrideCommandHandler CreatePackagingOverrideHandler() =>
        new(
            _packagingOverrideRepository,
            _inventoryRepository,
            _drugRepository,
            _effectivePackagingService,
            _loggerFactory.CreateLogger<CreatePackagingOverrideCommandHandler>());

    private UpdatePackagingLevelCommandHandler CreateUpdatePackagingLevelHandler() =>
        new(
            _packagingOverrideRepository,
            _inventoryRepository,
            _drugRepository,
            _effectivePackagingService,
            _loggerFactory.CreateLogger<UpdatePackagingLevelCommandHandler>());

    private GetShopInventoryQueryHandler CreateGetShopInventoryQueryHandler() =>
        new(_inventoryRepository, _effectivePackagingService);

    private GetLowStockQueryHandler CreateGetLowStockQueryHandler() =>
        new(_inventoryRepository);

    private GetExpiringBatchesQueryHandler CreateGetExpiringBatchesQueryHandler() =>
        new(_inventoryRepository);

    private GetTotalStockValueQueryHandler CreateGetTotalStockValueQueryHandler() =>
        new(_inventoryRepository);

    private MoveToShopFloorCommandHandler CreateMoveToShopFloorHandler() =>
        new(_inventoryRepository, _loggerFactory.CreateLogger<MoveToShopFloorCommandHandler>());

    private MoveToStorageCommandHandler CreateMoveToStorageHandler() =>
        new(_inventoryRepository, _loggerFactory.CreateLogger<MoveToStorageCommandHandler>());

    private GetCashierItemsQueryHandler CreateGetCashierItemsQueryHandler() =>
        new(_inventoryRepository, _drugRepository, _categoryRepository);

    private GetCashierItemByBarcodeQueryHandler CreateGetCashierItemByBarcodeQueryHandler() =>
        new(_inventoryRepository, _drugRepository, _categoryRepository);
}
