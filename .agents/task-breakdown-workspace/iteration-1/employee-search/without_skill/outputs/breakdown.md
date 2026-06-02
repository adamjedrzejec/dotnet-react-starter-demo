# Employee Search Feature — Implementation Breakdown

## Overview

Implement an employee search feature allowing users to search by name, filter by department and active/inactive status, with API pagination support.

**Stack:** .NET 8 Web API + EF Core (backend), React 18 + TypeScript + Tailwind CSS (frontend)

---

## Backend Tasks

### 1. Domain / Data Model

**1.1 — Ensure `Employee` entity has required fields**
- Fields needed: `Id`, `Name`, `DepartmentId` (FK), `ActiveIndicator`
- Add `Department` navigation property if not present
- Create EF Core migration if schema changes are needed

**1.2 — Ensure `Department` entity exists**
- Fields: `Id`, `Name`
- Seed reference data for departments if needed

---

### 2. DTOs

**2.1 — `EmployeeDto`**
```
EmployeeId, Name, DepartmentId, DepartmentName, ActiveIndicator, CreatedDate
```
- All properties decorated with `[JsonPropertyName("camelCase")]`
- XML doc comments on every property
- Boolean uses `activeIndicator` (no `is`/`has` prefix)

**2.2 — `EmployeeQuery` (query parameters)**
```
Name (string?), DepartmentId (int, 0 = no filter), ActiveIndicator (bool?, null = no filter),
Offset (int, default 0), Limit (int, default 20, max 100)
```

**2.3 — Reuse / confirm shared envelope DTOs exist**
- `CollectionResponseDto<T>`, `ItemResponseDto<T>`, `MetadataDto`, `LinksDto`, `ErrorResponseDto`

---

### 3. Repository

**File:** `Repositories/IEmployeeRepository.cs` + `Repositories/EmployeeRepository.cs`

**3.1 — `GetAllAsync(EmployeeQuery query, CancellationToken cancellationToken)`**
- Use `AsNoTracking()`
- Apply `Name` filter: `EF.Functions.Like` for partial match (case-insensitive)
- Apply `DepartmentId` filter: only when `> 0`
- Apply `ActiveIndicator` filter: only when non-null
- Apply `.Skip(query.Offset).Take(query.Limit)`
- Return `(IEnumerable<Employee> items, int totalCount)` tuple (run count query separately)

**3.2 — `GetByIdAsync(int id, CancellationToken cancellationToken)`**
- Include `Department` navigation property

---

### 4. Service

**File:** `Services/IEmployeeService.cs` + `Services/EmployeeService.cs`

**4.1 — `GetAllAsync(EmployeeQuery query, CancellationToken cancellationToken)`**
- Call repository, map `Employee` → `EmployeeDto`
- Build `CollectionResponseDto<EmployeeDto>` with:
  - `Metadata.TotalCount` from count query
  - `Links.Self` = `/v1/employees?offset={offset}&limit={limit}`
  - `Links.Next` / `Links.Prev` based on offset arithmetic

**4.2 — `GetByIdAsync(int id, CancellationToken cancellationToken)`**
- Return `ItemResponseDto<EmployeeDto>` or null

---

### 5. Controller

**File:** `Controllers/EmployeesController.cs`

**5.1 — `GET /v1/employees`**
```csharp
[HttpGet]
[ProducesResponseType(typeof(CollectionResponseDto<EmployeeDto>), 200)]
[ProducesResponseType(typeof(ErrorResponseDto), 400)]
[ProducesResponseType(typeof(ErrorResponseDto), 500)]
public async Task<ActionResult<CollectionResponseDto<EmployeeDto>>> GetAll(
    [FromQuery] EmployeeQuery query,
    CancellationToken cancellationToken)
```

**5.2 — `GET /v1/employees/{id}`**
```csharp
[HttpGet("{id:int}")]
[ProducesResponseType(typeof(ItemResponseDto<EmployeeDto>), 200)]
[ProducesResponseType(typeof(ErrorResponseDto), 404)]
```

---

### 6. Dependency Injection Registration

**File:** `Extensions/ServiceCollectionExtensions.cs`

- Register `IEmployeeRepository → EmployeeRepository` (scoped)
- Register `IEmployeeService → EmployeeService` (scoped)

---

### 7. Backend Tests

**7.1 — `EmployeeRepositoryTests`** (`tests/Api.Tests/Repositories/`)
- Use EF Core InMemory (`Guid.NewGuid()` db name per test), implement `IDisposable`
- `GetAllAsync_WithNameFilter_ReturnsMatchingEmployees`
- `GetAllAsync_WithDepartmentFilter_ReturnsFilteredResults`
- `GetAllAsync_WithActiveFilter_ReturnsActiveOnly`
- `GetAllAsync_WithActiveFilter_ReturnsInactiveOnly`
- `GetAllAsync_WithPagination_ReturnsCorrectPage`
- `GetAllAsync_WithNoFilters_ReturnsAll`

**7.2 — `EmployeeServiceTests`** (`tests/Api.Tests/Services/`)
- Mock `IEmployeeRepository` with Moq
- `GetAllAsync_MapsToDto_Correctly`
- `GetAllAsync_SetsNextLink_WhenMoreResultsExist`
- `GetAllAsync_SetsNullNextLink_WhenOnLastPage`
- `GetByIdAsync_WhenExists_ReturnsEnvelopedDto`
- `GetByIdAsync_WhenNotFound_ReturnsNull`

