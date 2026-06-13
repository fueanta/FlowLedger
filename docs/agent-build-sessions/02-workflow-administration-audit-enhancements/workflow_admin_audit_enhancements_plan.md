# FlowLedger Session 02 Build Plan - Workflow, Administration & Audit Enhancements

## 0. Session Goal

Enhance the signed-off FlowLedger MVP into a stronger ERP billing administration module.

Session 01 shipped billing request creation, approval, invoice generation, payment marking, dashboard reporting, seeded JWT login, role-based UI/API behavior, and audit timeline. Session 02 adds:

- Recent demo seed data that works with the default dashboard period.
- Clear dashboard distinction between period-filtered activity and current-state pending workload.
- Client management.
- User enrollment workflow.
- User administration.
- Configurable VAT, approval threshold, and invoice due days.
- User preferences.
- My Work Queue and explicit assignment behavior.
- Strong audit enforcement.
- SQL Server temporal table strategy.
- Standardized server-driven data tables.
- CSV export.
- Invoice print/PDF export.
- Updated navigation, demo journey, README, implementation log, and session-flow image.

Work one phase at a time. After each phase:

1. Implement only that phase scope.
2. Add or update tests for the phase.
3. Run the phase verification commands.
4. Fix failures.
5. Append built items, verification evidence, blockers, and sign-off status to `implementation-log.md`.
6. Do not move to the next phase until the current phase is signed off.

---

## 1. Baseline and Current Problem

Current stack:

- Backend: ASP.NET Core 8 Web API, EF Core, SQL Server, JWT auth, FluentValidation, xUnit, FluentAssertions, WebApplicationFactory, Testcontainers.
- Frontend: React, Vite, TypeScript, Tailwind, shadcn-style UI primitives, TanStack Query, Axios, React Hook Form, Zod, Lucide, Recharts.
- Current entities: `User`, `Customer`, `BillingRequest`, `BillingRequestLineItem`, `Comment`, `AuditLog`, `Invoice`, `Notification`, `AppSetting`.
- Current list pages use local, page-specific table markup rather than one shared data table.
- Current dashboard default period is 1 month.
- Current seed data is dated January 2026, so default 1-month dashboard activity can be empty while 6-month selection shows most demo data.
- Pending queues are current-state metrics and are intentionally not date-filtered, but the UI does not make that distinction clear enough.

Naming decision:

- User-facing language becomes **Client**.
- Existing code currently uses `Customer`. Either rename to `Client` through migration or keep storage compatibility while exposing `Client` API/UI names. The preferred path is a deliberate rename in Phase 1 with migration tests.

Design-system decision:

- Keep FlowLedger as a dense ERP/admin dashboard.
- Use semantic shadcn-style `Table` primitives for tabular data.
- Use TanStack Table for table state.
- Keep compact rows, visible focus, no hover layout shift, horizontal scroll on small screens, explicit input labels, and deep-linkable state through query parameters.

---

## 2. Demo Seed Data and Dashboard Semantics

### Problem

The reviewer opens the dashboard with the default `1 month` period and sees little or no seeded activity because seeded request/invoice dates are old. Changing to `6 months` reveals most data. This makes the MVP look empty by default.

### Goal

Default dashboard must show meaningful seeded activity without requiring the reviewer to discover the 6-month filter.

### Seed Data Strategy

Replace or augment static January-only seeded workflow dates with a relative demo seed refresh that anchors demo records to recent UTC dates.

Implementation options:

1. Preferred: add a startup `DemoSeedDataRefresher` that runs after migrations in Development/Docker when `SeedData__RefreshDemoDates=true`.
2. Use deterministic IDs and update existing seeded records rather than creating duplicates.
3. Use an injectable clock abstraction, for example `IDateTimeProvider`, so tests can fix "now".
4. Keep migration `HasData` valid if already present, but normalize demo dates after migration for local/demo environments.
5. Do not run date-refresh behavior in production unless explicitly enabled.

Suggested recent distribution:

| Record Type | Date Placement |
|---|---|
| Draft requests | today minus 1-4 days |
| Accounts Review requests | today minus 2-10 days |
| Manager Approval requests | today minus 4-14 days |
| Rejected requests | today minus 7-21 days |
| InvoiceGenerated requests | today minus 5-25 days |
| Paid requests | today minus 2-28 days |
| Older comparison records | today minus 45-150 days |

Default 1-month dashboard must include:

- At least 8 billing requests.
- At least 4 issued invoices.
- At least 2 paid invoices.
- At least 2 pending Accounts Review records.
- At least 1 pending Manager Approval record.
- At least 5 recent audit log entries.
- At least 3 months of optional older trend data visible when selecting 6 or 12 months.

Seed integrity rules:

- Every seeded workflow record in `AccountsReview`, `ManagerApproval`, `Rejected`, `InvoiceGenerated`, or `Paid` must have matching audit logs.
- Audit log dates must align with workflow dates.
- Invoice issue/paid dates must align with billing request approval/payment dates.
- Demo users remain seeded with password hashes bootstrapped from environment variables only.

### Dashboard Date Semantics

Dashboard response must distinguish:

