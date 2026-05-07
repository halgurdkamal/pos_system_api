# Drug System Documentation - POS System API

## Overview

The drug system in the POS System API implements a **centralized drug catalog** that serves as a single source of truth for all pharmaceutical products across all registered pharmacy shops. This architecture enables efficient multi-tenant operations where multiple pharmacies can access verified drug information while maintaining their own inventory and pricing.

## Drug Model Structure

### Core Drug Entity

Each drug represents a complete pharmaceutical product with the following key components:

#### Basic Identification

- **DrugId**: Unique identifier across the entire system
- **Barcode**: Universal barcode for scanning (supports multiple barcode types)
- **BrandName**: Commercial/trademark name of the drug
- **GenericName**: Active ingredient name
- **Manufacturer**: Company that produces the drug
- **OriginCountry**: Country of manufacture

#### Categorization & Media

- **Category**: Drug classification (e.g., Antibiotics, Pain Relief, Cardiovascular)
- **ImageUrls**: Product images and packaging photos
- **Tags**: Search and filtering tags
- **RelatedDrugs**: Links to similar or alternative medications

#### Clinical Information

- **Description**: Detailed product description
- **SideEffects**: Known adverse reactions
- **InteractionNotes**: Drug interaction warnings

### Value Objects

#### Formulation

Represents the physical form, strength, and administration route:

- **Form**: Tablet, Capsule, Syrup, Injection, etc.
- **Strength**: Dosage amount (e.g., "500mg", "10mg/ml")
- **RouteOfAdministration**: Oral, Intravenous, Topical, etc.

#### BasePricing

Suggested retail pricing guidelines (shops can override):

- **SuggestedRetailPrice**: Recommended selling price
- **Currency**: Pricing currency (default USD)
- **SuggestedTaxRate**: Recommended tax percentage
- **LastPriceUpdate**: Timestamp of last price change

#### Regulatory

Legal and compliance information:

- **IsPrescriptionRequired**: Whether prescription is mandatory
- **IsHighRisk**: Special handling requirements
- **DrugAuthorityNumber**: Regulatory approval number
- **ApprovalDate**: When drug was approved for use
- **ControlSchedule**: Controlled substance classification

#### PackagingInfo

Packaging hierarchy and units:

- Defines how the drug is packaged and sold
- Supports multiple packaging levels (box, strip, blister, etc.)

## How the Drug System Works

### 1. Centralized Catalog Management

**Key Principle**: Drugs are created once in the central catalog and shared across all shops.

- **Single Source of Truth**: All shops access the same verified drug information
- **Automatic Updates**: Changes to drug information apply to all shops instantly
- **Data Integrity**: No duplication of drug data across locations
- **Regulatory Compliance**: Centralized management of regulatory information

### 2. Shop-Specific Extensions

**Important Distinction**: The Drug entity contains ONLY shared catalog data. Shop-specific information is stored separately.

**Shop-Specific Data** (stored in ShopInventory entity):

- Local inventory levels and batch tracking
- Shop-specific pricing (can override base pricing)
- Supplier relationships and purchase terms
- Storage locations and reorder points
- Local regulatory requirements

### 3. Search & Discovery System

**Multi-Faceted Search Capabilities**:

- Search by brand name, generic name, or barcode
- Filter by category, manufacturer, or country of origin
- Stock availability filtering
- Prescription requirement filtering

**Browse Mode**: Lightweight listing optimized for quick product lookup
**Detail Mode**: Comprehensive view with full clinical and inventory information

## Available APIs

### 1. Create Drug

```http
POST /api/drugs
```

**Purpose**: Add new drugs to the central catalog
**Authorization**: Admin only
**Content-Type**: application/json

**Request Body**:

```json
{
  "drugId": "ASP001",
  "barcode": "1234567890123",
  "barcodeType": "EAN13",
  "brandName": "Aspirin 500mg",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "PharmaCorp",
  "originCountry": "Germany",
  "categoryName": "Pain Relief",
  "description": "Pain relief medication",
  "formulation": {
    "form": "Tablet",
    "strength": "500mg",
    "routeOfAdministration": "Oral"
  },
  "basePricing": {
    "suggestedRetailPrice": 5.99,
    "currency": "USD",
    "suggestedTaxRate": 0.08
  },
  "regulatory": {
    "isPrescriptionRequired": false,
    "isHighRisk": false,
    "drugAuthorityNumber": "DE-ASP-001",
    "approvalDate": "2020-01-15T00:00:00Z",
    "controlSchedule": "Unscheduled"
  }
}
```

