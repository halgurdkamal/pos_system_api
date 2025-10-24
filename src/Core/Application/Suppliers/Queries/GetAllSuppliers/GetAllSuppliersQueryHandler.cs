using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace pos_system_api.Core.Application.Suppliers.Queries.GetAllSuppliers;

/// <summary>
/// Handler for GetAllSuppliersQuery
/// </summary>
public class GetAllSuppliersQueryHandler : IRequestHandler<GetAllSuppliersQuery, PagedResult<SupplierDto>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetAllSuppliersQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<PagedResult<SupplierDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
    {
        var (suppliers, totalCount) = await _supplierRepository.GetAllAsync(
            request.Page,
            request.Limit,
            request.IsActive,
            request.SupplierType,
            cancellationToken
        );

        var supplierDtos = suppliers.Select(Commands.SupplierMapper.MapToDto).ToList();

        return new PagedResult<SupplierDto>(supplierDtos, request.Page, request.Limit, totalCount);
    }
}
