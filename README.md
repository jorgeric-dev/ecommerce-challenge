# ECommerce Challenge

A full-stack e-commerce application: product catalog management (CRUD + CSV import), product search and a purchase flow with a simulated payment provider.

| Layer | Technology |
|---|---|
| API | .NET 10, ASP.NET Core (controllers), EF Core 10, FluentValidation, Asp.Versioning, OpenAPI 3.1 + Swagger UI |
| Database | SQL Server (LocalDB for local development, SQL Server 2022 container in Docker) |
| UI | Angular 22 (standalone, zoneless, signals), Angular Material 22 with M3 theming |
| Tests | xUnit, NSubstitute, Shouldly, WebApplicationFactory + SQLite, Vitest |
| Delivery | Docker multi-stage images + Docker Compose |

See [ARCHITECTURE.md](ARCHITECTURE.md) for diagrams and a walkthrough of the layers.

---

## 1. Running the project

### Option A — Docker (recommended)

Requirements: Docker Desktop (Linux containers).

```bash
docker compose up --build
```

| URL | What |
|---|---|
| http://localhost:8081 | Angular UI (nginx; proxies `/api` to the API container) |
| http://localhost:8080/swagger | Swagger UI |
| http://localhost:8080/openapi/v1.json | OpenAPI contract |
| http://localhost:8080/health | Health check (includes the database) |

