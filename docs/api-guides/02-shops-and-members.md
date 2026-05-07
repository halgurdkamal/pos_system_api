# 2. Shops & Members

**What it's for**: defining the *tenant* — the pharmacy branch — and its staff. Almost every other endpoint takes a `shopId` in the path or query; those IDs come from here.

**Use this when**: a new branch opens (`/shops/create-own`), the manager hires a cashier (`POST /shops/{id}/members`), the printer model changes (`/hardware-config`), or VAT registration changes the receipt header (`/receipt-config`). Don't use this guide for stock or pricing — those are per-`ShopInventory`, not per-`Shop` (see [05](./05-inventory-and-stock.md)).

A **shop** is the multi-tenant boundary: inventory, pricing, sales orders, and staff are all scoped to it. A **shop member** (`ShopUser`) joins a `User` to a `Shop` with one role and a set of permissions.

## Endpoint summary

### Shops

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/api/shops/create-own` | bearer | Any user creates a shop they own |
| POST | `/api/shops` | `AdminOnly` | SuperAdmin registers a shop on behalf of someone |
| GET  | `/api/shops` | bearer | Paginated list, filter `?status=Active` |
| GET  | `/api/shops/{id}` | bearer | One shop |
| GET  | `/api/shops/search?q=...` | bearer | Search by name |
| PUT  | `/api/shops/{id}` | `ShopOwnerOrAdmin` | Update profile |
| PUT  | `/api/shops/{id}/receipt-config` | bearer ⚠ | Receipt branding & layout |
| PUT  | `/api/shops/{id}/hardware-config` | bearer ⚠ | Printer / scanner settings |

> ⚠ **Security gap G-2** — `receipt-config` and `hardware-config` are protected only by `[Authorize]` (any logged-in user); the `ShopOwnerOrAdmin` policy is missing. Full details and fix in [`99-known-gaps.md#g-2`](./99-known-gaps.md#g-2-shopupdatereceiptconfig-and-updatehardwareconfig-lack-the-shopowneroradmin-policy).

### Members

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET    | `/api/shops/{shopId}/members?activeOnly=true` | bearer | List staff |
| POST   | `/api/shops/{shopId}/members` | bearer | Add a user as cashier/manager/… |
| PUT    | `/api/shops/{shopId}/members/{userId}` | bearer | Change role / permissions |
| DELETE | `/api/shops/{shopId}/members/{userId}` | bearer | Remove from shop |
| GET    | `/api/users/{userId}/shops` | bearer | All shops a user belongs to |

> Member endpoints carry `[Authorize]` only — **the access check happens inside the handler** (it throws `UnauthorizedAccessException`, which the controller converts to `403 Forbidden`). Practically: only owners/managers of the target shop or a SuperAdmin succeed.

## Step-by-step

### Step 1 — Create the shop (owner-self-serve)

```http
POST /api/shops/create-own
Authorization: Bearer …

{
  "shopName": "Main Branch Pharmacy",
  "phoneNumber": "+9647500000000",
  "email": "main@pharma.com",
  "address": "123 Salim St",
  "city": "Erbil",
  "state": "Kurdistan",
  "country": "Iraq",
  "postalCode": "44001",
  "licenseNumber": "PH-2026-001",
  "taxId": "TAX-998877",
  "description": "24/7 community pharmacy"
}
```

Response `201`:

```json
{
  "id": "SHOP-AB12CD34",
  "shopName": "Main Branch Pharmacy",
  "status": "Active",
  "ownerUserId": "USER-9F3A2C1B",
  "createdAt": "2026-05-08T09:30:00Z"
}
```

The caller's user is automatically inserted as a `ShopUser` with `Role=Owner`, `IsOwner=true`, and a curated full permission set (37 permissions — every one in the `Permission` enum). **You must re-login (or refresh) to receive the new `shop:{shopId}:*` claims** — old tokens won't see the shop.

Quirks of `create-own` worth knowing (from `CreateOwnShopCommandHandler`):

| Field passed | What actually happens |
|--------------|-----------------------|
| `licenseNumber` (omitted) | Auto-generated as `TEMP-{8-char-guid}`. Replace later via `PUT /api/shops/{id}`. |
| `licenseNumber` (provided) | Validated globally unique — duplicate ⇒ `400 "Shop with license number '…' already exists"`. |
| `country` (omitted) | Defaults to `"Iraq"`. |
| `taxId` | Stored in `vatRegistrationNumber`. |
| `description` | Stored in `pharmacyRegistrationNumber` and **truncated to 100 chars**. (Yes, this field reuse is unusual — see [`CONTROLLER_REFACTOR_BACKLOG`](../CONTROLLER_REFACTOR_BACKLOG.md).) |
| `legalName` | Implicitly set equal to `shopName`. Use `PUT /api/shops/{id}` to set a different legal name. |
| `status` | Always `Active` on creation. |

### Step 2 — Add a cashier

```http
POST /api/shops/SHOP-AB12CD34/members
Authorization: Bearer …(owner token)…

{
  "userId": "USER-77AABBCC",
  "role": "Cashier",
  "isOwner": false,
  "customPermissions": null,
  "notes": "Hired May 2026"
}
```

Roles map to default permission sets (see [auth guide](./01-authentication.md)). Choose `"Custom"` and pass `customPermissions: ["ProcessSales", "ViewInventory"]` for a bespoke set.

