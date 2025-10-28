namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Defines the type of unit used for measuring drugs
/// Determines how the drug is measured and sold
/// </summary>
public enum UnitType
{
    /// <summary>
    /// Count-based units (discrete items like tablets, capsules)
    /// Base unit: Piece, Tablet, Capsule, Suppository, Patch, Vial, Ampoule
    /// </summary>
    Count = 1,
    
    /// <summary>
    /// Volume-based units (liquids measured in milliliters)
    /// Base unit: Milliliter (ml)
    /// Used for: Syrups, Suspensions, Drops, Injections, IV fluids
    /// </summary>
    Volume = 2,
    
    /// <summary>
    /// Weight-based units (powders/ointments measured in grams)
    /// Base unit: Gram (g) or Milligram (mg)
    /// Used for: Creams, Ointments, Gels, Powders for reconstitution
    /// </summary>
    Weight = 3,
    
    /// <summary>
    /// Dose-based units (metered doses)
    /// Base unit: Dose, Puff, Spray
    /// Used for: Metered-dose inhalers, Nasal sprays
    /// Cannot be subdivided
    /// </summary>
    Dose = 4
}