- **Period-filtered activity**: metrics and charts affected by selected period.
- **Current workload**: pending queue counts and aging that show current state and are not affected by selected period.

Period-filtered activity:

- Requests created in selected period.
- Requests approved in selected period.
- Requests rejected in selected period.
- Invoice value issued in selected period.
- Paid invoice value paid in selected period.
- Invoice trend.
- Status breakdown, if defined as requests created in period.
- Recent activity, if filtered by selected period.

Current workload:

- Pending Accounts Review.
- Pending Manager Approval.
- Aging buckets for pending items.
- My Work Queue counts.

Required API metadata:

```json
{
  "period": {
    "months": 1,
    "startUtc": "2026-05-13T00:00:00Z",
    "endUtc": "2026-06-13T00:00:00Z"
  },
  "metricScopes": {
    "totalRequests": "Period",
    "paidInvoiceAmount": "Period",
    "pendingAccountsReview": "Current",
    "pendingManagerApproval": "Current",
    "agingBuckets": "Current"
  }
}
```

Required UI behavior:

- Split dashboard into two labeled sections:
  - `Period Activity`
  - `Current Workload`
- Period selector appears only in the `Period Activity` header area.
- Current Workload section shows a small text note: `Current workload is not filtered by period.`
- Each metric card includes a small scope chip:
  - `Period filtered`
  - `Current state`
- No ambiguous hints like "recently" unless paired with exact period label.

---

## 3. Product Scope

### Client Management

Clients are invoice recipients, not system users.

Fields:

| Field | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `Guid` | Yes | Stable primary key |
| `CompanyName` | `string(200)` | Yes | Replaces/renames current `Customer.Name` |
| `ContactPerson` | `string(160)` | Yes | New |
| `Email` | `string(254)` | Yes | Rename/current `ContactEmail`; validate email |
| `Phone` | `string(40)` | No | Existing field can be retained |
| `Address` | `string(500)` | Yes | Rename/current `BillingAddress` |
| `TaxIdentifier` | `string(80)` | No | New |
| `Status` | enum | Yes | `Active`, `Inactive`, `Archived` |
| `CreatedAtUtc` | `DateTime` | Yes | Existing |
| `UpdatedAtUtc` | `DateTime` | Yes | New if missing |
| `ArchivedAtUtc` | `DateTime?` | No | New |
| `ArchivedByUserId` | `Guid?` | No | New |

Rules:

- Billing requests can be created only for `Active` clients.
- Existing requests/invoices keep client reference even if client later becomes inactive or archived.
- Archived clients cannot be edited except by Admin restore support if added later. Restore is not required in Session 02.
- Sales can create clients but cannot edit/archive.
- Accounts can create and edit active/inactive clients but cannot archive.
- Manager can view only.
- Admin can view, create, edit, and archive.

### User Enrollment Workflow

Flow:

```text
Guest registers
  -> Enrollment Request Pending
  -> Admin approves or rejects
  -> Approved user becomes active and can log in
```

Enrollment fields:

| Field | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `Guid` | Yes | Primary key |
| `FullName` | `string(160)` | Yes | User supplied |
| `Email` | `string(254)` | Yes | Unique against users and pending enrollments |
| `RequestedRole` | `RoleName` | Yes | Default `Sales`; Admin can override at approval |
| `PasswordHash` | `string` | Yes | Never return |
| `PasswordSalt` | `string` | Yes | Never return |
| `Status` | enum | Yes | `Pending`, `Approved`, `Rejected` |
| `ReviewedByUserId` | `Guid?` | No | Admin actor |
| `ReviewedAtUtc` | `DateTime?` | No | Approval/rejection time |
| `DecisionReason` | `string(1000)?` | No | Required for rejection |
| `CreatedAtUtc` | `DateTime` | Yes | Request time |
| `UpdatedAtUtc` | `DateTime` | Yes | Mutation time |

Rules:

- Pending/rejected enrollments cannot log in.
- Approved enrollment creates or activates a `User`.
- Rejection requires reason.
- Approval/rejection creates audit log in same transaction.
- Duplicate email returns a validation error without exposing sensitive account state beyond "email is unavailable".

### User Administration

Modify `User`:

| Field | Type | Required | Notes |
|---|---|---:|---|
| `Status` | enum | Yes | Prefer `Active`, `Inactive`; can replace `IsActive` or map to it |
| `UpdatedAtUtc` | `DateTime` | Yes | New if missing |
| `DeactivatedAtUtc` | `DateTime?` | No | New |
| `DeactivatedByUserId` | `Guid?` | No | New |
| `LastLoginAtUtc` | `DateTime?` | No | Optional but useful for admin table |
| `EnrollmentRequestId` | `Guid?` | No | Link approved user back to request |

Admin capabilities:

- View users.
- Change role.
- Activate user.
- Deactivate user.
- Review enrollment requests.

Rules:

- Admin cannot deactivate their own account through the UI/API.
- Admin cannot remove the last active Admin.
- Role change creates audit log.
- Activation/deactivation creates audit log.

### Configurable System Settings

Settings:

