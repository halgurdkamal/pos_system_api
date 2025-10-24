using MediatR;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Queries.GetSupplierById;

/// <summary>
/// Query to get a supplier by ID
/// </summary>
public record GetSupplierByIdQuery(string Id) : IRequest<SupplierDto?>;
