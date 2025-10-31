using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Infrastructure.SampleData;

/// <summary>
/// Helper class to create drugs with various packaging configurations
/// Demonstrates different unit types and packaging hierarchies
/// </summary>
public static class DrugPackagingExamples
{
    /// <summary>
    /// Example: Amoxicillin 500mg Capsules (Count-based, sold by strips)
    /// </summary>
    public static Drug CreateAmoxicillinCapsules()
    {
        var drug = new Drug
        {
            DrugId = "AMOX-500-CAP",
            Barcode = "6223001234567",
            BarcodeType = "EAN-13",
            BrandName = "Amoxil",
            GenericName = "Amoxicillin",
            Manufacturer = "GSK",
            CategoryId = "CAT-ABX",
            CategoryName = "Antibiotics",
            Formulation = new Formulation
            {
                Form = "Capsule",
                Strength = "500mg",
                RouteOfAdministration = "Oral"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Count,
                "capsule",
                "Capsule",
                isSubdivisible: true
            )
        };

        // Add packaging levels
        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 1,
            unitName: "Capsule",
            baseUnitQuantity: 1,
            isSellable: true, // Can sell loose capsules
            isDefault: false,
            isBreakable: false
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 2,
            unitName: "Strip",
            baseUnitQuantity: 10, // 10 capsules per strip
            isSellable: true,
            isDefault: true, // Default sell unit
            isBreakable: true, // Can break strip to sell individual capsules
            barcode: "6223001234574"
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 3,
            unitName: "Box",
            baseUnitQuantity: 100, // 10 strips = 100 capsules
            isSellable: true,
            isDefault: false,
            isBreakable: true,
            barcode: "6223001234581"
        ));

        return drug;
    }

    /// <summary>
    /// Example: Paracetamol Syrup 120mg/5ml (Volume-based, sold by bottle)
    /// </summary>
    public static Drug CreateParacetamolSyrup()
    {
        var drug = new Drug
        {
            DrugId = "PARA-SYR-120",
            Barcode = "6223002345678",
            BarcodeType = "EAN-13",
            BrandName = "Cetal Syrup",
            GenericName = "Paracetamol",
            Manufacturer = "Julphar",
            CategoryId = "CAT-PAIN",
            CategoryName = "Pain Relief",
            Formulation = new Formulation
            {
                Form = "Syrup",
                Strength = "120mg/5ml",
                RouteOfAdministration = "Oral"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Volume,
                "ml",
                "Milliliter",
                isSubdivisible: true
            )
        };

        // Add packaging levels
        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 1,
            unitName: "ml",
            baseUnitQuantity: 1,
            isSellable: true, // Can sell by milliliter
            isDefault: false,
            isBreakable: false,
            minimumSaleQuantity: 5 // Must buy at least 5ml
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 2,
            unitName: "Bottle",
            baseUnitQuantity: 120, // 120ml per bottle
            isSellable: true,
            isDefault: true, // Default sell unit
            isBreakable: true, // Can sell partial bottles
            barcode: "6223002345685"
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 3,
            unitName: "Box",
            baseUnitQuantity: 1200, // 10 bottles = 1200ml
            isSellable: true,
            isDefault: false,
            isBreakable: true,
            barcode: "6223002345692"
        ));

        return drug;
    }

    /// <summary>
    /// Example: Hydrocortisone Cream 1% (Weight-based, sold by tube)
    /// </summary>
    public static Drug CreateHydrocortisoneCream()
    {
        var drug = new Drug
        {
            DrugId = "HYDRO-CRM-1",
            Barcode = "6223003456789",
            BarcodeType = "EAN-13",
            BrandName = "Hydrocortisone Cream",
            GenericName = "Hydrocortisone",
            Manufacturer = "Spimaco",
            CategoryId = "CAT-SKIN",
            CategoryName = "Skin Care",
            Formulation = new Formulation
            {
                Form = "Cream",
                Strength = "1%",
                RouteOfAdministration = "Topical"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Weight,
                "g",
                "Gram",
                isSubdivisible: false // Cannot subdivide tubes
            )
        };

        // Add packaging levels
        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 1,
            unitName: "Gram",
            baseUnitQuantity: 1,
            isSellable: false, // Cannot sell by gram
            isDefault: false,
            isBreakable: false
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 2,
            unitName: "Tube",
            baseUnitQuantity: 15, // 15g per tube
            isSellable: true,
            isDefault: true, // Default sell unit
            isBreakable: false, // Cannot break tubes (contamination risk)
            barcode: "6223003456796"
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 3,
            unitName: "Box",
            baseUnitQuantity: 75, // 5 tubes = 75g
            isSellable: true,
            isDefault: false,
            isBreakable: true,
            barcode: "6223003456802"
        ));

        return drug;
    }

    /// <summary>
    /// Example: Ventolin Inhaler (Dose-based, cannot be subdivided)
    /// </summary>
    public static Drug CreateVentolinInhaler()
    {
        var drug = new Drug
        {
            DrugId = "VENT-INH-100",
            Barcode = "6223004567890",
            BarcodeType = "EAN-13",
            BrandName = "Ventolin Inhaler",
            GenericName = "Salbutamol",
            Manufacturer = "GSK",
            CategoryId = "CAT-ALLERGY",
            CategoryName = "Allergy & Asthma",
            Formulation = new Formulation
            {
                Form = "Metered Dose Inhaler",
                Strength = "100mcg/puff",
                RouteOfAdministration = "Inhalation"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Dose,
                "puff",
                "Puff",
                isSubdivisible: false // Cannot subdivide doses
            )
        };

        // Add packaging levels
        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 1,
            unitName: "Puff",
            baseUnitQuantity: 1,
            isSellable: false, // Cannot sell individual puffs
            isDefault: false,
            isBreakable: false
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 2,
            unitName: "Inhaler",
            baseUnitQuantity: 200, // 200 puffs per inhaler
            isSellable: true,
            isDefault: true, // Default sell unit
            isBreakable: false, // Cannot break inhaler device
            barcode: "6223004567906"
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 3,
            unitName: "Box",
            baseUnitQuantity: 200, // 1 inhaler per retail box
            isSellable: true,
            isDefault: false,
            isBreakable: false,
            barcode: "6223004567913"
        ));

        return drug;
    }

    /// <summary>
    /// Example: Aspirin 100mg Tablets (Count-based with flexible packaging)
    /// </summary>
    public static Drug CreateAspirinTablets()
    {
        var drug = new Drug
        {
            DrugId = "ASP-100-TAB",
            Barcode = "6223005678901",
            BarcodeType = "EAN-13",
            BrandName = "Aspirin",
            GenericName = "Acetylsalicylic Acid",
            Manufacturer = "Bayer",
            CategoryId = "CAT-PAIN",
            CategoryName = "Pain Relief",
            Formulation = new Formulation
            {
                Form = "Tablet",
                Strength = "100mg",
                RouteOfAdministration = "Oral"
            },
            PackagingInfo = new PackagingInfo(
                UnitType.Count,
                "tablet",
                "Tablet",
                isSubdivisible: true
            )
        };

        // Add packaging levels
        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 1,
            unitName: "Tablet",
            baseUnitQuantity: 1,
            isSellable: true, // Can sell loose tablets
            isDefault: false,
            isBreakable: false
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 2,
            unitName: "Strip",
            baseUnitQuantity: 10,
            isSellable: true,
            isDefault: false,
            isBreakable: true,
            barcode: "6223005678918"
        ));

        drug.PackagingInfo.AddPackagingLevel(new PackagingLevel(packagingLevelId: null,
            levelNumber: 3,
            unitName: "Box",
            baseUnitQuantity: 100, // 10 strips
            isSellable: true,
            isDefault: true, // OTC drugs often sold by box
            isBreakable: true,
            barcode: "6223005678925"
        ));

        return drug;
    }

    /// <summary>
    /// Get all example drugs with packaging info
    /// </summary>
    public static List<Drug> GetAllExamples()
    {
        return new List<Drug>
        {
            CreateAmoxicillinCapsules(),
            CreateParacetamolSyrup(),
            CreateHydrocortisoneCream(),
            CreateVentolinInhaler(),
            CreateAspirinTablets()
        };
    }
}