| Key | Type | Default | Admin Write | Other Read |
|---|---|---:|---:|---:|
| `VatPercentage` | decimal | `15` | Yes | Yes |
| `ManagerApprovalThreshold` | decimal | `100000` | Yes | Yes |
| `InvoiceDueDays` | int | `30` | Yes | Yes |

Use either a new `SystemSetting` entity or extend `AppSetting`. Preferred: introduce typed settings service over current `AppSetting`, then rename later only if useful.

Rules:

- No hardcoded VAT or approval threshold in workflow services.
- New billing request totals use current VAT.
- Approval routing uses current threshold at approval time.
- New invoices snapshot VAT percentage and due days.
- Existing invoices retain original amounts and due dates after settings change.
- Settings update creates audit log.

### User Preferences

New entity `UserPreference`:

| Field | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `Guid` | Yes | Primary key |
| `UserId` | `Guid` | Yes | Unique |
| `DefaultDashboardPeriodMonths` | int | Yes | `1`, `3`, `6`, `12` |
| `DefaultLandingPage` | enum/string | Yes | Role-safe route |
| `RowsPerPage` | int | Yes | `10`, `25`, `50`, `100` |
| `CreatedAtUtc` | `DateTime` | Yes | Created time |
| `UpdatedAtUtc` | `DateTime` | Yes | Last change |

Role defaults:

| Role | Landing Page | Dashboard Period | Rows Per Page |
|---|---|---:|---:|
| Sales | Billing Requests | 1 | 25 |
| Accounts | Dashboard | 1 | 25 |
| Manager | Dashboard | 1 | 25 |
| Admin | Dashboard | 1 | 50 |

Rules:

- If no preference exists, service returns role defaults.
- Rows per page persists from shared data table selector.
- Landing page must be authorized for role.

### Assignment and My Work Queue

Modify `BillingRequest`:

| Field | Type | Required | Notes |
|---|---|---:|---|
| `AssignedToUserId` | `Guid?` | Existing | Keep if useful |
| `AssignedQueue` | enum/string | Yes | New: `Sales`, `Accounts`, `Manager`, `None` |
| `AssignedAtUtc` | `DateTime?` | No | New |
| `SubmittedByUserId` | `Guid?` | No | New |
| `AccountsReviewedByUserId` | `Guid?` | No | New |
| `ManagerReviewedByUserId` | `Guid?` | No | New |
| `LastWorkflowActionAtUtc` | `DateTime?` | No | New |

Assignment rules:

- Draft/rejected requests assigned to creator/Sales queue.
- Submit assigns to Accounts queue.
- Accounts approval at or below threshold generates invoice and clears queue.
- Accounts approval above threshold assigns to Manager queue.
- Manager approval generates invoice and clears queue.
- Rejection assigns back to Sales creator queue.
- Paid/Cancelled have no active queue.
- Every assignment change creates audit log.

### Audit Enforcement

Modify `AuditLog`:

| Field | Type | Required | Notes |
|---|---|---:|---|
| `EntityType` | string/enum | Yes | `BillingRequest`, `Invoice`, `Client`, `User`, `EnrollmentRequest`, `SystemSetting` |
| `EntityId` | `Guid` | Yes | Target entity |
| `EntityNumber` | `string?` | No | `BR-...`, `INV-...`, company name, email |
| `ActorUserId` | `Guid?` | No | Null for system seed/system action if unavoidable |
| `ActorDisplayName` | `string(160)` | Yes | Snapshot for history |
| `ActionType` | enum | Yes | Expand current enum |
| `Message` | string | Yes | Human-readable |
| `BeforeStatus` | string? | No | Workflow/status changes |
| `AfterStatus` | string? | No | Workflow/status changes |
| `CreatedAtUtc` | DateTime | Yes | Existing |

Audit rule:

- Workflow transition and audit insert must commit or roll back together.
- If audit insert fails, workflow mutation fails.
- Use one helper/service, for example `IAuditWriter`, inside same EF transaction.

### Temporal Tables

Apply SQL Server temporal tables to:

- Clients.
- BillingRequests.
- Invoices.
- AppSettings/SystemSettings.

Temporal tables answer what changed and when. Audit logs answer who changed it and why.

### Invoice Export

Minimum:

- Browser print invoice layout remains supported.

Preferred:

- Add QuestPDF package to backend.
- Add `GET /api/invoices/{id}/pdf`.
- Return `application/pdf`.
- Frontend shows `Print` and `Download PDF` actions on invoice detail.

---

## 4. Backend API Plan

Use thin controllers. Business rules stay in application services and infrastructure implementations.

### New APIs

#### Clients

```text
GET    /api/clients
GET    /api/clients/{id}
POST   /api/clients
PUT    /api/clients/{id}
POST   /api/clients/{id}/archive
GET    /api/clients/export
```

`GET /api/clients` query:

```text
page
pageSize
search
status
sortBy = companyName | status | createdAtUtc | updatedAtUtc
sortDirection = asc | desc
```

`CreateClientDto`:

```csharp
public sealed record CreateClientDto(
    string CompanyName,
    string ContactPerson,
    string Email,
    string? Phone,
    string Address,
    string? TaxIdentifier);
```

`UpdateClientDto`:

