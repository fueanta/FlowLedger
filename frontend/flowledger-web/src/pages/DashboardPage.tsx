import { useQuery } from '@tanstack/react-query'
import { CheckCircle2, Clock, FileText, ReceiptText, TrendingUp } from 'lucide-react'
import { useState } from 'react'
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getDashboardSummary } from '../api/dashboard'
import { AuditTimeline } from '../components/AuditTimeline'
import { MetricCard } from '../components/MetricCard'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingBlock } from '../components/StateViews'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card'
import { Select } from '../components/ui/select'
import { formatMoney, formatStatus } from '../lib/format'

const chartColors = ['#1e3a8a', '#ca8a04', '#047857', '#b91c1c', '#475569', '#0f172a']
const periodOptions = [1, 3, 6, 12]

export function DashboardPage() {
  const [periodMonths, setPeriodMonths] = useState(1)
  const summaryQuery = useQuery({
    queryKey: ['dashboard-summary', periodMonths],
    queryFn: () => getDashboardSummary(periodMonths),
  })

  if (summaryQuery.isLoading) {
    return (
      <>
        <PageHeader title="Dashboard" description="Operational health for billing approvals and invoices." />
        <LoadingBlock />
      </>
    )
  }

  if (summaryQuery.isError || !summaryQuery.data) {
    return (
      <>
        <PageHeader title="Dashboard" description="Operational health for billing approvals and invoices." />
        <ErrorState message="Dashboard data could not be loaded." onRetry={() => void summaryQuery.refetch()} />
      </>
    )
  }

  const summary = summaryQuery.data

  return (
    <>
      <PageHeader
        title="Dashboard"
        description="Track request volume, pending approvals, invoice value, aging, and recent workflow activity."
      />

      <section aria-labelledby="period-activity-title">
        <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <h2 id="period-activity-title" className="text-lg font-semibold tracking-normal text-slate-950">
              Period Activity
            </h2>
            <p className="mt-1 text-sm text-slate-600">
              Filtered by selected period: {formatDateOnly(summary.period.startUtc)} to {formatDateOnly(summary.period.endUtc)}.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <label className="text-sm font-medium text-slate-700" htmlFor="dashboard-period">
              Period
            </label>
            <Select
              id="dashboard-period"
              value={String(periodMonths)}
              onChange={(event) => setPeriodMonths(Number(event.target.value))}
              className="w-36"
            >
              {periodOptions.map((option) => (
                <option key={option} value={option}>
                  {option} month{option > 1 ? 's' : ''}
                </option>
              ))}
            </Select>
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4" aria-label="Period activity metrics">
          <MetricCard label="Requests in period" value={String(summary.totalRequests)} hint="Created in selected period" scope="Period" icon={FileText} />
          <MetricCard label="Approved in period" value={String(summary.approvedThisMonth)} hint="Approved in selected period" scope="Period" icon={CheckCircle2} />
          <MetricCard label="Paid invoice value" value={formatMoney(summary.paidInvoiceAmount)} hint="Paid in selected period" scope="Period" icon={ReceiptText} />
          <MetricCard label="Issued invoice value" value={formatMoney(summary.totalInvoiceAmount)} hint="Issued in selected period" scope="Period" icon={TrendingUp} />
        </div>
      </section>

      <section className="mt-6 grid gap-4 xl:grid-cols-3">
        <Card className="xl:col-span-1">
          <CardHeader>
            <CardTitle>Status Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={288}>
              <PieChart>
                <Pie data={summary.statusBreakdown} dataKey="count" nameKey="status" innerRadius={60} outerRadius={90} paddingAngle={2}>
                  {summary.statusBreakdown.map((entry, index) => (
                    <Cell key={entry.status} fill={chartColors[index % chartColors.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(value, name) => [value, formatStatus(String(name) as never)]} />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="xl:col-span-2">
          <CardHeader>
            <CardTitle>Invoice Trend</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={288}>
              <BarChart data={summary.monthlyInvoiceTrend}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="month" />
                <YAxis tickFormatter={(value) => `${Number(value) / 1000}k`} />
                <Tooltip formatter={(value) => formatMoney(Number(value))} />
                <Bar dataKey="amount" fill="#1e3a8a" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </section>

      <section className="mt-8" aria-labelledby="current-workload-title">
        <div className="mb-4">
          <h2 id="current-workload-title" className="text-lg font-semibold tracking-normal text-slate-950">
            Current Workload
          </h2>
          <p className="mt-1 text-sm text-slate-600">Current workload is not filtered by period.</p>
        </div>

        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4" aria-label="Current workload metrics">
          <MetricCard label="Accounts review" value={String(summary.pendingAccountsReview)} hint="Current pending work" scope="Current" icon={Clock} />
          <MetricCard label="Manager approvals" value={String(summary.pendingManagerApproval)} hint="Current high-value queue" scope="Current" icon={CheckCircle2} />
          <MetricCard label="Rejected in period" value={String(summary.rejectedCount)} hint="Rejected in selected period" scope="Period" icon={FileText} />
          <MetricCard
            label="Avg approval hours"
            value={String(summary.averageApprovalHours)}
            hint="Approved in selected period"
            scope="Period"
            icon={TrendingUp}
          />
        </div>
      </section>

      <section className="mt-6 grid gap-4 xl:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>Aging Buckets</CardTitle>
            <CardDescription>Current pending requests only. Not filtered by period.</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={288}>
              <BarChart data={summary.agingBuckets} layout="vertical" margin={{ left: 16 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                <XAxis type="number" allowDecimals={false} />
                <YAxis dataKey="label" type="category" width={70} />
                <Tooltip />
                <Bar dataKey="count" fill="#ca8a04" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="xl:col-span-2">
          <CardHeader>
            <CardTitle>Recent Activity in Period</CardTitle>
            <CardDescription>Workflow audit events within selected period.</CardDescription>
          </CardHeader>
          <CardContent>
            <AuditTimeline items={summary.recentActivity} />
          </CardContent>
        </Card>
      </section>
    </>
  )
}

function formatDateOnly(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value))
}
