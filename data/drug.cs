using System.Text.Json;
using pos_system_api.data; // Make sure to import the namespace of your Drug model

namespace pos_system_api.data
{
    /// <summary>
    /// Provides a sample instance of the Drug model by parsing a JSON string.
    /// This is the recommended, strongly-typed approach.
    /// </summary>
    public static class SampleDrugProvider
    {
        // Store the raw JSON data as a multi-line string.
        // Using three double-quotes (""") creates a raw string literal,
        // which is perfect for embedding JSON.
        private const string SampleDrugJson = """
        {
          "drugId": "DRG-987654",
          "barcode": "1234567890123",
          "barcodeType": "EAN-13",
          "brandName": "CardioGuard",
          "genericName": "Atenolol",
          "manufacturer": "PharmaGlobal Inc.",
          "originCountry": "Germany",
          "category": "Cardiovascular",
          "imageUrls": [
            "https://example.com/drug_image_1.jpg",
            "https://example.com/drug_image_2.jpg"
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
                "purchasePrice": 15.50,
                "sellingPrice": 29.99
              },
              {
                "batchNumber": "B-1023B",
                "quantityOnHand": 250,
                "receivedDate": "2024-08-01T10:00:00Z",
                "expiryDate": "2026-07-31T23:59:59Z",
                "purchasePrice": 15.75,
                "sellingPrice": 30.50
              }
            ]
          },
          "pricing": {
            "costPrice": 15.75,
            "sellingPrice": 30.50,
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

        /// <summary>
        /// Deserializes the JSON string into a Drug object.
        /// </summary>
        /// <returns>A Drug object populated with sample data, or null if parsing fails.</returns>
        public static Dictionary<string, object>? GetDrug()
        {
            // Use the System.Text.Json.JsonSerializer to convert the JSON string
            // into an instance of your Drug class.
            return JsonSerializer.Deserialize<Dictionary<string, object>>(SampleDrugJson);
        }
    }
}
