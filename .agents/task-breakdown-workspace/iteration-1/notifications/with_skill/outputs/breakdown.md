## Task Breakdown: In-App Notification System

| # | Task | Component | Size | Depends On | Acceptance Criteria |
|---|------|-----------|------|------------|---------------------|
| 1 | Create `Notification` entity and EF Core migration with `notificationId`, `userId`, `message`, `notificationType`, `readIndicator`, and `createdDate` fields | Database | S | — | • Migration runs `up`/`down` cleanly • Table present with correct column names and types • `readIndicator` defaults to `false` |
| 2 | Create `NotificationDto` with full XML documentation and `[JsonPropertyName("camelCase")]` on all properties | Backend | S | #1 | • All properties serialise with camelCase JSON names • Boolean field named `readIndicator` (not `isRead`) • Every property has an XML `<summary>` doc |
| 3 | Implement `INotificationRepository` and `NotificationRepository` with `GetAllAsync` (supporting userId + unread filter + offset/limit), `GetByIdAsync`, `CreateAsync`, and `MarkAsReadAsync` | Backend | S | #1 | • All reads use `AsNoTracking()` • Offset/limit pagination applied via `Skip`/`Take` • `GetAllAsync` can filter to unread-only records |
| 4 | Implement `INotificationService` and `NotificationService` containing `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, and `MarkAsReadAsync`; register all notification types in `ServiceCollectionExtensions` | Backend | M | #3 | • Every async method has `CancellationToken` as final parameter • `GetAllAsync` returns `CollectionResponseDto<NotificationDto>` with `totalCount` in metadata • Both service and repository registered via `AddScoped` |
| 5 | Add `GET /v1/notifications` endpoint supporting `offset`, `limit`, and optional `unreadOnly` query parameters | Backend | S | #4 | • Returns `CollectionResponseDto<NotificationDto>` envelope • Defaults: `offset=0`, `limit=20`, max `limit=100` • `[ProducesResponseType]` decorators present for 200, 400, and 500 |
| 6 | Add `PATCH /v1/notifications/{id}/mark-read` endpoint | Backend | S | #4 | • Returns 200 with `ItemResponseDto<NotificationDto>` on success • Returns 404 with error code `ORG-NTF-001` if notification not found • Sets `readIndicator` to `true` and persists |
| 7 | Create `INotificationPublisher` / `NotificationPublisher` service and wire `PublishAsync` calls into existing invoice-overdue and new-employee event trigger points | Backend | L | #4 | • `PublishAsync` persists a `Notification` record via the service layer • Invoice-overdue trigger produces a notification with `notificationType = "invoice_overdue"` • New-employee trigger produces a notification with `notificationType = "new_employee"` |
| 8 | Add unit tests for `NotificationService` covering `GetAllAsync`, `GetByIdAsync`, and `MarkAsReadAsync` | Backend / Testing | M | #4 | • Uses Moq for repository mock • Covers happy-path and not-found cases for each method • All tests follow `{Method}_{Scenario}_{ExpectedResult}` naming with `// Arrange / Act / Assert` comments and `#region` grouping |
| 9 | Add repository tests for `NotificationRepository` using EF Core InMemory database | Testing | S | #3 | • Each test creates a unique DB via `Guid.NewGuid()` • Class implements `IDisposable` for context cleanup • Covers unfiltered, userId-filtered, and unread-only query paths |
| 10 | Add `Notification` interface to `lib/types.ts` and `getNotifications` / `markNotificationRead` fetch functions to `lib/api.ts` | Frontend | S | #5, #6 | • `Notification` type uses `readIndicator` (not `isRead`) • Both functions use the shared `fetchApi<T>` wrapper • API paths exactly match `/v1/notifications` and `/v1/notifications/{id}/mark-read` |
| 11 | Build `NotificationBell` component displaying a bell icon with an unread-count badge | Frontend | S | #10 | • Badge is hidden when unread count is 0 • Count reflects latest data from `getNotifications` • Styled exclusively with Tailwind utility classes, no hardcoded user-facing strings |
| 12 | Build `NotificationList` component rendering notification items with a per-item mark-as-read action | Frontend | M | #10 | • List fetches from `getNotifications` and re-renders on data change • Clicking mark-as-read calls `markNotificationRead` and updates local state optimistically • No hardcoded strings; unread items visually distinguished from read ones |
| 13 | Integrate `NotificationBell` and `NotificationList` into the nav; add integration tests for `GET /v1/notifications` and `PATCH /v1/notifications/{id}/mark-read` | Frontend / Testing | M | #11, #12 | • Bell icon visible in nav and toggles the list on click • Integration tests cover 200 path for GET and both 200 and 404 paths for PATCH • `CustomWebApplicationFactory` uses InMemory database |

---

## ⚠️ Risk Assessment

### Task #1 — Create `Notification` entity and EF Core migration

**Risk type:** Database migration  
**Risk:** A schema change deployed to production without testing could fail mid-migration or leave the database in a partially migrated state.  
**Mitigation:** Run the migration against a staging/lower environment first and verify the `up`/`down` scripts; prepare a rollback migration script; wrap the new notification endpoints behind a feature flag until the migration is confirmed stable in production.

### Task #7 — Wire `NotificationPublisher` into existing invoice-overdue and new-employee event triggers

**Risk type:** Shared code path  
**Risk:** Modifying existing invoice and employee event code could introduce unintended side effects or regressions in those existing flows.  
**Mitigation:** Add targeted unit tests for the affected trigger points before making changes; perform a manual smoke test of invoice and employee operations after the change is merged; keep the publisher call isolated so a failure to persist a notification does not break the originating operation (consider try/catch with logging).

---

## Summary

13 tasks with an estimated total effort of **6–10 days** of focused development. The critical path runs through **#1 → #3 → #4 → #5 → #10 → #11 / #12 → #13** — the database migration and backend service layer must complete before any frontend work can begin. Once #4 is done, tasks #5 and #6 can be built in parallel, and once #10 is done, #11 and #12 can be built simultaneously. Task #7 (event wiring) can also be worked in parallel with #5/#6 since it depends only on #4. The feature is full-stack with a genuine publishing concern, making it on the larger side for a single sprint; if capacity is tight, consider shipping the read/list/mark-read flow first (#1–#6, #8–#13) as a releasable slice, and deferring the publisher wiring (#7) to a follow-up story.