```csharp
public sealed record UpdateClientDto(
    string CompanyName,
    string ContactPerson,
    string Email,
    string? Phone,
    string Address,
    string? TaxIdentifier,
    ClientStatus Status);
```

`ClientListItemDto`:

```csharp
public sealed record ClientListItemDto(
    Guid Id,
    string CompanyName,
    string ContactPerson,
    string Email,
    string? Phone,
    string? TaxIdentifier,
    ClientStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
```

#### Enrollment

```text
POST   /api/enrollment-requests
GET    /api/enrollment-requests
GET    /api/enrollment-requests/{id}
POST   /api/enrollment-requests/{id}/approve
POST   /api/enrollment-requests/{id}/reject
```

`RegisterEnrollmentRequestDto`:

```csharp
public sealed record RegisterEnrollmentRequestDto(
    string FullName,
    string Email,
    string Password,
    RoleName RequestedRole);
```

`ApproveEnrollmentRequestDto`:

```csharp
public sealed record ApproveEnrollmentRequestDto(RoleName AssignedRole);
```

`RejectEnrollmentRequestDto`:

```csharp
public sealed record RejectEnrollmentRequestDto(string Reason);
```

List query:

```text
page
pageSize
search
status
requestedRole
sortBy = createdAtUtc | status | email | requestedRole
sortDirection = asc | desc
```

#### Users

```text
GET    /api/users
GET    /api/users/{id}
PUT    /api/users/{id}/role
POST   /api/users/{id}/activate
POST   /api/users/{id}/deactivate
```

List query:

```text
page
pageSize
search
role
status
sortBy = fullName | email | role | status | updatedAtUtc
sortDirection = asc | desc
```

#### Settings

```text
GET    /api/settings
PUT    /api/settings
```

`UpdateSystemSettingsDto`:

```csharp
public sealed record UpdateSystemSettingsDto(
    decimal VatPercentage,
    decimal ManagerApprovalThreshold,
    int InvoiceDueDays);
```

Validation:

- `VatPercentage` between `0` and `30`.
- `ManagerApprovalThreshold` greater than `0`.
- `InvoiceDueDays` between `1` and `365`.

#### Preferences

```text
GET    /api/preferences/me
PUT    /api/preferences/me
```

`UpdateUserPreferenceDto`:

```csharp
public sealed record UpdateUserPreferenceDto(
    int DefaultDashboardPeriodMonths,
    string DefaultLandingPage,
    int RowsPerPage);
```

#### Work Queue

```text
GET    /api/work-queue
```

Query:

```text
page
pageSize
search
queue
sortBy = createdAtUtc | updatedAtUtc | amount | status | clientName
sortDirection = asc | desc
```

Role behavior:

- Sales: own draft/rejected/pending-created requests.
- Accounts: Accounts queue.
- Manager: Manager queue.
- Admin: all active workflow queues.

#### Audit Logs

```text
GET    /api/audit-logs
GET    /api/audit-logs/export
```

Query:

```text
page
pageSize
search
entityType
actionType
actorUserId
fromDate
untilDate
sortBy = createdAtUtc | actionType | actor | entityType
sortDirection = asc | desc
```

#### Invoice PDF

```text
GET /api/invoices/{id}/pdf
```

Response:

- `200 OK`
- `Content-Type: application/pdf`
- `Content-Disposition: attachment; filename="INV-2026-0001.pdf"`

### Modified APIs

#### Auth

Modify:

```text
POST /api/auth/login
GET  /api/auth/me
```

Changes:

- Login rejects pending, rejected, inactive, or deactivated users.
- Successful login may update `LastLoginAtUtc`.
- `/me` returns preference summary when practical.

#### Billing Requests

Modify:

```text
GET    /api/billing-requests
GET    /api/billing-requests/{id}
POST   /api/billing-requests
PUT    /api/billing-requests/{id}
POST   /api/billing-requests/{id}/submit
POST   /api/billing-requests/{id}/approve
POST   /api/billing-requests/{id}/reject
POST   /api/billing-requests/{id}/comments
GET    /api/billing-requests/export
```

Changes:

- Rename query field `customerId` to `clientId`; keep compatibility alias temporarily if low cost.
- Validate client is active for create/update.
- Query supports `page`, `pageSize`, `search`, `sortBy`, `sortDirection`.
- List DTO includes `clientName`, `assignedQueue`, `assignedAtUtc`, `lastWorkflowActionAtUtc`.
- Approval uses current `ManagerApprovalThreshold`.
- Totals use current `VatPercentage` for new/updated draft line items.
- Workflow transitions call audit writer.

#### Invoices

Modify:

```text
GET    /api/invoices
GET    /api/invoices/{id}
POST   /api/invoices/{id}/mark-paid
GET    /api/invoices/export
```

Changes:

- Query supports `page`, `pageSize`, `search`, `status`, `clientId`, `sortBy`, `sortDirection`.
- Detail DTO includes VAT percentage, due days, client contact info snapshot if added.
- Mark paid creates audit log in same transaction.

#### Dashboard

Modify:

```text
GET /api/dashboard/summary?periodMonths=1
```

Changes:

