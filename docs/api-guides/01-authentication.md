# 1. Authentication

**What it's for**: every other endpoint in the system needs a token from here. This is the gatekeeper — anything in shops, inventory, sales, or PDF requires a valid `Authorization: Bearer …` header minted by `/login`.

**Use this when**: a user needs to sign in to the till / dashboard, or your script needs an access token. **Don't** use `/register` to bulk-import users — it's an unauthenticated public endpoint and lacks role-elevation guards (see warnings below).

How users sign in, get a JWT, and what their token allows them to do. The API uses **JWT bearer tokens** with a refresh-token rotation, plus a two-tier role model: a system role on the user (`SuperAdmin` / `User`) and per-shop roles on a separate `ShopUser` record.

## Endpoint summary

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/api/auth/register` | public | Create a user |
| POST | `/api/auth/login` | public | Get access + refresh token |
| POST | `/api/auth/refresh` | public | Swap an expired access token for a new one |
| GET  | `/api/auth/me` | bearer | Profile of the caller |
| POST | `/api/admin/seed` | public | Seed sample data (dev) |
| POST | `/api/admin/seed-users` | `AdminOnly` | Seed test users |
| GET  | `/api/admin/stats` | `AdminOnly` | DB stats |
| DELETE | `/api/admin/clear` | `AdminOnly` | Wipe seeded data |

## Step-by-step: first-time user

### Step 1 — Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "jane",
  "email": "jane@pharma.com",
  "password": "S3cret!Pa$$word",
  "fullName": "Jane Doe",
  "phone": "+9647500000001",
  "shopId": null
}
```

Response `201 Created`:

```json
{
  "id": "USER-9F3A2C1B",
  "username": "jane",
  "email": "jane@pharma.com",
  "fullName": "Jane Doe",
  "systemRole": "User",
  "shops": [],
  "isActive": true,
  "isEmailVerified": false,
  "lastLoginAt": null
}
```

