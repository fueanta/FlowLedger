# ERP Workflow Module — Full Build Plan for AI Agent

## 0. Project Decision

### Selected option
Build **Option 1: ERP Workflow Module**.

### Product name
**FlowLedger — Billing Request Approval & Invoice Workflow**

### One-line description
An internal ERP-style workflow module where Sales submits billing requests, Accounts reviews them, Management approves high-value requests, and approved requests generate invoices with full audit history and dashboard reporting.

---

## 1. Final Scope

### Main workflow

```text
Sales User creates a billing request
        ↓
Sales submits the request
        ↓
Accounts reviews the request
        ↓
If amount <= approval threshold:
    Accounts approves and invoice is generated
Else:
    Request goes to Management approval
        ↓
    Manager approves and invoice is generated
        ↓
Accounts can mark invoice as paid
```

### Rejection flow

```text
Accounts or Manager rejects request
        ↓
Request becomes Rejected
        ↓
Sales can revise and resubmit
```

### Core features

- Mock login with seeded users.
- Role-based UI and API behavior.
- Billing request creation.
- Billing request list and filtering.
- Billing request detail page.
- Approval/rejection/comments.
- Invoice generation.
- Invoice list/detail view.
- Dashboard with charts and metrics.
- Audit timeline.
- Seed data.
- Unit tests and integration tests.
- Docker Compose run experience.

---

## 2. Technology Stack

### Backend

Use:

- **.NET 8 ASP.NET Core Web API**
- **Entity Framework Core**
- **SQL Server**
- **JWT Bearer authentication**
- **FluentValidation**
- **Serilog**
- **Swagger / OpenAPI**
- **xUnit**
- **FluentAssertions**
- **WebApplicationFactory**
- **Testcontainers for SQL Server**

### Frontend

Use:

- **React + Vite + TypeScript**
- **React Router**
- **TanStack Query**
- **Axios**
- **React Hook Form**
- **Zod**
- **Tailwind CSS**
- **shadcn/ui**
- **Lucide React icons**
- **Recharts**
- **date-fns**
- **clsx + tailwind-merge**

### Database

Use:

- **SQL Server 2022 container**

Reason:

- Best fit for ERP-style relational workflow.
- Familiar to .NET reviewers.
- Strong fit for transactional data, audit logs, invoices, and reporting queries.
- Better choice than MongoDB for this project because the data has clear relationships.

---

## 3. Repository Structure

```text
flowledger/
├── backend/
│   ├── FlowLedger.Api/
│   ├── FlowLedger.Application/
│   ├── FlowLedger.Domain/
│   ├── FlowLedger.Infrastructure/
│   └── FlowLedger.Tests/
├── frontend/
│   └── flowledger-web/
├── docs/
│   ├── design-note.md
│   ├── api-overview.md
│   └── screenshots.md
├── docker-compose.yml
├── .env.example
├── README.md
└── Makefile
```

### Backend project responsibilities

```text
FlowLedger.Domain
- Entities
- Enums
- Domain rules where simple and meaningful

FlowLedger.Application
- DTOs
- Service interfaces
- Use-case services
- Validators
- Result objects

FlowLedger.Infrastructure
- EF Core DbContext
- Entity configurations
- Repositories if needed
- Seed data
- Date/time provider

FlowLedger.Api
- Controllers
- Auth setup
- Swagger
- Middleware
- Dependency injection wiring

FlowLedger.Tests
- Unit tests
- Integration tests
```

---

## 4. Local Development Commands

## 4.1 Create root folder

```bash
mkdir flowledger
cd flowledger
mkdir backend frontend docs
```

---

## 4.2 Backend creation commands

```bash
cd backend

dotnet new sln -n FlowLedger

dotnet new webapi -n FlowLedger.Api
dotnet new classlib -n FlowLedger.Domain
dotnet new classlib -n FlowLedger.Application
dotnet new classlib -n FlowLedger.Infrastructure
dotnet new xunit -n FlowLedger.Tests

dotnet sln add FlowLedger.Api/FlowLedger.Api.csproj
dotnet sln add FlowLedger.Domain/FlowLedger.Domain.csproj
dotnet sln add FlowLedger.Application/FlowLedger.Application.csproj
dotnet sln add FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj
dotnet sln add FlowLedger.Tests/FlowLedger.Tests.csproj

dotnet add FlowLedger.Application/FlowLedger.Application.csproj reference FlowLedger.Domain/FlowLedger.Domain.csproj

dotnet add FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj reference FlowLedger.Domain/FlowLedger.Domain.csproj
dotnet add FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj reference FlowLedger.Application/FlowLedger.Application.csproj

dotnet add FlowLedger.Api/FlowLedger.Api.csproj reference FlowLedger.Application/FlowLedger.Application.csproj
dotnet add FlowLedger.Api/FlowLedger.Api.csproj reference FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj

dotnet add FlowLedger.Tests/FlowLedger.Tests.csproj reference FlowLedger.Api/FlowLedger.Api.csproj
dotnet add FlowLedger.Tests/FlowLedger.Tests.csproj reference FlowLedger.Application/FlowLedger.Application.csproj
dotnet add FlowLedger.Tests/FlowLedger.Tests.csproj reference FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj
```

---

## 4.3 Backend packages

```bash
cd backend

dotnet add FlowLedger.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add FlowLedger.Api package Swashbuckle.AspNetCore
dotnet add FlowLedger.Api package Serilog.AspNetCore
dotnet add FlowLedger.Api package Serilog.Sinks.Console

dotnet add FlowLedger.Application package FluentValidation
dotnet add FlowLedger.Application package FluentValidation.DependencyInjectionExtensions

dotnet add FlowLedger.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add FlowLedger.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add FlowLedger.Infrastructure package Microsoft.EntityFrameworkCore.Design

dotnet add FlowLedger.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add FlowLedger.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add FlowLedger.Tests package FluentAssertions
dotnet add FlowLedger.Tests package Testcontainers.MsSql
dotnet add FlowLedger.Tests package Respawn
```

