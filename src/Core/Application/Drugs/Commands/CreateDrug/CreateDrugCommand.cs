using MediatR;
using pos_system_api.Core.Application.Drugs.DTOs;

namespace pos_system_api.Core.Application.Drugs.Commands.CreateDrug;

/// <summary>
/// Command to create a new drug in the catalog.
/// </summary>
public record CreateDrugCommand(CreateDrugDto Payload) : IRequest<DrugDto>;
