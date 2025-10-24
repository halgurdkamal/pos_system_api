using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Suppliers.DTOs;
using pos_system_api.Core.Domain.Suppliers.Entities;
using pos_system_api.Core.Domain.Common.ValueObjects;

namespace pos_system_api.Core.Application.Suppliers.Commands.CreateSupplier;

/// <summary>
/// Handler for CreateSupplierCommand
/// </summary>
public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;

    public CreateSupplierCommandHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        // Validate email uniqueness
        var emailExists = await _supplierRepository.EmailExistsAsync(request.Email, null, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException($"A supplier with email '{request.Email}' already exists.");
        }

        // Parse supplier type
        if (!Enum.TryParse<SupplierType>(request.SupplierType, true, out var supplierType))
        {
            throw new ArgumentException($"Invalid supplier type: {request.SupplierType}");
        }

        // Create address value object
        var address = new Address
        {
            Street = request.SupplierData.Address.Street,
            City = request.SupplierData.Address.City,
            State = request.SupplierData.Address.State,
            ZipCode = request.SupplierData.Address.ZipCode,
            Country = request.SupplierData.Address.Country
        };

        // Create supplier entity
        var supplier = new Supplier(
            supplierName: request.SupplierName,
            supplierType: supplierType,
            contactNumber: request.ContactNumber,
            email: request.Email,
            address: address
        )
        {
            PaymentTerms = request.SupplierData.PaymentTerms,
            DeliveryLeadTime = request.SupplierData.DeliveryLeadTime,
            MinimumOrderValue = request.SupplierData.MinimumOrderValue,
            Website = request.SupplierData.Website,
            TaxId = request.SupplierData.TaxId,
            LicenseNumber = request.SupplierData.LicenseNumber,
            IsActive = true
        };

        // Save to repository
        var createdSupplier = await _supplierRepository.AddAsync(supplier, cancellationToken);

        // Map to DTO
        return SupplierMapper.MapToDto(createdSupplier);
    }
}