Response `201`:

```json
{
  "id": "SHOPUSER-1A2B3C4D",
  "shopId": "SHOP-AB12CD34",
  "userId": "USER-77AABBCC",
  "role": "Cashier",
  "permissions": [
    "ProcessSales", "ViewSales", "RefundSales",
    "ApplyDiscounts", "ViewInventory", "CloseCashRegister"
  ],
  "isOwner": false,
  "isActive": true,
  "joinedDate": "2026-05-08T09:35:00Z"
}
```

### Step 3 — Configure receipt printing

```http
PUT /api/shops/SHOP-AB12CD34/receipt-config

{
  "logoUrl": "https://cdn.pharma.com/logo.png",
  "headerText": "Main Branch Pharmacy — Erbil",
  "footerText": "Thank you! No returns after 14 days.",
  "showLogo": true,
  "showQrCode": true,
  "showTaxBreakdown": true,
  "showVatNumber": true,
  "showPharmacyLicense": true,
  "vatRegistrationNumber": "VAT-1122",
  "pharmacyLicenseNumber": "PH-2026-001",
  "language": "en-US",
  "paperType": "Thermal80mm"
}
```

These fields are merged into every PDF receipt — see [07 — Barcodes & PDF](./07-barcodes-and-pdf.md).

### Step 4 — Update / remove members

Promote to manager:

```http
PUT /api/shops/SHOP-AB12CD34/members/USER-77AABBCC

{ "role": "Manager", "customPermissions": null }
```

Remove (soft — sets `IsActive=false`, preserves audit history):

```http
DELETE /api/shops/SHOP-AB12CD34/members/USER-77AABBCC
```

## Status lifecycle

`Shop.Status` enum:

```
Active ─── (suspended by admin) ──► Suspended ──┐
   │                                            │
   └────────── (closed by owner) ────────────► Closed
```

`Suspended` shops keep their data but block sale/inventory operations. `Closed` is soft — records remain for reporting.

## Common pitfalls

- **403 after creating a shop** — your old JWT lacks the new shop claims. Call `/api/auth/refresh`.
- **Adding a SuperAdmin as a member is unnecessary** — they implicitly pass `ShopAccess` for any shop.
- **Don't store passwords or PII in `notes`** — that field appears in member listings.

## Best practices

### Security
- **Add `[Authorize(Policy = "ShopOwnerOrAdmin")]` to `/{id}/receipt-config` and `/{id}/hardware-config`** before deploying — they currently accept any authenticated user. Pasted from the warning above for emphasis: this is the most likely-to-bite gap in this controller.
- **Treat `licenseNumber` as PII for compliance audits.** Don't expose it in cross-shop listings to non-admins.
- **Review `customPermissions` carefully when role = `Custom`.** Defaults are zero, so a typo in the permission name silently grants nothing — but typos in the *role* string fall back to behaviour you didn't intend.
- **Soft-delete is the default for member removal** (`DELETE /members/{userId}` flips `IsActive=false`). That's correct — never hard-delete; sales orders reference `cashierId`.
- **Per-shop role claims live in the JWT** (`shop:{shopId}:role`, `shop:{shopId}:permission`). Changing a role in DB doesn't invalidate already-issued tokens — the user still has the old privileges until the token expires or refreshes. Force `/refresh` after privilege changes if it matters.

### Performance
- **`GET /api/shops/{id}` loads the full shop** including `receiptConfig` and `hardwareConfig` — fine for the till on startup, expensive for a list view. Use `GET /api/shops` (paginated, summary) for grids.
- **`GetShopMembers` returns the full member list in one shot** (no pagination as of writing). Acceptable for typical pharmacy team sizes; if a shop ever has 100+ members, add filtering server-side rather than fetching all.
- **The login response embeds every shop's full `shopDetails`.** A user belonging to many shops gets a heavy payload — encourage one user → one or two shops, not dozens.

### Correctness
- **Always re-issue the JWT after `create-own`** (call `/refresh`). The old token has no `shop:{newShopId}:*` claims; the next call to a `ShopAccess` endpoint returns 403 even though the user just made the shop.
- **`legalName` ≠ `shopName` for compliant receipts.** `create-own` sets them equal; update `legalName` via `PUT /api/shops/{id}` if regulator names differ.
- **`description` is repurposed to `pharmacyRegistrationNumber` and truncated at 100 chars.** Don't put marketing copy there; put a real registration number.
- **One `IsOwner=true` per shop is the contract**, but the API doesn't enforce uniqueness. Keep this invariant in your client UI: promote-and-demote in the same call, not as two separate edits.

### Clean code
- **In your client, model the active shop as a top-level navigation concept**, not a field on the user. The user is a person; the active shop is a context.
- **Use `Custom` role only for genuine exceptions.** Pre-defined roles (Cashier/Manager/InventoryClerk/Viewer) carry well-known permission sets — a long-lived audit trail of "what could this person do?" is easier to read with named roles than custom permission lists.
- **`receipt-config` and `hardware-config` are separate endpoints for a reason** — receipt is a marketing concern (logo, footer), hardware is an ops concern (printer model, IP). Keep them on different screens.

## Next

→ [03 — Items & Catalog](./03-items-and-catalog.md)
