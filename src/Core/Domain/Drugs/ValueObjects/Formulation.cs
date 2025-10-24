namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents the formulation details of a drug
/// </summary>
public class Formulation
{
    public string Form { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string RouteOfAdministration { get; set; } = string.Empty;
}
