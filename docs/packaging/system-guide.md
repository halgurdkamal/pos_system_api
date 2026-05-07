# Multi-Tenant Pharmacy POS System - Packaging & Pricing Guide

## Overview

This is a **multi-tenant Point-of-Sale (POS) API** for pharmacy chains that supports **shop-specific packaging-level pricing**. Each pharmacy shop can maintain its own inventory and pricing while sharing a centralized drug catalog.

## Key Features

- ✅ **Multi-Tenant Architecture**: Multiple shops share drug catalog but maintain separate inventory/pricing
- ✅ **Packaging-Level Pricing**: Different prices for Box, Strip, Tablet levels per shop
- ✅ **Auto-Pricing**: POS automatically applies correct prices based on selected packaging
- ✅ **Stock Tracking**: Accurate inventory management at base unit level (tablets)
- ✅ **Bulk Discounts**: Support for quantity-based pricing (e.g., Box cheaper than individual Strips)

## System Architecture

### Data Flow

```
Drug Catalog (Shared)
    ↓
Shop Inventory (Shop-Specific)
    ↓
Shop Pricing (Packaging-Level)
    ↓
Sales Orders (Auto-Priced)
    ↓
Stock Reduction (Base Units)
```

## Core Concepts

### 1. Drug Catalog (Shared Across All Shops)

**Purpose**: Centralized drug information that all shops can reference.

**Example Drug Structure**:

```json
{
  "id": "paracetamol-500mg",
  "brandName": "Paracetamol 500mg",
  "genericName": "Acetaminophen",
  "packagingInfo": {
    "unitType": "Solid",
    "baseUnit": "tablet",
    "baseUnitDisplayName": "Tablet",
    "packagingLevels": [
      {
        "levelNumber": 1,
        "unitName": "Tablet",
        "baseUnitQuantity": 1.0,
        "isSellable": true
      },
      {
        "levelNumber": 2,
        "unitName": "Strip",
        "baseUnitQuantity": 10.0,
        "isSellable": true
      },
      {
        "levelNumber": 3,
        "unitName": "Box",
        "baseUnitQuantity": 50.0,
        "isSellable": true
      }
    ]
  }
}
```

### 2. Shop Inventory (Shop-Specific)

**Purpose**: Each shop maintains its own stock levels, batches, and pricing.

**Key Fields**:

- `shopId`: Identifies the pharmacy location
- `drugId`: References shared drug catalog
- `totalStock`: Total base units (tablets) in stock
- `batches[]`: Individual stock batches with expiry dates
- `shopPricing`: Shop-specific pricing configuration

### 3. Shop Pricing (Packaging-Level Pricing)

**Purpose**: Each shop sets its own prices for different packaging levels.

**Structure**:

```json
{
  "costPrice": 4.5,
  "sellingPrice": 6.0,
  "packagingLevelPrices": {
    "Box": 8.0,
    "Strip": 2.0,
    "Tablet": 0.4
  },
  "currency": "USD",
  "lastPriceUpdate": "2025-10-28T10:30:00Z"
}
```

**Pricing Strategy Examples**:

#### Bulk Discount Strategy

```json
{
  "packagingLevelPrices": {
    "Box": 8.0, // 5 strips = $8 ($1.60/strip)
    "Strip": 2.0, // Retail = $2.00/strip
    "Tablet": 0.5 // Individual = $0.50/tablet
  }
}
```

_Box saves $2 vs buying 5 strips individually_

#### Competitive Pricing Strategy

```json
{
  "packagingLevelPrices": {
    "Box": 7.5, // Lower bulk price
    "Strip": 1.8, // Lower retail price
    "Tablet": 0.4 // Lower individual price
  }
}
```

#### Premium Pricing Strategy

```json
{
  "packagingLevelPrices": {
    "Box": 9.5, // Premium bulk price
    "Strip": 2.5, // Premium retail price
    "Tablet": 0.6 // Premium individual price
  }
}
```

## API Endpoints

### Drug Management (Admin)

#### Create Drug

```http
POST /api/admin/drugs
```

**Request**:

```json
{
  "brandName": "Paracetamol 500mg",
  "genericName": "Acetaminophen",
  "packagingInfo": {
    "unitType": "Solid",
    "baseUnit": "tablet",
    "baseUnitDisplayName": "Tablet",
    "packagingLevels": [
      {
        "levelNumber": 1,
        "unitName": "Tablet",
        "baseUnitQuantity": 1.0,
        "isSellable": true
      },
      {
        "levelNumber": 2,
        "unitName": "Strip",
        "baseUnitQuantity": 10.0,
        "isSellable": true
      },
      {
        "levelNumber": 3,
        "unitName": "Box",
        "baseUnitQuantity": 50.0,
        "isSellable": true
      }
    ]
  }
}
```

