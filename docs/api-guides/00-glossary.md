# 0. Glossary

Short definitions for terms that appear across the guides without explanation. Skim this once, then refer back as needed.

## Inventory & stock terms

**Active batch** — a `Batch` whose `status = Active` and `quantityOnHand > 0`. Only active batches participate in FIFO depletion or are reported as "available".

**Backorder** — selling something the shop doesn't currently have. Not supported by this API; sales fail at `/reduce` time if stock is insufficient.

**Base unit** — the smallest unit of a drug (often "Tablet"). Defined by `packagingInfo.baseUnit` and `baseUnitDisplayName`. All quantities ultimately convert to base units for inventory math.

**Batch** — one physical lot of a drug at a shop, with its own batch number, supplier, expiry, purchase price, and selling price. Lives inside `ShopInventory.batches[]` (not a top-level entity).

**Cycle count** — counting just one drug or one section of the warehouse, on a rolling schedule. The opposite of an annual full count. The `/api/stock-counts/*` endpoints support both.

**Dead stock** — stock that hasn't moved (no sale, no transfer) for a long time. The `/inventory-reports/.../dead-stock?daysThreshold=N` report flags it.

**Default sell unit** — the packaging level the cashier defaults to. Defined by `packagingLevel.isDefault: true`. Should be set on exactly one level per drug.

**EAN-13** — European Article Number, 13 digits — the barcode standard most consumer products use. The default `barcodeType` for drugs.

**FEFO** (First-Expired, First-Out) — replenishment / display policy: push the soonest-to-expire stock onto the shelf first. Used by `move-to-floor` when `batchNumber` is null.

**FIFO** (First-In, First-Out) — depletion policy: when reducing stock, consume the oldest-received batch first. Used by `ShopInventory.ReduceStock(qty)` and therefore by every `/reduce` call.

**Packaging hierarchy / packaging level** — the chain Tablet → Strip → Box. Each level is a `packagingLevel` with a `levelNumber`, `unitName`, `baseUnitQuantity`, `quantityPerParent`, and flags (`isSellable`, `isBreakable`, `isDefault`).

**Quarantined stock** — held for inspection (damaged, recalled, expired, returned). Not sellable. Tracked separately as `quarantinedStock` on `ShopInventory`.

**Reorder point** — the threshold at which a low-stock alert fires. Set per drug per shop via `PUT /api/inventory/.../reorder-point`. Default is 50 base units when `AddStock` creates the inventory row.

**Reserved stock** — set aside for a pending sale or transfer. Tracked separately from on-hand. Reduces what's available to other transactions but doesn't reduce `totalStock`.

**Shop floor / storage** — physical zones inside the same shop. `shopFloorStock` is on display where the cashier picks from; `storageStock` is the back room reserve. Move between them with `move-to-floor` / `move-to-storage`.

**Shrinkage** — quantity lost to theft, damage, mis-counting. Recorded as a `StockAdjustment` with `adjustmentType: "Theft"`, `"Damage"`, or `"Correction"`.

**SKU** (Stock Keeping Unit) — a unique sellable product. In this API, one `Drug` is one SKU.

**Variance** — `physicalQuantity − systemQuantity` from a stock count. Non-zero variance auto-creates a `StockAdjustment` to reconcile.

## Sales & commerce terms

**ABC analysis** — splitting items by cumulative value: A = top 70%, B = next 20%, C = last 10%. Helps prioritise stock-management attention. Available via `/inventory-reports/.../abc-analysis`.

**baseUnitsConsumed** — for a sale of N at packaging level "Box" (where 1 Box = 100 tablets), `baseUnitsConsumed = 100 × N`. Stored on each `SalesOrderItem` so reductions know how many base units to take.

**CIF / shipping cost** — the freight charge on a purchase order. Stored as `shippingCost` on `PurchaseOrder`, added into `totalAmount`.

**Net 30 / Net 15** — invoice payment terms. "Net 30" = pay within 30 days of invoice date. Encoded in `PaymentTerms` enum.

**PO** (Purchase Order) — a buying-side record: an order placed with a supplier. Different from a `SalesOrder` (selling-side, customer transaction).

