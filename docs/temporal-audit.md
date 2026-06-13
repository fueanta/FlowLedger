# Temporal History and Audit Logs

FlowLedger uses two complementary change-tracking mechanisms.

## SQL Server Temporal Tables

Temporal tables preserve row versions for data reconstruction and technical history.

Enabled tables:

- `Customers`
- `BillingRequests`
- `Invoices`
- `AppSettings`

History tables:

- `CustomersHistory`
- `BillingRequestsHistory`
- `InvoicesHistory`
- `AppSettingsHistory`

Use temporal history to answer:

- What did this row look like before a change?
- When did a persisted value change?
- Which data state existed during a prior period?

Temporal history is database-owned. It does not explain user intent.

## Audit Logs

Audit logs preserve business actions and actor context.

Use audit logs to answer:

- Who performed the workflow or administration action?
- What workflow status changed?
- Why was a request rejected, approved, assigned, or marked paid?
- Which enrollment or user administration action happened?

Audit logs should be written in the same unit of work as the workflow change they describe.

## Rule of Thumb

Temporal tables answer `what changed and when`.

Audit logs answer `who changed it and why`.