### Inventory Management (Shop-Level)

#### Add Stock

```http
POST /api/inventory/shops/{shopId}/stock
```

**Request**:

```json
{
  "drugId": "paracetamol-id",
  "batchNumber": "BATCH-2025-001",
  "supplierId": "supplier-pharma",
  "quantity": 500,
  "expiryDate": "2026-10-28",
  "purchasePrice": 4.5,
  "sellingPrice": 6.0,
  "storageLocation": "Shelf A-3"
}
```

#### Set Packaging Pricing

```http
PUT /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-pricing
```

**Request**:

```json
{
  "Box": 8.0,
  "Strip": 2.0,
  "Tablet": 0.4
}
```

#### Get Shop Inventory

```http
GET /api/inventory/shops/{shopId}
```

**Response**:

```json
{
  "items": [
    {
      "id": "inv-123",
      "shopId": "downtown-pharmacy",
      "drugId": "paracetamol-id",
      "totalStock": 500,
      "shopPricing": {
        "packagingLevelPrices": {
          "Box": 8.0,
          "Strip": 2.0,
          "Tablet": 0.4
        }
      },
      "batches": [
        {
          "batchNumber": "BATCH-2025-001",
          "quantityOnHand": 500,
          "expiryDate": "2026-10-28"
        }
      ]
    }
  ]
}
```

### Sales Orders (POS/Cashier)

#### Create Sales Order with Auto-Pricing

```http
POST /api/salesorders
```

**Request**:

```json
{
  "shopId": "downtown-pharmacy",
  "cashierId": "cashier-123",
  "customerId": "customer-456",
  "customerName": "John Doe",
  "items": [
    {
      "drugId": "paracetamol-id",
      "quantity": 1,
      "packagingLevel": "Box"
    },
    {
      "drugId": "paracetamol-id",
      "quantity": 3,
      "packagingLevel": "Strip"
    }
  ],
  "taxAmount": 2.5
}
```

**Response** (Auto-Priced):

```json
{
  "id": "so-20251028-123456",
  "orderNumber": "SO-20251028123456-1234",
  "shopId": "downtown-pharmacy",
  "subTotal": 14.0,
  "taxAmount": 2.5,
  "totalAmount": 16.5,
  "items": [
    {
      "id": "item-1",
      "drugId": "paracetamol-id",
      "quantity": 1,
      "unitPrice": 8.0,
      "packagingLevelSold": "Box",
      "baseUnitsConsumed": 50,
      "totalPrice": 8.0
    },
    {
      "id": "item-2",
      "drugId": "paracetamol-id",
      "quantity": 3,
      "unitPrice": 2.0,
      "packagingLevelSold": "Strip",
      "baseUnitsConsumed": 30,
      "totalPrice": 6.0
    }
  ]
}
```

## Complete Workflow Examples

### Scenario 1: Bulk Discount Strategy

**Shop Setup**:

```bash
# 1. Admin creates drug
POST /api/admin/drugs
{
  "brandName": "Paracetamol",
  "packagingInfo": {
    "baseUnit": "tablet",
    "packagingLevels": [
      {"unitName": "Strip", "baseUnitQuantity": 10},
      {"unitName": "Box", "baseUnitQuantity": 50}
    ]
  }
}

# 2. Shop sets bulk discount pricing
PUT /api/inventory/shops/downtown-pharmacy/drugs/paracetamol-id/packaging-pricing
{
  "Box": 8.00,    // $8 for 50 tablets (bulk discount)
  "Strip": 2.00   // $2 for 10 tablets (retail)
}

# 3. Add stock
POST /api/inventory/shops/downtown-pharmacy/stock
{
  "drugId": "paracetamol-id",
  "quantity": 1000,  // 1000 tablets = 20 boxes
  "batchNumber": "BATCH-001"
}
```

**Sales Transactions**:

```bash
# Customer buys 1 box (bulk discount)
POST /api/salesorders
{
  "shopId": "downtown-pharmacy",
  "items": [{"drugId": "paracetamol-id", "quantity": 1, "packagingLevel": "Box"}]
}
# Result: $8.00 (vs $10 if bought as 5 strips)

# Customer buys 2 strips (retail)
POST /api/salesorders
{
  "shopId": "downtown-pharmacy",
  "items": [{"drugId": "paracetamol-id", "quantity": 2, "packagingLevel": "Strip"}]
}
# Result: $4.00
```