**7.3 — `EmployeesControllerTests`** (`tests/Api.Tests/Controllers/`)
- Mock `IEmployeeService` with Moq
- `GetAll_ReturnsOk_WithEmployeeCollection`
- `GetAll_PassesQueryParameters_ToService`
- `GetById_ReturnsOk_WhenFound`
- `GetById_Returns404_WhenNotFound`

**7.4 — `TestDataBuilders`** (add to `tests/Api.Tests/Utils/`)
```csharp
CreateEmployee(int id, string name, int deptId, bool active)
CreateDepartment(int id, string name)
```

---

## Frontend Tasks

### 8. Types

**File:** `src/lib/types.ts`

```typescript
export interface Department {
  departmentId: number;
  name: string;
}

export interface Employee {
  employeeId: number;
  name: string;
  departmentId: number;
  departmentName: string;
  activeIndicator: boolean;
  createdDate: string;
}
```

---

### 9. API Client

**File:** `src/lib/api.ts`

**9.1 — `fetchEmployees(params)`**
```typescript
interface EmployeeSearchParams {
  name?: string;
  departmentId?: number;
  activeIndicator?: boolean;
  offset?: number;
  limit?: number;
}
```
- Builds URLSearchParams, omits undefined/empty values
- Returns `ApiResponse<Employee>` (collection envelope)

**9.2 — `fetchDepartments()`**
- `GET /v1/departments` — used to populate filter dropdown
- Returns `ApiResponse<Department>` (collection envelope)

---

### 10. Components

**10.1 — `EmployeeSearchBar`** (`src/components/EmployeeSearchBar/`)
- Controlled input for name search (debounced ~300ms)
- Emits `onSearch(name: string)` callback

**10.2 — `EmployeeFilters`** (`src/components/EmployeeFilters/`)
- Department dropdown (populated from API)
- Active status dropdown: All / Active / Inactive
- Emits `onFilterChange({ departmentId, activeIndicator })` callback

**10.3 — `EmployeeTable`** (`src/components/EmployeeTable/`)
- Columns: Name, Department, Status (Active/Inactive badge)
- Shows loading skeleton while fetching
- Shows empty state message when no results

**10.4 — `EmployeePagination`** (`src/components/EmployeePagination/`)
- Previous / Next buttons driven by `links.prev` / `links.next` from envelope
- Shows current range: "Showing 1–20 of 87"

**10.5 — `EmployeeSearchPage`** (`src/pages/EmployeeSearchPage/`)
- Orchestrates all above components
- Holds state: `name`, `departmentId`, `activeIndicator`, `offset`, `limit`
- Resets `offset` to 0 on any filter/search change
- Uses `useEffect` to trigger `fetchEmployees` on state changes

---

### 11. State & Side Effects (within `EmployeeSearchPage`)

```
[name, departmentId, activeIndicator, offset] ──► fetchEmployees() ──► setEmployees / setTotal
```

- On name change → reset offset, debounce fetch
- On filter change → reset offset, immediate fetch
- On pagination → update offset only

---

## Implementation Order (Recommended)

```
1. Domain entities + migration
2. DTOs (shared envelopes, EmployeeDto, EmployeeQuery)
3. Repository + repository tests
4. Service + service tests
5. Controller + controller tests
6. DI registration
7. Frontend types + API client
8. EmployeeTable + EmployeeSearchBar + EmployeeFilters components
9. EmployeePagination component
10. EmployeeSearchPage (wires everything together)
```

---

## Key Constraints (per org standards)

| Rule | Application |
|------|-------------|
| Route: `/v1/employees` (no `/api/`) | Controller route attribute |
| Boolean: `activeIndicator` not `isActive` | DTO + query param naming |
| Pagination: `offset` + `limit` (not `page`/`pageSize`) | `EmployeeQuery`, frontend params |
| Async: always include `CancellationToken` | All repository/service methods |
| Tests: AAA comments, `#region` grouping | All test files |
| EF: `AsNoTracking()` for reads, no stored procs | Repository implementation |
| InMemory DB (not SQLite) for tests | Repository test setup |

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `Models/Employee.cs` | Modify (ensure fields) |
| `Models/Department.cs` | Create if missing |
| `DTOs/Employees/EmployeeDto.cs` | Create |
| `DTOs/Employees/EmployeeQuery.cs` | Create |
| `Repositories/IEmployeeRepository.cs` | Create |
| `Repositories/EmployeeRepository.cs` | Create |
| `Services/IEmployeeService.cs` | Create |
| `Services/EmployeeService.cs` | Create |
| `Controllers/EmployeesController.cs` | Create |
| `Extensions/ServiceCollectionExtensions.cs` | Modify |
| `tests/.../Repositories/EmployeeRepositoryTests.cs` | Create |
| `tests/.../Services/EmployeeServiceTests.cs` | Create |
| `tests/.../Controllers/EmployeesControllerTests.cs` | Create |
| `tests/.../Utils/TestDataBuilders.cs` | Create/Modify |
| `src/lib/types.ts` | Modify |
| `src/lib/api.ts` | Modify |
| `src/components/EmployeeSearchBar/` | Create |
| `src/components/EmployeeFilters/` | Create |
| `src/components/EmployeeTable/` | Create |
| `src/components/EmployeePagination/` | Create |
| `src/pages/EmployeeSearchPage/` | Create |

**Total: ~20 files created or modified**
