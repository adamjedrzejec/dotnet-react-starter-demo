---
name: create-api-endpoint
description: Generates a new API endpoint following organization standards
---

# Create API Endpoint

Generate a new RESTful API endpoint with all required layers.

## Input Required

- Resource name (e.g., "employees", "companies")
- Entity properties with types
- Required operations (GET all, GET by ID, POST, PUT, DELETE)

## Output Structure

1. **DTO** in `DTOs/{Feature}/`
   - Request DTO (if needed)
   - Response DTO with XML documentation
   - All properties use `[JsonPropertyName]`

2. **Repository** in `Repositories/`
   - Interface: `I{Name}Repository.cs`
   - Implementation: `{Name}Repository.cs`
   - Use `AsNoTracking()` for reads

3. **Service** in `Services/`
   - Interface: `I{Name}Service.cs`
   - Implementation: `{Name}Service.cs`
   - Return envelope DTOs

4. **Controller** in `Controllers/`
   - Route: `v1/[controller]` (no /api/ prefix)
   - All async methods have CancellationToken
   - ProducesResponseType attributes

5. **Tests** in `tests/Api.Tests/`
   - Controller, Service, Repository tests
   - AAA pattern with comments
   - #region organization

## Standards Checklist

- [ ] Route uses v1/ prefix (not api/)
- [ ] Boolean properties end with Indicator
- [ ] Pagination uses offset/limit
- [ ] All responses wrapped in envelope
- [ ] CancellationToken on all async methods
- [ ] XML documentation on DTOs
