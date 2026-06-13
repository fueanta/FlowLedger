# Design Note

## Problem

Internal Sales, Accounts, and Management users need a simple way to submit, review, approve, invoice, and track billing requests without losing audit history.

## Selected Option

Option 1: ERP Workflow Module.

## Users and roles

- Sales creates, edits, submits, and resubmits billing requests.
- Accounts reviews submitted requests, approves low-value requests, rejects requests, and marks invoices paid.
- Manager approves or rejects high-value requests.
- Admin can view and perform operational workflow actions across roles, approve enrollment requests, manage users, and update billing settings.

## Workflow

Sales creates a draft request and submits it to Accounts. Accounts approves requests at or below the approval threshold, generating an invoice. Requests above the threshold move to Manager approval. Manager approval generates an invoice. Accounts can mark issued invoices paid. Accounts or Manager can reject a request, after which Sales can revise and resubmit it.

## Architecture

The app uses a React SPA, ASP.NET Core Web API, EF Core, and SQL Server. The frontend is role-aware but does not own security. The backend owns workflow rules, authorization checks, persistence, and audit logging.

## Backend design

Controllers authenticate requests, bind DTOs, and delegate to application service interfaces. Services enforce workflow transitions and role permissions. EF Core persists relational data and runs migrations/seed data during startup for the local assignment flow.

## Frontend design

The UI is a compact internal operations tool: dashboard metrics, reusable paginated tables, request form, request detail with action panel, invoice list/detail with print/PDF actions, client administration, enrollment/user administration, settings, work queue, and audit logs. It uses Tailwind, shadcn-style primitives, TanStack Query, React Hook Form, Zod validation, Lucide icons, and Recharts.

## Data model

Core entities are `User`, `EnrollmentRequest`, `UserPreference`, `Customer`, `BillingRequest`, `BillingRequestLineItem`, `Comment`, `AuditLog`, `Invoice`, and `AppSetting`. A billing request belongs to a customer and creator, has many line items/comments/audit logs, and can generate one invoice. `AppSetting` stores runtime configuration such as VAT, approval threshold, invoice due days, and JWT access-token lifetime. SQL Server temporal history is enabled for clients, billing requests, invoices, and settings.

## Security

The project uses mock login backed by seeded users, stored password hashes/salts, JWT bearer authentication, role-based authorization policies, and service-level permission checks. Demo passwords and JWT keys must come from local environment variables or secret stores, not committed source.

## Testing

Backend unit tests cover workflow calculations and service rules. Integration tests use WebApplicationFactory and Testcontainers SQL Server for login, authentication, authorization, request creation/submission, approvals, invoice generation, payment marking, dashboard validation, settings, enrollment, standardized tables, CSV export, PDF export, audit behavior, and migrations. Frontend tests cover validation helpers, auth client behavior, permission helpers, data tables, dashboard scope labels, and invoice detail actions.

## Tradeoffs

- Simple JWT auth instead of full identity provider.
- SQL Server instead of MongoDB because the data is relational and transactional.
- EF Core is used directly as the persistence abstraction; no extra repository layer was added.
- No real email, payment provider, file upload, or accounting ledger.
- Route-level frontend code splitting is deferred.

## Known limitations

- Mock auth only.
- No active JWT session revocation yet.
- No endpoint rate limiting yet.
- No notifications.
- No file attachments.
- No real payment integration.
- Invoice PDF export uses a lightweight internal PDF writer; richer branded layouts are future work.

## Future improvements

- Real identity provider.
- Admin-driven session revocation.
- Endpoint-specific rate limiting.
- Email or in-app notifications.
- Attachments.
- Richer branded invoice PDF layouts.
- Advanced reporting.
- Approval rules UI.
- CI pipeline with build/test/Compose smoke verification.

## AI usage

AI was used to accelerate scaffolding and UI generation, but code was reviewed, simplified, tested, and adjusted manually.
