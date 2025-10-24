using MediatR;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Commands.CreateSupplier;

/// <summary>
/// Command to create a new supplier
/// </summary>
public record CreateSupplierCommand(
    string SupplierName,
    string SupplierType,
    string ContactNumber,
    string Email,
    CreateSupplierDto SupplierData
) : IRequest<SupplierDto>;
