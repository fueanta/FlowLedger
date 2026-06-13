import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import MockAdapter from 'axios-mock-adapter'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../auth/authContextValue'
import { AppLayout } from '../layout/AppLayout'
import { apiClient } from '../lib/apiClient'
import type { BillingRequestListItem, User } from '../types'
import { MyWorkQueuePage } from './MyWorkQueuePage'

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

const queuedRequest: BillingRequestListItem = {
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

describe('MyWorkQueuePage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('renders a table-shaped skeleton while the work queue loads', async () => {
    mock = new MockAdapter(apiClient)
    let resolveQueue!: (value: [number, unknown]) => void
    const queueResponse = new Promise<[number, unknown]>((resolve) => {
      resolveQueue = resolve
    })
    mock.onGet('/work-queue').reply(() => queueResponse)

    render(
      <MemoryRouter>
        <AuthContext.Provider value={authValue}>
          <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
            <MyWorkQueuePage />
          </QueryClientProvider>
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: 'My Work Queue' })).toBeInTheDocument()
    expect(screen.getByLabelText('Work queue table loading skeleton')).toBeInTheDocument()
    expect(screen.getByLabelText('Search')).toBeInTheDocument()
    expect(screen.queryByText('No queued work')).not.toBeInTheDocument()

    await act(async () => {
      resolveQueue([200, { items: [], page: 1, pageSize: 50, totalCount: 0 }])
    })
  })

  it('opens comment dialogs for approving and rejecting queued work', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [queuedRequest], page: 1, pageSize: 50, totalCount: 1 })

    renderWorkQueuePage()

    await userEvent.click(await screen.findByRole('button', { name: 'Approve' }))
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('Approve request')).toBeInTheDocument()
    expect(screen.getByLabelText('Comment')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Close dialog' }))

    await userEvent.click(await screen.findByRole('button', { name: 'Reject' }))
    expect(await screen.findByText('Reject request')).toBeInTheDocument()
    expect(screen.getByLabelText('Reason')).toBeInTheDocument()
  })

  it('refreshes the My Work badge after the work queue table loads', async () => {
    mock = new MockAdapter(apiClient)
    let resolveQueue!: (value: [number, unknown]) => void
    let navCountRequests = 0

    mock.onGet('/work-queue').reply((config) => {
      const pageSize = Number(config.params?.pageSize)

      if (pageSize === 1) {
        navCountRequests += 1
        return [200, { items: [], page: 1, pageSize: 1, totalCount: navCountRequests === 1 ? 5 : 2 }]
      }

      return new Promise((resolve) => {
        resolveQueue = resolve
      })
    })

    render(
      <MemoryRouter initialEntries={['/app/work-queue']}>
        <AuthContext.Provider value={authValue}>
          <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
            <Routes>
              <Route path="/app" element={<AppLayout />}>
                <Route path="work-queue" element={<MyWorkQueuePage />} />
              </Route>
            </Routes>
          </QueryClientProvider>
        </AuthContext.Provider>
      </MemoryRouter>,
    )

    expect(await screen.findAllByLabelText('5 work items need attention')).toHaveLength(2)

    await act(async () => {
      resolveQueue([200, { items: [], page: 1, pageSize: 50, totalCount: 2 }])
    })

    expect(await screen.findAllByLabelText('2 work items need attention')).toHaveLength(2)
    expect(navCountRequests).toBeGreaterThanOrEqual(2)
  })
})

function renderWorkQueuePage() {
  render(
    <MemoryRouter>
      <AuthContext.Provider value={authValue}>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <MyWorkQueuePage />
        </QueryClientProvider>
      </AuthContext.Provider>
    </MemoryRouter>,
  )
}
