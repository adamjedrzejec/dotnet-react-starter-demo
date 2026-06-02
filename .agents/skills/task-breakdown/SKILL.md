---
name: task-breakdown
description: >
  Breaks down feature requests and user stories into actionable, implementable sub-tasks
  with T-shirt size estimates, component ownership, dependencies, and acceptance criteria.
  Includes a risk assessment step for tasks that touch shared code paths or require
  database migrations.

  Use this skill whenever the user asks to break down, decompose, plan, or scope a
  feature or user story — even if they say things like "what would it take to build X",
  "help me plan this feature", "how should we tackle this", "estimate this story",
  or "what tasks are involved in X". If there's a feature to plan, invoke this skill.
---

# Task Breakdown Skill

Your job is to take a feature request or user story and decompose it into a well-structured
set of implementable sub-tasks, complete with estimates, component labels, dependencies,
and acceptance criteria. The goal is a breakdown a developer could pick up and start
working from immediately.

---

## Step 1 — Understand the Feature

Before breaking anything down, make sure you understand the feature well enough to split
it correctly. If you're missing context, ask one focused question. Don't ask multiple
questions at once — pick the most important gap and fill it.

Things to look for:
- What system or codebase does this touch? (backend, frontend, database, infra?)
- Are there existing patterns or conventions to follow?
- Is this greenfield or modifying existing code?
- Who are the end users and what outcome do they need?

If the context is already clear from the conversation, skip straight to Step 2.

---

## Step 2 — Decompose into Sub-Tasks

Break the feature into the smallest independently-shippable tasks you can. Each task
should be something one developer can complete without being blocked by in-progress work
on another task (except where explicit dependencies exist).

Aim for 4–12 tasks for a typical feature. If you find yourself listing 15+, consider
grouping related tasks or raising a flag that the feature might need to be split into
smaller stories.

Think in layers:
- Data model / migration tasks first
- Backend service/API tasks next
- Frontend / UI tasks after
- Testing, docs, and config last

---

## Step 3 — Assign T-Shirt Sizes

For each task, assign a T-shirt size based on effort and complexity:

| Size | Meaning |
|------|---------|
| **XS** | < 1 hour — trivial change, no branching logic (e.g., add a config flag, update a label) |
| **S**  | 1–4 hours — small focused change, clear path (e.g., add a new DTO field, simple CRUD endpoint) |
| **M**  | 4–8 hours — moderate work, some decisions needed (e.g., new service method with tests) |
| **L**  | 1–2 days — significant scope, multiple moving parts (e.g., new entity + repository + service + controller) |
| **XL** | 2–5 days — large/complex, warrants splitting if possible (e.g., auth system, multi-tenant data model) |

When in doubt, size up. Underestimation is more costly than overestimation in planning.

---

## Step 4 — Output the Breakdown Table

Present the task breakdown as a Markdown table with these exact columns:

| # | Task | Component | Size | Depends On | Acceptance Criteria |
|---|------|-----------|------|------------|---------------------|

- **#**: Sequential number (1, 2, 3…)
- **Task**: Imperative verb phrase, specific enough to act on (e.g., "Add `active` column to employees table via EF Core migration")
- **Component**: One of `Backend`, `Frontend`, `Database`, `Infra`, `Testing`, `Docs` (use multiple if needed, e.g., `Backend / Testing`)
- **Size**: XS / S / M / L / XL
- **Depends On**: Task numbers this task is blocked by (e.g., `#1, #2`), or `—` if none
- **Acceptance Criteria**: 1–3 bullet points describing what "done" looks like, written as observable outcomes

---

## Step 5 — Risk Assessment

After the table, scan for tasks that match any of these risk patterns and call them out
in a **⚠️ Risk Assessment** section:

**Database migrations**
Any task involving a schema change (new table, new column, rename, drop) carries
deployment risk. Flag it and suggest: migration tested in a lower environment first,
rollback script prepared, and feature flag wrapping the consuming code.

**Shared code paths**
Tasks modifying code used by multiple features (e.g., base services, shared utilities,
auth middleware, base controllers) risk unintended side effects. Flag these and suggest:
targeted unit tests for the affected shared code, and a manual smoke test of dependent
features before merging.

**Cross-cutting concerns**
Tasks that touch auth, logging, error handling, or caching patterns — suggest extra
review since these tend to have broad blast radius.

Format the risk section like this:

```
## ⚠️ Risk Assessment

### Task #N — [Task Title]
**Risk type:** Database migration / Shared code path / Cross-cutting
**Risk:** [One sentence describing what could go wrong]
**Mitigation:** [One or two concrete actions to reduce the risk]
```

If no tasks carry elevated risk, write: `No elevated risks identified.`

---

## Step 6 — Summary

Close with a brief summary (3–5 sentences) that:
1. States the total task count and overall effort range (sum of size ranges)
2. Identifies the critical path (the sequence of tasks that must finish before the feature is shippable)
3. Notes any tasks that are good candidates to parallelize
4. Flags if the feature scope seems larger than expected and might warrant splitting

---

## Example Output Shape

```markdown
## Task Breakdown: [Feature Name]

| # | Task | Component | Size | Depends On | Acceptance Criteria |
|---|------|-----------|------|------------|---------------------|
| 1 | Add `paidIndicator` column to invoices table via EF Core migration | Database | S | — | • Migration runs cleanly up/down • Column present in DB schema |
| 2 | Update `Invoice` entity and `InvoiceDto` to include `paidIndicator` | Backend | S | #1 | • Property serializes as `"paidIndicator"` in API response • XML doc present on DTO |
| 3 | Add `MarkAsPaidAsync` method to `InvoiceService` with unit tests | Backend / Testing | M | #2 | • Service method updates `paidIndicator` to `true` • At least 2 unit tests cover happy path and not-found case |
| 4 | Add `PATCH /v1/invoices/{id}/mark-paid` endpoint | Backend | S | #3 | • Returns 200 with updated invoice envelope • Returns 404 if invoice not found |
| 5 | Display paid badge on invoice list in frontend | Frontend | S | #4 | • Badge shows for paid invoices • Hidden for unpaid invoices |
| 6 | Add integration test for mark-paid flow | Testing | M | #4 | • Integration test covers 200 and 404 paths |

## ⚠️ Risk Assessment

### Task #1 — Add `paidIndicator` column via EF migration
**Risk type:** Database migration
**Risk:** Schema change could fail or require manual intervention in production if data already exists.
**Mitigation:** Test migration on staging first; prepare a rollback migration; wrap the UI changes in a feature flag until the migration is confirmed stable.

## Summary

6 tasks, estimated 1.5–2.5 days of focused development. The critical path is #1 → #2 → #3 → #4, so backend work needs to complete before frontend can start. Tasks #5 and #6 can run in parallel once #4 is done. Scope looks appropriate for a single sprint story.
```
