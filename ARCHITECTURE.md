# Architecture

## 1. System context

```mermaid
flowchart LR
    user([Customer / Catalog admin]) -->|HTTPS| web["Angular SPA<br/>(nginx container)"]
    web -->|/api/v1 JSON| api["ECommerce.Api<br/>.NET 10"]
    api -->|EF Core / TDS| db[(SQL Server)]
    api -->|IPaymentGateway| pay["Fake payment gateway<br/>(in-process)"]
```

| Container | Image | Port |
|---|---|---|
| `web` | `nginx:alpine` serving the Angular build; reverse-proxies `/api`, `/openapi`, `/swagger`, `/health` to `api` | 8081 → 80 |
| `api` | `mcr.microsoft.com/dotnet/aspnet:10.0`, non-root user | 8080 |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest`, data in a named volume, health-checked | 1433 |

The API waits for the database health check, applies migrations, seeds data and then starts listening.

## 2. Backend — Clean Architecture

```mermaid
flowchart TB
    subgraph Api["ECommerce.Api (presentation)"]
        controllers[Controllers v1]
        middleware[RequestLogging + ExceptionHandling middlewares]
        openapi[OpenAPI / Swagger / Versioning / Health]
    end
    subgraph Infrastructure["ECommerce.Infrastructure (adapters)"]
        dbctx[AppDbContext + configurations + migrations]
        repos[Repositories + UnitOfWork]
        reads[Read services - AsNoTracking projections]
        cache[CachedCategoryCatalog]
        csv[CsvProductReader]
        payment[FakePaymentGateway]
    end
    subgraph Application["ECommerce.Application (use cases)"]
        services[Application services: Product, ProductImport, Order, Category]
        validators[FluentValidation validators]
        ports[Interfaces: repositories, read services, catalog, payment, csv]
    end
    subgraph Domain["ECommerce.Domain (enterprise rules)"]
        entities[Product, Category, Order, OrderItem]
        rules[Guards, constraints, domain exceptions]
    end

    Api --> Application
    Api --> Infrastructure
    Infrastructure --> Application
    Application --> Domain
```

Dependencies point inward only. `Domain` references nothing. `Application` references only `Domain` and defines the ports it needs. `Infrastructure` implements those ports. `Api` composes everything in `Program.cs` through three extension methods: `AddApplication()`, `AddInfrastructure(configuration)` and `AddApi(configuration)`.

### Folder layout

```
backend/
├── src/
│   ├── ECommerce.Domain/
│   │   ├── Common/          Entity, AuditableEntity, Guard, IAggregateRoot, IConcurrencyAware
│   │   ├── Categories/      Category, CategoryConstraints
│   │   ├── Products/        Product, ProductConstraints
│   │   ├── Orders/          Order, OrderItem, OrderStatus, OrderConstraints
│   │   └── Exceptions/      DomainException, InsufficientStockException
│   ├── ECommerce.Application/
│   │   ├── Abstractions/    Persistence, Payments, Import
│   │   ├── Common/          Exceptions, paging, shared validation rules + EnsureValidAsync
│   │   ├── Products/        ProductService, Commands/{Create,Update,ImportProducts (+ ProductImportService)}, Queries/SearchProducts
│   │   ├── Categories/      CategoryService
│   │   └── Orders/          OrderService, Commands/PlaceOrder, Queries/GetOrders
│   ├── ECommerce.Infrastructure/
│   │   ├── Persistence/     AppDbContext, Configurations, Migrations, Repositories, ReadServices, Seeding, UnitOfWork
│   │   ├── Caching/         CachedCategoryCatalog
│   │   ├── Import/          CsvProductReader
│   │   └── Payments/        FakePaymentGateway
│   └── ECommerce.Api/
│       ├── Controllers/     Products, Categories, Orders (+ ApiControllerBase)
│       ├── Contracts/       Request models that are not service inputs (query strings, PUT body)
│       ├── Middleware/      ExceptionHandling, RequestLogging, ExceptionMapper, ProblemDetailsBuilder
│       └── Extensions/      Controllers, JSON, versioning, OpenAPI, CORS, health checks
├── tests/                   Domain / Application / Infrastructure / Api.IntegrationTests
└── contracts/               OpenAPI document generated on build
```

Each feature area exposes one application service behind an interface (`IProductService`, `IProductImportService`, `IOrderService`, `ICategoryService`). Controllers inject the service they need and call it directly. Input models (`CreateProductCommand`, `SearchProductsQuery`…) live next to their FluentValidation validators, and each service method validates its input before doing any work.

### Request pipeline

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant RL as RequestLoggingMiddleware
    participant EH as ExceptionHandlingMiddleware
    participant Ctl as Controller
    participant S as Application service
    participant P as Repository / ReadService

    C->>RL: HTTP request (X-Correlation-ID optional)
    RL->>EH: next (log scope with correlation id)
    EH->>Ctl: next
    Ctl->>S: e.g. productService.CreateAsync(command)
    S-->>EH: ValidationException (400) if invalid (validator.EnsureValidAsync)
    S->>P: load / save via IUnitOfWork
    S-->>Ctl: DTO
    Ctl-->>C: 200 / 201 + Location / 204
    Note over EH: Any exception is mapped by ExceptionMapper<br/>to a ProblemDetails response (400/402/404/409/422/500)
```

