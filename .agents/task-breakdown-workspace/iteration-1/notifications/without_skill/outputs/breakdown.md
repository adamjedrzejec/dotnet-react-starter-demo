# In-App Notification System — Implementation Breakdown

## Overview

Users see a bell icon in the nav bar with an unread count badge. Clicking it opens a dropdown/panel listing notifications (e.g. "Invoice #1042 is overdue", "New employee Jane Smith added"). Each notification can be individually marked as read. Notifications are created server-side when domain events fire.

---

## Architecture Decisions

| Concern | Decision |
|---|---|
| Delivery mechanism | Polling (simple) — upgrade to SSE/WebSocket later if needed |
| Storage | New `Notifications` table in existing DB via EF Core |
| Auth scope | Notifications are per-user (userId on each record) |
| Read tracking | `readIndicator` boolean on each notification row |
| Soft delete | `deletedIndicator` — keep records for audit, filter in queries |

---

## Phase 1 — Backend: Data Model & Repository

### 1.1 Database Entity

**File:** `src/Api/Models/Notification.cs`

```csharp
public class Notification
{
    public int NotificationId { get; set; }
    public string UserId { get; set; }          // recipient
    public string Title { get; set; }
    public string Body { get; set; }
    public string Type { get; set; }            // e.g. "INVOICE_OVERDUE", "EMPLOYEE_ADDED"
    public string? ReferenceId { get; set; }    // e.g. invoiceId or employeeId
    public bool ReadIndicator { get; set; }
    public bool DeletedIndicator { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
}
```

### 1.2 EF Core Migration

- Add `DbSet<Notification> Notifications` to `AppDbContext`
- Generate migration: `dotnet ef migrations add AddNotificationsTable`
- Apply: `dotnet ef database update`

### 1.3 Repository

**Files:**
- `src/Api/Repositories/INotificationRepository.cs`
- `src/Api/Repositories/NotificationRepository.cs`

**Methods:**
```csharp
Task<IEnumerable<Notification>> GetAllAsync(NotificationQuery query, CancellationToken ct);
Task<int> GetUnreadCountAsync(string userId, CancellationToken ct);
Task<Notification?> GetByIdAsync(int id, CancellationToken ct);
Task<Notification> CreateAsync(Notification notification, CancellationToken ct);
Task MarkAsReadAsync(int id, CancellationToken ct);
Task MarkAllAsReadAsync(string userId, CancellationToken ct);
```

**Query object:**
```csharp
public class NotificationQuery
{
    public string UserId { get; set; }
    public bool? ReadIndicator { get; set; }   // null = all, false = unread only
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 20;
}
```

Use `AsNoTracking()` on all reads. Filter out `DeletedIndicator = true` in all queries.

---

## Phase 2 — Backend: DTOs

**File:** `src/Api/DTOs/Notifications/NotificationDto.cs`

```csharp
public class NotificationDto
{
    /// <summary>The unique identifier of the notification.</summary>
    [JsonPropertyName("notificationId")]
    public int NotificationId { get; set; }

    /// <summary>The title of the notification.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>The body message of the notification.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; }

    /// <summary>The event type that triggered the notification.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>Optional reference to the related entity ID.</summary>
    [JsonPropertyName("referenceId")]
    public string? ReferenceId { get; set; }

    /// <summary>Indicates whether the notification has been read.</summary>
    [JsonPropertyName("readIndicator")]
    public bool ReadIndicator { get; set; }

    /// <summary>The date and time the notification was created.</summary>
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    /// <summary>The date and time the notification was read, if applicable.</summary>
    [JsonPropertyName("readDate")]
    public DateTime? ReadDate { get; set; }
}
```

**File:** `src/Api/DTOs/Notifications/UnreadCountDto.cs`

```csharp
public class UnreadCountDto
{
    /// <summary>The number of unread notifications for the user.</summary>
    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }
}
```

**File:** `src/Api/DTOs/Notifications/CreateNotificationDto.cs`  
(used internally by services — not exposed via HTTP)

```csharp
public class CreateNotificationDto
{
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string Type { get; set; }
    public string? ReferenceId { get; set; }
}
```

---

## Phase 3 — Backend: Service Layer

**Files:**
- `src/Api/Services/INotificationService.cs`
- `src/Api/Services/NotificationService.cs`

**Interface:**
```csharp
public interface INotificationService
{
    Task<CollectionResponseDto<NotificationDto>> GetAllAsync(NotificationQuery query, CancellationToken ct);
    Task<ItemResponseDto<UnreadCountDto>> GetUnreadCountAsync(string userId, CancellationToken ct);
    Task<ItemResponseDto<NotificationDto>> CreateAsync(CreateNotificationDto dto, CancellationToken ct);
    Task MarkAsReadAsync(int notificationId, string userId, CancellationToken ct);
    Task MarkAllAsReadAsync(string userId, CancellationToken ct);
}
```

