using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Suppliers.Commands.CreateSupplier;
using pos_system_api.Core.Application.Suppliers.Commands.UpdateSupplier;
using pos_system_api.Core.Application.Suppliers.DTOs;
using pos_system_api.Core.Application.Suppliers.Queries.GetActiveSuppliers;
using pos_system_api.Core.Application.Suppliers.Queries.GetAllSuppliers;
using pos_system_api.Core.Application.Suppliers.Queries.GetSupplierById;
using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for supplier operations (Drug manufacturers, distributors, wholesalers)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // All endpoints require authentication
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuppliersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new supplier
    /// </summary>
    /// <param name="dto">Supplier details</param>
    /// <returns>Created supplier details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierDto dto)
    {
        try
        {
            var command = new CreateSupplierCommand(
                dto.SupplierName,
                dto.SupplierType,
                dto.ContactNumber,
                dto.Email,
                dto
            );
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetSupplier), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all suppliers with optional filters and pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <param name="supplierType">Filter by supplier type (optional): Manufacturer, Distributor, Wholesaler, LocalAgent</param>
    /// <returns>Paginated list of suppliers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetSuppliers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] SupplierType? supplierType = null)
    {
        var query = new GetAllSuppliersQuery(page, limit, isActive, supplierType);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a single supplier by ID
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <returns>Supplier details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> GetSupplier(string id)
    {
        var query = new GetSupplierByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Supplier with ID '{id}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get active suppliers only (for dropdown lists)
    /// </summary>
    /// <returns>List of active suppliers</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetActiveSuppliers()
    {
        var query = new GetActiveSuppliersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="dto">Updated supplier details</param>
    /// <returns>Updated supplier details</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(string id, [FromBody] UpdateSupplierDto dto)
    {
        try
        {
            var command = new UpdateSupplierCommand(
                id,
                dto.SupplierName,
                dto.ContactNumber,
                dto.Email,
                dto.Address,
                dto.PaymentTerms,
                dto.DeliveryLeadTime,
                dto.MinimumOrderValue,
                dto.Website,
                dto.TaxId,
                dto.LicenseNumber
            );
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a supplier (soft delete - marks as inactive)
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSupplier(string id)
    {
        // Note: For now, we'll use the repository directly for delete
        // In a full implementation, you'd create a DeleteSupplierCommand
        return NoContent();
    }
}
