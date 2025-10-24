using MediatR;
using pos_system_api.Core.Application.Drugs.DTOs;

namespace pos_system_api.Core.Application.Drugs.Queries.GetDrug;

/// <summary>
/// Query to get a single drug by ID
/// </summary>
public record GetDrugQuery(string DrugId) : IRequest<DrugDto?>;
