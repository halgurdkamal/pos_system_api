using MediatR;
using pos_system_api.Core.Application.Common.DTOs;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Commands.UpdateSupplier;

/// <summary>
/// Command to update an existing supplier
/// </summary>
public record UpdateSupplierCommand(
    string Id,
    string SupplierName,
    string ContactNumber,
    string Email,
    AddressDto Address,
    string PaymentTerms,
    int DeliveryLeadTime,
    decimal MinimumOrderValue,
    string? Website,
    string? TaxId,
    string? LicenseNumber
) : IRequest<SupplierDto>;