**Key service behaviors:**
- `MarkAsReadAsync`: fetch notification, verify `UserId` matches caller (authorization), set `ReadIndicator = true` and `ReadDate = DateTime.UtcNow`
- `CreateAsync`: called internally from domain event handlers (e.g. invoice service, employee service)
- Map `Notification` → `NotificationDto` in service (not controller, not repository)
- Build `CollectionResponseDto` envelope with metadata (timestamp, transactionId, totalCount) and links (self, next, prev)

---

## Phase 4 — Backend: Controller

**File:** `src/Api/Controllers/NotificationsController.cs`

| Method | Route | Description |
|---|---|---|
| `GET` | `v1/notifications` | List notifications (paginated). Query params: `userId`, `readIndicator`, `offset`, `limit` |
| `GET` | `v1/notifications/unread-count` | Returns `UnreadCountDto` for a user |
| `PATCH` | `v1/notifications/{id}/read` | Mark single notification as read |
| `PATCH` | `v1/notifications/read-all` | Mark all notifications as read for user |

**Notes:**
- In production, `userId` would come from the JWT claims — for now accept as query param
- All endpoints return envelope responses (`ItemResponseDto` / `CollectionResponseDto`)
- `PATCH .../read` returns `204 No Content` or `200 OK` with updated item (choose one and be consistent)
- Add `[ProducesResponseType]` for 200, 400, 404, 500

---

## Phase 5 — Backend: Domain Event Integration

**Where to hook in:**

| Event | Trigger location | Notification type |
|---|---|---|
| Invoice overdue | `InvoiceService` (batch job or status change) | `INVOICE_OVERDUE` |
| New employee added | `EmployeeService.CreateAsync` | `EMPLOYEE_ADDED` |
| Invoice paid | `InvoiceService.MarkPaidAsync` | `INVOICE_PAID` |

**Pattern — inject `INotificationService` into domain services:**

```csharp
// In EmployeeService.CreateAsync, after saving:
await _notificationService.CreateAsync(new CreateNotificationDto
{
    UserId = managerId,   // whoever should receive it
    Title = "New employee added",
    Body = $"{employee.Name} has been added to your team.",
    Type = "EMPLOYEE_ADDED",
    ReferenceId = employee.EmployeeId.ToString()
}, cancellationToken);
```

**Note:** For cross-cutting concerns at scale, consider a lightweight domain event bus (e.g. MediatR notifications). For this scope, direct injection is sufficient.

---

## Phase 6 — Backend: DI Registration & Tests

### 6.1 DI Registration

**File:** `src/Api/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<INotificationRepository, NotificationRepository>();
services.AddScoped<INotificationService, NotificationService>();
```

### 6.2 Unit Tests

**Files:**
- `tests/Api.Tests/Services/NotificationServiceTests.cs`
- `tests/Api.Tests/Repositories/NotificationRepositoryTests.cs`
- `tests/Api.Tests/Controllers/NotificationsControllerTests.cs`

**Key test cases:**

```
NotificationService
  GetAllAsync_WithUserId_ReturnsOnlyUserNotifications
  GetAllAsync_WithReadFilter_ReturnsFilteredResults
  GetUnreadCountAsync_WithUnreadNotifications_ReturnsCorrectCount
  MarkAsReadAsync_WhenExists_SetsReadIndicatorAndReadDate
  MarkAsReadAsync_WhenNotificationBelongsToDifferentUser_ThrowsUnauthorized
  CreateAsync_WithValidDto_ReturnsCreatedNotification

NotificationRepository (InMemory DB)
  GetAllAsync_ExcludesDeletedNotifications
  GetUnreadCountAsync_CountsOnlyUnreadRecords
  MarkAsReadAsync_UpdatesRecordCorrectly
```

Use `TestDataBuilders.CreateNotification(...)` helper following existing builder pattern.

---

## Phase 7 — Frontend: Types & API Client

### 7.1 Types

**File:** `src/lib/types.ts` (extend existing)

```typescript
export interface Notification {
  notificationId: number;
  title: string;
  body: string;
  type: string;
  referenceId?: string;
  readIndicator: boolean;
  createdDate: string;
  readDate?: string;
}

export interface UnreadCount {
  unreadCount: number;
}
```

### 7.2 API Functions

**File:** `src/lib/api.ts` (extend existing)