**Receipt (PO)** — a delivery event against a PO line. Each `PurchaseOrderItem.receipts[]` entry records one delivery: quantity, batch number, expiry, who received it.

**Tender** — money taken in payment. The current API supports a single `paymentMethod` per order. Split tender (cash + card) is not modelled separately.

## Architecture & .NET terms

**CQRS** (Command-Query Responsibility Segregation) — the pattern this codebase uses: writes go through a `*Command` + `*CommandHandler` (state-changing), reads go through a `*Query` + `*QueryHandler` (no side effects). MediatR dispatches both.

**DTO** (Data Transfer Object) — a flat shape passed between layers or to/from the API. E.g. `DrugDto`, `SalesOrderDto`. Distinct from the domain entity.

**EF Core** (Entity Framework Core) — the .NET ORM used here for PostgreSQL access.

**FluentValidation** — the library used for request validation. Validators run in a MediatR pipeline behaviour before each handler.

**MediatR** — the in-process message dispatcher. Controllers send commands/queries; handlers consume them. Decouples HTTP from business logic.

**Mediator pipeline** — the chain of behaviours (validation, logging, transaction management) that wraps every command/query. New behaviours can be inserted without touching handlers.

**Multi-tenant** — one deployment serves multiple isolated tenants. Here a "tenant" ≈ a `Shop`. Tenant boundary is enforced by the `ShopAccess` policy.

**Soft delete** — flagging a row inactive (`isActive = false`) instead of physically removing it. The API soft-deletes suppliers, shop members, and categories.

**Value object** — an immutable record (no identity of its own) embedded in an entity. `Batch`, `Address`, `Contact`, `Formulation`, `BasePricing` are value objects.

## Auth & security terms

**Bearer token** — the JWT access token, sent on every protected request as `Authorization: Bearer <token>`.

**Claim** — a name-value pair inside a JWT. This API uses standard claims (`nameidentifier`, `email`, `role`) plus custom ones (`shopIds`, `shop:{id}:role`, `shop:{id}:permission`).

**JWT** (JSON Web Token) — a signed, base64-encoded token containing claims. Issued by `/login`, validated on every protected request.

**PII** (Personally Identifiable Information) — name, phone, email, prescription number, license number. Treat as sensitive: don't log at info level, restrict who can list/export.

**PAN** (Primary Account Number) — a credit/debit card number. Never store, log, or accept full PAN — use last-4 or a processor token.

**RBAC** (Role-Based Access Control) — granting permissions through named roles (Owner, Manager, Cashier…). Layered with explicit per-permission flags via `customPermissions`.

**Refresh token** — a 64-byte random string used to mint a new access token after the access token expires. Rotates on each `/refresh` call.

**Rotation** — issuing a new credential and invalidating the old one. Refresh tokens rotate every refresh; secrets should rotate periodically.

**SSRF** (Server-Side Request Forgery) — when an attacker tricks your server into fetching an arbitrary URL of their choice (e.g. via `logoUrl`). Validate URLs server-side.

**XSS** (Cross-Site Scripting) — when attacker-controlled content runs as JS in another user's browser. The risk surface here is anywhere image URLs, descriptions, or notes are rendered without escaping.

## Domain / pharmacy-specific terms

**Controlled substance** — a drug with regulatory restrictions on prescription, sale, or storage. Flagged via `regulatory.controlSchedule` (e.g. "Schedule IV") and `regulatory.isHighRisk`.

**Generic name** — the active ingredient (e.g. "Amoxicillin"). One generic can have many brand names.

**Brand name** — the marketed product name (e.g. "Amoxil 500"). What customers ask for; what cashiers see.

**Formulation** — `form` (Capsule / Tablet / Liquid…), `strength` ("500mg"), `routeOfAdministration` ("Oral"). Identifies one specific presentation of a drug.

**Prescription required** — an Rx flag. Sales of these drugs may require capturing `prescriptionNumber` on the `SalesOrder` and showing an Rx indicator on the receipt.

---

If a term used in the guides isn't here, it's worth opening a PR to add it. The aim is a one-stop reference for any reader, regardless of background.
