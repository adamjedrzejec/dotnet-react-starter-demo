# CSV Export Endpoint for Invoices — Implementation Breakdown

> **Scope:** Backend only. New endpoint `GET /v1/invoices/export` that accepts the same query filters as `GET /v1/invoices` and returns a downloadable CSV file.
>
> **Codebase context:** The project is a .NET 8 Web API starter. There is **no existing invoices feature** — no domain model, no DbContext, no controller, no service. Everything must be scaffolded from scratch following org standards.

---

## Prerequisites / Assumptions

| # | Assumption |
|---|------------|
| A1 | An `Invoice` entity needs to be defined (fields TBD by domain; reasonable defaults used below). |
| A2 | A `AppDbContext` (EF Core) must be created and wired up — it doesn't exist yet. |
| A3 | CSV generation uses a manual `StringBuilder` approach (no third-party lib) to avoid adding a new dependency. If `CsvHelper` is preferred, add Step 1a. |
| A4 | The export endpoint streams all matching records (no pagination limit) since it's a file download. |
| A5 | The `Content-Disposition` header uses `attachment; filename="invoices.csv"`. |

---

## Implementation Tasks

### Phase 1 — Domain & Persistence

---

#### Task 1 · Define the `Invoice` domain entity
**File:** `src/Api/Domain/Invoice.cs`

Create the EF Core entity class. Suggested fields (adjust to actual schema):

```csharp
public class Invoice
{
    public int InvoiceId { get; set; }
    public int CompanyId { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly IssuedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public bool PaidIndicator { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
```

**Naming rules to follow:** boolean → `PaidIndicator`, dates → `IssuedDate`/`DueDate`, ID → `InvoiceId`.

---

#### Task 2 · Create `AppDbContext`
**File:** `src/Api/Domain/AppDbContext.cs`

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Invoice> Invoices => Set<Invoice>();
}
```

Register it in `ServiceCollectionExtensions.AddApplicationServices()`:
```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("Default")));
```

> **Note:** `Program.cs` already calls `AddApplicationServices()`. The method signature must accept `IConfiguration` (or use `IServiceCollection` + `IConfiguration` overload) to pass the connection string. Update `Program.cs` accordingly.

---

#### Task 3 · Add EF Core migration
**Command:**
```bash
dotnet ef migrations add AddInvoices --project src/Api
dotnet ef database update --project src/Api
```

> Only required if using a real database. For dev/test with InMemory this step is skipped.

---

### Phase 2 — Query Object & DTOs

---

#### Task 4 · Create `InvoiceQuery` (filter/pagination object)
**File:** `src/Api/DTOs/Invoices/InvoiceQuery.cs`

Mirror the same filter set used by `GET /v1/invoices`. Per org standards: `0` = no filter, `offset`/`limit` for pagination.

```csharp
/// <summary>Query parameters for filtering and paginating invoices.</summary>
public class InvoiceQuery
{
    /// <summary>Filter by company. 0 = all companies.</summary>
    [JsonPropertyName("companyId")]
    public int CompanyId { get; set; } = 0;

    /// <summary>Filter by paid status. null = all.</summary>
    [JsonPropertyName("paidIndicator")]
    public bool? PaidIndicator { get; set; }

    /// <summary>Filter invoices issued on or after this date.</summary>
    [JsonPropertyName("issuedFromDate")]
    public DateOnly? IssuedFromDate { get; set; }

    /// <summary>Filter invoices issued on or before this date.</summary>
    [JsonPropertyName("issuedToDate")]
    public DateOnly? IssuedToDate { get; set; }

    /// <summary>Number of records to skip. Default: 0.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    /// <summary>Maximum records to return. Default: 20. Max: 100.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}
```

---

#### Task 5 · Create `InvoiceDto`
**File:** `src/Api/DTOs/Invoices/InvoiceDto.cs`

```csharp
/// <summary>Represents a single invoice returned by the API.</summary>
public class InvoiceDto
{
    /// <summary>Unique identifier of the invoice.</summary>
    [JsonPropertyName("invoiceId")]
    public int InvoiceId { get; set; }

    /// <summary>Identifier of the associated company.</summary>
    [JsonPropertyName("companyId")]
    public int CompanyId { get; set; }