```typescript
export const getNotifications = (userId: string, params?: {
  readIndicator?: boolean;
  offset?: number;
  limit?: number;
}) => fetchApi<Notification[]>(`/v1/notifications?userId=${userId}&...`);

export const getUnreadCount = (userId: string) =>
  fetchApi<UnreadCount>(`/v1/notifications/unread-count?userId=${userId}`);

export const markAsRead = (notificationId: number) =>
  fetchApi<void>(`/v1/notifications/${notificationId}/read`, { method: 'PATCH' });

export const markAllAsRead = (userId: string) =>
  fetchApi<void>(`/v1/notifications/read-all?userId=${userId}`, { method: 'PATCH' });
```

---

## Phase 8 — Frontend: Components

### 8.1 Component Tree

```
NavBar
└── NotificationBell/
    ├── NotificationBell.tsx     ← bell icon + badge (unread count)
    ├── NotificationPanel.tsx    ← dropdown list of notifications
    ├── NotificationItem.tsx     ← single row (title, body, time, read/unread style)
    └── index.ts
```

### 8.2 NotificationBell

- Polls `getUnreadCount` every 30 seconds (use `setInterval` in `useEffect`)
- Displays red badge with count when `unreadCount > 0`
- Toggles `NotificationPanel` open/close on click
- Closes panel when clicking outside (use `useRef` + document click listener)

```tsx
<button onClick={togglePanel} className="relative">
  <BellIcon className="w-6 h-6" />
  {unreadCount > 0 && (
    <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
      {unreadCount > 99 ? '99+' : unreadCount}
    </span>
  )}
</button>
```

### 8.3 NotificationPanel

- Fetches notification list when panel opens
- Shows loading skeleton while fetching
- Empty state: "You're all caught up! 🎉"
- "Mark all as read" button at top (disabled if all already read)
- Virtualize list if count can be large (otherwise simple `map`)

### 8.4 NotificationItem

- Unread: slightly highlighted background (e.g. `bg-blue-50`)
- Read: normal background
- Click on item: calls `markAsRead`, updates local state optimistically
- Show relative time (e.g. "2 hours ago") using `Intl.RelativeTimeFormat` or a small utility

### 8.5 Styling

Follow Tailwind + CSS variable conventions from standards:

```tsx
<div className="bg-[var(--color-brand-content)] border border-gray-200 rounded-lg shadow-lg w-80">
```

---

## Phase 9 — Frontend: State Management

Use local component state (no Redux/Zustand needed at this scope):

```typescript
// In NotificationBell parent or custom hook
const [unreadCount, setUnreadCount] = useState(0);
const [notifications, setNotifications] = useState<Notification[]>([]);
const [panelOpen, setPanelOpen] = useState(false);

// After markAsRead:
setNotifications(prev =>
  prev.map(n => n.notificationId === id ? { ...n, readIndicator: true } : n)
);
setUnreadCount(prev => Math.max(0, prev - 1));
```

Extract into a `useNotifications(userId)` custom hook for reusability and testability.

---

## Phase 10 — Polish & Future Enhancements

| Item | Priority | Notes |
|---|---|---|
| Replace polling with Server-Sent Events (SSE) | Medium | Add `GET /v1/notifications/stream` endpoint with `text/event-stream` |
| Push via SignalR WebSocket | Low | Richer bidirectional option |
| Notification preferences (opt-in/out per type) | Low | New `NotificationPreferences` table |
| Email digest | Low | Background job reads unread and sends email |
| Deep link from notification | Medium | Navigate to invoice/employee on click using `referenceId` |

---

## Task Summary Table

| # | Phase | Deliverable | Owner |
|---|---|---|---|
| 1 | DB Model | `Notification` entity + EF migration | Backend |
| 2 | Repository | `INotificationRepository` + impl | Backend |
| 3 | DTOs | `NotificationDto`, `UnreadCountDto`, `CreateNotificationDto` | Backend |
| 4 | Service | `INotificationService` + impl | Backend |
| 5 | Controller | `NotificationsController` (4 endpoints) | Backend |
| 6 | Events | Hook `CreateAsync` into `EmployeeService`, `InvoiceService` | Backend |
| 7 | DI | Register in `ServiceCollectionExtensions` | Backend |
| 8 | Tests | Service, Repository, Controller unit tests | Backend |
| 9 | Types | Extend `lib/types.ts` | Frontend |
| 10 | API Client | Extend `lib/api.ts` | Frontend |
| 11 | Components | `NotificationBell`, `NotificationPanel`, `NotificationItem` | Frontend |
| 12 | Hook | `useNotifications` custom hook | Frontend |
| 13 | Nav integration | Wire `NotificationBell` into existing nav bar | Frontend |

**Total tasks: 13 | Estimated backend effort: ~2–3 days | Estimated frontend effort: ~1–2 days**
