import { useQuery } from '@tanstack/react-query'
import { CheckCircle2, Clock, FileText, ReceiptText, TrendingUp } from 'lucide-react'
import { useState } from 'react'
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getDashboardSummary } from '../api/dashboard'
import { AuditTimeline } from '../components/AuditTimeline'
import { MetricCard } from '../components/MetricCard'
import { PageHeader } from '../components/PageHeader'
import { ErrorState } from '../components/StateViews'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card'
import { Select } from '../components/ui/select'
import { Skeleton } from '../components/ui/skeleton'
import { formatMoney, formatStatus } from '../lib/format'

const chartColors = ['#1e3a8a', '#ca8a04', '#047857', '#b91c1c', '#475569', '#0f172a']
const periodOptions = [1, 3, 6, 12]
const dashboardDescription = 'Track request volume, pending approvals, invoice value, aging, and recent workflow activity.'

export function DashboardPage() {
  const [periodMonths, setPeriodMonths] = useState(1)
  const summaryQuery = useQuery({
    queryKey: ['dashboard-summary', periodMonths],
    queryFn: () => getDashboardSummary(periodMonths),
  })

  if (summaryQuery.isLoading) {
    return (
      <>
        <PageHeader title="Dashboard" description={dashboardDescription} />
        <DashboardSkeleton />
      </>
    )
  }

  if (summaryQuery.isError || !summaryQuery.data) {
    return (
      <>
        <PageHeader title="Dashboard" description={dashboardDescription} />
        <ErrorState message="Dashboard data could not be loaded." onRetry={() => void summaryQuery.refetch()} />
      </>
    )
  }

  const summary = summaryQuery.data

  return (
    <>
      <PageHeader title="Dashboard" description={dashboardDescription} />

      <section aria-labelledby="current-workload-title">
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

      <section className="mt-8" aria-labelledby="period-activity-title">
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

function DashboardSkeleton() {
  return (
    <div aria-label="Dashboard loading skeleton">
      <section aria-label="Current workload loading">
        <div className="mb-4">
          <Skeleton className="h-6 w-44" />
          <Skeleton className="mt-2 h-5 w-72 max-w-full" />
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4" aria-label="Current workload metrics loading">
          {Array.from({ length: 4 }).map((_, index) => (
            <MetricCardSkeleton key={index} />
          ))}
        </div>
      </section>

      <section className="mt-8" aria-label="Period activity loading">
        <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <Skeleton className="h-6 w-40" />
            <Skeleton className="mt-2 h-5 w-80 max-w-full" />
          </div>
          <div className="flex items-center gap-2">
            <Skeleton className="h-5 w-12" />
            <Skeleton className="h-10 w-36" />
          </div>
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4" aria-label="Period activity metrics loading">
          {Array.from({ length: 4 }).map((_, index) => (
            <MetricCardSkeleton key={index} />
          ))}
        </div>
      </section>

      <section className="mt-6 grid gap-4 xl:grid-cols-3" aria-label="Period charts loading">
        <ChartCardSkeleton className="xl:col-span-1" />
        <ChartCardSkeleton className="xl:col-span-2" />
      </section>

      <section className="mt-6 grid gap-4 xl:grid-cols-3" aria-label="Aging and recent activity loading">
        <ChartCardSkeleton hasDescription />
        <TimelineCardSkeleton />
      </section>
    </div>
  )
}

function MetricCardSkeleton() {
  return (
    <Card>
      <CardContent className="p-5">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0 flex-1">
            <Skeleton className="mb-3 h-6 w-28" />
            <Skeleton className="h-5 w-32" />
            <Skeleton className="mt-2 h-8 w-20" />
            <Skeleton className="mt-1 h-4 w-36" />
          </div>
          <Skeleton className="h-10 w-10 rounded-md" />
        </div>
      </CardContent>
    </Card>
  )
}

function ChartCardSkeleton({ className, hasDescription = false }: { className?: string; hasDescription?: boolean }) {
  return (
    <Card className={className}>
      <CardHeader>
        <Skeleton className="h-6 w-40" />
        {hasDescription ? <Skeleton className="h-5 w-64 max-w-full" /> : null}
      </CardHeader>
      <CardContent>
        <Skeleton className="h-72 w-full" />
      </CardContent>
    </Card>
  )
}

function TimelineCardSkeleton() {
  return (
    <Card className="xl:col-span-2">
      <CardHeader>
        <Skeleton className="h-6 w-48" />
        <Skeleton className="h-5 w-72 max-w-full" />
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <div key={index} className="relative pl-6">
              <Skeleton className="absolute left-0 top-1.5 h-2.5 w-2.5 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-5 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}

function formatDateOnly(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value))
}
