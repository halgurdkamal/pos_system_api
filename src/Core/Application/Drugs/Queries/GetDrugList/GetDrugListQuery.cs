using MediatR;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.DTOs;

namespace pos_system_api.Core.Application.Drugs.Queries.GetDrugList;

/// <summary>
/// Query to get a paginated list of drugs
/// </summary>
public record GetDrugListQuery(int Page = 1, int Limit = 20) : IRequest<PagedResult<DrugDto>>;
