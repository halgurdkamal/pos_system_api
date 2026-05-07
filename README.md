# POS System API

[![CI](https://github.com/halgurdkamal/pos_system_api/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/halgurdkamal/pos_system_api/actions/workflows/ci.yml)

Multi-tenant pharmacy Point-of-Sale REST API built with .NET 6, EF Core 6, PostgreSQL, and a Clean Architecture / CQRS layout.

## Features

- **Multi-tenant**: each shop has its own inventory, pricing, and configuration
- **Drug catalog** with multi-level packaging hierarchy (e.g. Box → Strip → Tablet) and shop-specific overrides
- **Batch tracking** with FIFO selling and automatic price propagation when active batches switch
- **Sales, purchases, suppliers, customers, inventory alerts**
- **JWT auth** with system and shop-scoped roles
- **PDF receipts** via QuestPDF, with multiple paper-type support (A4, 80mm thermal, etc.)
- **Barcode generation** (ZXing.Net + SkiaSharp)
- **Structured logging** via Serilog (console + rolling file)
- **Swagger UI** for live API exploration
- **Health checks** at `/health/live` (process is up) and `/health/ready` (DB reachable) for orchestrators and load balancers

## Project structure

```
src/
├── API/             ← Controllers, middleware, DI extensions (HTTP layer)
├── Core/
│   ├── Application/ ← CQRS handlers (MediatR), validators (FluentValidation), DTOs
│   └── Domain/      ← Entities, value objects, domain enums
└── Infrastructure/  ← EF Core DbContext, migrations, repositories, external services
```

A single deployable monolith with strict layer boundaries. See [`docs/CODING_STANDARDS.md`](docs/CODING_STANDARDS.md).

## Getting started

### 1. Prerequisites

- .NET 6 SDK
- PostgreSQL (local or hosted, e.g. [Neon](https://neon.tech))

### 2. Configure secrets

Read [`SECURITY_SETUP.md`](SECURITY_SETUP.md) — the app refuses to start without a valid DB connection string and JWT secret. Quickest path for local dev:

```bash
cp appsettings.Example.json appsettings.Development.json
# edit appsettings.Development.json with real values
```

### 3. Apply migrations

```bash
dotnet ef database update
```

### 4. Run

```bash
dotnet run
```

Then open <http://localhost:5000/swagger>.

## Documentation

Full docs are under [`docs/`](docs/) — see [`docs/README.md`](docs/README.md) for the index.

| I want to... | Read |
|--------------|------|
| Set up secrets | [`SECURITY_SETUP.md`](SECURITY_SETUP.md) |
| Understand the drug model | [`docs/drugs/drug-system.md`](docs/drugs/drug-system.md) |
| Understand packaging & pricing | [`docs/packaging/system-guide.md`](docs/packaging/system-guide.md) |
| Understand batch-driven pricing | [`docs/pricing/from-batch-guide.md`](docs/pricing/from-batch-guide.md) |
| Generate PDF receipts | [`docs/pdf/quickstart.md`](docs/pdf/quickstart.md) |
| Create the first admin user | [`docs/auth/create-admin-user.md`](docs/auth/create-admin-user.md) |
| Follow project conventions | [`docs/CODING_STANDARDS.md`](docs/CODING_STANDARDS.md) |

## Build & test

```bash
dotnet build pos_system_api.sln
dotnet test pos_system_api.sln
```

CI runs on every push and PR to `main` — see `.github/workflows/ci.yml`.

## Deployment

- `Dockerfile` for container deployment
- `web.config` and `publish-*.ps1` scripts for IIS hosting

## License

Private project.
