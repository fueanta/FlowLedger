import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen } from '@testing-library/react'
import MockAdapter from 'axios-mock-adapter'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../auth/authContextValue'
import { apiClient } from '../lib/apiClient'
import type { BillingRequestDetail, User } from '../types'
import { RequestDetailPage } from './RequestDetailPage'

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

const requestDetail: BillingRequestDetail = {
  id: '22222222-2222-2222-2222-222222222222',
  requestNumber: 'BR-2026-0011',
  title: 'BluePeak onboarding',
  description: 'Initial billing request for BluePeak Systems.',
  status: 'AccountsReview',
  assignedQueue: 'Accounts',
  assignedAtUtc: '2026-06-13T08:00:00Z',
  submittedAtUtc: '2026-06-13T07:30:00Z',
  approvedAtUtc: null,
  rejectedAtUtc: null,
  lastWorkflowActionAtUtc: '2026-06-13T08:00:00Z',
  subtotalAmount: 50000,
  vatAmount: 7500,
  totalAmount: 57500,
  createdAtUtc: '2026-06-13T07:00:00Z',
  updatedAtUtc: '2026-06-13T08:00:00Z',
  customer: {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    name: 'BluePeak Systems',
    contactEmail: 'billing@bluepeak.example',
  },
  createdBy: {
    id: 'sales-user',
    fullName: 'Sam Sales',
    email: 'sales@flowledger.local',
    role: 'Sales',
  },
  assignedTo: accountsUser,
  lineItems: [
    {
      id: 'line-1',
      description: 'Subscription setup',
      quantity: 1,
      unitPrice: 50000,
      lineTotal: 50000,
    },
  ],
  comments: [],
  auditLogs: [],
  invoice: null,
  availableActions: [],
}

describe('RequestDetailPage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('renders a request-detail skeleton while billing request data loads', async () => {
    mock = new MockAdapter(apiClient)
    let resolveRequest!: (value: [number, unknown]) => void
    const requestResponse = new Promise<[number, unknown]>((resolve) => {
      resolveRequest = resolve
    })
    mock.onGet('/billing-requests/22222222-2222-2222-2222-222222222222').reply(() => requestResponse)

    render(
      <MemoryRouter initialEntries={['/app/requests/22222222-2222-2222-2222-222222222222']}>
        <AuthContext.Provider value={authValue}>
          <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
            <Routes>
              <Route path="/app/requests/:id" element={<RequestDetailPage />} />
            </Routes>
          </QueryClientProvider>
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    expect(screen.getByLabelText('Request detail loading skeleton')).toBeInTheDocument()
    expect(screen.queryByText('Loading request detail.')).not.toBeInTheDocument()

    await act(async () => {
      resolveRequest([200, requestDetail])
    })
  })

  it('shows an inaccessible state for forbidden billing request detail', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/billing-requests/22222222-2222-2222-2222-222222222222').reply(403, { message: 'You do not have access to this billing request.' })

    renderRequestDetailPage()

    expect(await screen.findByText('Request not accessible')).toBeInTheDocument()
    expect(screen.getByText('You do not have access to this billing request.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument()
  })
})

function renderRequestDetailPage() {
  render(
    <MemoryRouter initialEntries={['/app/requests/22222222-2222-2222-2222-222222222222']}>
      <AuthContext.Provider value={authValue}>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <Routes>
            <Route path="/app/requests/:id" element={<RequestDetailPage />} />
          </Routes>
        </QueryClientProvider>
      </AuthContext.Provider>
    </MemoryRouter>,
  )
}
