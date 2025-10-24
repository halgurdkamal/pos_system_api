namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents regulatory information for a drug
/// </summary>
public class Regulatory
{
    public bool IsPrescriptionRequired { get; set; }
    public bool IsHighRisk { get; set; }
    public string DrugAuthorityNumber { get; set; } = string.Empty;
    public DateTime ApprovalDate { get; set; }
    public string ControlSchedule { get; set; } = string.Empty;
}
