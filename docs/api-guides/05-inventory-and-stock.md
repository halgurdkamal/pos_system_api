# 5. Inventory & Stock — index

This topic was split into two focused guides because it grew too long for a single page.

| Guide | Covers |
|-------|--------|
| [**5a — Inventory Core**](./05a-inventory-core.md) | View stock, add stock (create batches), pricing & packaging-pricing, packaging overrides, move between storage and shop floor, FIFO mechanics. The day-to-day APIs. |
| [**5b — Stock Operations**](./05b-stock-operations.md) | Stock adjustments (damage / theft / return), stock counts, inter-shop transfers, inventory alerts, and reports. The supporting workflows. |

If you're not sure which one you want:

- *"How much do I have? What does it cost? Where is it?"* → **5a**
- *"Something changed quantity outside a sale."* → **5b**

---

## Quick endpoint map

```
/api/inventory/...                  → 5a
/api/stock-adjustments/...          → 5b
/api/stock-counts/...               → 5b
/api/stock-transfers/...            → 5b
/api/inventory-alerts/...           → 5b
/api/inventory-reports/...          → 5b
```

## Next

→ [05a — Inventory Core](./05a-inventory-core.md)
→ [05b — Stock Operations](./05b-stock-operations.md)
→ [06 — Cashier / POS Checkout](./06-cashier-pos-checkout.md)