- Add `period` metadata.
- Add metric scope metadata.
- Keep pending queue counts current-state, not date-filtered.
- Add tests proving default 1-month seeded dashboard has activity.
- Add tests proving pending counts do not change when switching 1 month to 6 months.

---

## 5. Standard Data Table Plan

### Shared Frontend Components

Create:

```text
frontend/flowledger-web/src/components/data-table/
  DataTable.tsx
  DataTableToolbar.tsx
  DataTablePagination.tsx
  DataTablePageSizeSelect.tsx
  DataTableSearch.tsx
  DataTableSortableHeader.tsx
  DataTableExportButton.tsx
  dataTableState.ts
  dataTableTypes.ts
```

Component responsibilities:

- `DataTable`: renders semantic shadcn table, loading rows, empty state, error state, and horizontal overflow wrapper.
- `DataTableToolbar`: search, filters slot, export button slot.
- `DataTablePagination`: previous/next, numeric pages, total count text.
- `DataTablePageSizeSelect`: `10`, `25`, `50`, `100`, persists to preferences.
- `DataTableSearch`: labeled, debounced search input.
- `DataTableSortableHeader`: button-style column header with Lucide sort icons and `aria-sort`.
- `DataTableExportButton`: uses current filters and downloads CSV.
- `dataTableState`: URL query sync for `page`, `pageSize`, `search`, `sortBy`, `sortDirection`.

Accessibility:

- All inputs have visible labels.
- Icon-only buttons have `aria-label`.
- Sort headers expose `aria-sort`.
- Empty states include next action guidance.
- Keyboard focus ring visible.

### Backend Query Model

Create shared query contracts:

```csharp
public enum SortDirection
{
    Asc,
    Desc
}

public abstract record PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}
```

Validation:

- `Page >= 1`.
- `PageSize` in `10`, `25`, `50`, `100`.
- Unknown `sortBy` returns `400 Bad Request`.
- Search trimmed and length-capped, for example max 200 chars.

### CSV Export

Shared backend service:

```text
CsvExportService
CsvColumn<T>
CsvResult
```

Rules:

- Exports respect filters.
- Exports respect authorization.
- Exports omit password hashes, salts, JWT/security stamps, internal IDs unless useful to reviewer.
- CSV uses stable headers.
- CSV escapes commas, quotes, and newlines.
- Large export cap can start at 5,000 rows with a documented UI note.

Required exports:

- Billing Requests.
- Invoices.
- Clients.

Optional:

- Audit Logs.
- Users.

---

## 6. Frontend UI Plan

### Global App Shell

Sidebar:

- Dashboard.
- My Work Queue.
- Billing Requests.
- Invoices.
- Clients.
- User Administration group:
  - Users.
  - Enrollment Requests.
- Settings.
- Audit Logs.

Topbar:

- Current user name and role.
- Optional preference/landing-page access.
- Logout.

Navigation rules:

- Hide routes/actions unauthorized for current role.
- Active route visibly highlighted.
- Deep links preserve table state through query params.

### Dashboard Page

File:

```text
src/pages/DashboardPage.tsx
```

Components:

- `PageHeader`.
- `PeriodActivityPanel`.
- `CurrentWorkloadPanel`.
- `MetricCard`.
- `MetricScopeBadge`.
- `DashboardPeriodSelect`.
- `StatusBreakdownChart`.
- `InvoiceTrendChart`.
- `AgingBucketsChart`.
- `AuditTimeline`.

Layout:

- Header title: `Dashboard`.
- Subtitle: operational billing approval and invoice health.
- First section: `Period Activity`.
  - Period selector in section header.
  - Scope note: `Filtered by selected period.`
  - Cards: Requests in period, Approved in period, Rejected in period, Issued invoice value, Paid invoice value, Avg approval hours.
  - Charts: Status breakdown, Invoice trend.
- Second section: `Current Workload`.
  - Scope note: `Not filtered by period.`
  - Cards: Accounts review, Manager approvals.
  - Chart: Aging buckets.
  - CTA links: open My Work Queue, open Billing Requests with queue filter.
- Third section: `Recent Activity`.
  - If filtered by period, label it `Recent Activity in Period`.
  - If changed to current-only later, label it `Latest Activity`.

### My Work Queue Page

Components:

- `PageHeader`.
- `QueueSummaryCards`.
- Shared `DataTable`.
- Filters: queue/status, search, date range optional.
- Row actions: View, Approve, Reject where allowed.

Role defaults:

- Sales: own draft/rejected/submitted records.
- Accounts: `Accounts` queue.
- Manager: `Manager` queue.
- Admin: all queues.

### Billing Requests Page

Components:

- `PageHeader` with `New Request`.
- Shared `DataTable`.
- Filters:
  - Status.
  - Client.
  - Assigned to me.
  - Created by me.
  - Queue.
- Columns:
  - Request No.
  - Title.
  - Client.
  - Status.
  - Queue.
  - Amount.
  - Created.
  - Updated.
  - Actions.
- Actions:
  - View.
  - Approve.
  - Reject.
  - Export CSV.

### Billing Request Form Page

Update:

