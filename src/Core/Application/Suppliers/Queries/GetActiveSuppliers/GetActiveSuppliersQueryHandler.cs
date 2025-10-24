using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Queries.GetActiveSuppliers;

/// <summary>
/// Handler for GetActiveSuppliersQuery
/// </summary>
public class GetActiveSuppliersQueryHandler : IRequestHandler<GetActiveSuppliersQuery, IEnumerable<SupplierDto>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetActiveSuppliersQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IEnumerable<SupplierDto>> Handle(GetActiveSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await _supplierRepository.GetActiveAsync(cancellationToken);

        return suppliers.Select(Commands.SupplierMapper.MapToDto).ToList();
    }
}