**Response**: Created drug details with generated DrugId

### 2. Get Single Drug

```http
GET /api/drugs/{id}
```

**Purpose**: Get basic drug information
**Authorization**: Public (no authentication required)
**Parameters**:

- `id`: Drug ID

**Response**: Drug details without shop-specific data

{
"drugId": "DRG-1001",
"barcode": "123456789012",
"barcodeType": "EAN-13",
"brandName": "Lipitor",
"genericName": "Atorvastatin",
"manufacturer": "Pfizer Inc.",
"originCountry": "United States",
"categoryId": "CAT-B770BCA7",
"category": "Diabetes",
"imageUrls": [
"https://i.ibb.co/B5h3m8HH/Atorvastatin-20mg-Blister-scaled.webp"
],
"description": "Used to treat high cholesterol and prevent heart disease by lowering bad cholesterol levels.",
"sideEffects": [
"Muscle pain",
"Liver problems",
"Increased blood sugar"
],
"interactionNotes": [
"Avoid grapefruit juice",
"May interact with certain antibiotics"
],
"tags": [
"Cholesterol",
"Cardiovascular",
"Prescription"
],
"relatedDrugs": [
"DRG-1002",
"DRG-1003"
],
"formulation": {
"form": "Tablet",
"strength": "20 mg",
"routeOfAdministration": "Oral"
},
"basePricing": {
"suggestedRetailPrice": 250,
"currency": "USD",
"suggestedTaxRate": 8.25,
"lastPriceUpdate": "2025-10-29T22:01:45.854669Z"
},
"regulatory": {
"isPrescriptionRequired": true,
"isHighRisk": false,
"drugAuthorityNumber": "NDA020762",
"approvalDate": "1996-12-17T00:00:00Z",
"controlSchedule": "Not Scheduled"
},
"packagingInfo": {
"unitType": 1,
"baseUnit": "tablet",
"baseUnitDisplayName": "Tablet",
"isSubdivisible": true,
"packagingLevels": [
{
"packagingLevelId": "PKG-LV-606E4ACBBD034691ACBDA18DF68FAB1B",
"levelNumber": 1,
"unitName": "Tablet",
"parentPackagingLevelId": null,
"baseUnitQuantity": 1,
"quantityPerParent": 1,
"isSellable": true,
"isDefault": true,
"isBreakable": true,
"barcode": null,
"minimumSaleQuantity": null
},
{
"packagingLevelId": "PKG-LV-9242380694124EF2A65D7045D8076395",
"levelNumber": 2,
"unitName": "Bottle",
"parentPackagingLevelId": "PKG-LV-606E4ACBBD034691ACBDA18DF68FAB1B",
"baseUnitQuantity": 10,
"quantityPerParent": 10,
"isSellable": true,
"isDefault": false,
"isBreakable": true,
"barcode": null,
"minimumSaleQuantity": null
}
]
},
"createdAt": "2025-10-29T22:01:45.859128Z",
"createdBy": "system",
"lastUpdated": null,
"updatedBy": null
}

### 3. Get Drug List (Paginated)

```http
GET /api/drugs?page=1&limit=20
```

**Purpose**: Browse drug catalog with pagination
**Authorization**: Public
**Query Parameters**:

- `page`: Page number (default: 1)
- `limit`: Items per page (default: 20)

**Response**:

```json
{
  "data": [...],
  "page": 1,
  "limit": 20,
  "total": 1250
}
```

### 4. Browse Drugs (Enhanced)

```http
GET /api/drugs/browse?page=1&limit=20&searchTerm=paracetamol&category=Pain Relief&inStock=true
```

**Purpose**: Advanced drug browsing with filtering
**Authorization**: Public
**Query Parameters**:

- `page`, `limit`: Pagination
- `searchTerm`: Search by name, barcode, or manufacturer
- `category`: Filter by drug category
- `inStock`: Show only drugs available in inventory (true/false)

**Response**: Lightweight drug list items optimized for browsing

### 5. Get Drug Details (Complete)

```http
GET /api/drugs/{id}/detail
```

**Purpose**: Complete drug information including inventory across all shops
**Authorization**: Public
**Parameters**:

- `id`: Drug ID

**Response**: Full drug details with inventory information from all shops

## API Usage Patterns

### For Pharmacy POS Systems

1. **Product Lookup**:

   ```http
   GET /api/drugs/browse?searchTerm=paracetamol&inStock=true
   ```

   Use for quick product search during transactions

