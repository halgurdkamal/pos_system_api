# Coding Rules & Project Standards (Rules.md)

These rules define how the Pharmacy POS System must be written, structured, and maintained. All developers working on the system must follow these standards.

---

# 1. Architecture Rules

### **1.1 Use Clean Architecture Layers**

Each module MUST follow:

- **Domain** (Business rules)
- **Application** (Use cases)
- **Infrastructure** (Database, external services)
- **API** (Controllers/endpoints)

### **1.2 Feature-based Modular Monolith**

Each feature/module MUST have its own mini clean architecture:

```
Sales.Api
Sales.Application
Sales.Domain
Sales.Infrastructure
```

No cross-project folders.

### **1.3 Dependencies Direction**

- API → Application
- Application → Domain
- Infrastructure → Application + Domain
- Domain → **no dependencies**

### **1.4 Modules Cannot Directly Reference Each Other**

Modules communicate through:

- Application contracts (interfaces)
- Domain/Application events

Example:

```
Sales → Inventory (via interface IInventoryService)
```

Not:

```
Sales → Inventory.Infrastructure (forbidden)
```

---

# 2. Domain Layer Rules

### **2.1 No EF Core, No Http, No External Libraries**

Domain MUST stay pure C#.

### **2.2 Use Entities, Value Objects, and Domain Events**

- Every business rule must exist inside Domain
- No business logic allowed in controllers or handlers

### **2.3 Enforce Invariants Inside Aggregates**

Domain must ALWAYS validate:

- Quantity cannot be negative
- Sale total price = sum of line items
- Expired batch cannot be sold

### **2.4 Use Value Objects for Behavior**

Examples:

- `Money`
- `Quantity`
- `BatchExpiry`

---

# 3. Application Layer Rules

### **3.1 Use CQRS Pattern**

Each use case must be one of:

- **Command** (write)
- **Query** (read)

### **3.2 Each Command/Query MUST Have:**

- Request model
- Handler
- Validation (FluentValidation)
- DTO result

### **3.3 Do Not Put Business Logic in Handlers**

Handlers orchestrate only:

- Validate → Load Entities → Call Domain → Persist → Return result

### **3.4 Use Interfaces, Not Implementations**

Example: use

```
IInventoryRepository
```

not

```
InventoryRepository
```

### **3.5 All Exceptions Are Custom**

No throwing raw exceptions.
Use:

```
throw new DomainException("...")
throw new NotFoundException("...")
```

---

# 4. Infrastructure Layer Rules

### **4.1 EF Core Mapping Only**

Infrastructure CANNOT contain business logic.
Only:

- Entity configurations
- Repositories
- EF Core database access
- External integrations (SMS, payment, storage)

### **4.2 Repository Naming**

```
SaleRepository : ISaleRepository
InventoryRepository : IInventoryRepository
```

### **4.3 No Direct API Calls Between Modules**

Use:

- MediatR events
- Application-level interfaces

### **4.4 Keep SQL in Repository Layer Only**

No raw SQL inside Application.

---

# 5. API Layer Rules

### **5.1 API Only Receives + Sends Data**

No business logic in controllers.

### **5.2 API Routes Must Follow Format**

```
/api/{module}/{resource}
```

Examples:

```
/api/pharmacy/sales
/api/inventory/batches
/api/identity/login
/api/pharmacy/products
```

### **5.3 Consistent Response DTOs**

Every response must be wrapped:

```
SuccessResponse<T>
ErrorResponse
PagedResult<T>
```

### **5.4 Authorization Required**

Except for endpoints marked `[AllowAnonymous]` (e.g., public drug list).

---

# 6. Naming Conventions

### **6.1 Project Names**

```
{Module}.{Layer}
Examples:
Sales.Application
Pharmacy.Domain
Inventory.Api
```

### **6.2 Class Names**

- Entities: `Sale`, `Drug`, `Batch`
- Commands: `CreateSaleCommand`
- Queries: `GetSalesByDateQuery`
- Controllers: `SalesController`
- Repositories: `SaleRepository`

### **6.3 File Names Match Class Names**

Example:

```
CreateSaleCommand.cs
SaleRepository.cs
InventoryItem.cs
```

---

# 7. Directory Rules

### **7.1 Each module must follow structure**

```
/ModuleName
   /ModuleName.Api
   /ModuleName.Application
   /ModuleName.Domain
   /ModuleName.Infrastructure
```

### **7.2 No Shared Logic Inside Modules**

Common logic must go under:

```
Shared.Application
Shared.Infrastructure
Shared.Domain
```

---

# 8. Events & Communication

### **8.1 Use Domain Events inside Domain**

Example:

```
SaleCompletedEvent
StockDecreasedEvent
BatchExpiredEvent
```

### **8.2 Use Application Events for Cross-Module Communication**

Example:

```
SaleCompletedEvent → Inventory reduces stock
```

### **8.3 Event Handlers Must Be Idempotent**

Event processing must avoid duplicates.

---

# 9. Validation Rules

### **9.1 Use FluentValidation for All Commands**

Example:

```
RuleFor(x => x.Quantity).GreaterThan(0)
```

### **9.2 API Must Not Contain Validation Logic**

Only Application layer handles validation.

---

# 10. Error Handling Rules

### **10.1 No Generic Exceptions**

Bad:

```
throw new Exception("Error")
```

Good:

```
throw new InvalidBatchException(batchId)
```

### **10.2 Use Global Exception Middleware**

Automatically format errors.

---

# 11. Logging Rules

### **11.1 Use Structured Logging**

```
Log.Information("Sale created {@SaleId}", sale.Id)
```

### **11.2 Do Not Log Sensitive Data**

- Passwords
- Tokens
- Payment info

---

# 12. Testing Rules

### **12.1 Unit Tests Target Domain and Application**

- Entities
- Value Objects
- Handlers
- Domain events

### **12.2 Integration Tests Target Infrastructure + API**

- Repositories
- Controllers

---

# 13. Performance Rules

### **13.1 Use Pagination for All List Endpoints**

Default:

```
?page=1&limit=20
```

### **13.2 Use Efficient Includes**

Avoid `Include()` chains when not needed.

### **13.3 Use Caching for Heavy Read Queries**

(Inventory list, drug list)

---

# 14. Security Rules

### **14.1 JWT Required for All Authenticated Endpoints**

### **14.2 Implement Role & Permission System**

Example roles:

- Admin
- Owner
- Cashier
- InventoryManager

### **14.3 Protect High-Risk Drug Endpoints**

Requires:

```
[Authorize(Roles = "Pharmacist,Admin")]
```

---

# 15. Git & Commit Rules

### **15.1 Commit Messages in Standard Format**

```
feat: add create sale command
fix: correct batch expiry check
refactor: move validation logic
```

### **15.2 PR Must Include**

- Description
- Linked issue
- Testing steps
- Screenshots if applicable

---

# 16. Documentation Rules

### **16.1 Every Module MUST Have README.md**

Explain:

- What feature does
- API endpoints
- Use cases

### **16.2 Architecture.md must stay updated**

### **16.3 Rules.md must stay consistent across modules**

---

# ✔ Summary

This project follows strict:

- Clean Architecture
- Modular Monolith
- Vertical Slice structure
- Strong rules for naming, events, validation, and performance

These rules ensure the system stays scalable, maintainable, and easy for a team to work on.

---

If you want, I can also generate:

- **Folder Structure Template** (.csproj + folders)
- **ERD Diagram**
- **Sales module real starter code**
