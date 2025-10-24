using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Queries.GetSupplierById;

/// <summary>
/// Handler for GetSupplierByIdQuery
/// </summary>
public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto?>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierByIdQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDto?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (supplier == null)
        {
            return null;
        }

        return Commands.SupplierMapper.MapToDto(supplier);
    }
}