2. **Barcode Scanning**:

   ```http
   GET /api/drugs/{barcode}
   ```

   Use for instant drug details when scanning barcodes

3. **Inventory Check**:
   ```http
   GET /api/drugs/{id}/detail
   ```
   Use to check stock availability across locations

### For Pharmacy Management Software

1. **Catalog Integration**:

   ```http
   GET /api/drugs?page=1&limit=100
   ```

   Sync complete drug catalog for local caching

2. **Product Search**:

   ```http
   GET /api/drugs/browse?searchTerm={query}&category={category}
   ```

   Implement advanced search functionality

3. **Regulatory Compliance**:
   ```http
   GET /api/drugs/{id}
   ```
   Access regulatory information for compliance checks

### For Administrative Systems

1. **Drug Management**:

   ```http
   POST /api/drugs
   ```

   Add new drugs to the central catalog

2. **Catalog Maintenance**:

   ```http
   GET /api/drugs/{id}/detail
   ```

   Comprehensive drug information for management

3. **Audit & Reporting**:
   ```http
   GET /api/drugs/browse?page=1&limit=1000
   ```
   Access complete drug information for reporting

## Data Architecture

### CQRS Pattern Implementation

**Read Operations (Queries)**:

1. Client sends GET request to API endpoint
2. Controller receives request and creates Query object
3. MediatR routes Query to appropriate Handler
4. Handler calls Repository interface (Application layer)
5. Repository implementation (Infrastructure) queries database
6. Data mapped to DTO and returned to client

**Write Operations (Commands)**:

1. Client sends POST request with drug data
2. Controller validates and creates Command object
3. MediatR routes Command to Handler
4. Handler validates business rules and calls Repository
5. Repository saves to database with transaction
6. Success response with created drug details

### Data Isolation

**Shared Data** (Drug entity):

- Catalog information accessible to all shops
- Regulatory and clinical data
- Base pricing guidelines
- Product specifications

**Shop-Specific Data** (ShopInventory entity):

- Inventory levels and batch tracking
- Local pricing overrides
- Supplier information
- Storage locations

## Business Benefits

### For Pharmacy Chains

- **Consistency**: Same drug information across all locations
- **Efficiency**: Centralized catalog management
- **Compliance**: Unified regulatory information
- **Cost Savings**: Shared infrastructure and maintenance

### For Independent Pharmacies

- **Quality Data**: Access to verified drug catalog
- **Competitive Pricing**: Professional pricing guidance
- **Regulatory Support**: Centralized compliance management
- **Technology Access**: Advanced features without high costs

### For Distributors

- **Data Provision**: Supply verified drug information
- **Market Reach**: Service multiple retail partners
- **Quality Assurance**: Centralized data validation
- **Business Intelligence**: Access to usage analytics

## Technical Implementation Notes

### Performance Optimizations

- **AsNoTracking()**: Used for read-only queries to improve performance
- **Pagination**: Prevents large data transfers
- **Indexing**: Database indexes on commonly searched fields
- **Caching**: Consider implementing Redis for frequently accessed drugs

### Security Considerations

- **Public Access**: Drug catalog is publicly accessible (no auth required)
- **Admin Operations**: Drug creation requires admin authorization
- **Data Validation**: Comprehensive validation of drug data
- **Audit Trail**: All changes logged for compliance

### Error Handling

- **404 Not Found**: Drug ID doesn't exist
- **400 Bad Request**: Invalid request data
- **409 Conflict**: Duplicate drug ID or barcode
- **401 Unauthorized**: Admin operations without proper authorization

## Future Enhancements

### Planned Features

- **Bulk Import/Export**: CSV/Excel drug catalog management
- **Image Upload**: Drug packaging and product photos
- **Advanced Search**: Fuzzy search and autocomplete
- **API Versioning**: Backward compatibility for integrations
- **Real-time Updates**: WebSocket notifications for catalog changes

### Integration Possibilities

- **Electronic Prescriptions**: Integration with prescription systems
- **Insurance Claims**: Automated insurance processing
- **Supplier Systems**: Direct supplier catalog integration
- **Mobile Apps**: Companion mobile applications for pharmacies

---

_This documentation covers the drug system as implemented in the POS System API. For technical implementation details, refer to the source code in the `src/Core/Domain/Drugs/` and `src/API/Controllers/DrugsController.cs` directories._</content>
<parameter name="filePath">c:\Users\pc\Documents\My Projects\pos_system_api\DRUG_SYSTEM_DOCUMENTATION.md
