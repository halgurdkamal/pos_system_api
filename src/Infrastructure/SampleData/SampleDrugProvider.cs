using System;
using System.Collections.Generic;
using System.Text.Json;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Infrastructure.SampleData;

/// <summary>
/// Provides sample drug data for development and testing purposes
/// </summary>
public static class SampleDrugProvider
{
    private static readonly Random _random = new Random();

    private static readonly (string BrandName, string GenericName)[] _sampleDrugs = new[]
    {
        ("Lipitor", "Atorvastatin"), ("Advil", "Ibuprofen"), ("Tylenol", "Acetaminophen"),
        ("Xanax", "Alprazolam"), ("Zoloft", "Sertraline"), ("Prozac", "Fluoxetine"),
        ("Amoxil", "Amoxicillin"), ("Augmentin", "Amoxicillin/clavulanate"), ("Crestor", "Rosuvastatin"),
        ("Nexium", "Esomeprazole"), ("Singulair", "Montelukast"), ("Plavix", "Clopidogrel"),
        ("Ventolin", "Albuterol"), ("Lyrica", "Pregabalin"), ("Celebrex", "Celecoxib"),
        ("Viagra", "Sildenafil"), ("Cialis", "Tadalafil"), ("Lexapro", "Escitalopram"),
        ("Wellbutrin", "Bupropion"), ("Ambien", "Zolpidem")
    };

    private static readonly string[] _imageUrls = new[]
    {
        "https://cdn01.pharmeasy.in/dam/products_otc/159115/shelcal-500mg-strip-of-15-tablets-2-1679999355.jpg?dim=1440x0",
        "https://cdn01.pharmeasy.in/dam/products_otc/T22634/liveasy-wellness-calcium-magnesium-vitamin-d3-zinc-bones-dental-health-bottle-60-tabs-6.1-1733485732.jpg?dim=1440x0",
        "https://cdn01.pharmeasy.in/dam/products_otc/S04683/evion-400mg-strip-of-20-capsule-6.01-1732857646.jpg?dim=1440x0",
        "https://cdn01.pharmeasy.in/dam/products_otc/I05582/dr-morepen-gluco-one-bg-03-glucometer-test-strips-box-of-50-6.1-1728900382.jpg?dim=1440x0",
        "https://cdn01.pharmeasy.in/dam/products_otc/192351/i-pill-emergency-contraceptive-pill-2-1736842745.jpg?dim=1440x0",
        "https://cdn01.pharmeasy.in/dam/products_otc/T70695/supradyn-daily-multivitamin-for-men-women-builds-energy-immunity-strip-of-15-tablets-6.01-1739962331.jpg?dim=1440x0"
    };

    /// <summary>
    /// Generates a single drug with random data
    /// </summary>
    public static Drug GenerateDrug()
    {
        int randomId = _random.Next(1000, 9999);
        int barcode = _random.Next(100000, 999999);
        int imageIndex = _random.Next(0, _imageUrls.Length);
        var (brandName, genericName) = _sampleDrugs[_random.Next(_sampleDrugs.Length)];

        return new Drug
        {
            DrugId = $"DRG-{randomId}",
            Barcode = barcode.ToString(),
            BarcodeType = "EAN-13",
            BrandName = brandName,
            GenericName = genericName,
            Manufacturer = "PharmaGlobal Inc.",
            OriginCountry = "Germany",
            CategoryId = "CAT-CARDIO",
            CategoryName = "Heart & Blood Pressure",
            ImageUrls = new List<string> { _imageUrls[imageIndex] },
            Description = "Used to treat high blood pressure and prevent angina.",
            SideEffects = new List<string> { "Dizziness", "Fatigue", "Nausea" },
            InteractionNotes = new List<string> { "Avoid alcohol", "Consult doctor if taking other beta-blockers" },
            Tags = new List<string> { "Beta-Blocker", "Hypertension", "Prescription" },
            RelatedDrugs = new List<string> { "DRG-112233", "DRG-445566" },
            Formulation = new Formulation
            {
                Form = "Tablet",
                Strength = "50 mg",
                RouteOfAdministration = "Oral"
            },
            BasePricing = new BasePricing
            {
                SuggestedRetailPrice = 2000,
                Currency = "USD",
                SuggestedTaxRate = 8.25m
            },
            Regulatory = new Regulatory
            {
                IsPrescriptionRequired = true,
                IsHighRisk = false,
                DrugAuthorityNumber = "NDA018274",
                ApprovalDate = DateTime.Parse("1981-09-30T00:00:00Z"),
                ControlSchedule = "Schedule IV"
            },
            // NOTE: Inventory, pricing, and supplier info are now managed per-shop in ShopInventory
            CreatedAt = DateTime.Parse("2024-01-15T09:30:00Z"),
            CreatedBy = "admin_user",
            LastUpdated = DateTime.Parse("2025-07-12T14:45:10Z"),
            UpdatedBy = "inventory_manager"
        };
    }

    /// <summary>
    /// Generates a list of drugs
    /// </summary>
    public static List<Drug> GenerateDrugList(int count = 20)
    {
        var drugs = new List<Drug>();
        for (int i = 0; i < count; i++)
        {
            drugs.Add(GenerateDrug());
        }
        return drugs;
    }
}