On first start the API applies the EF Core migrations, seeds the category catalog and imports `file_example.csv` as sample data (84 products created, 3 rows updated as duplicates, 8 rows rejected — see [CSV import rules](#4-csv-import-rules)).

The SA password defaults to a development value; override it with `MSSQL_SA_PASSWORD=... docker compose up --build`. The SQL Server image is x64 only.

### Option B — Local development

Requirements: .NET SDK 10, Node.js 22+ (24 used), SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance.

```bash
# 1. API — applies migrations and seeds data on startup
cd backend
dotnet tool restore
dotnet run --project src/ECommerce.Api --launch-profile http
# -> http://localhost:5037/swagger

# 2. UI (new terminal) — dev server proxies /api to http://localhost:5037
cd frontend
npm install
npm start
# -> http://localhost:4200
```

The default connection string targets `(localdb)\MSSQLLocalDB`. To use another server set `ConnectionStrings__ECommerce`, e.g.
`ConnectionStrings__ECommerce="Server=localhost,1433;Database=ECommerce;User Id=sa;Password=...;TrustServerCertificate=True"`.
To run only the database in Docker: `docker compose up sqlserver`.

Useful settings (`appsettings.json` / environment variables):

| Key | Default | Purpose |
|---|---|---|
| `Database:Provider` | `SqlServer` | `Sqlite` is also supported (used by the integration tests) |
| `Database:ApplyMigrationsOnStartup` | `true` | Run `Migrate()` at startup |
| `Database:SeedSampleProducts` | `true` | Import the sample CSV when the products table is empty |
| `CatalogCache:CategoriesTimeToLive` | `00:30:00` | In-memory cache lifetime of the category catalog |
| `Swagger:Enabled` | `true` | Serve Swagger UI |
| `Cors:AllowedOrigins` | `http://localhost:4200` | Origins allowed to call the API directly |

### Migrations

```bash
cd backend
dotnet ef migrations add <Name> -p src/ECommerce.Infrastructure -s src/ECommerce.Infrastructure -o Persistence/Migrations
dotnet ef database update      -p src/ECommerce.Infrastructure -s src/ECommerce.Infrastructure
```

A design-time factory lets the EF tooling run without starting the API.

### Fake payments

| Card number | Result |
|---|---|
| `4242 4242 4242 4242` (or any Luhn-valid number) | Approved |
| `4000 0000 0000 0002` | Declined by the issuer → HTTP 402 |
| `4000 0000 0000 9995` | Insufficient funds → HTTP 402 |

Any future expiry date and any 3–4 digit CVV work. Card data is never stored; only the last 4 digits and the payment reference are kept on the order.

---

## 2. Tests and coverage

```bash
# Backend: 325 tests (Domain, Application, Infrastructure, API integration)
cd backend
dotnet test --settings coverlet.runsettings --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;TextSummary"

# Frontend: 93 tests
cd frontend
npx ng test --watch=false --coverage
```

| Suite | Tests | Line coverage | Branch coverage |
|---|---|---|---|
| Backend (all assemblies, migrations excluded) | 325 | 98.8 % | 93.3 % |
| Frontend (every file under `src/app`, not only the ones a test imports) | 93 | 94.3 % | 92.5 % |

- **Domain tests** — factory methods, invariants and state transitions of `Product`, `Category`, `Order`.
- **Application tests** — every application service (including that invalid input is rejected before any data access), validator, the CSV row parser and the DI registrations, with repositories mocked.
- **Infrastructure tests** — repositories, read services (search, sorting, paging, LIKE escaping), unit of work (auditing, optimistic concurrency, unique and foreign keys), cache decorator, CSV reader against the real `file_example.csv`, DI wiring and seeding, run against in-memory SQLite.
- **API integration tests** — the whole HTTP pipeline through `WebApplicationFactory`: status codes, ProblemDetails shape, versioning headers, correlation id, multipart import, purchase flow including decline and stock release.

---

## 3. API overview (v1)

All routes are versioned through the URL segment (`/api/v1/...`) and use plural nouns. GET reads, POST creates (201 + `Location`), PUT replaces (200), DELETE removes (204).

| Method | Route | Description |
|---|---|---|
| GET | `/api/v1/products?search=&categoryId=&minPrice=&maxPrice=&inStockOnly=&sortBy=&sortDirection=&page=&pageSize=` | Search products (paged) |
| GET | `/api/v1/products/{id}` | Product by id |
| POST | `/api/v1/products` | Create product |
| PUT | `/api/v1/products/{id}` | Update product (the SKU cannot change) |
| DELETE | `/api/v1/products/{id}` | Soft-delete product |
| POST | `/api/v1/products/import` | Import CSV (`multipart/form-data`, field `file`, max 5 MB / 10 000 rows) |
| GET | `/api/v1/categories` | Category catalog (cached) |
| POST | `/api/v1/orders` | Purchase products |
| GET | `/api/v1/orders/{id}` | Order with its lines |
| GET | `/api/v1/orders?customerEmail=&page=&pageSize=` | List orders |

Errors are always `application/problem+json` (RFC 9457) with a readable `detail`, a `traceId` and, for validation errors, an `errors` dictionary:

| Situation | Status |
|---|---|
| Invalid input / malformed JSON / invalid CSV file | 400 |
| Payment declined (`orderNumber` included) | 402 |
| Resource not found | 404 |
| Duplicate SKU, insufficient stock, concurrent modification | 409 |
| Broken business rule detected by the domain | 422 |
| Unexpected error (details hidden, logged with the trace id) | 500 |

**Contract-first / SDK generation.** Every build of the API writes the OpenAPI document to [`backend/contracts/ECommerce.Api.json`](backend/contracts/ECommerce.Api.json) (via `Microsoft.Extensions.ApiDescription.Server`), so the contract is versioned with the code and changes show up in code review. Clients can be generated from it, for example:

```bash
npx @openapitools/openapi-generator-cli generate -i backend/contracts/ECommerce.Api.json -g typescript-angular -o generated/api-client
npx nswag openapi2csclient /input:backend/contracts/ECommerce.Api.json /output:ECommerceClient.cs
```

---

## 4. CSV import rules

> **Note:** the sample file was provided by LoanPro and downloaded on 2026-09-30.

The sample file deliberately contains dirty data. The importer processes the file row by row, **never aborts on a bad row**, saves all valid rows in one unit of work and returns a report with the line number, SKU and reason of every rejected row.

| Case in `file_example.csv` | Decision |
|---|---|
| `$29.99` | Accepted — a leading currency symbol (`$ € £`) is stripped |
| `free` as price | Rejected — not a number |
| `0.00` price (Mystery Box) | Rejected — price must be ≥ 0.01; a free product breaks the purchase flow |
| Negative stock (`-5`) | Rejected |
| Empty / whitespace-only name | Rejected |
| `<script>alert('xss')</script>` as name | Rejected — names and descriptions must not contain markup (Angular escapes output anyway; this is defense in depth) |
| `Robert'); DROP TABLE products;--` | Accepted as plain text — EF Core parameterizes every query, so it is harmless data |
| Missing weight | Rejected — weight is required (0 is allowed, e.g. digital goods) |
| Missing category | Rejected; an unknown category is rejected too, categories come from the seeded catalog |
| Duplicate SKU in the same file (`RS-001`, `BS-021`) | Last row wins; a warning names the line it overrides |
| SKU already in the database | Upsert — the existing product is updated |
| Blank rows (`,,,,,,`) | Skipped and counted separately |
| Quoted commas, escaped quotes, `—`, `™` | Parsed correctly (CsvHelper, UTF-8, `nvarchar` columns) |

---

## 5. Decisions, approach and alternatives considered

### Approach

The work went inside out, following the Clean Architecture dependency rule: domain model first, then application use cases, then infrastructure adapters, then the HTTP surface, then the UI, then containers. Each layer was built test-first where it made sense: domain invariants and use-case behavior were written as tests first, then implemented until they passed. The integration tests were written once the pipeline existed, to check the cross-cutting behavior (middleware, serialization, versioning, persistence).

### Backend

| Topic | Decision | Alternatives considered |
|---|---|---|
| Architecture | Clean Architecture in 4 projects (`Domain` ← `Application` ← `Infrastructure` ← `Api`). Application owns the interfaces; Infrastructure implements them (dependency inversion). | Vertical-slice in a single project: less ceremony, but weaker compile-time boundaries for a codebase meant to grow. |
| Application services | Controllers inject one service per feature area (`IProductService`, `IProductImportService`, `IOrderService`, `ICategoryService`) and call it directly. Input models sit next to their validators, and each service method validates its input before any work. | **Mediator (in-house or MediatR)**: decouples controllers from use cases and gives a pipeline for cross-cutting concerns, but adds indirection that a codebase this size does not need. MediatR also needs a commercial license since v13. |
| Read vs write side | Commands load tracked aggregates through repositories and save them through `IUnitOfWork`. Queries go through read services that project straight to DTOs with `AsNoTracking()`, so one SQL statement per request and no N+1. | Using repositories for reads too, which loads full entities and then maps them. Separate read database: not needed at this scale. |
| Repository / UoW | Generic `Repository<TEntity,TId>` base (DRY) + aggregate-specific repositories; `UnitOfWork` wraps `SaveChanges` and translates EF exceptions into application exceptions (409). | Using `DbContext` directly in services. It is already a UoW, but that would couple the Application layer to EF Core. |
| Domain model | Private setters, private constructors, static factory methods (`Product.Create`, `Order.Create`) and intent-revealing methods (`DecreaseStock`, `MarkAsPaid`). Validation runs before any assignment, so a failed update leaves the entity unchanged. | Anemic entities with validation only in the services. |
| Validation | Two levels: FluentValidation run by each service through `EnsureValidAsync` (user-friendly, grouped per field, async category check) and domain guards (invariants that can never be bypassed). Shared rules (`ProductDetailsValidator`, `ValidSku`, `RequiredPlainText`) are reused by create, update and CSV import. Limits live once in `*Constraints` classes used by entities, validators and EF configurations. | DataAnnotations: weaker composition and no async rules. |
| Errors | Typed exceptions (`NotFoundException`, `ConflictException`, `ValidationException`, `PaymentDeclinedException`, `DomainException`…) mapped to status codes in one place (`ExceptionMapper`) by a global middleware that writes ProblemDetails. Malformed-JSON errors are reshaped to the same format. | `IExceptionHandler` (equivalent, newer API; a middleware was explicitly requested). Result objects instead of exceptions: more explicit, but noisier for this size. |
| Logging | `RequestLoggingMiddleware` (method, path, status, duration, correlation id in a log scope + `X-Correlation-ID` header), source-generated `LoggerMessage` methods. | Serilog: easy to add later as a logging provider without code changes. |
| Purchase consistency | Stock is reserved and the order saved as `Pending` first, guarded by an optimistic concurrency token on `Product`, so two buyers cannot oversell. Then the payment is charged. On approval the order becomes `Paid`; on decline the stock is released and the order is marked `PaymentFailed`. No DB transaction stays open during the external call. | One transaction around the payment call: holds locks while waiting on a third party, and a retry could charge twice. A full saga/outbox is the next step with a real provider. |
| Delete | Soft delete with a global query filter; orders keep a snapshot (name, SKU, unit price) of each line. The unique SKU index is filtered to active rows so a SKU can be reused. | Hard delete: breaks the foreign keys of historical orders. |
| IDs | `Guid.CreateVersion7()` generated in the domain: time-ordered (index-friendly) and known before saving. | Identity ints: IDs known only after insert, and sequential ints in URLs leak volume. |
| Database | SQL Server with explicit types and lengths (`varchar` for SKU/order number, `nvarchar` for text, `decimal(18,2)` money, `decimal(10,3)` weight, `char(4)` card digits), foreign keys with `Restrict`/`Cascade` chosen per relation, indexes on SKU (unique, filtered), name, price, created date, category, order number (unique) and customer email. Enum stored as string. | PostgreSQL would work identically with the Npgsql provider. |
| Seeding | Categories are seeded with `HasData`, so they are part of the migration. Sample products are seeded by sending the regular `ImportProductsCommand` with the embedded CSV, reusing the importer instead of duplicating rules. | A separate SQL seed script, which would duplicate the validation rules. |
| Catalog cache | `CachedCategoryCatalog` decorator over the DB catalog with `IMemoryCache` and a configurable TTL. It serves the categories endpoint, validators and the importer. | Distributed cache (Redis) once there are several API instances. |
| Versioning & docs | `Asp.Versioning` (URL segment) + built-in `Microsoft.AspNetCore.OpenApi` (OpenAPI 3.1) with one document per version, Swagger UI for exploration, contract exported at build. | Swashbuckle's generator: the built-in generator is now the default in .NET 10. |
| Libraries avoided | AutoMapper and FluentAssertions (commercial licenses now); mapping is explicit (`ToDto`), assertions use Shouldly. | — |
| Integration tests DB | SQLite (in-memory) through the same `AddInfrastructure` code path, with `EnsureCreated`. The provider switch is configuration only. The context converts `decimal` to `double` only on SQLite, because SQLite cannot sort decimals. | Testcontainers with SQL Server: more realistic, but needs Docker for every test run. The migrations themselves are checked by running against LocalDB/SQL Server. |

### Frontend

| Topic | Decision |
|---|---|
| Structure | `core/` (API clients, interceptor, cart store, notifications), `shared/` (wrapper UI components, form helpers), `features/` (`shop`, `product-admin`, `orders`) and `layout/`. Every feature is a lazy route (`loadChildren` → routes file → `loadComponent`), so each page ships in its own chunk. |
| Components | Standalone only, importing just the Material pieces they use (`MatButton`, `MatTable`…, never whole modules), `ChangeDetectionStrategy.OnPush` everywhere, zoneless change detection, signals (`input`, `model`, `computed`, `linkedSignal` to reset paging when filters change) and `rxResource` for data loading. |
| Wrapper components | `app-button`, `app-icon-button`, `app-search-field` (debounced), `app-category-select`, `app-page-header`, `app-empty-state`, `app-inline-alert`, `app-stock-badge`, `app-order-status-chip`, `app-quantity-stepper`, `app-confirm-dialog`. Features use these instead of raw Material elements, so look and behavior change in one place. |
| Theming | M3 via `mat.theme()` (violet/orange palettes, light + dark through `color-scheme`) and design tokens (`--app-shape-*`). Customization uses the official `mat.*-overrides()` mixins and `--mat-sys-*` variables. **No `!important` and no overriding of internal Material selectors.** |
| Forms | Reactive forms whose validators mirror the API limits (`PRODUCT_CONSTRAINTS`, Luhn, markup, decimals). Server validation errors (`errors` in ProblemDetails) are mapped back onto the matching controls, including nested paths such as `Payment.Expiry`. |
| HTTP | Typed API clients against the contract, a functional error interceptor that turns responses into `ApiError` and shows a snackbar only for network or 5xx errors; 4xx errors are shown where they happen. |
| Cart | Signal store persisted to `localStorage`, capped by available stock and the server's per-line limit. |

### Assumptions and known limitations

- No authentication/authorization. The admin screens and the order list are open; in production they would sit behind an identity provider with role-based policies.
- A single currency (USD) and no taxes or shipping costs.
- The payment provider is simulated; the gateway interface (`IPaymentGateway`) is where a real provider (Stripe, Adyen…) would plug in.
- Running both the Docker stack and a local `ng serve` needs port 4200 free for the dev server; the Docker UI uses 8081 to avoid that conflict.
