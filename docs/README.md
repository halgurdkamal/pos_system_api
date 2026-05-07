# Documentation

Index for the POS System API docs. Each topic folder has multiple files — the **canonical** one is marked, others are supporting context.

## API Guides (start here if you want to call the API)

Step-by-step walkthroughs of every feature with endpoints, payloads, and lifecycles.

- [`api-guides/README.md`](./api-guides/README.md) — index + end-to-end story
- [`api-guides/00-glossary.md`](./api-guides/00-glossary.md) — FIFO, FEFO, SKU, EAN, PII, CQRS terms used throughout
- [`api-guides/01-authentication.md`](./api-guides/01-authentication.md)
- [`api-guides/02-shops-and-members.md`](./api-guides/02-shops-and-members.md)
- [`api-guides/03-items-and-catalog.md`](./api-guides/03-items-and-catalog.md)
- [`api-guides/04-suppliers-and-purchase-orders.md`](./api-guides/04-suppliers-and-purchase-orders.md)
- [`api-guides/05-inventory-and-stock.md`](./api-guides/05-inventory-and-stock.md) — index for ↓
  - [`api-guides/05a-inventory-core.md`](./api-guides/05a-inventory-core.md) — view, add stock, pricing, packaging
  - [`api-guides/05b-stock-operations.md`](./api-guides/05b-stock-operations.md) — adjustments, counts, transfers, alerts, reports
- [`api-guides/06-cashier-pos-checkout.md`](./api-guides/06-cashier-pos-checkout.md)
- [`api-guides/07-barcodes-and-pdf.md`](./api-guides/07-barcodes-and-pdf.md)
- [`api-guides/08-data-model-and-recipes.md`](./api-guides/08-data-model-and-recipes.md) — **how it all joins up** + cookbook recipes
- [`api-guides/99-known-gaps.md`](./api-guides/99-known-gaps.md) — security gaps, functional TODOs, workarounds

## Setup

- [`../SECURITY_SETUP.md`](../SECURITY_SETUP.md) — required reading before first run; how to provide DB and JWT secrets
- [`CODING_STANDARDS.md`](./CODING_STANDARDS.md) — project conventions all contributors should follow
- [`CONTROLLER_REFACTOR_BACKLOG.md`](./CONTROLLER_REFACTOR_BACKLOG.md) — ranked list of controllers still needing CQRS migration
- [`observability.md`](./observability.md) — log sinks (console, file, optional Seq)

## Topics

### Auth
How users sign in, get tokens, and what's returned in auth responses.

- [`auth/create-admin-user.md`](./auth/create-admin-user.md) — **start here** — how to seed/create the first admin
- [`auth/api-testing.md`](./auth/api-testing.md) — test calls for login/register endpoints
- [`auth/shop-details-enhancement.md`](./auth/shop-details-enhancement.md) — what the auth response contains and why
- *(Note: this folder is about authentication. For RBAC role definitions, see `src/Core/Domain/Users/Enums/`)*

### Drugs
The drug catalog: domain model, lifecycle, real-world scenarios.

- [`drugs/drug-system.md`](./drugs/drug-system.md) — **canonical** — entities, relationships, key concepts
- [`drugs/item-lifecycle.md`](./drugs/item-lifecycle.md) — walkthrough: adding a drug, configuring packaging, common flows

### Packaging
Multi-level packaging hierarchy (Box → Strip → Tablet) with shop-specific overrides.

- [`packaging/system-guide.md`](./packaging/system-guide.md) — **canonical** — overall packaging & pricing model
- [`packaging/overrides.md`](./packaging/overrides.md) — how shops customize packaging without touching the global catalog
- [`packaging/implementation.md`](./packaging/implementation.md) — implementation details and code references

### Pricing & Batches
How prices flow from incoming batches into the active selling price, with FIFO batch switching.

- [`pricing/from-batch-guide.md`](./pricing/from-batch-guide.md) — **canonical** — the main concept and how to use it
- [`pricing/auto-switch-demo.md`](./pricing/auto-switch-demo.md) — concrete end-to-end example
- [`pricing/batch-propagation.md`](./pricing/batch-propagation.md) — how price changes propagate
- [`pricing/flow-diagram.md`](./pricing/flow-diagram.md) — visual flow with sequence/state diagrams
- [`pricing/implementation-summary.md`](./pricing/implementation-summary.md) — code-level summary of what was built

### PDF Receipts
Receipt generation with QuestPDF, paper-type support (A4 / 80mm thermal / etc.).

- [`pdf/system.md`](./pdf/system.md) — **canonical** — full architecture and API
- [`pdf/quickstart.md`](./pdf/quickstart.md) — fastest path to generating your first receipt
- [`pdf/paper-types.md`](./pdf/paper-types.md) — supported paper sizes and configuration

## Reference

External / inspirational material, not part of this codebase.

- [`reference/go-api-reference.md`](./reference/go-api-reference.md) — README of a separate Go drug API kept as design reference

## How to keep this organized

When adding a new doc:

1. **Pick the right topic folder** (or create one if a real new topic emerges)
2. **Use kebab-case filenames** (`my-feature.md`, not `MY_FEATURE.md`)
3. **Add a link in this index** under the right section
4. **If it supersedes an existing doc**, delete or merge — don't leave parallel sources of truth