### Read vs write side

| | Writes | Reads |
|---|---|---|
| Entry point | Service methods that change state (`CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ImportAsync`, `PlaceOrderAsync`) | Service methods that read (`SearchAsync`, `GetByIdAsync`, `GetOrdersAsync`, `GetAllAsync`) |
| Data access | Repositories return tracked aggregates; changes are saved through `IUnitOfWork.SaveChangesAsync` | Read services (`IProductReadService`, `IOrderReadService`, `ICategoryCatalog`) project to DTOs with `AsNoTracking()` |
| Rules | Enforced by the domain entities | None, only filtering, sorting and paging |
| N+1 avoidance | Batch loading (`GetByIdsAsync`, `GetBySkusAsync`: one `IN (...)` query per request) | One SQL statement per request (navigation properties become joins inside the projection) |

### Purchase flow

```mermaid
sequenceDiagram
    autonumber
    participant H as OrderService.PlaceOrderAsync
    participant R as ProductRepository
    participant O as Order (aggregate)
    participant U as UnitOfWork
    participant G as IPaymentGateway

    H->>R: GetByIdsAsync(all product ids)  -- single query
    H->>O: Order.Create(customer)
    loop each line
        H->>O: AddItem(product, qty) → product.DecreaseStock()
    end
    H->>U: SaveChanges  (status Pending, stock reserved,<br/>optimistic concurrency on Product.ConcurrencyStamp)
    H->>G: ChargeAsync(total, card)
    alt approved
        H->>O: MarkAsPaid(reference, last4)
        H->>U: SaveChanges → 201 Created
    else declined
        H->>O: MarkPaymentFailed() + release stock
        H->>U: SaveChanges → PaymentDeclinedException (402)
    end
```

If two customers buy the last unit at the same time, the second `SaveChanges` fails the concurrency check. That becomes a `ConflictException` (409) before any money is charged.

### CSV import

```mermaid
flowchart LR
    file[multipart file] --> reader["CsvProductReader<br/>(CsvHelper, header validation,<br/>max rows)"]
    reader --> records[raw ProductCsvRecord list]
    records --> parser["ProductImportRowParser<br/>currency symbols, numbers, category lookup"]
    parser --> validator["ProductImportRowValidator<br/>(same rules as create/update)"]
    validator --> upsert["Upsert by SKU<br/>(existing loaded in one query)"]
    upsert --> uow[single SaveChanges]
    upsert --> report["ProductImportResult<br/>created / updated / skipped / failed + issues per line"]
```

## 3. Data model