---

## 4.4 EF Core tools

```bash
dotnet tool install --global dotnet-ef
```

Create migration after entities are ready:

```bash
cd backend

dotnet ef migrations add InitialCreate \
  --project FlowLedger.Infrastructure \
  --startup-project FlowLedger.Api \
  --output-dir Persistence/Migrations
```

Apply locally:

```bash
dotnet ef database update \
  --project FlowLedger.Infrastructure \
  --startup-project FlowLedger.Api
```

---

## 4.5 Frontend creation commands

```bash
cd frontend
npm create vite@latest flowledger-web -- --template react-ts
cd flowledger-web
npm install
```

Install frontend libraries:

```bash
npm install @tanstack/react-query axios react-router-dom react-hook-form zod @hookform/resolvers recharts lucide-react date-fns clsx tailwind-merge class-variance-authority
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

Install shadcn/ui:

```bash
npx shadcn@latest init
```

Suggested shadcn settings:

```text
Style: New York
Base color: Slate
CSS variables: yes
```

Add shadcn components:

```bash
npx shadcn@latest add button card badge table tabs dialog dropdown-menu select input textarea label form separator sheet skeleton alert toast avatar progress breadcrumb
```

Optional but useful:

```bash
npx shadcn@latest add calendar popover command
```

---

## 5. Docker Compose

Create this at repository root: `docker-compose.yml`

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: flowledger-sqlserver
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${SQLSERVER_SA_PASSWORD:?Set SQLSERVER_SA_PASSWORD in .env or shell}"
      MSSQL_PID: "Developer"
    ports:
      - "14333:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$${MSSQL_SA_PASSWORD}\" -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10

  api:
    build:
      context: ./backend
      dockerfile: FlowLedger.Api/Dockerfile
    container_name: flowledger-api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=${SQLSERVER_DATABASE:-FlowLedgerDb};User Id=sa;Password=${SQLSERVER_SA_PASSWORD:?Set SQLSERVER_SA_PASSWORD in .env or shell};TrustServerCertificate=True;Encrypt=False"
      Jwt__Issuer: "${JWT_ISSUER:-FlowLedger}"
      Jwt__Audience: "${JWT_AUDIENCE:-FlowLedgerWeb}"
      Jwt__Key: "${JWT_KEY:?Set JWT_KEY in .env or shell}"
    ports:
      - "8080:8080"
    depends_on:
      sqlserver:
        condition: service_healthy

  web:
    build:
      context: ./frontend/flowledger-web
      dockerfile: Dockerfile
    container_name: flowledger-web
    environment:
      VITE_API_BASE_URL: "${VITE_API_BASE_URL:-http://localhost:8080/api}"
    ports:
      - "5173:80"
    depends_on:
      - api

volumes:
  sqlserver-data:
```

Expected reviewer command:

```bash
docker compose up --build
```

App URLs:

```text
Frontend: http://localhost:5173
Backend Swagger: http://localhost:8080/swagger
SQL Server: localhost,14333
```

---

## 6. Backend Dockerfile

Create: `backend/FlowLedger.Api/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FlowLedger.sln ./
COPY FlowLedger.Api/FlowLedger.Api.csproj FlowLedger.Api/
COPY FlowLedger.Application/FlowLedger.Application.csproj FlowLedger.Application/
COPY FlowLedger.Domain/FlowLedger.Domain.csproj FlowLedger.Domain/
COPY FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj FlowLedger.Infrastructure/

RUN dotnet restore FlowLedger.Api/FlowLedger.Api.csproj

COPY . .
RUN dotnet publish FlowLedger.Api/FlowLedger.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FlowLedger.Api.dll"]
```

---

## 7. Frontend Dockerfile

Create: `frontend/flowledger-web/Dockerfile`

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Create: `frontend/flowledger-web/nginx.conf`

```nginx
server {
  listen 80;
  server_name localhost;
  root /usr/share/nginx/html;
  index index.html;

  location / {
    try_files $uri $uri/ /index.html;
  }
}
```

---

# 8. Domain Model

## 8.1 Enums

Create in `FlowLedger.Domain/Enums`.

### RoleName

```csharp
public enum RoleName
{
    Sales = 1,
    Accounts = 2,
    Manager = 3,
    Admin = 4
}
```

### BillingRequestStatus

```csharp
public enum BillingRequestStatus
{
    Draft = 1,
    Submitted = 2,
    AccountsReview = 3,
    ManagerApproval = 4,
    Approved = 5,
    Rejected = 6,
    InvoiceGenerated = 7,
    Paid = 8,
    Cancelled = 9
}
```

### AuditActionType

```csharp
public enum AuditActionType
{
    Created = 1,
    Updated = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    Commented = 6,
    InvoiceGenerated = 7,
    PaymentMarked = 8,
    Assigned = 9,
    Cancelled = 10
}
```

### InvoiceStatus

```csharp
public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Cancelled = 4
}
```

---

## 8.2 Entities

### User

```csharp
public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RoleName Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
```

### Customer