    /// <summary>Invoice number / reference.</summary>
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    /// <summary>Total invoice amount.</summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>Date the invoice was issued.</summary>
    [JsonPropertyName("issuedDate")]
    public DateOnly IssuedDate { get; set; }

    /// <summary>Date payment is due.</summary>
    [JsonPropertyName("dueDate")]
    public DateOnly DueDate { get; set; }

    /// <summary>Indicates whether the invoice has been paid.</summary>
    [JsonPropertyName("paidIndicator")]
    public bool PaidIndicator { get; set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}
```

All properties have `[JsonPropertyName]` and XML docs per org standards.

---

### Phase 3 — Repository

---

#### Task 6 · Define `IInvoiceRepository`
**File:** `src/Api/Repositories/IInvoiceRepository.cs`

```csharp
public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync(InvoiceQuery query, CancellationToken cancellationToken);
    Task<int> CountAsync(InvoiceQuery query, CancellationToken cancellationToken);
    // Export: no pagination, same filters
    Task<IEnumerable<Invoice>> GetAllForExportAsync(InvoiceQuery query, CancellationToken cancellationToken);
}
```

> `GetAllForExportAsync` applies the same filters but **ignores offset/limit** so all matching records are returned for the CSV file.

---

#### Task 7 · Implement `InvoiceRepository`
**File:** `src/Api/Repositories/InvoiceRepository.cs`

- Inject `AppDbContext`
- Use `AsNoTracking()` for all reads
- Apply filters with the `query.CompanyId > 0` pattern
- `GetAllAsync` → applies `Skip(offset).Take(limit)`
- `GetAllForExportAsync` → same filters, **no** `Skip`/`Take`

```csharp
public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;
    public InvoiceRepository(AppDbContext context) => _context = context;

