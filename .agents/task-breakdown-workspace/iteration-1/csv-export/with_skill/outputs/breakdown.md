# Task Breakdown: CSV Export Endpoint for Invoices

## Context

This is a **greenfield feature** — no `Invoice` entity, `AppDbContext`, repository, service, or controller currently exists in the codebase. The full vertical slice must be built before the CSV export endpoint can be added. The breakdown below layers tasks in the correct order: domain → data access → service logic → HTTP → tests.

The CSV export endpoint will live at `GET /v1/invoices/export/csv`, accept the same `InvoiceQuery` filter parameters as `GET /v1/invoices` (but **without** pagination — export returns all matching records), and respond with `Content-Type: text/csv` and a `Content-Disposition: attachment` header rather than the standard JSON envelope.

---

## Task Breakdown Table

| # | Task | Component | Size | Depends On | Acceptance Criteria |
|---|------|-----------|------|------------|---------------------|
| 1 | Create `Invoice` domain entity with all relevant properties (`InvoiceId`, `CompanyId`, `InvoiceNumber`, `TotalAmount`, `PaidIndicator`, `IssuedDate`, `DueDate`) | Backend | S | — | • Entity class exists in `Domain/` • All boolean fields use `Indicator` suffix per naming convention • No business logic in the entity |
| 2 | Create `AppDbContext` with `Invoices` DbSet and register EF Core in `Program.cs` | Backend | S | #1 | • `AppDbContext` compiles with `DbSet<Invoice> Invoices` • EF Core registered in DI with connection string from config • Existing `HealthController` integration tests still pass |
| 3 | Create `InvoiceDto` (with `[JsonPropertyName]` + XML docs on every property) and `InvoiceQuery` filter/pagination model | Backend | S | #1 | • All `InvoiceDto` properties serialise as camelCase in API responses • XML summary present on every property • `InvoiceQuery` exposes `offset`, `limit` (default 0/20), and at least `companyId` and `paidIndicator` filter fields |
| 4 | Create `IInvoiceRepository` interface and `InvoiceRepository` implementation with `GetAllAsync` (paginated) and `GetAllForExportAsync` (unpaginated, all matching rows) | Backend | M | #2, #3 | • `GetAllAsync` applies all filters, respects `offset`/`limit`, uses `AsNoTracking()` • `GetAllForExportAsync` applies same filters but returns all rows with no Skip/Take • Both methods accept `CancellationToken` as last parameter |
| 5 | Create `IInvoiceService` interface and `InvoiceService.GetAllAsync` returning `CollectionResponseDto<InvoiceDto>` | Backend | M | #4 | • Service maps domain entities to `InvoiceDto` • Response includes correct `metadata.totalCount`, `metadata.timestamp`, `metadata.transactionId`, and `links.self` • Unit-testable without a real database |
| 6 | Add `ExportToCsvAsync(InvoiceQuery, CancellationToken)` to `IInvoiceService` and `InvoiceService` — generates a UTF-8 CSV byte stream from all filtered invoices | Backend | M | #5 | • Returns a `Stream` or `byte[]` of valid UTF-8 CSV • First row is a header row matching `InvoiceDto` property names • Calls `GetAllForExportAsync` (no pagination limit) • Special characters in string fields are correctly CSV-escaped |
| 7 | Create `InvoicesController` with `GET /v1/invoices` (JSON envelope) and `GET /v1/invoices/export/csv` (file download) actions | Backend | M | #6 | • `GET /v1/invoices` returns `CollectionResponseDto<InvoiceDto>` with HTTP 200 • `GET /v1/invoices/export/csv` returns `FileContentResult` with `Content-Type: text/csv` and `Content-Disposition: attachment; filename="invoices.csv"` • Both actions accept `[FromQuery] InvoiceQuery` • All `[ProducesResponseType]` attributes present including 400 and 500 |
| 8 | Register `IInvoiceRepository`, `InvoiceRepository`, `IInvoiceService`, and `InvoiceService` in `ServiceCollectionExtensions.AddApplicationServices` | Backend | XS | #4, #5 | • Application starts without DI errors • Registrations use `AddScoped` • No other existing registrations are modified |
| 9 | Add `InvoiceRepositoryTests` — unit tests using EF Core InMemory database covering filtered queries and export query | Testing | M | #4 | • Test class implements `IDisposable`, uses `Guid.NewGuid()` database name per test • At minimum: no-filter returns all, `companyId` filter returns only matching rows, `paidIndicator` filter works, export query returns all rows ignoring offset/limit • AAA comments on every test |
| 10 | Add `InvoiceServiceTests` — unit tests using Moq covering `GetAllAsync` (with and without filters) and `ExportToCsvAsync` (CSV shape validation) | Testing | M | #6 | • Mocks `IInvoiceRepository` via Moq • Tests verify correct mapping to `InvoiceDto` and correct envelope metadata • `ExportToCsvAsync` test asserts CSV header row and at least one data row are present • AAA comments on every test; tests grouped with `#region` blocks |
| 11 | Add integration test for `GET /v1/invoices/export/csv` using `CustomWebApplicationFactory` and InMemory database | Testing | M | #7 | • Seeds known invoice rows, calls the export endpoint, asserts HTTP 200 • Asserts `Content-Type` header contains `text/csv` • Asserts `Content-Disposition` header is `attachment` • Asserts CSV body contains expected header row and seeded data rows |

---

## ⚠️ Risk Assessment

### Task #2 — Create `AppDbContext` with `Invoices` DbSet

**Risk type:** Database migration  
**Risk:** Introducing `AppDbContext` and `Invoice` table into a production database requires a schema migration; if the migration is not tested or rolled back cleanly it can leave the database in an inconsistent state.  
**Mitigation:** Create an EF Core initial migration (`dotnet ef migrations add InitialInvoiceSchema`) and test it up and down in a staging environment before deploying to production. Wrap the consuming controller behind a feature flag until the migration is confirmed stable in production.

### Task #6 — `ExportToCsvAsync` returns all matching rows without pagination

**Risk type:** Shared code path / cross-cutting concern  
**Risk:** Calling `GetAllForExportAsync` with no row limit could return an unbounded result set for large datasets, causing memory pressure or timeouts on the server.  
**Mitigation:** Add a hard cap (e.g., 10 000 rows) inside `GetAllForExportAsync` and return a `413 Payload Too Large` or `400 Bad Request` if the unconstrained count exceeds it, with a clear error message advising the caller to narrow their filters. Document this limit in the XML summary on the endpoint.

---

## Summary

11 tasks totalling approximately **3–5 days** of focused development. The critical path is strictly sequential through the data layer: **#1 → #2 → #3 → #4 → #5 → #6 → #7 → #8**; none of these can start until its predecessor is complete because each layer depends on the one below it.

Once the repository (#4) is done, **#9** (repository tests) can be picked up by a second developer in parallel with #5–#8. Similarly, **#10** (service tests) can start as soon as #6 is done, and **#11** (integration tests) can start as soon as #7 is done, allowing test work to overlap with the final wiring steps.

The feature scope is moderately large given that the invoices vertical slice is fully greenfield — if timeline is tight, consider splitting into two stories: **Story A** (tasks #1–#5, #8–#10: the standard `GET /v1/invoices` endpoint) and **Story B** (tasks #6–#7, #11: the CSV export layer on top), so the list endpoint can ship independently.
