## Task Breakdown: Employee Search Feature

| # | Task | Component | Size | Depends On | Acceptance Criteria |
|---|------|-----------|------|------------|---------------------|
| 1 | Create `Department` entity, `Departments` table, and `DepartmentId` FK column on `Employee` via EF Core migration | Database | M | — | • Migration runs cleanly up and down • `Departments` table present in schema • `Employee` table has non-null `DepartmentId` FK column |
| 2 | Update `Employee` EF entity class and create/update `EmployeeDto` with `departmentId`, `departmentName`, and `activeIndicator` fields (all with `[JsonPropertyName]` and XML doc comments) | Backend | S | #1 | • `EmployeeDto` serializes as `"departmentId"`, `"departmentName"`, `"activeIndicator"` in JSON • Every DTO property has XML doc comment • Boolean uses `activeIndicator` naming (no `is`/`has` prefix) |
| 3 | Create `EmployeeQuery` model with `name` (string), `departmentId` (int), `activeIndicator` (bool?), `offset` (default 0), and `limit` (default 20, max 100) | Backend | S | — | • Query model binds correctly from `[FromQuery]` • `0` for `departmentId` means no filter per convention • Empty `name` string means no filter |
| 4 | Update `IEmployeeRepository` and `EmployeeRepository.GetAllAsync` with LINQ filters: case-insensitive name contains, departmentId match, activeIndicator match, plus a separate `CountAsync` for `totalCount` | Backend | M | #1, #3 | • Name filter applies `EF.Functions.Like` or `.Contains` (case-insensitive) • `departmentId > 0` guard applied before filtering • `activeIndicator` null means no filter; non-null filters exactly • `AsNoTracking()` used for all reads |
| 5 | Update `IEmployeeService` and `EmployeeService.GetAllAsync` to build `CollectionResponseDto<EmployeeDto>` with `totalCount` in metadata, and `self`/`next`/`prev` links derived from offset/limit | Backend | M | #2, #4 | • `metadata.totalCount` reflects filtered record count • `links.next` is null when `offset + limit >= totalCount` • `links.prev` is null when `offset == 0` • `transactionId` is a new `Guid` per request |
| 6 | Update `EmployeesController.GetAll` to accept `[FromQuery] EmployeeQuery`, add `[ProducesResponseType]` for 200/400/500, and register any new scoped services (e.g., `IDepartmentRepository`) in `ServiceCollectionExtensions` | Backend | S | #3, #5 | • `GET /v1/employees` returns `200` with envelope • Route has no `/api/` prefix • All three `[ProducesResponseType]` attributes present • DI container resolves controller without errors |
| 7 | Write `EmployeeRepositoryTests` covering: no-filter returns all, name filter, departmentId filter, activeIndicator filter, and combined filters — using EF InMemory DB with unique `Guid` name per test | Backend / Testing | M | #4 | • Tests use `// Arrange / Act / Assert` structure • At least 5 test cases • Each test creates its own InMemory DB instance • Class implements `IDisposable` |
| 8 | Write `EmployeeServiceTests` covering: search result mapping to DTO, `totalCount` in metadata, `next`/`prev` link generation for first/middle/last pages | Backend / Testing | M | #5 | • Moq used to mock `IEmployeeRepository` • At least 4 test cases • Tests grouped in `#region` blocks per method • Naming follows `{Method}_{Scenario}_{Result}` convention |
| 9 | Add integration test for `GET /v1/employees` covering: unfiltered results, name search, departmentId filter, activeIndicator filter, and pagination using `CustomWebApplicationFactory` with InMemory DB | Testing | M | #6 | • Tests hit the actual HTTP layer via `WebApplicationFactory` • InMemory DB seeded with known test data • Covers both filtered (non-empty) and empty-result scenarios |
| 10 | Update `Employee` TypeScript interface in `lib/types.ts` to add `departmentId` and `departmentName`; add `EmployeeSearchParams` interface; create `EmployeeSearch` component with name text input, department dropdown, and active/inactive status toggle | Frontend | M | — | • `EmployeeSearchParams` includes `name`, `departmentId`, `activeIndicator`, `offset`, `limit` • Component renders three filter controls • No hardcoded user-facing strings (use constants) • Styled with Tailwind utility classes only |
| 11 | Wire `EmployeeSearch` to `GET /v1/employees` via `fetchApi`, display paginated employee list, and render Next/Prev controls driven by `links.next` / `links.prev` from the API envelope | Frontend | M | #10 | • Results update on filter change • Next button disabled/hidden when `links.next` is null • Prev button disabled/hidden when `links.prev` is null • Loading and error states handled visibly |

## ⚠️ Risk Assessment

### Task #1 — Create `Department` entity, `Departments` table, and `DepartmentId` FK on `Employee`
**Risk type:** Database migration  
**Risk:** Adding a non-nullable FK column to an existing `Employee` table will fail if rows already exist and no default/seed value is provided.  
**Mitigation:** Make `DepartmentId` nullable initially (or provide a default seed department record); test the migration up/down in a lower environment before deploying to production; prepare a rollback migration script and wrap the new filter UI behind a feature flag until the migration is confirmed stable.

### Task #4 — Update `EmployeeRepository.GetAllAsync` with search/filter LINQ
**Risk type:** Shared code path  
**Risk:** `GetAllAsync` may already be called by other features (e.g., reporting, export); changing its signature or query behaviour could silently break callers that do not pass the new `EmployeeQuery` fields.  
**Mitigation:** Ensure all existing callers pass a valid `EmployeeQuery` with neutral defaults (`name = ""`, `departmentId = 0`, `activeIndicator = null`); run the full unit and integration test suite before merging; perform a manual smoke test of any other feature that reads employees.

---

## Summary

11 tasks spanning an estimated **3–5 days** of focused development (roughly 2–3 days backend, 1–2 days frontend). The critical path is **#1 → #2 → #3 → #4 → #5 → #6**, which must complete before the controller is production-ready. Once #6 is done, tasks #7, #8, #9, and #11 can all run in parallel — the test tasks (#7–#9) are independent of each other, and frontend task #10 has no backend dependency and can start immediately. The migration in task #1 carries the highest deployment risk and should be addressed first. Scope is appropriate for a single sprint story and does not warrant splitting.
