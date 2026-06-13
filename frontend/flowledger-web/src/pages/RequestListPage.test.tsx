import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import MockAdapter from 'axios-mock-adapter'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../auth/authContextValue'
import { apiClient } from '../lib/apiClient'
import type { BillingRequestListItem, User } from '../types'
import { RequestListPage } from './RequestListPage'

let mock: MockAdapter | undefined

const accountsUser: User = {
  id: 'accounts-user',
  fullName: 'Accounts Reviewer',
  email: 'accounts@flowledger.local',
  role: 'Accounts',
  status: 'Active',
  isActive: true,
  createdAtUtc: '2026-06-13T00:00:00Z',
  updatedAtUtc: '2026-06-13T00:00:00Z',
  lastLoginAtUtc: null,
}

const authValue: AuthContextValue = {
  user: accountsUser,
  token: 'token',
  isAuthenticated: true,
  login: vi.fn(),
  logout: vi.fn(),
}

const request: BillingRequestListItem = {
  id: 'request-1',
  requestNumber: 'BR-2026-0011',
  title: 'BluePeak onboarding',
  customerName: 'BluePeak Systems',
  status: 'AccountsReview',
  assignedQueue: 'Accounts',
  assignedAtUtc: '2026-06-13T08:00:00Z',
  lastWorkflowActionAtUtc: '2026-06-13T08:00:00Z',
  totalAmount: 57500,
  createdAtUtc: '2026-06-13T07:00:00Z',
  updatedAtUtc: '2026-06-13T08:00:00Z',
}

describe('RequestListPage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('opens the approve comment dialog from the request table', async () => {
    mockListPageRequests()
    renderRequestListPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Approve' }))

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Approve request')).toBeInTheDocument()
    expect(screen.getByLabelText('Comment')).toBeInTheDocument()
  })

  it('opens the reject reason dialog from the request table', async () => {
    mockListPageRequests()
    renderRequestListPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Reject' }))

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Reject request')).toBeInTheDocument()
    expect(screen.getByLabelText('Reason')).toBeInTheDocument()
  })
})

function mockListPageRequests() {
  mock = new MockAdapter(apiClient)
  mock.onGet('/preferences/me').reply(200, { defaultDashboardPeriodMonths: 1, defaultLandingPage: '/app/dashboard', rowsPerPage: 25 })
  mock.onGet('/customers').reply(200, [])
  mock.onGet('/billing-requests').reply(200, { items: [request], page: 1, pageSize: 25, totalCount: 1 })
}

function renderRequestListPage() {
  render(
    <MemoryRouter>
      <AuthContext.Provider value={authValue}>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <RequestListPage />
        </QueryClientProvider>
      </AuthContext.Provider>
    </MemoryRouter>,
  )
}
