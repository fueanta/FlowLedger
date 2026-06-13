import { PageHeader } from '../components/PageHeader'
import { Badge } from '../components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'

const demoUsers = [
  { role: 'Sales', email: 'sales@flowledger.local', passwordSource: 'SeedUsers__SalesPassword' },
  { role: 'Accounts', email: 'accounts@flowledger.local', passwordSource: 'SeedUsers__AccountsPassword' },
  { role: 'Manager', email: 'manager@flowledger.local', passwordSource: 'SeedUsers__ManagerPassword' },
  { role: 'Admin', email: 'admin@flowledger.local', passwordSource: 'SeedUsers__AdminPassword' },
]

export function SettingsPage() {
  return (
    <>
      <PageHeader title="Settings / About" description="Reviewer context for the FlowLedger take-home assignment." />

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Selected Option</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-700">
            <p>
              <strong className="text-slate-950">Option 1: ERP Workflow Module.</strong> Sales creates billing requests,
              Accounts reviews them, Manager approves high-value requests, and approved work generates invoices.
            </p>
            <div className="flex flex-wrap gap-2">
              <Badge variant="secondary">React</Badge>
              <Badge variant="secondary">ASP.NET Core</Badge>
              <Badge variant="secondary">SQL Server</Badge>
              <Badge variant="secondary">JWT</Badge>
              <Badge variant="secondary">EF Core</Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Architecture Summary</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-700">
            <p>Frontend is a React SPA using role-aware routes, TanStack Query, Axios, React Hook Form, Zod, Tailwind, and Recharts.</p>
            <p>Backend controllers call application services. Services own workflow permissions and status transitions. EF Core persists SQL Server data.</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Demo Access</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Role</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Password Source</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {demoUsers.map((user) => (
                    <TableRow key={user.role}>
                      <TableCell className="font-semibold text-slate-950">{user.role}</TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>
                        <code className="rounded bg-slate-100 px-2 py-1 text-xs text-slate-900">{user.passwordSource}</code>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Known Limitations</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-700">
            <p>Authentication uses seeded demo users instead of a real identity provider.</p>
            <p>No email notifications, file attachments, PDF export, or real payment provider integration are included.</p>
            <p>Session revocation is tracked in backlog for later; expired JWT handling is implemented in the UI.</p>
            <p>AI assisted implementation, but workflow behavior is covered by backend tests and frontend validation tests.</p>
          </CardContent>
        </Card>
      </div>
    </>
  )
}
