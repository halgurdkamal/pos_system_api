using MediatR;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Queries.GetActiveSuppliers;

/// <summary>
/// Query to get active suppliers (for dropdown lists)
/// </summary>
public record GetActiveSuppliersQuery() : IRequest<IEnumerable<SupplierDto>>;