    private IQueryable<Invoice> BuildBaseQuery(InvoiceQuery query)
    {
        var q = _context.Invoices.AsNoTracking();
        if (query.CompanyId > 0)
            q = q.Where(i => i.CompanyId == query.CompanyId);
        if (query.PaidIndicator.HasValue)
            q = q.Where(i => i.PaidIndicator == query.PaidIndicator.Value);
        if (query.IssuedFromDate.HasValue)
            q = q.Where(i => i.IssuedDate >= query.IssuedFromDate.Value);
        if (query.IssuedToDate.HasValue)
            q = q.Where(i => i.IssuedDate <= query.IssuedToDate.Value);
        return q;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(InvoiceQuery query, CancellationToken cancellationToken)
        => await BuildBaseQuery(query).Skip(query.Offset).Take(query.Limit).ToListAsync(cancellationToken);

    public async Task<int> CountAsync(InvoiceQuery query, CancellationToken cancellationToken)
        => await BuildBaseQuery(query).CountAsync(cancellationToken);

    public async Task<IEnumerable<Invoice>> GetAllForExportAsync(InvoiceQuery query, CancellationToken cancellationToken)
        => await BuildBaseQuery(query).ToListAsync(cancellationToken);
}
```

---

### Phase 4 — Service

---

#### Task 8 · Define `IInvoiceService`
**File:** `src/Api/Services/IInvoiceService.cs`

```csharp
public interface IInvoiceService
{
    Task<CollectionResponseDto<InvoiceDto>> GetAllAsync(InvoiceQuery query, CancellationToken cancellationToken);
    Task<byte[]> ExportCsvAsync(InvoiceQuery query, CancellationToken cancellationToken);
}
```

---

#### Task 9 · Implement `InvoiceService`
**File:** `src/Api/Services/InvoiceService.cs`

- `GetAllAsync`: maps `Invoice` → `InvoiceDto`, builds envelope with metadata + links
- `ExportCsvAsync`: calls `GetAllForExportAsync`, maps to `InvoiceDto`, serialises to CSV bytes using `StringBuilder`

CSV serialisation approach (no external dependency):

```csharp
private static byte[] BuildCsv(IEnumerable<InvoiceDto> invoices)
{
    var sb = new StringBuilder();
    sb.AppendLine("InvoiceId,CompanyId,Number,Amount,IssuedDate,DueDate,PaidIndicator,CreatedDate");
    foreach (var inv in invoices)
    {
        sb.AppendLine(string.Join(',',
            inv.InvoiceId,
            inv.CompanyId,
            $"\"{inv.Number}\"",   // quote string fields to handle commas
            inv.Amount,
            inv.IssuedDate,
            inv.DueDate,
            inv.PaidIndicator,
            inv.CreatedDate.ToString("O")));
    }
    return Encoding.UTF8.GetBytes(sb.ToString());
}
```

> **Alternative:** Add `CsvHelper` NuGet package for full RFC 4180 compliance (handles quotes/newlines in field values). Recommended if invoice data contains free-text fields.

---

### Phase 5 — Controller

---

#### Task 10 · Add export action to `InvoicesController`
**File:** `src/Api/Controllers/InvoicesController.cs`

The controller exposes two actions under `[Route("v1/[controller]")]`:

**`GET /v1/invoices`** — paginated list (existing action):
```csharp
[HttpGet]
[ProducesResponseType(typeof(CollectionResponseDto<InvoiceDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<CollectionResponseDto<InvoiceDto>>> GetAll(
    [FromQuery] InvoiceQuery query,
    CancellationToken cancellationToken)
{
    var result = await _service.GetAllAsync(query, cancellationToken);
    return Ok(result);
}
```

**`GET /v1/invoices/export`** — CSV download (new action):
```csharp
/// <summary>
/// Exports invoices matching the given filters as a downloadable CSV file.
/// </summary>
[HttpGet("export")]
[Produces("text/csv")]
[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> ExportCsv(
    [FromQuery] InvoiceQuery query,
    CancellationToken cancellationToken)
{
    var csvBytes = await _service.ExportCsvAsync(query, cancellationToken);
    var fileName = $"invoices-{DateTime.UtcNow:yyyyMMdd}.csv";
    return File(csvBytes, "text/csv", fileName);
}
```

Key points:
- Route is `[HttpGet("export")]` — resolves to `GET /v1/invoices/export`
- Returns `FileContentResult` via `File(bytes, contentType, downloadName)`
- `Content-Disposition: attachment; filename="invoices-20260206.csv"` is set automatically by ASP.NET Core
- Pagination fields (`offset`/`limit`) in `InvoiceQuery` are **ignored** for this action — the service handles that

---

### Phase 6 — Dependency Injection

---

#### Task 11 · Register services in `ServiceCollectionExtensions`
**File:** `src/Api/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<IInvoiceRepository, InvoiceRepository>();
services.AddScoped<IInvoiceService, InvoiceService>();
```

Also wire up `AppDbContext` here (see Task 2).

---

### Phase 7 — Tests

---

#### Task 12 · Repository tests — `InvoiceRepositoryTests`
**File:** `tests/Api.Tests/Repositories/InvoiceRepositoryTests.cs`

- Implements `IDisposable`; uses EF Core **InMemory** database with `Guid.NewGuid()` name per test
- Test cases (following `{Method}_{Scenario}_{Result}` naming):

| Test name | Verifies |
|-----------|----------|
| `GetAllAsync_WithNoFilter_ReturnsAllInvoices` | No filters → all seeded records returned |
| `GetAllAsync_WithCompanyFilter_ReturnsFilteredInvoices` | `CompanyId > 0` filter applied |
| `GetAllAsync_WithPaidFilter_ReturnsOnlyPaidInvoices` | `PaidIndicator` filter applied |
| `GetAllAsync_WithDateRange_ReturnsInvoicesInRange` | `IssuedFromDate`/`IssuedToDate` filters |
| `GetAllAsync_WithPagination_RespectsOffsetAndLimit` | `Skip`/`Take` behaves correctly |
| `CountAsync_WithFilter_ReturnsMatchingCount` | Count matches filtered set size |
| `GetAllForExportAsync_WithFilter_ReturnsAllMatchingWithoutPagination` | Export ignores offset/limit |

---

#### Task 13 · Service tests — `InvoiceServiceTests`
**File:** `tests/Api.Tests/Services/InvoiceServiceTests.cs`

- Uses `Moq` to mock `IInvoiceRepository`
- `#region` blocks per method group

| Test name | Verifies |
|-----------|----------|
| `GetAllAsync_WhenInvoicesExist_ReturnsEnvelopedCollection` | Response wraps items in `CollectionResponseDto` |
| `GetAllAsync_WhenEmpty_ReturnsEmptyCollection` | Empty list handled gracefully |
| `GetAllAsync_SetsCorrectTotalCount` | Metadata `TotalCount` is set from `CountAsync` result |
| `ExportCsvAsync_WhenInvoicesExist_ReturnsCsvBytes` | Returns non-empty byte array |
| `ExportCsvAsync_CsvContainsHeader` | First line is the CSV header |
| `ExportCsvAsync_CsvRowCountMatchesInvoiceCount` | One data row per invoice |
| `ExportCsvAsync_WhenEmpty_ReturnsHeaderOnly` | Empty dataset → header only, no data rows |

---

#### Task 14 · Controller tests — `InvoicesControllerTests`
**File:** `tests/Api.Tests/Controllers/InvoicesControllerTests.cs`

- Uses `Moq` to mock `IInvoiceService`

| Test name | Verifies |
|-----------|----------|
| `GetAll_ReturnsOkWithCollectionEnvelope` | 200 + `CollectionResponseDto` body |
| `GetAll_PassesQueryToService` | Filters forwarded to service |
| `ExportCsv_ReturnsFileContentResult` | Returns `FileContentResult` |
| `ExportCsv_ContentTypeIsTextCsv` | `Content-Type: text/csv` |
| `ExportCsv_FileNameContainsDate` | `Content-Disposition` filename set |
| `ExportCsv_PassesQueryToService` | Same filters forwarded to service |

---

#### Task 15 · Add `CreateInvoice` to `TestDataBuilders`
**File:** `tests/Api.Tests/Utils/TestDataBuilders.cs`

```csharp
public static Invoice CreateInvoice(
    int invoiceId = 1,
    int companyId = 1,
    string number = "INV-001",
    decimal amount = 100.00m,
    bool paid = false)
{
    return new Invoice
    {
        InvoiceId = invoiceId,
        CompanyId = companyId,
        Number = number,
        Amount = amount,
        IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow),
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        PaidIndicator = paid,
        CreatedDate = DateTime.UtcNow,
        UpdatedDate = DateTime.UtcNow
    };
}
```

---

## Summary: Files to Create / Modify

| Action | File |
|--------|------|
| ✅ Create | `src/Api/Domain/Invoice.cs` |
| ✅ Create | `src/Api/Domain/AppDbContext.cs` |
| ✅ Create | `src/Api/DTOs/Invoices/InvoiceQuery.cs` |
| ✅ Create | `src/Api/DTOs/Invoices/InvoiceDto.cs` |
| ✅ Create | `src/Api/Repositories/IInvoiceRepository.cs` |
| ✅ Create | `src/Api/Repositories/InvoiceRepository.cs` |
| ✅ Create | `src/Api/Services/IInvoiceService.cs` |
| ✅ Create | `src/Api/Services/InvoiceService.cs` |
| ✅ Create | `src/Api/Controllers/InvoicesController.cs` |
| ✏️ Modify | `src/Api/Extensions/ServiceCollectionExtensions.cs` |
| ✅ Create | `tests/Api.Tests/Repositories/InvoiceRepositoryTests.cs` |
| ✅ Create | `tests/Api.Tests/Services/InvoiceServiceTests.cs` |
| ✅ Create | `tests/Api.Tests/Controllers/InvoicesControllerTests.cs` |
| ✏️ Modify | `tests/Api.Tests/Utils/TestDataBuilders.cs` |

**Total: 12 new files, 2 modified files**

---

## Gotchas & Notes

1. **No `DbContext` exists yet.** `AppDbContext` must be created *and* registered in DI before anything else compiles.
2. **`Program.cs` needs `IConfiguration` threading** if the DB connection string is read inside `AddApplicationServices` — pass it through or use `IServiceCollection`'s extension pattern.
3. **CSV quoting:** The simple `StringBuilder` approach does not handle commas/quotes inside `Number` field values. If invoice numbers can contain commas, use proper RFC 4180 escaping or add `CsvHelper`.
4. **Export has no pagination limit** — for large datasets, consider adding a hard maximum row count (e.g. 10,000) and returning `400` if exceeded, or streaming the response instead of buffering `byte[]`.
5. **CORS:** The `Content-Disposition` header is already in `WithExposedHeaders` via `Program.cs` — no change needed.
6. **Swagger `[Produces("text/csv")]`:** The `[ApiController]` default produces `application/json`. Add `[Produces("text/csv")]` explicitly on the export action so Swagger documents it correctly, and remove the controller-level `[Produces("application/json")]` or scope it only to the list action.
