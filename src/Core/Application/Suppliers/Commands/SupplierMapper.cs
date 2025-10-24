using pos_system_api.Core.Application.Suppliers.DTOs;
using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.Core.Application.Suppliers.Commands;

/// <summary>
/// Shared mapper for Supplier entity to SupplierDto
/// </summary>
public static class SupplierMapper
{
    public static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            SupplierName = supplier.SupplierName,
            SupplierType = supplier.SupplierType.ToString(),
            ContactNumber = supplier.ContactNumber,
            Email = supplier.Email,
            Address = new Common.DTOs.AddressDto
            {
                Street = supplier.Address.Street,
                City = supplier.Address.City,
                State = supplier.Address.State,
                ZipCode = supplier.Address.ZipCode,
                Country = supplier.Address.Country
            },
            PaymentTerms = supplier.PaymentTerms,
            DeliveryLeadTime = supplier.DeliveryLeadTime,
            MinimumOrderValue = supplier.MinimumOrderValue,
            IsActive = supplier.IsActive,
            Website = supplier.Website,
            TaxId = supplier.TaxId,
            LicenseNumber = supplier.LicenseNumber,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.LastUpdated ?? supplier.CreatedAt
        };
    }
}