- Replace Customer selector with Client selector.
- Show only active clients for new requests.
- If editing old request with inactive/archived client, show historical client but block resubmission until active client chosen.
- Totals show VAT percentage from settings.
- Helper text: `VAT uses current system settings until invoice generation.`

### Request Detail Page

Update:

- Client summary block.
- Assignment summary block:
  - Current queue.
  - Assigned at.
  - Last workflow action.
- Audit timeline shows all workflow transitions.
- Available actions remain backend-driven.

### Invoices Page

Components:

- `PageHeader`.
- Shared `DataTable`.
- Filters: status, client, search.
- Columns:
  - Invoice No.
  - Client.
  - Request No.
  - Status.
  - Total.
  - Issued.
  - Due.
  - Paid.
  - Actions.
- Actions:
  - View.
  - Mark paid.
  - Export CSV.

### Invoice Detail Page

Components:

- `PrintableInvoice`.
- `InvoiceActionBar`.
- `ClientBillingBlock`.
- `InvoiceLineItemsTable`.
- `InvoiceTotalsPanel`.
- `AuditTimeline`.

Actions:

- Print.
- Download PDF, if endpoint implemented.
- Mark Paid for Accounts/Admin when status is issued.

Print requirements:

- Hide app chrome in print.
- Show company/client/request/invoice details.
- Show VAT percentage and due date.

### Clients Pages

Pages:

- `ClientListPage`.
- `ClientCreatePage`.
- `ClientEditPage`.
- `ClientDetailPage`.

Client List components:

- Shared `DataTable`.
- Filters: status, search.
- Columns:
  - Company Name.
  - Contact Person.
  - Email.
  - Phone.
  - Tax Identifier.
  - Status.
  - Updated.
  - Actions.
- Actions:
  - View.
  - Edit.
  - Archive.
  - Export CSV.

Client Form components:

- `ClientForm`.
- Fields: company name, contact person, email, phone, address, tax identifier, status.
- Validation with Zod.

Client Detail components:

- Client info.
- Status badge.
- Recent billing requests table.
- Recent invoices table.
- Audit timeline filtered to client.

### Register Page

Public route:

- `/register`.

Components:

- `RegisterForm`.
- Fields: full name, email, password, confirm password, requested role.
- Success state: pending approval message.
- Link back to login.

Validation:

- Name required.
- Email valid.
- Password minimum length and confirmation match.
- Requested role valid; default Sales.

### Enrollment Requests Page

Admin route:

- `/app/admin/enrollment-requests`.

Components:

- Shared `DataTable`.
- Filters: status, requested role, search.
- Columns:
  - Name.
  - Email.
  - Requested Role.
  - Status.
  - Requested.
  - Reviewed.
  - Actions.
- Actions:
  - Approve.
  - Reject.
  - View detail.

Approve dialog:

- Assign role selector.
- Confirm action.

Reject dialog:

- Reason textarea required.

### Users Page

Admin route:

- `/app/admin/users`.

Components:

- Shared `DataTable`.
- Filters: role, status, search.
- Columns:
  - Name.
  - Email.
  - Role.
  - Status.
  - Last Login.
  - Updated.
  - Actions.
- Actions:
  - Change role.
  - Activate.
  - Deactivate.

Safety UX:

- Disable self-deactivation.
- Show clear error if last active Admin rule blocks action.

### Settings Page

Route:

- `/app/settings`.

Components:

- `SystemSettingsForm`.
- `UserPreferencesForm`.
- `SettingsAuditNote`.

Admin:

- Can edit VAT percentage, manager threshold, invoice due days.
- Save button visible/enabled.

Other roles:

- See read-only system settings.
- Can edit own preferences.

Copy requirements:

- Make clear that setting changes affect new calculations/invoices only.
- Existing invoices retain original generated values.

### Audit Logs Page

Route:

- `/app/audit-logs`.

Components:

- Shared `DataTable`.
- Filters: entity type, action type, actor, date range, search.
- Columns:
  - Time.
  - Actor.
  - Action.
  - Entity.
  - Before.
  - After.
  - Message.
- Actions:
  - View linked entity.
  - Export CSV optional.

### UI Visual Rules

- Use Lucide icons only.
- No emoji icons.
- Cards only for panels/repeated items; do not nest cards inside cards.
- Dense but readable ERP style.
- Tables use `overflow-x-auto` on mobile.
- Buttons with icons need text unless icon-only pattern has tooltip and accessible label.
- Use status badges consistently across requests, invoices, clients, users, and enrollments.

---

## 7. Backend Structure Plan

Add folders:

```text
backend/FlowLedger.Application/
  Clients/
  Enrollment/
  Users/
  Settings/
  Preferences/
  WorkQueue/
  AuditLogs/
  Exports/
  Common/Paging/

backend/FlowLedger.Infrastructure/
  Clients/
  Enrollment/
  Users/
  Settings/
  Preferences/
  WorkQueue/
  AuditLogs/
  Exports/
  Time/
  Persistence/SeedData/
```

Service interfaces:

