import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen } from '@testing-library/react'
import MockAdapter from 'axios-mock-adapter'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from '../lib/apiClient'
import { DashboardPage } from './DashboardPage'

let mock: MockAdapter | undefined

vi.mock('recharts', () => {
  const Chart = ({ children }: { children?: ReactNode }) => <div>{children}</div>
  const Primitive = () => <div />

  return {
    ResponsiveContainer: Chart,
    BarChart: Chart,
    PieChart: Chart,
    Pie: Chart,
    Bar: Primitive,
    CartesianGrid: Primitive,
    Cell: Primitive,
    Tooltip: Primitive,
    XAxis: Primitive,
    YAxis: Primitive,
  }
})

const dashboardDescription = 'Track request volume, pending approvals, invoice value, aging, and recent workflow activity.'

const dashboardSummary = {
  period: {
    months: 1,
    startUtc: '2026-05-13T00:00:00Z',
    endUtc: '2026-06-13T00:00:00Z',
  },
  metricScopes: {
    totalRequests: 'Period',
    pendingAccountsReview: 'Current',
  },
  totalRequests: 8,
  pendingAccountsReview: 2,
  pendingManagerApproval: 1,
  approvedThisMonth: 4,
  totalInvoiceAmount: 250000,
  paidInvoiceAmount: 120000,
  rejectedCount: 1,
  averageApprovalHours: 18.5,
  statusBreakdown: [{ status: 'AccountsReview', count: 2 }],
  monthlyInvoiceTrend: [{ month: 'Jun', amount: 250000 }],
  agingBuckets: [{ label: '0-1 days', count: 2 }],
  recentActivity: [],
}

describe('DashboardPage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('distinguishes period activity from current workload', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/dashboard/summary', { params: { periodMonths: 1 } }).reply(200, dashboardSummary)

    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <DashboardPage />
      </QueryClientProvider>,
    )

    const periodHeading = await screen.findByRole('heading', { name: 'Period Activity' })
    const workloadHeading = screen.getByRole('heading', { name: 'Current Workload' })

    expect(workloadHeading.compareDocumentPosition(periodHeading) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(screen.getByText('Current workload is not filtered by period.')).toBeInTheDocument()
    expect(screen.getAllByText('Period filtered').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Current state').length).toBeGreaterThan(0)
  })

  it('keeps the dashboard subtitle stable while summary data loads', async () => {
    mock = new MockAdapter(apiClient)
    let resolveSummary!: (value: [number, unknown]) => void
    const summaryResponse = new Promise<[number, unknown]>((resolve) => {
      resolveSummary = resolve
    })
    mock.onGet('/dashboard/summary', { params: { periodMonths: 1 } }).reply(() => summaryResponse)

    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <DashboardPage />
      </QueryClientProvider>,
    )

    expect(screen.getByText(dashboardDescription)).toBeInTheDocument()
    expect(screen.queryByText('Operational health for billing approvals and invoices.')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Dashboard loading skeleton')).toBeInTheDocument()

    await act(async () => {
      resolveSummary([200, dashboardSummary])
    })

    expect(await screen.findByRole('heading', { name: 'Current Workload' })).toBeInTheDocument()
    expect(screen.getByText(dashboardDescription)).toBeInTheDocument()
  })
})
