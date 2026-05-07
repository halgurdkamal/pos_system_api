# Controller refactor backlog

Living list of controllers that still have logic leaks (DB access, DTO assembly, business logic, try/catch noise) inside HTTP actions instead of MediatR handlers. Generated from a full audit on 2026-05-07.

## Pattern

Each refactor follows the same template (see `PdfController` and `AdminController` for examples already done):

1. **Identify the leak**: DbContext access, multi-step orchestration, DTO mapping, or private helper methods doing data work.
2. **Create CQRS commands/queries** under `src/Core/Application/<Feature>/{Commands|Queries}/<Verb><Noun>/`.
3. **Move the logic** into the handler. Controller becomes thin: dispatch + return result.
4. **Remove try/catch noise** — global exception middleware handles it.
5. **Add a handler test** using `TestDbContextFactory.Create()` for DB-dependent handlers.

## Done

- ✅ `PdfController` — extracted `GenerateReceiptPdfQuery` (Phase 2 step 1)
- ✅ `AdminController` — extracted `GetDatabaseStatsQuery`, injected seeders via DI, removed try/catch (Phase 2 step 3, partial)

## Top remaining candidates (worst first)

### 1. DrugsController (633 LOC, **very leaky**)

Biggest offender. ~180 LOC of business logic in the controller.

- `CreateDrugInternalAsync()` (lines 192-295) — 104-line method building entities and saving via `_context` directly
- `BuildDrugDetailDto()` (lines 526-626) — 100+ line DTO assembly
- `MapToDto()` / `MapToListItemDto()` (lines 335-362) — should live in query handlers
- `GetPagedDrugsAsync()` (lines 297-320) — pagination logic with raw EF queries
- Direct `_context.Drugs.Include(...).FirstOrDefaultAsync(...)` throughout

**Approach**: This is multi-session work. Suggested order:
1. Extract `BuildDrugDetailDto` → `GetDrugDetailQuery` handler (read path, low risk)
2. Extract `MapToDto` / `MapToListItemDto` → into the existing list query handlers
3. Extract `CreateDrugInternalAsync` → `CreateDrugCommand` handler (write path, needs validator + tests)
4. Replace `_context` injection with `IDrugRepository` (already exists)

### 2. PurchaseOrdersController (351 LOC, **leaky**)

Three load-mutate-save actions bypass MediatR:

- `ConfirmPurchaseOrder` (lines 141-154)
- `CancelPurchaseOrder` (lines 184-200)
- `MarkAsPaid` (lines 207-222)

All follow: load via repository → call domain method → save. This sequence belongs in command handlers.

**Approach**: Three small command handlers (`ConfirmPurchaseOrderCommand`, `CancelPurchaseOrderCommand`, `MarkPurchaseOrderAsPaidCommand`). Plus extract `MapToSummaryDto` (lines 307-324). Maybe 2-3 hours of work.

### 3. SalesOrdersController (411 LOC, **leaky**)

Same pattern as PurchaseOrders, slightly worse:

- `ConfirmSalesOrder`, `CompleteSalesOrder`, `CancelSalesOrder`, `RefundSalesOrder` — load-mutate-save
- Cashier performance calculation inline (lines 237-263) — multi-entity arithmetic that belongs in a query handler

**Approach**: Four command handlers + extract `GetCashierPerformanceQuery`. Similar effort to PurchaseOrders.

### 4. InventoryController (661 LOC, **moderate**)

Architectural smell rather than business-logic leak:

- Lines 603-659 — private factory methods like `CreateAddStockHandler()` that `new` up handlers manually, bypassing DI
- `GetPackagingPricing` (lines 340-367) — inline DTO assembly

**Approach**: Replace handler factories with proper DI registration; extract pricing DTO mapping into a query handler.

### 5. AuthController (181 LOC, **moderate**)

- `GetCurrentUser()` (lines 120-179) — manual DTO construction with nested shop list
- Three try/catch blocks returning specific error codes (lines 33-61)

**Approach**: Move `GetCurrentUser` body into `GetCurrentUserQuery` handler; rely on global middleware for the error mapping (or use a problem-details mapper).

## Lower priority

The remaining controllers (Barcodes, Categories, InventoryAlerts, InventoryReports, ShopMembers, Shops, Stock*, Suppliers) are mostly thin. They have try/catch noise that could be removed when the global exception middleware is verified to handle their error cases, but no business logic leaks worth dedicated refactor sessions.

## Order of attack (recommended)

1. **AuthController** next — small, isolated, sets up the pattern for read-side queries with auth context
2. **PurchaseOrdersController** — three small command handlers, very repetitive (good template)
3. **SalesOrdersController** — apply the PurchaseOrders template
4. **InventoryController** — fix DI smell first, extract pricing query second
5. **DrugsController** — saved for last because it's the biggest. Tackle in 3-4 sub-sessions, not one go.
