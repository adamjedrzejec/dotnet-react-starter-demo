---
name: naming-checker
description: >
  Validates file and variable naming conventions against project standards. Scans the
  codebase for naming violations including camelCase filenames that should be kebab-case,
  incorrect boolean prefixes (is/has instead of Indicator suffix), abbreviations in
  identifiers, and type/constant naming issues.

  Use this skill whenever the user asks to check naming conventions, lint names, validate
  file names, audit code style for naming, or asks things like "are my file names correct",
  "check for naming issues", "does this follow our conventions", or "scan for style
  violations". Also use when the user mentions kebab-case, camelCase file problems, or
  naming standards compliance.
allowed-tools: shell
---

# Naming Checker

You are a naming convention validator for a full-stack project (.NET 8 backend + React/TypeScript frontend). Your job is to scan the codebase and report naming violations against the project's established standards, outputting findings as a structured table.

## Conventions to Enforce

### File Naming

| Location | Convention | Example |
|----------|-----------|---------|
| Frontend `src/` files | kebab-case (`.tsx`, `.ts`, `.css`) | `user-profile.tsx`, `api-client.ts` |
| Frontend components | PascalCase directory + PascalCase file | `UserProfile/UserProfile.tsx` |
| Backend C# files | PascalCase | `EmployeeService.cs`, `CompanyRepository.cs` |
| Test files | PascalCase with `Tests` suffix | `EmployeeServiceTests.cs` |

**Exclusions** — do NOT flag these as violations:
- Files containing `.agent.md` (agent configuration files)
- Files containing `.test.` (test files may use different conventions)
- Files in `node_modules/`, `bin/`, `obj/`, `.git/`, `.agents/` directories
- Configuration root files (`vite.config.ts`, `tailwind.config.js`, `tsconfig.json`, etc.)

### Variable & Property Naming

| Context | Convention | Good | Bad |
|---------|-----------|------|-----|
| Boolean properties | `Indicator` suffix, no `is`/`has` prefix | `activeIndicator` | `isActive`, `hasPermission` |
| Date properties | `Date` suffix | `createdDate` | `createdAt`, `created_at` |
| C# async methods | `Async` suffix | `GetByIdAsync` | `GetById` (if async) |
| TypeScript interfaces | PascalCase, no `I` prefix | `Employee` | `IEmployee` |
| Constants (TS) | UPPER_SNAKE_CASE or PascalCase | `MAX_RETRY_COUNT` | `maxRetryCount` |
| C# constants | PascalCase | `MaxRetryCount` | `MAX_RETRY_COUNT` |
| Abbreviations | Expand fully | `number`, `verification` | `nr`, `dv` |

### Type Naming

| Type | Convention | Example |
|------|-----------|---------|
| C# DTOs | PascalCase with `Dto` suffix | `EmployeeDto` |
| C# interfaces | `I` prefix + PascalCase | `IEmployeeService` |
| C# services | PascalCase with `Service` suffix | `EmployeeService` |
| C# repositories | PascalCase with `Repository` suffix | `EmployeeRepository` |
| TS interfaces | PascalCase (no prefix) | `ApiResponse` |
| TS type aliases | PascalCase | `ButtonVariant` |

## How to Check

### Step 1: Run the file naming scanner

Execute the bundled script to find filename violations:

```bash
bash <skill-path>/scripts/check-file-names.sh <project-root>
```

This scans for frontend files that use camelCase instead of kebab-case (excluding the documented exceptions).

### Step 2: Scan for variable/property naming issues

Use grep to search for common violations in the source code:

**Boolean prefix violations (C# and TypeScript):**
```bash
grep -rn --include="*.cs" --include="*.ts" --include="*.tsx" \
  -E '\b(is[A-Z]|has[A-Z]|Is[A-Z]|Has[A-Z])[a-zA-Z]*\b' \
  src/ backend/src/ 2>/dev/null | grep -v node_modules | grep -v '.test.'
```

**Abbreviation violations:**
```bash
grep -rn --include="*.cs" --include="*.ts" --include="*.tsx" \
  -E '\b(nr|dv|qty|amt|desc|addr|dept|mgr|emp)\b' \
  src/ backend/src/ 2>/dev/null | grep -v node_modules
```

### Step 3: Check async methods missing `Async` suffix

```bash
grep -rn --include="*.cs" \
  -E 'async Task.*\s+[A-Z][a-zA-Z]+\(' backend/src/ 2>/dev/null | \
  grep -v 'Async(' | grep -v node_modules
```

### Step 4: Output findings table

Present ALL findings in this table format:

```
| # | File | Line | Issue | Severity | Suggested Fix |
|---|------|------|-------|----------|---------------|
| 1 | src/components/userCard.tsx | — | camelCase filename | 🔴 High | Rename to `user-card.tsx` |
| 2 | backend/src/Api/Services/EmployeeService.cs | 42 | Boolean uses `is` prefix: `isActive` | 🟡 Medium | Rename to `activeIndicator` |
| 3 | src/lib/api.ts | 15 | Abbreviation: `desc` | 🟢 Low | Expand to `description` |
```

**Severity levels:**
- 🔴 **High** — File naming violations (affects imports, CI, and cross-platform compatibility)
- 🟡 **Medium** — Property/variable naming violations (breaks API contract conventions)
- 🟢 **Low** — Abbreviations, missing suffixes (readability concern)

### Step 5: Summary

After the table, provide:
1. Total violation count by severity
2. Top recommendation (the single highest-impact fix)
3. Whether any violations would break the API contract (boolean properties serialized without `Indicator` suffix)

## Important Notes

- Only report actual violations — do not flag files or patterns that are correct
- If zero violations are found, say so clearly: "✅ No naming violations detected"
- When in doubt about whether something is a violation, mark it 🟢 Low and note the ambiguity
- The script and grep patterns are starting points — use judgment for edge cases
