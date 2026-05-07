using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Infrastructure.SampleData;

public static class DrugSeedData
{
    public static List<Drug> GetDrugs()
    {
        var drugs = new List<Drug>();

        // Cardiovascular Drugs (CAT-F558660D)
        var lipitor = new Drug
        {
            DrugId = "DRG-1001",
            Barcode = "6223001001001",
            BarcodeType = "EAN-13",
            BrandName = "Lipitor",
            GenericName = "Atorvastatin",
            Manufacturer = "Pfizer",
            OriginCountry = "USA",
            CategoryId = "CAT-F558660D",
            CategoryName = "Cardiovascular",
            ImageUrls = new List<string> { "https://cdn01.pharmeasy.in/dam/products_otc/T22634/liveasy-wellness-calcium-magnesium-vitamin-d3-zinc-bones-dental-health-bottle-60-tabs-6.1-1733485732.jpg?dim=1440x0" },
            Description = "Statin medication used to treat high cholesterol and prevent cardiovascular disease",
            SideEffects = new List<string> { "Muscle pain", "Liver damage", "Digestive problems" },
            InteractionNotes = new List<string> { "May interact with grapefruit juice", "Monitor liver function regularly" },
            Tags = new List<string> { "Statin", "Cholesterol", "Cardiovascular", "HMG-CoA Reductase Inhibitor", "Prescription" },
            RelatedDrugs = new List<string> { "DRG-1002", "DRG-1003" },
            Formulation = new Formulation
            {
                Form = "Tablet",
                Strength = "10mg",
                RouteOfAdministration = "Oral"
            },
            BasePricing = new BasePricing
            {
                SuggestedRetailPrice = 25.50m,
                Currency = "USD",
                SuggestedTaxRate = 8.25m,
                LastPriceUpdate = DateTime.Parse("2024-01-15T00:00:00Z").ToUniversalTime()
            },
            Regulatory = new Regulatory
            {
                IsPrescriptionRequired = true,
                IsHighRisk = false,
                DrugAuthorityNumber = "NDA020702",
                ApprovalDate = DateTime.Parse("1996-12-17T00:00:00Z").ToUniversalTime(),
                ControlSchedule = "Not Controlled"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Count,
                "tablet",
                "Tablet",
                isSubdivisible: true
            ),
            CreatedAt = DateTime.Parse("2024-01-15T09:30:00Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        drugs.Add(lipitor);

        // Pain Relief Drugs (CAT-45877BC0)
        var ibuprofen = new Drug
        {
            DrugId = "DRG-1002",
            Barcode = "6223001002002",
            BarcodeType = "EAN-13",
            BrandName = "Advil",
            GenericName = "Ibuprofen",
            Manufacturer = "Pfizer",
            OriginCountry = "USA",
            CategoryId = "CAT-45877BC0",
            CategoryName = "Pain Relief",
            ImageUrls = new List<string> { "https://cdn01.pharmeasy.in/dam/products_otc/T22634/liveasy-wellness-calcium-magnesium-vitamin-d3-zinc-bones-dental-health-bottle-60-tabs-6.1-1733485732.jpg?dim=1440x0" },
            Description = "Nonsteroidal anti-inflammatory drug (NSAID) used to reduce fever and treat pain",
            SideEffects = new List<string> { "Stomach upset", "Heartburn", "Dizziness" },
            InteractionNotes = new List<string> { "May interact with blood thinners", "Avoid with aspirin" },
            Tags = new List<string> { "NSAID", "Pain", "Fever", "Anti-inflammatory", "OTC" },
            RelatedDrugs = new List<string> { "DRG-1003", "DRG-1004" },
            Formulation = new Formulation
            {
                Form = "Tablet",
                Strength = "200mg",
                RouteOfAdministration = "Oral"
            },
            BasePricing = new BasePricing
            {
                SuggestedRetailPrice = 8.99m,
                Currency = "USD",
                SuggestedTaxRate = 8.25m,
                LastPriceUpdate = DateTime.Parse("2024-01-15T00:00:00Z").ToUniversalTime()
            },
            Regulatory = new Regulatory
            {
                IsPrescriptionRequired = false,
                IsHighRisk = false,
                DrugAuthorityNumber = "NDA020429",
                ApprovalDate = DateTime.Parse("1974-01-01T00:00:00Z").ToUniversalTime(),
                ControlSchedule = "Not Controlled"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Count,
                "tablet",
                "Tablet",
                isSubdivisible: true
            ),
            CreatedAt = DateTime.Parse("2024-01-15T09:30:00Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        drugs.Add(ibuprofen);

        // Respiratory Drugs (CAT-C3A40592)
        var albuterol = new Drug
        {
            DrugId = "DRG-1003",
            Barcode = "6223001003003",
            BarcodeType = "EAN-13",
            BrandName = "Ventolin",
            GenericName = "Albuterol",
            Manufacturer = "GlaxoSmithKline",
            OriginCountry = "UK",
            CategoryId = "CAT-C3A40592",
            CategoryName = "Respiratory",
            ImageUrls = new List<string> { "https://cdn01.pharmeasy.in/dam/products_otc/T22634/liveasy-wellness-calcium-magnesium-vitamin-d3-zinc-bones-dental-health-bottle-60-tabs-6.1-1733485732.jpg?dim=1440x0" },
            Description = "Bronchodilator used to treat asthma and COPD",
            SideEffects = new List<string> { "Tremor", "Nervousness", "Rapid heartbeat" },
            InteractionNotes = new List<string> { "May interact with beta-blockers", "Use with caution in heart conditions" },
            Tags = new List<string> { "Bronchodilator", "Asthma", "COPD", "Beta-2 Agonist", "Prescription" },
            RelatedDrugs = new List<string> { "DRG-1001", "DRG-1004" },
            Formulation = new Formulation
            {
                Form = "Inhaler",
                Strength = "90mcg",
                RouteOfAdministration = "Inhalation"
            },
            BasePricing = new BasePricing
            {
                SuggestedRetailPrice = 45.00m,
                Currency = "USD",
                SuggestedTaxRate = 8.25m,
                LastPriceUpdate = DateTime.Parse("2024-01-15T00:00:00Z").ToUniversalTime()
            },
            Regulatory = new Regulatory
            {
                IsPrescriptionRequired = true,
                IsHighRisk = false,
                DrugAuthorityNumber = "NDA020983",
                ApprovalDate = DateTime.Parse("1981-01-01T00:00:00Z").ToUniversalTime(),
                ControlSchedule = "Not Controlled"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Count,
                "puff",
                "Inhaler",
                isSubdivisible: false
            ),
            CreatedAt = DateTime.Parse("2024-01-15T09:30:00Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        drugs.Add(albuterol);

        // Diabetes Drugs (CAT-B770BCA7)
        var metformin = new Drug
        {
            DrugId = "DRG-1004",
            Barcode = "6223001004004",
            BarcodeType = "EAN-13",
            BrandName = "Glucophage",
            GenericName = "Metformin",
            Manufacturer = "Bristol Myers Squibb",
            OriginCountry = "USA",
            CategoryId = "CAT-B770BCA7",
            CategoryName = "Diabetes",
            ImageUrls = new List<string> { "https://cdn01.pharmeasy.in/dam/products_otc/T22634/liveasy-wellness-calcium-magnesium-vitamin-d3-zinc-bones-dental-health-bottle-60-tabs-6.1-1733485732.jpg?dim=1440x0" },
            Description = "Oral diabetes medicine that helps control blood sugar levels",
            SideEffects = new List<string> { "Nausea", "Diarrhea", "Stomach upset" },
            InteractionNotes = new List<string> { "May interact with contrast dyes", "Monitor kidney function" },
            Tags = new List<string> { "Antidiabetic", "Biguanide", "Type 2 Diabetes", "Blood Sugar", "Prescription" },
            RelatedDrugs = new List<string> { "DRG-1005", "DRG-1006" },
            Formulation = new Formulation
            {
                Form = "Tablet",
                Strength = "500mg",
                RouteOfAdministration = "Oral"
            },
            BasePricing = new BasePricing
            {
                SuggestedRetailPrice = 12.50m,
                Currency = "USD",
                SuggestedTaxRate = 8.25m,
                LastPriceUpdate = DateTime.Parse("2024-01-15T00:00:00Z").ToUniversalTime()
            },
            Regulatory = new Regulatory
            {
                IsPrescriptionRequired = true,
                IsHighRisk = false,
                DrugAuthorityNumber = "NDA021202",
                ApprovalDate = DateTime.Parse("1995-01-01T00:00:00Z").ToUniversalTime(),
                ControlSchedule = "Not Controlled"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Count,
                "tablet",
                "Tablet",
                isSubdivisible: true
            ),
            CreatedAt = DateTime.Parse("2024-01-15T09:30:00Z").ToUniversalTime(),
            CreatedBy = "system"
        };
        drugs.Add(metformin);

        return drugs;
    }
}
