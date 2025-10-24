using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Suppliers.DTOs;
using pos_system_api.Core.Domain.Common.ValueObjects;

namespace pos_system_api.Core.Application.Suppliers.Commands.UpdateSupplier;

/// <summary>
/// Handler for UpdateSupplierCommand
/// </summary>
public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;

    public UpdateSupplierCommandHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDto> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        // Get existing supplier
        var supplier = await _supplierRepository.GetByIdAsync(request.Id, cancellationToken);
        if (supplier == null)
        {
            throw new InvalidOperationException($"Supplier with ID '{request.Id}' not found.");
        }

        // Validate email uniqueness (excluding current supplier)
        var emailExists = await _supplierRepository.EmailExistsAsync(request.Email, request.Id, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException($"A supplier with email '{request.Email}' already exists.");
        }

        // Update basic info using domain method
        supplier.UpdateContactInfo(request.ContactNumber, request.Email);

        // Update other properties
        supplier.SupplierName = request.SupplierName;
        supplier.PaymentTerms = request.PaymentTerms;
        supplier.DeliveryLeadTime = request.DeliveryLeadTime;
        supplier.MinimumOrderValue = request.MinimumOrderValue;
        supplier.Website = request.Website;
        supplier.TaxId = request.TaxId;
        supplier.LicenseNumber = request.LicenseNumber;

        // Update address
        supplier.Address = new Address
        {
            Street = request.Address.Street,
            City = request.Address.City,
            State = request.Address.State,
            ZipCode = request.Address.ZipCode,
            Country = request.Address.Country
        };

        // Save changes
        var updatedSupplier = await _supplierRepository.UpdateAsync(supplier, cancellationToken);

        // Map to DTO
        return SupplierMapper.MapToDto(updatedSupplier);
    }
}
