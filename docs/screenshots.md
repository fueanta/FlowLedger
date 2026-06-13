# Screenshots

Screenshots were not committed because this repository keeps source and documentation lightweight. To inspect the UI locally:

```bash
cp .env.example .env
docker compose up --build
```

Then open:

```text
Login: http://localhost:5173/login
Dashboard: http://localhost:5173/app/dashboard
Billing requests: http://localhost:5173/app/requests
Invoices: http://localhost:5173/app/invoices
```

Suggested review screenshots:

- Login page.
- Dashboard with period filter and charts.
- Billing request list with role-aware filters.
- Billing request detail with audit timeline and actions.
- Create/edit billing request form.
- Invoice detail printable view.