```csharp
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

### BillingRequest

```csharp
public class BillingRequest
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BillingRequestStatus Status { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public decimal SubtotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<BillingRequestLineItem> LineItems { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
    public Invoice? Invoice { get; set; }
}
```

### BillingRequestLineItem

```csharp
public class BillingRequestLineItem
{
    public Guid Id { get; set; }
    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
```

### Comment

```csharp
public class Comment
{
    public Guid Id { get; set; }
    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

### AuditLog

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public AuditActionType ActionType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

### Invoice

```csharp
public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid BillingRequestId { get; set; }
    public BillingRequest BillingRequest { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal SubtotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }

    public DateTime IssuedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
```

### Notification

```csharp
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

---

# 9. Business Rules

## 9.1 Approval threshold

Use this constant:

```csharp
public static class ApprovalRules
{
    public const decimal ManagerApprovalThreshold = 100000m;
    public const decimal VatRate = 0.15m;
}
```

Rules:

- Sales can create draft billing requests.
- Sales can submit their own draft/rejected request.
- Accounts can approve/reject requests in `AccountsReview`.
- If total amount is less than or equal to 100,000 BDT, Accounts approval directly generates invoice.
- If total amount is above 100,000 BDT, Accounts approval moves request to `ManagerApproval`.
- Manager can approve/reject requests in `ManagerApproval`.
- Manager approval generates invoice.
- Accounts can mark invoices as paid.
- Admin can view everything.

## 9.2 Status transition table

| Current Status | Action | Role | Next Status |
|---|---|---|---|
| Draft | Submit | Sales/Admin | AccountsReview |
| Rejected | Resubmit | Sales/Admin | AccountsReview |
| AccountsReview | Approve <= 100k | Accounts/Admin | InvoiceGenerated |
| AccountsReview | Approve > 100k | Accounts/Admin | ManagerApproval |
| AccountsReview | Reject | Accounts/Admin | Rejected |
| ManagerApproval | Approve | Manager/Admin | InvoiceGenerated |
| ManagerApproval | Reject | Manager/Admin | Rejected |
| InvoiceGenerated | Mark Paid | Accounts/Admin | Paid |
| Draft | Cancel | Sales/Admin | Cancelled |

---

# 10. Database Design

## Tables

```text
Users
Customers
BillingRequests
BillingRequestLineItems
Comments
AuditLogs
Invoices
Notifications
```

## Important indexes

```text
BillingRequests.Status
BillingRequests.CreatedByUserId
BillingRequests.AssignedToUserId
BillingRequests.CustomerId
BillingRequests.CreatedAtUtc
Invoices.Status
Invoices.InvoiceNumber unique
BillingRequests.RequestNumber unique
AuditLogs.BillingRequestId + CreatedAtUtc
```

## EF Core configuration guidance

Use decimal precision:

```csharp
builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
builder.Property(x => x.VatAmount).HasPrecision(18, 2);
builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
```

Use string max lengths:

```csharp
Title: 200
Description: 2000
Comment body: 2000
Email: 256
Name: 200
RequestNumber: 50
InvoiceNumber: 50
```

Use delete behavior carefully:

- Do not cascade delete `BillingRequest` to `User`.
- Cascade delete line items/comments/audit logs only when deleting a billing request, though delete API is not needed.
- Use restricted delete for invoices.

---

# 11. Seed Data

## 11.1 Users

Seed exactly these users.

| Name | Email | Role | Password |
|---|---|---|---|
| Sarah Sales | sales@flowledger.local | Sales | password |
| Amir Accounts | accounts@flowledger.local | Accounts | password |
| Mona Manager | manager@flowledger.local | Manager | password |
| Adam Admin | admin@flowledger.local | Admin | password |

For simplicity, do not store passwords in the database. The login endpoint can accept these emails and password `password`, then issue a JWT based on seeded user identity.

This is acceptable because the assignment says mocked users/roles are fine.

## 11.2 Customers

Seed:

```text
Fiber Retail Ltd.
Metro Logistics Bangladesh
Northstar Enterprise
Greenline Distribution
BluePeak Systems
Eastern Trading Co.
```

## 11.3 Billing requests

Seed 12-18 billing requests across statuses:

```text
Draft: 2
AccountsReview: 3
ManagerApproval: 2
Rejected: 2
InvoiceGenerated: 4
Paid: 3
Cancelled: 1
```

Include different amounts:

```text
Small: 12,000 - 40,000
Medium: 50,000 - 95,000
High: 125,000 - 350,000
```

## 11.4 Seeded journey examples

### Journey 1: Accounts can approve directly

```text
Request: BR-2026-0004
Customer: Fiber Retail Ltd.
Amount: 45,000
Status: AccountsReview
Expected action: Accounts approves → invoice generated
```

### Journey 2: High-value approval

```text
Request: BR-2026-0006
Customer: Metro Logistics Bangladesh
Amount: 180,000
Status: AccountsReview
Expected action: Accounts approves → ManagerApproval
Then Manager approves → invoice generated
```

### Journey 3: Rejected request revision

```text
Request: BR-2026-0008
Customer: Northstar Enterprise
Status: Rejected
Expected action: Sales revises → resubmits → AccountsReview
```

### Journey 4: Payment completion

```text
Invoice: INV-2026-0003
Status: Issued
Expected action: Accounts marks paid → request becomes Paid
```

---

# 12. Backend API Design

Base URL:

```text
/api
```

## 12.1 Auth endpoints

### POST `/api/auth/login`

Request:

```json
{
  "email": "sales@flowledger.local",
  "password": "password"
}
```

Response:

```json
{
  "accessToken": "jwt-token",
  "user": {
    "id": "guid",
    "fullName": "Sarah Sales",
    "email": "sales@flowledger.local",
    "role": "Sales"
  }
}
```

### GET `/api/auth/me`

Returns current user.

---

## 12.2 Customers

### GET `/api/customers`

Used by create request form.

---

## 12.3 Billing requests

### GET `/api/billing-requests`

Query params:

```text
status
customerId
assignedToMe
createdByMe
search
fromDate
untilDate
page
pageSize
```

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 100
}
```

### GET `/api/billing-requests/{id}`

Return:

- Request header.
- Customer.
- Line items.
- Comments.
- Audit logs.
- Invoice if generated.
- Available actions for current user.

### POST `/api/billing-requests`

Sales/Admin only.

### PUT `/api/billing-requests/{id}`

Allowed when Draft or Rejected and owned by Sales user or Admin.

### POST `/api/billing-requests/{id}/submit`

Sales/Admin only.

### POST `/api/billing-requests/{id}/approve`

Accounts/Manager/Admin depending on status.

Request:

```json
{
  "comment": "Looks good. Approved."
}
```

### POST `/api/billing-requests/{id}/reject`

Request:

```json
{
  "reason": "Missing supporting document."
}
```

### POST `/api/billing-requests/{id}/comments`

Request:

```json
{
  "body": "Please verify VAT calculation."
}
```

---

## 12.4 Invoices

### GET `/api/invoices`

Query params:

```text
status
customerId
search
page
pageSize
```

### GET `/api/invoices/{id}`

### POST `/api/invoices/{id}/mark-paid`

Accounts/Admin only.

---

## 12.5 Dashboard

### GET `/api/dashboard/summary`

Response:

```json
{
  "totalRequests": 18,
  "pendingAccountsReview": 3,
  "pendingManagerApproval": 2,
  "approvedThisMonth": 7,
  "totalInvoiceAmount": 720000,
  "paidInvoiceAmount": 430000,
  "rejectedCount": 2,
  "averageApprovalHours": 14.5,
  "statusBreakdown": [
    { "status": "AccountsReview", "count": 3 },
    { "status": "ManagerApproval", "count": 2 }
  ],
  "monthlyInvoiceTrend": [
    { "month": "Jan", "amount": 120000 },
    { "month": "Feb", "amount": 180000 }
  ],
  "agingBuckets": [
    { "label": "0-1 days", "count": 4 },
    { "label": "2-3 days", "count": 3 },
    { "label": "4+ days", "count": 2 }
  ],
  "recentActivity": []
}
```

---

# 13. Backend Service Design

## 13.1 Application services

Create these interfaces in Application layer:

```csharp
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IBillingRequestService
{
    Task<PagedResult<BillingRequestListItemDto>> GetAsync(BillingRequestQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<BillingRequestDetailDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<Guid> CreateAsync(CreateBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, UpdateBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task SubmitAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task ApproveAsync(Guid id, ApproveBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task RejectAsync(Guid id, RejectBillingRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task AddCommentAsync(Guid id, AddCommentDto request, CurrentUser currentUser, CancellationToken cancellationToken);
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetAsync(InvoiceQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<InvoiceDetailDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task MarkPaidAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CurrentUser currentUser, CancellationToken cancellationToken);
}
```

## 13.2 Avoid over-engineering

Do not add:

- MediatR.
- Full repository pattern over EF Core unless necessary.
- Event bus.
- Background jobs.
- Real email/SMS.
- Real file attachments.
- Real OAuth.

This keeps the code KISS and YAGNI.

## 13.3 Where business logic should live

Put workflow logic in `BillingRequestService`.

Do not put status-transition logic directly in controllers.

Controllers should:

- Validate auth.
- Receive request.
- Call service.
- Return response.

Services should:

- Load entities.
- Check permissions.
- Validate status transitions.
- Update status.
- Create audit logs.
- Create notifications.
- Save changes.

---

# 14. SOLID, KISS, DRY, YAGNI Guidance

## SOLID

### Single Responsibility

- Controllers handle HTTP.
- Services handle use cases.
- DbContext handles persistence.
- Validators handle input validation.
- Token service handles JWT creation.

### Open/Closed

Keep status transition logic centralized so adding another role/action later does not require changing controllers everywhere.

### Liskov Substitution

Not heavily relevant. Do not force inheritance.

### Interface Segregation

Separate services by use case:

```text
IBillingRequestService
IInvoiceService
IDashboardService
IAuthService
```

Do not make one huge `IErpService`.

### Dependency Inversion

Application services can depend on abstractions such as:

```text
ICurrentUserAccessor
IDateTimeProvider
ITokenService
```

Infrastructure provides implementations.

## KISS

- Use normal REST controllers.
- Use simple JWT auth.
- Use SQL Server and EF Core.
- Use simple role checks.
- Use one SPA frontend.

## DRY

- Shared API response types.
- Shared status badge component in frontend.
- Shared date/amount formatters.
- Shared audit log helper method.
- Shared permission/action calculation.

## YAGNI

Do not build:

- Multi-tenant support.
- File uploads.
- Real email notifications.
- Payment gateway.
- Complex accounting ledger.
- Real user registration.
- Refresh tokens.
- Complex dashboard customization.

Mention these as future improvements in README.

---

# 15. Authentication and Authorization

## 15.1 Login approach

Use simple seeded-user login.

The user enters email and password. If password is `password` and email matches a seeded user, return JWT.

JWT claims:

```text
sub: user id
email: user email
name: full name
role: Sales / Accounts / Manager / Admin
```

## 15.2 Backend authorization policies

In `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SalesOnly", policy => policy.RequireRole("Sales", "Admin"));
    options.AddPolicy("AccountsOnly", policy => policy.RequireRole("Accounts", "Admin"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("InternalUser", policy => policy.RequireRole("Sales", "Accounts", "Manager", "Admin"));
});
```

Use role attributes for coarse access:

```csharp
[Authorize(Policy = "InternalUser")]
```

Use service-level permission checks for detailed business rules.

## 15.3 Frontend auth storage

Store token in `localStorage` for this take-home project.

```text
localStorage key: flowledger_access_token
localStorage key: flowledger_user
```

This is acceptable for a take-home mocked internal app. Mention in design note that production would use more secure token handling.

## 15.4 Axios interceptor

Attach token to every API request:

```typescript
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("flowledger_access_token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
```

Handle 401 globally:

```typescript
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      localStorage.removeItem("flowledger_access_token");
      localStorage.removeItem("flowledger_user");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);
```

---

# 16. Frontend Routing

Use React Router.

Routes:

```text
/login
/app/dashboard
/app/requests
/app/requests/new
/app/requests/:id
/app/invoices
/app/invoices/:id
/app/customers
/app/settings
```

Protected route behavior:

- If no token, redirect to `/login`.
- If logged in, redirect `/` to `/app/dashboard`.

---

# 17. Frontend Project Structure

```text
src/
├── app/
│   ├── router.tsx
│   ├── providers.tsx
│   └── layout/
│       ├── AppLayout.tsx
│       ├── Sidebar.tsx
│       ├── Topbar.tsx
│       └── RoleSwitcherHint.tsx
├── components/
│   ├── ui/                    # shadcn components
│   ├── common/
│   │   ├── PageHeader.tsx
│   │   ├── StatusBadge.tsx
│   │   ├── MoneyText.tsx
│   │   ├── EmptyState.tsx
│   │   ├── ErrorState.tsx
│   │   ├── LoadingSkeleton.tsx
│   │   ├── ConfirmDialog.tsx
│   │   └── AuditTimeline.tsx
├── features/
│   ├── auth/
│   │   ├── api.ts
│   │   ├── types.ts
│   │   ├── useAuth.tsx
│   │   └── LoginPage.tsx
│   ├── dashboard/
│   │   ├── api.ts
│   │   ├── DashboardPage.tsx
│   │   ├── MetricCard.tsx
│   │   ├── StatusPieChart.tsx
│   │   ├── InvoiceTrendChart.tsx
│   │   └── AgingBucketChart.tsx
│   ├── billing-requests/
│   │   ├── api.ts
│   │   ├── types.ts
│   │   ├── RequestListPage.tsx
│   │   ├── RequestCreatePage.tsx
│   │   ├── RequestDetailPage.tsx
│   │   ├── RequestForm.tsx
│   │   ├── RequestActionPanel.tsx
│   │   ├── RequestFilters.tsx
│   │   └── LineItemsEditor.tsx
│   ├── invoices/
│   │   ├── api.ts
│   │   ├── types.ts
│   │   ├── InvoiceListPage.tsx
│   │   └── InvoiceDetailPage.tsx
│   └── customers/
│       ├── api.ts
│       └── types.ts
├── lib/
│   ├── api.ts
│   ├── formatters.ts
│   ├── permissions.ts
│   └── utils.ts
├── styles/
│   └── globals.css
└── main.tsx
```

---

# 18. UI Design

## 18.1 Overall visual style

Use a clean internal SaaS dashboard look.

Layout:

```text
Left sidebar
Top bar
Main content area
Cards/tables/forms
```

Color direction:

- Slate/neutral base.
- Blue accent for primary actions.
- Green for approved/paid.
- Amber for pending review.
- Red for rejected.
- Purple for manager approval.

With shadcn, rely mostly on default theme. Do not spend too much time on custom CSS.

## 18.2 Sidebar items

```text
Dashboard
Billing Requests
Invoices
Customers
Settings/About
```

## 18.3 Topbar

Show:

- Page title or breadcrumb.
- Current user name.
- Role badge.
- Logout button.

Example:

```text
Sarah Sales    Sales Badge    Logout
```

## 18.4 Status badge colors

```text
Draft: gray
AccountsReview: amber
ManagerApproval: purple
Approved: blue
Rejected: red
InvoiceGenerated: green
Paid: emerald
Cancelled: muted gray
```

---

# 19. Pages and User Journeys

## 19.1 Login Page

Path:

```text
/login
```

UI:

- Centered login card.
- Email input.
- Password input.
- Login button.
- Demo credential cards/buttons.

Show quick login buttons:

```text
Login as Sales
Login as Accounts
Login as Manager
Login as Admin
```

This helps reviewer test flows quickly.

Components:

- Card
- Input
- Label
- Button
- Alert
- Badge

---

## 19.2 Dashboard Page

Path:

```text
/app/dashboard
```

Purpose:

Shows operational overview and gives reviewer a strong first impression.

UI sections:

### Top metric cards

Cards:

```text
Total Requests
Pending Accounts Review
Pending Manager Approval
Invoice Amount This Month
Paid Amount
Rejected Requests
Average Approval Time
```

Use shadcn Card + Lucide icons.

### Charts

Use Recharts.

Chart 1: Status breakdown

- Pie chart or donut chart.
- Shows count by request status.

Chart 2: Monthly invoice trend

- Bar chart or line chart.
- Shows invoice amount by month.

Chart 3: Aging buckets

- Bar chart.
- Shows pending requests by age.

### Recent activity

Show last 8 audit events:

```text
Amir Accounts approved BR-2026-0004
Mona Manager rejected BR-2026-0006
Sarah Sales submitted BR-2026-0008
```

### Role-aware CTA

If Sales:

```text
Create Billing Request
View My Requests
```

If Accounts:

```text
Review Pending Requests
View Invoices
```

If Manager:

```text
Review High-Value Approvals
```

If Admin:

```text
View All Requests
```

---

## 19.3 Billing Request List Page

Path:

```text
/app/requests
```

Purpose:

Shows all relevant requests with filtering and role-aware visibility.

UI:

- Page header with `New Request` button for Sales/Admin.
- Filter bar.
- Table.
- Empty state.

Filters:

```text
Search
Status dropdown
Customer dropdown
Assigned to me checkbox
Created by me checkbox
Date range optional
```

Table columns:

```text
Request No
Title
Customer
Status
Amount
Created By
Assigned To
Created Date
Actions
```

Actions:

```text
View
Approve if allowed
Reject if allowed
```

Role behavior:

- Sales sees own requests by default.
- Accounts sees AccountsReview first.
- Manager sees ManagerApproval first.
- Admin sees all.

Components:

- Table
- Badge
- Button
- Select
- Input
- DropdownMenu
- Skeleton
- EmptyState

---

## 19.4 Create Billing Request Page

Path:

```text
/app/requests/new
```

Allowed:

- Sales
- Admin

UI:

- Customer dropdown.
- Title.
- Description.
- Dynamic line items table.
- Calculated subtotal/VAT/total.
- Save Draft button.
- Submit button.

Line item fields:

```text
Description
Quantity
Unit Price
Line Total
Remove button
```

Validation:

- Customer required.
- Title required, max 200.
- At least one line item.
- Quantity > 0.
- Unit price > 0.

Components:

- Form
- Input
- Textarea
- Select
- Table
- Button
- Card
- Alert

---

## 19.5 Billing Request Detail Page

Path:

```text
/app/requests/:id
```

Purpose:

This is the most important screen. Make it look thoughtful.

Layout:

```text
Header card
Two-column layout
Left: request details, line items, comments
Right: status/action panel, audit timeline, invoice card
```

Header:

```text
BR-2026-0006
High-value connectivity setup billing
Status badge
Customer
Total amount
```

Left side:

- Request details.
- Customer information.
- Line items table.
- Comments section.

Right side:

- Current status.
- Next possible action.
- Action buttons.
- Audit timeline.
- Invoice summary if generated.

Action panel examples:

For Accounts on `AccountsReview`:

```text
Approve
Reject
Add Comment
```

For Manager on `ManagerApproval`:

```text
Approve High-Value Request
Reject
Add Comment
```

For Sales on `Rejected`:

```text
Edit and Resubmit
Add Comment
```

For Accounts on `InvoiceGenerated`:

```text
View Invoice
Mark Paid
```

Timeline example:

```text
Created by Sarah Sales
Submitted for review
Assigned to Accounts
Approved by Amir Accounts
Escalated to Manager because total exceeded BDT 100,000
Invoice generated
```

Components:

- Card
- Badge
- Button
- Dialog
- Textarea
- Separator
- Alert
- Table
- Timeline custom component

---

## 19.6 Invoice List Page

Path:

```text
/app/invoices
```

UI:

- Invoice table.
- Filters by status/customer/search.
- Paid/unpaid badges.

Columns:

```text
Invoice No
Request No
Customer
Status
Amount
Issued Date
Due Date
Actions
```

Actions:

```text
View
Mark Paid if Accounts/Admin and not paid
```

---

## 19.7 Invoice Detail Page

Path:

```text
/app/invoices/:id
```

Design this like a printable invoice.

UI:

```text
Invoice header
Customer billing info
Line items
Subtotal
VAT
Total
Status badge
Due date
Mark paid button if allowed
```

Optional:

- Print button using `window.print()`.
- Export JSON or CSV is optional.

---

## 19.8 Customers Page

Path:

```text
/app/customers
```

Simple read-only page.

UI:

- Customer list cards/table.
- Number of requests per customer if easy.

This page is optional but useful for looking complete.

---

## 19.9 Settings/About Page

Path:

```text
/app/settings
```

Show:

- Selected option.
- Architecture summary.
- Demo credentials.
- Known limitations.
- AI usage note.

This helps reviewers understand the project inside the app.

---

# 20. Role-Based User Journeys

## 20.1 Sales journey

Login as:

```text
sales@flowledger.local / password
```

Journey:

1. Open Dashboard.
2. See own request stats.
3. Create billing request.
4. Add customer and line items.
5. Submit request.
6. See status as `AccountsReview`.
7. Add comment.
8. If rejected, revise and resubmit.

## 20.2 Accounts journey

Login as:

```text
accounts@flowledger.local / password
```

Journey:

1. Open Dashboard.
2. See pending Accounts Review count.
3. Open Billing Requests.
4. Filter `AccountsReview`.
5. Open a low-value request.
6. Approve it.
7. See invoice generated.
8. Open invoice.
9. Mark invoice as paid.
10. Open high-value request.
11. Approve it.
12. See it move to `ManagerApproval`.

## 20.3 Manager journey

Login as:

```text
manager@flowledger.local / password
```

Journey:

1. Open Dashboard.
2. See pending high-value approvals.
3. Open ManagerApproval request.
4. Review line items and audit history.
5. Approve or reject.
6. If approved, see invoice generated.

## 20.4 Admin journey

Login as:

```text
admin@flowledger.local / password
```

Journey:

1. Can view all requests.
2. Can view all invoices.
3. Can perform any action.
4. Useful for reviewer to test everything quickly.

---

# 21. Authorized Views

## Backend

All protected endpoints require JWT.

Use:

```csharp
[Authorize]
```

Then inside service check detailed permissions.

Example:

```csharp
if (currentUser.Role == RoleName.Sales && request.CreatedByUserId != currentUser.Id)
{
    throw new ForbiddenException("Sales users can only view their own requests.");
}
```

## Frontend

Create `permissions.ts`:

```typescript
export function canCreateRequest(role: Role) {
  return role === "Sales" || role === "Admin";
}

export function canApproveRequest(role: Role, status: BillingRequestStatus) {
  if (role === "Admin") return status === "AccountsReview" || status === "ManagerApproval";
  if (role === "Accounts") return status === "AccountsReview";
  if (role === "Manager") return status === "ManagerApproval";
  return false;
}

export function canMarkInvoicePaid(role: Role, invoiceStatus: InvoiceStatus) {
  return (role === "Accounts" || role === "Admin") && invoiceStatus === "Issued";
}
```

Use this to hide/show UI buttons, but still enforce security on backend.

---

# 22. Validation Rules

## Create/update billing request

- CustomerId required.
- Title required, 3-200 characters.
- Description max 2000 characters.
- At least one line item.
- Each line item description required.
- Quantity between 1 and 10,000.
- UnitPrice between 1 and 10,000,000.

## Comments

- Body required.
- Max 2000 characters.

## Reject

- Reason required.
- Max 1000 characters.

---

# 23. Error Handling

## Backend error response shape

Use a consistent shape:

```json
{
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "title": ["Title is required"]
  }
}
```

Use ASP.NET Core ProblemDetails if possible.

Create middleware for:

- ValidationException → 400
- NotFoundException → 404
- ForbiddenException → 403
- InvalidOperationException for workflow errors → 409
- Unhandled → 500

## Frontend states

For every important page show:

- Loading skeleton.
- Error state with retry.
- Empty state.
- Success toast.

Examples:

```text
No billing requests found for this filter.
You have no pending approvals.
Invoice was marked as paid.
Request was approved and invoice INV-2026-0012 was generated.
```

---

# 24. Dashboard Charts

Use Recharts.

## Chart 1: Status Breakdown

Type:

```text
Donut/Pie chart
```

Data:

```text
Draft
AccountsReview
ManagerApproval
Rejected
InvoiceGenerated
Paid
```

## Chart 2: Invoice Trend

Type:

```text
Bar chart
```

Data:

```text
Month vs invoice amount
```

## Chart 3: Aging Buckets

Type:

```text
Horizontal bar chart
```

Buckets:

```text
0-1 days
2-3 days
4-7 days
8+ days
```

## Chart 4: Approval Funnel

Optional:

```text
Submitted → Accounts Review → Manager Approval → Invoice Generated → Paid
```

Could be shown as cards, not necessarily a chart.

---

# 25. API DTOs

## CreateBillingRequestDto

```csharp
public class CreateBillingRequestDto
{
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CreateBillingRequestLineItemDto> LineItems { get; set; } = new();
    public bool SubmitImmediately { get; set; }
}
```

## CreateBillingRequestLineItemDto

```csharp
public class CreateBillingRequestLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## ApproveBillingRequestDto

```csharp
public class ApproveBillingRequestDto
{
    public string? Comment { get; set; }
}
```

## RejectBillingRequestDto

```csharp
public class RejectBillingRequestDto
{
    public string Reason { get; set; } = string.Empty;
}
```

## BillingRequestListItemDto

```csharp
public class BillingRequestListItemDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

## AvailableActionDto

```csharp
public class AvailableActionDto
{
    public string Action { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
```

For detail response, include available actions so frontend and backend stay aligned.

---

# 26. Implementation Order for AI Agent

Use this exact sequence to reduce risk.

## Phase 1: Skeleton and Docker

1. Create repo structure.
2. Create .NET solution.
3. Create React app.
4. Add Dockerfiles.
5. Add docker-compose.
6. Verify `docker compose up --build` starts SQL Server, API, and web.
7. API should expose `/health` returning `{ status: "ok" }`.
8. Frontend should show a simple landing page.

Do not proceed until this works.

## Phase 2: Backend domain and database

1. Add entities/enums.
2. Add DbContext.
3. Add EF configurations.
4. Add seed data.
5. Add migration.
6. Verify database tables are created.
7. Verify seed data appears.

## Phase 3: Auth

1. Add login endpoint.
2. Add JWT generation.
3. Add `/api/auth/me`.
4. Add Swagger auth support.
5. Test login from Swagger.

## Phase 4: Billing request APIs

1. Add create request.
2. Add list requests.
3. Add detail request.
4. Add submit.
5. Add comments.
6. Add approval.
7. Add rejection.
8. Add invoice generation during approval.
9. Add audit logs for all workflow actions.

## Phase 5: Invoice APIs

1. Add invoice list.
2. Add invoice detail.
3. Add mark paid.
4. Add audit log and status update for payment.

## Phase 6: Dashboard APIs

1. Add summary cards.
2. Add status breakdown.
3. Add invoice trend.
4. Add aging buckets.
5. Add recent activity.

## Phase 7: Frontend auth and layout

1. Add Tailwind and shadcn.
2. Add API client.
3. Add AuthProvider.
4. Add login page.
5. Add protected routes.
6. Add app layout with sidebar/topbar.

## Phase 8: Frontend pages

1. Dashboard.
2. Billing request list.
3. Create billing request.
4. Billing request detail.
5. Invoice list.
6. Invoice detail.
7. Customers.
8. Settings/About.

## Phase 9: Tests

1. Unit test workflow rules.
2. Integration test login.
3. Integration test create + submit.
4. Integration test accounts approval under threshold creates invoice.
5. Integration test high-value approval moves to manager.
6. Integration test manager approval creates invoice.
7. Integration test unauthorized role cannot approve.

## Phase 10: Documentation polish

1. README.
2. Design note.
3. Demo credentials.
4. Known limitations.
5. Future improvements.
6. AI usage reflection.
7. Screenshots if possible.

---

# 27. Testing Plan

## 27.1 Unit tests

Test service/domain behavior:

```text
Calculate total amount from line items.
Apply VAT correctly.
Sales can submit draft request.
Accounts approval below threshold generates invoice.
Accounts approval above threshold moves to ManagerApproval.
Manager approval generates invoice.
Rejected request can be resubmitted.
Invalid transition throws conflict.
Unauthorized role throws forbidden.
```

## 27.2 Integration tests

Use WebApplicationFactory + Testcontainers SQL Server.

Tests:

```text
POST /api/auth/login returns JWT for seeded user.
GET /api/billing-requests requires authentication.
Sales can create billing request.
Accounts cannot create billing request.
Accounts can approve AccountsReview request.
Manager cannot approve AccountsReview request.
Manager can approve ManagerApproval request.
Mark paid changes invoice and request status.
```

## 27.3 Frontend testing

Optional. If time is short, skip frontend tests. Backend tests are more valuable for this assignment.

If adding frontend tests:

- Vitest.
- React Testing Library.
- Test `StatusBadge` and permission helpers only.

---

# 28. README Contents

Create `README.md` with:

```text
# FlowLedger

## What I built
## Selected option
## Demo credentials
## How to run with Docker
## Local development without Docker
## Key user flows
## Architecture overview
## Backend structure
## Frontend structure
## Data model overview
## API overview
## Testing
## Known limitations
## What I would improve with more time
## AI tools used and review process
```

## Demo credentials section

```markdown
| Role | Email | Password |
|---|---|---|
| Sales | sales@flowledger.local | password |
| Accounts | accounts@flowledger.local | password |
| Manager | manager@flowledger.local | password |
| Admin | admin@flowledger.local | password |
```

## Run section

```bash
git clone <repo-url>
cd flowledger
docker compose up --build
```

Then:

```text
Frontend: http://localhost:5173
Swagger: http://localhost:8080/swagger
```

---

# 29. Design Note Contents

Create `docs/design-note.md`.

Include:

```text
# Design Note

## Problem
Internal teams need a simple way to submit, review, approve, invoice, and track billing requests.

## Selected Option
Option 1: ERP Workflow Module.

## Users and roles
Sales, Accounts, Manager, Admin.

## Workflow
Explain status transitions.

## Architecture
React SPA + ASP.NET Core API + SQL Server.

## Backend design
Controllers call application services. Services enforce workflow rules. EF Core persists data.

## Frontend design
Role-aware dashboard, tables, forms, detail pages, and charts.

## Data model
Explain main entities.

## Security
Mock login, JWT auth, role-based authorization, service-level permission checks.

## Testing
Unit and integration tests for workflow and API authorization.

## Tradeoffs
Simple JWT instead of full identity.
SQL Server instead of MongoDB.
No real email/payment/file upload.
No complex accounting ledger.

## Known limitations
Mock auth, no real notifications, no file attachments, no real payment integration.

## Future improvements
Real identity provider, email notifications, attachments, PDF export, advanced reporting, approval rules UI.

## AI usage
AI was used to accelerate scaffolding and UI generation, but code was reviewed, simplified, tested, and adjusted manually.
```

---

# 30. UI Component Checklist

Use these shadcn components:

```text
Button
Card
Badge
Table
Tabs
Dialog
DropdownMenu
Select
Input
Textarea
Label
Form
Separator
Sheet
Skeleton
Alert
Toast/Sonner
Avatar
Progress
Breadcrumb
```

Use custom components:

```text
StatusBadge
MoneyText
PageHeader
MetricCard
AuditTimeline
ConfirmDialog
EmptyState
ErrorState
LoadingSkeleton
RequestActionPanel
LineItemsEditor
```

---

# 31. Nice Details That Impress Reviewers

Add these if time permits:

## 31.1 Available actions from backend

In request detail API, return actions like:

```json
"availableActions": [
  { "action": "approve", "label": "Approve Request", "isPrimary": true },
  { "action": "reject", "label": "Reject", "isPrimary": false }
]
```

This shows the backend owns workflow permission logic.

## 31.2 Audit timeline

Every status change should create an audit log.

This directly satisfies the auditability requirement.

## 31.3 Aging bucket

Show pending requests by age.

This shows operational/business thinking.

## 31.4 Role-aware empty states

Examples:

```text
Sales: You have not created any billing requests yet.
Accounts: There are no requests waiting for accounts review.
Manager: No high-value approvals are pending.
```

## 31.5 Design note inside app

The Settings/About page can mention the architecture and demo users. This helps reviewers quickly understand the submission.

---

# 32. Time Management Plan

Assuming limited time, prioritize like this.

## Must finish

```text
Docker Compose
Backend APIs
SQL Server persistence
Seed data
Auth/roles
Request workflow
Invoice generation
Dashboard basics
Frontend list/detail/create pages
README
```

## Should finish

```text
Audit timeline
Charts
Integration tests
Design note
Role-aware views
```

## Nice to have

```text
Print invoice
CSV export
Frontend tests
Notification center
Advanced charts
```

Do not build nice-to-have features until the core workflow is fully working.

---

# 33. Exact Agent Prompt to Use

Use this prompt with your AI agent after creating the repo:

```text
You are building FlowLedger, an ERP workflow module take-home assignment.
Follow the project plan in docs/erp_workflow_build_plan.md exactly.
Do not over-engineer.
Use .NET 8 ASP.NET Core Web API, EF Core, SQL Server, React Vite TypeScript, Tailwind, shadcn/ui, TanStack Query, Axios, Recharts.
Implement one phase at a time.
After each phase, make sure the app builds and runs.
Keep the code clean, simple, readable, and aligned with SOLID, KISS, DRY, and YAGNI.
Do not add features outside the plan unless explicitly asked.
Start with Phase 1 only.
```

Then ask the agent phase by phase:

```text
Implement Phase 1 only. Stop after verifying Docker Compose starts the API, frontend, and SQL Server.
```

Then:

```text
Implement Phase 2 only. Add entities, DbContext, EF configurations, seed data, migration, and database initialization. Stop after it builds.
```

Continue phase by phase.

---

# 34. Final Submission Checklist

Before submission, verify:

```text
[ ] docker compose up --build works from clean clone
[ ] Frontend opens at http://localhost:5173
[ ] Swagger opens at http://localhost:8080/swagger
[ ] Demo users can login
[ ] Sales can create and submit request
[ ] Accounts can approve low-value request and invoice is generated
[ ] Accounts can escalate high-value request to manager
[ ] Manager can approve high-value request and invoice is generated
[ ] Accounts can mark invoice paid
[ ] Audit timeline appears
[ ] Dashboard charts show seeded data
[ ] Unauthorized buttons are hidden in frontend
[ ] Unauthorized API calls are rejected in backend
[ ] Tests pass
[ ] README includes setup and credentials
[ ] Design note explains architecture and tradeoffs
[ ] AI usage note is honest
```

Run:

```bash
cd backend
dotnet test
```

Run frontend build:

```bash
cd frontend/flowledger-web
npm run build
```

Run Docker:

```bash
docker compose down -v
docker compose up --build
```

---

# 35. Final Advice

The goal is not to build a huge ERP. The goal is to show that you can take an ambiguous business requirement, scope it responsibly, model it cleanly, implement a working full-stack module, protect actions by role, test key behavior, and document tradeoffs.

A small complete module with clean workflow and auditability will score better than a visually flashy but incomplete system.