```mermaid
erDiagram
    Categories ||--o{ Products : classifies
    Orders ||--|{ OrderItems : contains
    Products ||--o{ OrderItems : "referenced by (snapshot)"

    Categories {
        int Id PK
        nvarchar_100 Name UK
    }
    Products {
        uniqueidentifier Id PK
        varchar_50 Sku "unique where IsDeleted = 0"
        nvarchar_200 Name "indexed"
        nvarchar_1000 Description
        int CategoryId FK "indexed"
        decimal_18_2 Price "indexed"
        int Stock
        decimal_10_3 WeightKg
        bit IsDeleted "global query filter"
        uniqueidentifier ConcurrencyStamp "concurrency token"
        datetime2 CreatedAtUtc "indexed"
        datetime2 UpdatedAtUtc
    }
    Orders {
        uniqueidentifier Id PK
        varchar_20 OrderNumber UK
        nvarchar_150 CustomerName
        nvarchar_256 CustomerEmail "indexed"
        nvarchar_500 ShippingAddress
        varchar_20 Status
        decimal_18_2 TotalAmount
        varchar_100 PaymentReference
        char_4 CardLast4
        datetime2 PaidAtUtc
        datetime2 CreatedAtUtc "indexed"
        datetime2 UpdatedAtUtc
    }
    OrderItems {
        uniqueidentifier Id PK
        uniqueidentifier OrderId FK "cascade"
        uniqueidentifier ProductId FK "restrict"
        nvarchar_200 ProductName
        varchar_50 Sku
        decimal_18_2 UnitPrice
        int Quantity
    }
```

- Audit columns (`CreatedAtUtc`, `UpdatedAtUtc`) are set centrally in `AppDbContext.SaveChanges` using an injected `TimeProvider`, and read back as UTC.
- The category catalog (18 categories) is part of the `InitialCreate` migration (`HasData`) and cached in memory by the API.

## 4. Frontend

```mermaid
flowchart TB
    subgraph App["app (eager)"]
        shell[Shell: toolbar, nav, cart badge]
        core["core/: ProductsApi, CategoriesApi, OrdersApi,<br/>errorInterceptor → ApiError, CartStore (signals), NotificationService"]
    end
    subgraph Shared["shared/"]
        ui["ui wrappers: button, icon-button, search-field, category-select,<br/>page-header, empty-state, inline-alert, stock-badge,<br/>order-status-chip, quantity-stepper, confirm-dialog"]
        forms["forms: AppValidators, fieldError pipe, applyServerErrors"]
    end
    subgraph Lazy["features/ (lazy chunks)"]
        shop["/shop: catalog · cart · checkout (guarded)"]
        admin["/admin/products: table · form dialog · import dialog"]
        orders["/orders: list · detail"]
    end
    shell --> Lazy
    Lazy --> Shared
    Lazy --> core
```

- **Change detection:** zoneless with `OnPush` on every component. State lives in signals; server data is loaded with `rxResource`, whose parameters are computed signals, so changing a filter cancels the in-flight request and starts a new one. `linkedSignal` resets the page index whenever a filter changes.
- **Theming:** `src/styles/_theme.scss` defines the M3 theme (`mat.theme`), shape tokens and app-wide component token overrides. Wrapper components apply their own `mat.*-overrides()` in their `:host`. Nothing uses `!important` or targets internal `.mdc-*` classes.
- **Error UX:** the interceptor turns every failed response into an `ApiError` (status, readable message, field errors). Forms map field errors back onto the matching controls; other errors show inline. Network and 5xx errors also raise a snackbar.

## 5. Extending the system

| Change | Where |
|---|---|
| New use case | A method on the feature service (or a new `I<Feature>Service` registered in `AddApplication`); input validators are picked up automatically by assembly scanning |
| New cross-cutting concern (caching, transactions, metrics) | A decorator around the service interface (like `CachedCategoryCatalog`), or middleware for HTTP-wide concerns |
| Real payment provider | A new `IPaymentGateway` implementation registered in `AddInfrastructure` |
| API v2 | Controllers with `[ApiVersion("2.0")]`; a new OpenAPI document is generated automatically |
| Another database | A provider in `DependencyInjection.ConfigureProvider` plus a new migrations set |
| New UI feature | A `features/<name>/<name>.routes.ts` lazy route built from the shared wrappers |
