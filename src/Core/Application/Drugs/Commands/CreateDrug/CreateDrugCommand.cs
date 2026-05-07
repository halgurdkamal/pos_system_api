using MediatR;
using pos_system_api.Core.Application.Drugs.DTOs;

namespace pos_system_api.Core.Application.Drugs.Commands.CreateDrug;

/// <summary>
/// Creates a new drug in the catalog. The optional <see cref="CreateDrugDto.DrugId"/>
/// is used as-is when supplied; otherwise a `DRG-XXXXXXXX` identifier is generated.
/// </summary>
public record CreateDrugCommand(CreateDrugDto Payload, string CreatedBy = "system")
    : IRequest<DrugDto>;