### Scenario 2: Multi-Shop Pricing Comparison

**Shop A (Downtown - Premium)**:

```json
PUT /api/inventory/shops/downtown-pharmacy/drugs/paracetamol-id/packaging-pricing
{
  "Box": 8.50,
  "Strip": 2.25,
  "Tablet": 0.25
}
```

**Shop B (Suburban - Competitive)**:

```json
PUT /api/inventory/shops/suburban-pharmacy/drugs/paracetamol-id/packaging-pricing
{
  "Box": 7.75,
  "Strip": 1.95,
  "Tablet": 0.20
}
```

**Same Drug, Different Shop Prices**:

- Downtown Box: $8.50
- Suburban Box: $7.75
- Downtown Strip: $2.25
- Suburban Strip: $1.95

## Data Relationships

### Entity Relationships

```
Drug (1) ────→ ShopInventory (Many)
    │              │
    │              │
    └───→ PackagingLevels (Owned)
                   │
                   └───→ ShopPricing (Owned)
                          │
                          └───→ PackagingLevelPrices (Dictionary)
```

### Key Business Rules

1. **Shared Drug Catalog**: All shops reference the same drug definitions
2. **Shop-Specific Inventory**: Each shop maintains its own stock levels
3. **Shop-Specific Pricing**: Each shop sets its own prices for packaging levels
4. **Base Unit Tracking**: All stock calculations happen in base units (tablets)
5. **Auto-Pricing**: Sales orders automatically use shop-specific packaging prices
6. **Stock Reduction**: Sales automatically reduce inventory at base unit level

### Data Integrity

- **Drug Catalog**: Immutable once created (admin only)
- **Shop Inventory**: Shop-specific, can be modified by shop managers
- **Shop Pricing**: Shop-specific, can be updated by shop managers
- **Sales Orders**: Immutable once created, track all transactions
- **Stock Levels**: Always accurate at base unit level

## Benefits

### For Pharmacy Chains

- **Centralized Drug Management**: Single source of truth for drug information
- **Shop Autonomy**: Each location can set competitive pricing
- **Accurate Inventory**: Real-time stock tracking across all locations
- **Bulk Discounts**: Encourage larger purchases with quantity pricing

### For Individual Pharmacies

- **Shared Drug Database**: Access to verified drug information
- **Flexible Pricing**: Set prices based on local market conditions
- **Automated POS**: Fast checkout with auto-pricing
- **Stock Optimization**: Better inventory management and ordering

### For Customers

- **Consistent Experience**: Same drugs across pharmacy locations
- **Price Transparency**: Clear pricing at different packaging levels
- **Bulk Savings**: Discounts for larger purchases
- **Convenient Options**: Buy by tablet, strip, or box as needed

## Technical Implementation

### Architecture

- **Clean Architecture**: Domain → Application → Infrastructure layers
- **CQRS Pattern**: Separate read/write operations
- **Entity Framework Core**: ORM with PostgreSQL
- **MediatR**: In-process messaging for commands/queries
- **JWT Authentication**: Multi-tenant security

### Key Technologies

- **C# .NET 6.0**: Backend API
- **PostgreSQL**: Database with JSONB support
- **Entity Framework Core**: ORM
- **MediatR**: CQRS implementation
- **FluentValidation**: Request validation
- **Swagger/OpenAPI**: API documentation

### Database Schema

```sql
-- Shared drug catalog
CREATE TABLE Drugs (
    Id VARCHAR(50) PRIMARY KEY,
    BrandName VARCHAR(200),
    GenericName VARCHAR(200),
    PackagingInfo JSONB
);

-- Shop-specific inventory
CREATE TABLE ShopInventory (
    Id VARCHAR(50) PRIMARY KEY,
    ShopId VARCHAR(50),
    DrugId VARCHAR(50),
    TotalStock INT,
    ShopPricing JSONB,  -- Contains packaging prices
    Batches JSONB
);

-- Sales transactions
CREATE TABLE SalesOrders (
    Id VARCHAR(50) PRIMARY KEY,
    OrderNumber VARCHAR(50),
    ShopId VARCHAR(50),
    Items JSONB,
    TotalAmount DECIMAL(18,2)
);
```

This system provides a complete multi-tenant pharmacy solution with flexible packaging-level pricing and accurate inventory management.</content>
<parameter name="filePath">c:\Users\pc\Documents\My Projects\pos_system_api\PACKAGING_SYSTEM_GUIDE.md
