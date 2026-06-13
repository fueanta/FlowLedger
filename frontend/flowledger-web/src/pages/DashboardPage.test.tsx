import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
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

describe('DashboardPage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('distinguishes period activity from current workload', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/dashboard/summary', { params: { periodMonths: 1 } }).reply(200, {
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
    })

    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <DashboardPage />
      </QueryClientProvider>,
    )

    expect(await screen.findByRole('heading', { name: 'Period Activity' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Current Workload' })).toBeInTheDocument()
    expect(screen.getByText('Current workload is not filtered by period.')).toBeInTheDocument()
    expect(screen.getAllByText('Period filtered').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Current state').length).toBeGreaterThan(0)
  })
})