```csharp
IClientService
IEnrollmentService
IUserAdministrationService
ISystemSettingsService
IUserPreferenceService
IWorkQueueService
IAuditLogService
IAuditWriter
ICsvExportService
IInvoicePdfService
IDateTimeProvider
IDemoSeedDataRefresher
```

Controller additions:

```text
ClientsController
EnrollmentRequestsController
UsersController
SettingsController
PreferencesController
WorkQueueController
AuditLogsController
```

Controller modifications:

```text
AuthController
BillingRequestsController
InvoicesController
DashboardController
```

---

## 8. Implementation Phases

### Phase 0: Demo Seed Recency and Dashboard Scope Labels

Build:

- Add `IDateTimeProvider`.
- Add demo seed refresh service or equivalent seed-date update mechanism.
- Update seeded billing request, invoice, audit, comment, and notification dates to recent relative dates in Development/Docker.
- Add dashboard period metadata and metric scope metadata.
- Split frontend dashboard into `Period Activity` and `Current Workload`.
- Add metric scope chips: `Period filtered` and `Current state`.
- Make clear pending counts and aging are not filtered by period.

Tests:

- Backend test: default `periodMonths=1` returns seeded activity.
- Backend test: `periodMonths=6` includes at least as much activity as 1 month.
- Backend test: pending counts are same for 1 month and 6 months.
- Backend test: seeded final-state workflow records have audit logs.
- Frontend test if practical: dashboard renders separate section labels and scope chips.

Sign-off checks:

- `cd backend && dotnet test FlowLedger.sln` or Docker SDK equivalent.
- `cd frontend/flowledger-web && npm test`.
- `cd frontend/flowledger-web && npm run lint`.
- `cd frontend/flowledger-web && npm run build`.
- Docker smoke: default dashboard period shows non-zero seeded activity and current pending cards are labeled as current-state.

### Phase 1: Client Management

Build:

- Add `ClientStatus`.
- Rename/evolve `Customer` to Client-facing model and migration.
- Add new client fields: contact person, tax identifier, status, updated/archive metadata.
- Add client validators and service.
- Add client CRUD/archive APIs.
- Modify billing request create/update to require active client.
- Rename frontend Customers page/API to Clients.
- Add Client List, Create, Edit, Detail pages.

Tests:

- Unit tests for active/archive business rules.
- Integration tests for Client CRUD/archive role permissions.
- Integration test: archived client cannot be used for new billing request.
- Frontend form validation tests for client form.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Smoke: Sales can create client; Accounts can edit; Manager cannot edit; Admin can archive.

### Phase 2: Configurable Settings

Build:

- Add typed settings DTOs and service.
- Add Admin-write/read-all settings API.
- Seed defaults: VAT 15, Manager threshold 100000, Invoice due days 30.
- Replace hardcoded VAT and threshold in billing workflow.
- Invoice generation snapshots VAT percentage and due days.
- Settings UI supports Admin edit and other-role read-only display.

Tests:

- Unit tests for totals with configured VAT.
- Unit tests for threshold routing with configured threshold.
- Integration test: Admin can update settings.
- Integration test: non-Admin cannot update settings.
- Regression test: existing invoice values do not change after settings update.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Smoke: Admin changes threshold; next approval route follows new threshold.

### Phase 3: Enrollment and User Administration

Build:

- Add `EnrollmentRequest` entity and status enum.
- Add registration endpoint and public Register page.
- Add enrollment list/detail/approve/reject APIs.
- Add Users list/detail/admin action APIs.
- Modify login to reject pending/rejected/inactive users.
- Add Admin Enrollment Requests page.
- Add Admin Users page.
- Add audit logs for enrollment/user admin actions.

Tests:

- Integration test: guest registers and cannot log in until approval.
- Integration test: Admin approves and user can log in.
- Integration test: Admin rejects and login fails.
- Integration test: deactivated user cannot log in.
- Integration test: Admin cannot deactivate self or last active Admin.
- Frontend tests for registration validation and admin dialogs where useful.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Smoke: reviewer journey steps 1-3 pass.

### Phase 4: Assignment, My Work Queue, and Audit Enforcement

Build:

- Add `AssignedQueue`, `AssignedAtUtc`, reviewer fields, and workflow action timestamps to billing requests.
- Add work queue service/API.
- Add My Work Queue page.
- Centralize workflow transitions through audit-enforced transaction helper.
- Update submit/approve/reject/mark-paid to create audit logs in same transaction.
- Update seed generation/builders so seeded workflow records have full audit history.

Tests:

- Unit tests for assignment routing.
- Integration tests for submit -> Accounts queue.
- Integration tests for high-value Accounts approval -> Manager queue.
- Integration tests for Manager approval -> invoice and no active queue.
- Integration test: simulated audit failure rolls back workflow transition.
- Seed integrity test for final states and audit logs.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Docker workflow smoke for low-value and high-value paths.

### Phase 5: Preferences and Standard Data Table Foundation

Build:

- Add `UserPreference` entity/API.
- Add role defaults for landing page, dashboard period, rows per page.
- Install and wire `@tanstack/react-table`.
- Build shared data table components.
- Add URL query sync for page/search/sort/pageSize.
- Add page-size persistence to preferences.
- Add reusable export button component.

Tests:

- Backend tests for preference defaults and updates.
- Frontend component tests for pagination, sorting, search, page-size select, empty/error states.
- Accessibility test coverage where current tooling allows.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Browser/manual smoke at 375px, 768px, 1024px, 1440px.

### Phase 6: Apply Data Tables and CSV Export

Build:

- Migrate Billing Requests, Invoices, Clients, Users, Enrollment Requests, and Audit Logs to shared table.
- Add server-side pagination/search/sort to each list endpoint.
- Add CSV export endpoints for Billing Requests, Invoices, Clients.
- Add optional CSV export for Audit Logs and Users if required exports are green.
- Preserve role-specific defaults and filters.

Tests:

- Integration tests for paging bounds.
- Integration tests for page-size validation.
- Integration tests for search fields.
- Integration tests for sort allow-lists.
- Integration tests for CSV filters and safe columns.
- Frontend tests on representative table page.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Smoke each required list page: previous, next, page number, page size, search, sort.
- Smoke required CSV exports.

### Phase 7: Temporal Tables and Audit Log Administration

Build:

- Add SQL Server temporal table migrations for Clients, BillingRequests, Invoices, Settings.
- Add Audit Logs page using shared data table.
- Add audit filters and search.
- Document temporal-vs-audit responsibilities in docs and comments where helpful.

Tests:

- Migration tests verify temporal tables exist.
- Integration test updates client/request/settings and confirms temporal history exists.
- Audit log list filter/sort/search tests.

Sign-off checks:

- Backend tests through SQL Server/Testcontainers.
- Docker Compose startup smoke after migrations.

### Phase 8: Invoice Print and PDF Export

Build:

- Refine printable invoice UI.
- Add QuestPDF generation if package/runtime fit.
- Add invoice PDF service and endpoint.
- Add Download PDF frontend action.
- Keep browser Print action.

Tests:

- Integration test PDF endpoint auth.
- Integration test content type and non-empty bytes.
- Frontend smoke for action visibility.

Sign-off checks:

- Full backend tests.
- Frontend tests/lint/build.
- Runtime smoke: issued invoice prints and downloads PDF.

### Phase 9: Navigation, Demo Journey, README, Session Flow

Build:

- Final sidebar navigation.
- Updated role-safe route guards.
- Updated README with new features, APIs, known limitations, and run notes.
- Update `implementation-log.md` sign-off from bottom.
- Generate `session-flow.svg` and `session-flow.png`.
- Document rate limiting remains backlog, not Session 02 implementation.

Final smoke journey:

1. Register user.
2. Admin approves enrollment.
3. Login as Sales.
4. Create active client.
5. Create billing request.
6. Submit billing request.
7. Login as Accounts.
8. Approve low-value request.
9. Verify invoice generated.
10. Mark paid.
11. Create high-value request.
12. Accounts approves.
13. Verify request assigned to Manager queue.
14. Manager approves.
15. Verify invoice generated.
16. Print/download invoice.
17. Verify audit logs exist for transitions.
18. Verify standard tables on all required list pages.
19. Verify default dashboard 1-month period shows seeded activity.
20. Verify Current Workload metrics do not change solely because period changes.

Sign-off checks:

- Full backend test suite.
- Full frontend test suite.
- Frontend lint.
- Frontend production build.
- Docker Compose runtime smoke.
- Session flow image generated and visually checked.

---

## 9. Testing Plan

Backend unit tests:

- Client active/archive rules.
- Configurable VAT calculations.
- Configurable approval threshold routing.
- Assignment routing.
- User preference defaults.
- CSV escaping helper.

Backend integration tests:

- Client CRUD/archive permissions.
- Billing request client validation.
- Dashboard default seed visibility.
- Dashboard period vs current metric behavior.
- Enrollment registration/approval/rejection/login gating.
- User admin activation/deactivation/role change.
- Settings read/write permissions.
- Workflow audit transaction rollback.
- Work queue role visibility.
- Data table paging/search/sort validation.
- CSV export filters/safe columns.
- PDF endpoint.
- Temporal table migrations.

Frontend tests:

- Dashboard section labels and metric scope chips.
- Client form validation.
- Registration form validation.
- Data table pagination/search/sort/page-size behavior.
- Admin action dialogs.
- Permission helper updates.

Runtime smoke:

- Docker Compose startup.
- API health.
- Login for seeded users.
- Dashboard default period non-empty.
- Complete reviewer journey.

---

## 10. Delivery Rules

- Do not skip failing tests.
- Do not install machine-wide tools without asking.
- Project-local npm/NuGet dependency installs are allowed when needed by phase.
- Keep controllers thin.
- Keep workflow rules out of controllers.
- Keep UI dense, accessible, consistent, and role-aware.
- Avoid over-engineering; add abstractions only where they centralize shared behavior.
- Update implementation log after every phase.
- Do not commit or push until user explicitly asks.

---

## 11. Backlog Note

Rate limiting is intentionally not part of Session 02 implementation. Keep it in `docs/backlog.md` as future security hardening for:

- Login.
- Registration.
- Enrollment approval/rejection.
- Billing approval/rejection.
- CSV export.
- PDF export.
