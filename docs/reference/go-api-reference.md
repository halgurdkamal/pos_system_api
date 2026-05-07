# Go Drug API

A REST API for managing a drug catalog in a pharmacy POS system, built with Go and PostgreSQL.

## Overview

This API provides comprehensive drug catalog management for pharmacy Point-of-Sale systems. It handles drug information including formulations, pricing, regulatory data, and packaging details.

## Features

- **Drug Catalog Management**: CRUD operations for pharmaceutical products
- **Advanced Search & Filtering**: Browse drugs by category, search terms, and stock status
- **Detailed Drug Information**: Complete drug profiles with regulatory and formulation data
- **Pagination Support**: Efficient handling of large drug catalogs
- **PostgreSQL Integration**: Robust database operations with connection pooling
- **RESTful Design**: Clean API endpoints following REST principles

## Tech Stack

- **Language**: Go 1.21+
- **Database**: PostgreSQL
- **Router**: Gorilla Mux
- **Driver**: lib/pq PostgreSQL driver
- **Architecture**: REST API with layered design

## Quick Start

### Prerequisites

- Go 1.21 or higher
- PostgreSQL database
- Git

### Installation

1. Clone the repository:

```bash
git clone <repository-url>
cd go-drug-api
```

2. Install dependencies:

```bash
go mod download
```

3. Set up environment variables (optional):

```bash
export DB_HOST=your-host
export DB_PORT=5432
export DB_USER=your-user
export DB_PASSWORD=your-password
export DB_NAME=your-database
export DB_SSLMODE=require
```

4. Run the application:

```bash
go run main.go
```

The API will start on `http://localhost:8080`

## API Endpoints

### Drug Management

#### Create Drug

```http
POST /api/drugs
Content-Type: application/json

{
  "drugId": "ASP001",
  "barcode": "1234567890123",
  "barcodeType": "EAN13",
  "brandName": "Aspirin",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "PharmaCorp",
  "originCountry": "Germany",
  "categoryName": "Pain Relief",
  "categoryId": "PAIN001",
  "description": "Pain relief medication",
  "formulation": {
    "form": "Tablet",
    "strength": "100",
    "unit": "mg",
    "route": "Oral"
  },
  "basePricing": {
    "suggestedRetailPrice": 5.99,
    "currency": "USD"
  },
  "regulatory": {
    "requiresPrescription": false,
    "controlledSubstance": false,
    "approvalNumber": "FDA12345",
    "expiryDate": "2025-12-31"
  },
  "packagingInfo": {
    "primaryPack": "Bottle",
    "secondaryPack": "Box",
    "defaultSellUnit": "Tablet"
  }
}
```

#### Get Drugs (Paginated)

```http
GET /api/drugs?page=1&limit=20
```

#### Get Single Drug

```http
GET /api/drugs/{id}
```

#### Browse Drugs with Filters

```http
GET /api/drugs/browse?page=1&limit=20&searchTerm=aspirin&category=pain&inStock=true
```

#### Get Drug Details

```http
GET /api/drugs/{id}/detail
```

## Data Models

### Drug

- `drugId`: Unique identifier
- `barcode`: Product barcode
- `barcodeType`: Type of barcode (EAN13, UPC, etc.)
- `brandName`: Commercial brand name
- `genericName`: Generic chemical name
- `manufacturer`: Manufacturing company
- `originCountry`: Country of origin
- `categoryName`: Drug category name
- `categoryId`: Category identifier
- `imageUrls`: Array of image URLs
- `description`: Product description
- `sideEffects`: Array of side effects
- `interactionNotes`: Array of drug interaction notes
- `tags`: Array of tags for categorization
- `relatedDrugs`: Array of related drug IDs
- `formulation`: Drug formulation details
- `basePricing`: Suggested pricing information
- `regulatory`: Regulatory and compliance data
- `packagingInfo`: Packaging hierarchy information

### Formulation

- `form`: Dosage form (Tablet, Capsule, Injection, etc.)
- `strength`: Drug strength
- `unit`: Unit of measurement (mg, ml, etc.)
- `route`: Administration route (Oral, IV, Topical, etc.)

### BasePricing

- `suggestedRetailPrice`: Recommended retail price
- `currency`: Currency code (USD, EUR, etc.)

### Regulatory

- `requiresPrescription`: Whether prescription is required
- `controlledSubstance`: Whether it's a controlled substance
- `schedule`: Controlled substance schedule
- `approvalNumber`: Regulatory approval number
- `expiryDate`: Expiry date of approval

### PackagingInfo

- `primaryPack`: Primary packaging (Bottle, Blister, etc.)
- `secondaryPack`: Secondary packaging (Box, Carton, etc.)
- `defaultSellUnit`: Default unit for selling

## Database Schema

The API expects a PostgreSQL database with a `drugs` table containing the drug catalog data. The schema should include columns for all the fields mentioned in the Drug model.

## Error Handling

The API returns appropriate HTTP status codes:

- `200`: Success
- `201`: Created
- `400`: Bad Request
- `404`: Not Found
- `409`: Conflict (duplicate data)
- `500`: Internal Server Error

Error responses include a JSON object with an `error` field containing the error message.

## Development

### Running Tests

```bash
go test ./...
```

### Building for Production

```bash
go build -o drug-api main.go
```

## Support

For support and questions, please open an issue in the GitHub repository.</content>
<filePath">c:\Users\pc\Documents\My Projects\pos_system_api\go_readme.md
