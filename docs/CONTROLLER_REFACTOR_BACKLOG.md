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
- ✅ `AuthController` — extracted `GetCurrentUserQuery`, removed redundant try/catch blocks (global middleware already handles `UnauthorizedAccessException` / `InvalidOperationException` / `ArgumentException`) (Phase 3 step 1)
- ✅ `PurchaseOrdersController` — extracted Confirm/Cancel/MarkAsPaid commands and 4 list/analytics queries (paged, pending, overdue payments, supplier performance); removed direct `IPurchaseOrderRepository` injection; introduced `PurchaseOrderMappers` shared helper (Phase 3 step 2)
- ✅ `SalesOrdersController` — extracted Confirm/Complete/Cancel/Refund commands and 5 list/analytics queries (paged, today's, cashier performance, sales-by-payment-method, top-selling drugs); removed direct `ISalesOrderRepository` injection; introduced `SalesOrderMappers` shared helper (Phase 3 step 3)
- ✅ `InventoryController` — replaced 16 manual `CreateXxxHandler()` factory methods with `IMediator.Send(...)`; removed all 8 repository/service injections; extracted inline DTO assembly into new `GetPackagingPricingQuery`; wired existing `GetEffectivePackagingQuery` into the controller; removed redundant try/catch (global middleware handles `KeyNotFoundException` / `InvalidOperationException` / `ArgumentException`); added `CancellationToken` parameter binding (Phase 3 step 4)
- ✅ `DrugsController` read paths (Phase 3 step 5a) — wired existing `GetDrugQuery`, `GetDrugListQuery`, `GetDrugListEnhancedQuery`, `GetDrugDetailQuery` handlers (already existed in the codebase but the controller wasn't using them). Removed `MapToDto` / `MapToListItemDto` / `BuildDrugDetailDto` / `EnsurePagedResultConsistency` / `GetPagedDrugsAsync` / `ApplyBrowseFilters` private helpers — all duplicated in the handlers. `CreateDrug` write path still uses `_context` directly and is intentionally untouched until step 5b/5c.

## Top remaining candidates (worst first)

### 0. ~~AuthController, PurchaseOrdersController, SalesOrdersController, InventoryController~~ — done

### 1. DrugsController (write path remaining, ~250 LOC)

Read paths done in Phase 3.5a. The write path (`CreateDrug` + 6 supporting helpers) still injects `ApplicationDbContext` directly. To finish:

- **Step 5b**: Move `CreateDrugInternalAsync` body into a `CreateDrugCommand` + handler. Move `MapFormulation` / `MapBasePricing` / `MapRegulatory` / `BuildPackagingInfo` / `MapToDto` / `GenerateDrugId` along with it. Add a FluentValidation `CreateDrugCommandValidator` so the inline argument-checks in `CreateDrugInternalAsync` become declarative validation.
- **Step 5c**: Remove the controller's `ApplicationDbContext` injection entirely. Add a few handler-level integration tests using `TestDbContextFactory.Create()` to lock in the create flow.

## Lower priority

The remaining controllers (Barcodes, Categories, InventoryAlerts, InventoryReports, ShopMembers, Shops, Stock*, Suppliers) are mostly thin. They have try/catch noise that could be removed when the global exception middleware is verified to handle their error cases, but no business logic leaks worth dedicated refactor sessions.

## Order of attack (recommended)

1. **DrugsController write path** — split into 5b (CreateDrugCommand handler + validator) and 5c (remove ApplicationDbContext injection + add integration tests).