Notes:
- The endpoint always creates a `SystemRole.User` account. The historical `role` field has been removed from the request DTO (commit `0832062`, closing G-3 / F-4) — privilege escalation is intentionally not exposed here. Bootstrap a SuperAdmin via direct DB update (see "Bootstrapping the first SuperAdmin" below) and then promote/demote users via an authenticated admin route.
- The endpoint is still public. Lock it down behind a gateway feature flag, IP allow-list, or remove the route entirely in non-dev environments if you don't want open self-registration.
- `shopId` is **only validated for existence** — it does **not** create a `ShopUser` membership. Use `POST /api/shops/{shopId}/members` after registration to actually grant shop access (see [02 — Shops](./02-shops-and-members.md)).
- Register does not return a token. Call `/login` straight after.
- Password is hashed with PBKDF2 server-side; never sent back.
- Username and email are unique. The check is in `IUserRepository.ExistsAsync` and returns 400 on duplicates.
- `id` is generated server-side as a raw GUID (e.g. `2cb653f1-abc3-4b04-b3ad-ef41b7eb430a`), not the `USER-XXXXXXXX` form earlier examples in this guide use. See Q-11 in [`99-known-gaps.md`](./99-known-gaps.md#q-11-id-prefix-scheme-is-inconsistent-across-entities).
- `createdBy` on the new user is hard-coded `"System"` for self-registration (see `RegisterCommandHandler`).

### Step 2 — Log in

```http
POST /api/auth/login
Content-Type: application/json

{
  "identifier": "jane",
  "password": "S3cret!Pa$$word"
}
```

`identifier` accepts username, email, or phone.

Response `200 OK`:

```json
{
  "accessToken": "eyJhbGciOi…",
  "refreshToken": "k7Hx…base64…",
  "expiresAt": "2026-05-08T15:30:00Z",
  "user": {
    "id": "USER-9F3A2C1B",
    "username": "jane",
    "systemRole": "User",
    "shops": [
      {
        "shopId": "SHOP-AB12",
        "shopName": "Main Branch",
        "role": "Cashier",
        "permissions": ["ProcessSales", "ViewSales", "ViewInventory"],
        "isOwner": false,
        "isActive": true,
        "joinedDate": "2026-04-01T09:00:00Z"
      }
    ]
  }
}
```

Failure handling (`User.RecordFailedLogin`):
- Each wrong password increments `FailedLoginAttempts` and persists.
- On the 5th failure, `LockedUntil = utcNow + 15 min` is set; further logins return 401 `"Account is locked until …"` until the timer expires.
- A successful login calls `User.UpdateLastLogin`, which **resets both `FailedLoginAttempts = 0` and clears `LockedUntil`**.
- Inactive users (`IsActive = false`) get `"Account is inactive"` regardless of password.

Login response notes:
- `expiresAt` is the **access token** expiry. The refresh token's expiry is not returned — it's stored on the user record (`RefreshTokenExpiryTime`) and silently rotated on each `/refresh`.
- `user.shops` is populated from `User.ShopMemberships` filtered to `IsActive = true`. Each entry includes the entire `shopDetails` object (legal name, address, receipt config, hardware config) — handy for the till to render the storefront without a second round-trip.
- `LastLoginAt` is persisted on every successful login (commit `a0414ec`, closing Q-12). `GET /api/auth/me` issued straight after the login returns the just-stamped timestamp.

### Step 3 — Use the token

Send on every protected request:

```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOi…
```

The token carries these claims (used by authorisation policies):

| Claim | Example | Used by |
|-------|---------|---------|
| `nameidentifier` | `USER-9F3A2C1B` | every handler that needs `currentUserId` |
| `systemRole` | `User` / `SuperAdmin` | `AdminOnly` policy |
| `shopIds` | `SHOP-AB12,SHOP-CD34` | `ShopAccess` policy |
| `shop:{shopId}:role` | `Cashier` | `ShopAccess` policy |
| `shop:{shopId}:isOwner` | `True` / `False` | `ShopOwnerOrAdmin` policy |
| `shop:{shopId}:permission` | repeated, one per permission | per-permission checks |

### Step 4 — Refresh before expiry

Access tokens expire (default 60 min). When you get a `401`, swap them:

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "accessToken": "eyJhbGciOi…(expired)…",
  "refreshToken": "k7Hx…"
}
```

Returns the same shape as `/login`. Refresh tokens default to 7 days and rotate on each use — store the new one and discard the old.

How the handler validates (`JwtTokenService.GetPrincipalFromExpiredToken`):
- Signature must verify against `Jwt:SecretKey` (HMAC-SHA256).
- Expiry, issuer, and audience are **not** checked here — that's the whole point of "from expired".
- `nameidentifier` is read out of the principal, the user is loaded, and the stored `RefreshToken` plus `RefreshTokenExpiryTime` are compared. Any mismatch → 401.
- A new access token + new refresh token are minted, the old refresh token is discarded.

Configurable in `appsettings.json` under `Jwt:`:

| Key | Default |
|-----|---------|
| `Jwt:SecretKey` | required, ≥ 32 chars (validated at startup) |
| `Jwt:Issuer` / `Jwt:Audience` | required for normal token validation |
| `Jwt:AccessTokenExpirationMinutes` | 60 |
| `Jwt:RefreshTokenExpirationDays` | 7 |

## Authorization policies

Three named policies are wired up; controllers use them via `[Authorize(Policy = "...")]`:

| Policy | Allows |
|--------|--------|
| `AdminOnly` | `systemRole == "SuperAdmin"` |
| `ShopOwnerOrAdmin` | SuperAdmin OR any shop membership where `isOwner == true` |
| `ShopAccess` | SuperAdmin OR caller has a role in the `shopId` taken from the route/query |

## Role & permission cheat sheet

System roles (on `User`):
- **SuperAdmin** — every shop, every action, including admin endpoints.
- **User** — nothing until added to a shop.

Shop roles (on `ShopUser`, one per user-shop pair):

| Role | Can do |
|------|--------|
| Owner | Everything inside that shop |
| Manager | Operations + reports; cannot remove staff or trigger backups |
| Cashier | `ProcessSales`, `ViewSales`, `RefundSales`, `ApplyDiscounts`, `ViewInventory`, `CloseCashRegister` |
| InventoryClerk | `ViewInventory`, `AddStock`, `ReduceStock`, `StockAudit`, order receiving |
| Viewer | Read-only across the shop |
| Custom | No defaults; permissions assigned individually via `CustomPermissions` |

Granular permissions live in `src/Core/Domain/Auth/` and include `ProcessSales`, `RefundSales`, `ManageProducts`, `UpdatePricing`, `CreateOrders`, `ReceiveOrders`, `ViewReports`, `BackupData`, `DeleteRecords`, etc.

## Common error responses

| Status | When | Body |
|--------|------|------|
| 400 | Username/email already in use | `{ "error": "Username or email already exists" }` |
| 400 | `shopId` provided but doesn't exist | `{ "error": "Shop with ID … not found" }` |
| 401 | Wrong password / unknown identifier | `{ "error": "Invalid credentials" }` |
| 401 | 5+ failed attempts | `{ "error": "Account is locked until 2026-05-08 15:30:00" }` |
| 401 | Account flagged inactive | `{ "error": "Account is inactive" }` |
| 401 | `/me` without/expired token | `{ "error": "Invalid token" }` |
| 403 | Caller lacks role/shop access | empty body — policy denied |

## Bootstrapping the first SuperAdmin

There is no endpoint that can mint a SuperAdmin against a fresh DB:

- `POST /api/auth/register` always creates `SystemRole.User`. The `role` field that previously promised elevation has been removed from the request DTO (commit `0832062`).
- `POST /api/admin/seed-users` (which would create the documented `admin@possystem.com` / `Admin@123` account via `UserSeeder.SeedAsync`) is gated behind `[Authorize(Policy = "AdminOnly")]` — i.e. needs an existing SuperAdmin token.
- `POST /api/admin/seed` is `[AllowAnonymous]` but only seeds categories / shops / suppliers / drugs / inventory — **not users**.

So for a brand-new DB, bootstrap with a direct SQL update. The connection string is in `appsettings.Development.json`. The `SystemRole` column is the enum's int value: `0 = SuperAdmin`, `1 = User`.

```sql
-- 1. Register an ordinary user via POST /api/auth/register first (any password you'll remember)
-- 2. Then promote that user to SuperAdmin:
UPDATE "Users" SET "SystemRole" = 0 WHERE "Username" = '<your-bootstrap-user>';
```

Once that user exists you can log in, hit `POST /api/admin/seed-users` to create the seeded test accounts (`admin@possystem.com` / `Admin@123`, plus per-shop owners and cashiers — see `UserSeeder.cs` for the full list), and then optionally retire the bootstrap user.

Older notes pointing at [`../auth/create-admin-user.md`](../auth/create-admin-user.md) describe the same approach.

## Best practices

### Security
- **Run only over HTTPS.** Bearer tokens are credentials; on plain HTTP they're stolen by every middlebox between client and server.
- **Lock down `/api/auth/register` in production.** The endpoint is public; while it can no longer mint a SuperAdmin (the `role` field is gone), open self-registration is still rarely what you want. Put it behind a gateway feature flag, IP allow-list, or remove the route in non-dev environments. Bootstrap the first SuperAdmin by updating the DB row directly.
- **Rotate `Jwt:SecretKey` periodically.** Use ≥ 64 random bytes (`openssl rand -base64 64`). Rotation invalidates every issued token — schedule it.
- **Never log tokens, refresh tokens, or password hashes.** Serilog enrichers are easy to misconfigure; review log output for accidental leaks.
- **Don't put tokens in URLs or query strings.** They land in browser history, server logs, and referrer headers.
- **Keep `Jwt:Issuer` and `Jwt:Audience` set in `appsettings`.** They're checked on normal requests; `/refresh` skips them deliberately, so token forgery defence rests on the signing key alone.
- **Treat `LockedUntil` as advisory rate-limiting, not security.** Add a real per-IP rate limiter (gateway, middleware) — five tries per account is trivially side-stepped by a script that walks usernames.

### Performance
- **Refresh access tokens *before* they expire**, not after a 401. The 60-min default plus a 5–10 min skew gives you a clean window — track `expiresAt` on the client.
- **Cache the user's claims locally.** Don't call `/me` every page load — the JWT already holds username, system role, shop list, and per-shop permissions. Re-call only after `/refresh`.
- **Login response carries full `shopDetails`** for every membership (legal name, address, receipt config, hardware config). Useful for one-shot till bootstrap, but it's a heavy payload — strip what you don't use before storing client-side.

### Correctness
- **Tolerate clock skew.** Validate `expiresAt` against the server's own time, not the client's; many tokens look "expired" because of phone-clock drift.
- **Refresh tokens rotate** — every `/refresh` issues a brand-new refresh token and invalidates the old one. If you store the refresh token in two places, both must be updated atomically or one will fail.
- **A logged-in user who is added to a new shop sees no change** until the next `/refresh`. Force a refresh after any membership change.
- **Account lockout (`LockedUntil`) clears on successful login**, not on expiry. If the lock was caused by a typo, the user can simply wait it out.

### Clean code
- **Use a single auth service in your client.** Centralise token storage, refresh logic, and 401 retry. Don't sprinkle `Authorization` headers across components.
- **Stop using the DTO's `"Staff"` default in code paths you control.** Pass `"User"` explicitly so future enum changes don't quietly behave differently.

## Next

→ [02 — Shops & Members](./02-shops-and-members.md)
