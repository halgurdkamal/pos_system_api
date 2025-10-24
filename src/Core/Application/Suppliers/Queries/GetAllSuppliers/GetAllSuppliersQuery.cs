using MediatR;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Suppliers.DTOs;
using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.Core.Application.Suppliers.Queries.GetAllSuppliers;

/// <summary>
/// Query to get all suppliers with optional filters
/// </summary>
public record GetAllSuppliersQuery(
    int Page = 1,
    int Limit = 20,
    bool? IsActive = null,
    SupplierType? SupplierType = null
) : IRequest<PagedResult<SupplierDto>>;
