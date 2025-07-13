using System;
using System.Collections.Generic;
using System.Text.Json;

namespace pos_system_api.data
{
    /// <summary>
    /// Provides sample drug data, including methods to get a single drug
    /// or a list of drugs as dictionaries.
    /// </summary>
    public static class SampleDrugProvider
    {
        // A single Random instance should be created and reused to ensure
        // that it produces different numbers over a short time span.
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a raw JSON string for a single drug with a randomized ID.
        /// </summary>
        /// <returns>A JSON string representing a drug object.</returns>
        private static string GenerateSampleDrugJson()
        {
            // Generate a random number for the drug ID.
            int randomId = _random.Next(1000, 9999);
            int barcode = _random.Next(100000, 999999);
            int imageIndex = _random.Next(0, 6);

            string[] image =
            [
                "https://cdn01.pharmeasy.in/dam/products_otc/159115/shelcal-500mg-strip-of-15-tablets-2-1679999355.jpg?dim=1440x0",
                "https://cdn01.pharmeasy.in/dam/products_otc/T22634/liveasy-wellness-calcium-magnesium-vitamin-d3-zinc-bones-dental-health-bottle-60-tabs-6.1-1733485732.jpg?dim=1440x0",
                "https://cdn01.pharmeasy.in/dam/products_otc/S04683/evion-400mg-strip-of-20-capsule-6.01-1732857646.jpg?dim=1440x0",
                "https://cdn01.pharmeasy.in/dam/products_otc/I05582/dr-morepen-gluco-one-bg-03-glucometer-test-strips-box-of-50-6.1-1728900382.jpg?dim=1440x0",
                "https://cdn01.pharmeasy.in/dam/products_otc/192351/i-pill-emergency-contraceptive-pill-2-1736842745.jpg?dim=1440x0",
                "https://cdn01.pharmeasy.in/dam/products_otc/T70695/supradyn-daily-multivitamin-for-men-women-builds-energy-immunity-strip-of-15-tablets-6.01-1739962331.jpg?dim=1440x0"
            ];
            // Use C# raw string literals with interpolation ($"") to build the JSON.
            // This correctly injects the randomId into the string.
            return $$"""
            {
              "drugId": "DRG-{{randomId}}",
              "barcode": "{{barcode}}",
              "barcodeType": "EAN-13",
              "brandName": "CardioGuard",
              "genericName": "Dolo 650mg Strip Of 15 Tablets",
              "manufacturer": "PharmaGlobal Inc.",
              "originCountry": "Germany",
              "category": "Cardiovascular",
              "imageUrls": [
                "{{image[imageIndex]}}"
              ],
              "description": "Used to treat high blood pressure and prevent angina.",
              "sideEffects": ["Dizziness", "Fatigue", "Nausea"],
              "interactionNotes": ["Avoid alcohol", "Consult doctor if taking other beta-blockers"],
              "tags": ["Beta-Blocker", "Hypertension", "Prescription"],
              "relatedDrugs": ["DRG-112233", "DRG-445566"],
              "formulation": {
                "form": "Tablet",
                "strength": "50 mg",
                "routeOfAdministration": "Oral"
              },
              "inventory": {
                "totalStock": 500,
                "reorderPoint": 50,
                "storageLocation": "Shelf A-3, Cold Storage",
                "batches": [
                  {
                    "batchNumber": "B-1022A",
                    "quantityOnHand": 250,
                    "receivedDate": "2024-07-20T10:00:00Z",
                    "expiryDate": "2026-06-30T23:59:59Z",
                    "purchasePrice": 1500,
                    "sellingPrice": 2000
                  },
                  {
                    "batchNumber": "B-1023B",
                    "quantityOnHand": 250,
                    "receivedDate": "2024-08-01T10:00:00Z",
                    "expiryDate": "2026-07-31T23:59:59Z",
                    "purchasePrice": 1500,
                    "sellingPrice": 2000
                  }
                ]
              },
              "pricing": {
                "costPrice": 1500,
                "sellingPrice": 2000,
                "currency": "USD",
                "discount": 5.0,
                "taxRate": 8.25
              },
              "regulatory": {
                "isPrescriptionRequired": true,
                "isHighRisk": false,
                "drugAuthorityNumber": "NDA018274",
                "approvalDate": "1981-09-30T00:00:00Z",
                "controlSchedule": "Schedule IV"
              },
              "supplierInfo": {
                "supplierId": "SUP-001",
                "supplierName": "Med-Distributors LLC",
                "contactNumber": "+1-800-555-0199",
                "email": "sales@med-distributors.com"
              },
              "metadata": {
                "createdAt": "2024-01-15T09:30:00Z",
                "createdBy": "admin_user",
                "lastUpdated": "2025-07-12T14:45:10Z",
                "updatedBy": "inventory_manager"
              }
            }
            """;
        }

        /// <summary>
        /// Deserializes the JSON string into a Dictionary.
        /// </summary>
        /// <returns>A dictionary populated with sample data, or null if parsing fails.</returns>
        public static Dictionary<string, object>? GetDrug()
        {
            // Use the System.Text.Json.JsonSerializer to convert the JSON string
            // into a dictionary.
            return JsonSerializer.Deserialize<Dictionary<string, object>>(GenerateSampleDrugJson());
        }

        /// <summary>
        /// Provides a sample list of drugs.
        /// </summary>
        /// <returns>A list of dictionaries, each representing a drug with a unique ID.</returns>
        public static Dictionary<string, object> GetDrugList()
        {
            var drugList = new List<Dictionary<string, object>>();
            
            // Create a list of 20 drugs.
            for (int i = 0; i < 20; i++)
            {
                // Calling GetDrug() in a loop ensures that GenerateSampleDrugJson()
                // is called each time, creating a new random ID for each drug.
                var drug = GetDrug();
                if (drug != null)
                {
                    drugList.Add(drug);
                }
            }
            return 
            new Dictionary<string, object>
            {
                { "page", 1 },
                { "limit", 20 },
                { "total", 100 },
                { "drugs", drugList }
            };;
        }
    }
}
