namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents inventory information for a drug
/// </summary>
public class Inventory
{
    public int TotalStock { get; set; }
    public int ReorderPoint { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public List<Batch> Batches { get; set; } = new();
}
