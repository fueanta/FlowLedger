import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import MockAdapter from 'axios-mock-adapter'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type AuthContextValue } from '../auth/authContextValue'
import { apiClient } from '../lib/apiClient'
import { AppLayout } from './AppLayout'
import { RouteTransition } from './RouteTransition'

let mock: MockAdapter | undefined

const auth: AuthContextValue = {
  user: {
    id: '22222222-2222-2222-2222-222222222222',
    fullName: 'Amir Accounts',
    email: 'accounts@flowledger.local',
    role: 'Accounts',
    status: 'Active',
    isActive: true,
    createdAtUtc: '2026-01-05T09:00:00Z',
    updatedAtUtc: '2026-06-13T09:00:00Z',
    lastLoginAtUtc: null,
  },
  token: 'token',
  isAuthenticated: true,
  login: async () => undefined,
  logout: () => undefined,
}

const adminAuth: AuthContextValue = {
  ...auth,
  user: auth.user
    ? {
        ...auth.user,
        id: '44444444-4444-4444-4444-444444444444',
        fullName: 'Nadia Admin',
        email: 'admin@flowledger.local',
        role: 'Admin',
      }
    : null,
}

describe('AppLayout work queue badge', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('renders the work queue badge from total count', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 4 })

    renderAppLayout()

    expect(await screen.findAllByLabelText('4 work items need attention')).toHaveLength(2)
  })

  it('keeps the badge visible after My Work is clicked when queue still has work', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 3 })

    renderAppLayout()
    expect(await screen.findAllByLabelText('3 work items need attention')).toHaveLength(2)

    await userEvent.click(screen.getAllByRole('link', { name: /My Work/ })[0])

    expect(await screen.findAllByLabelText('3 work items need attention')).toHaveLength(2)
  })

  it('shows the badge again after a fresh mount when work still exists', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 3 })

    const firstRender = renderAppLayout()
    expect(await screen.findAllByLabelText('3 work items need attention')).toHaveLength(2)
    firstRender.unmount()

    renderAppLayout()

    expect(await screen.findAllByLabelText('3 work items need attention')).toHaveLength(2)
  })

  it('stays hidden when the queue is empty', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 0 })

    renderAppLayout()

    await waitFor(() => expect(mock!.history.get.length).toBeGreaterThan(0))
    expect(screen.queryByLabelText(/work items need attention/)).not.toBeInTheDocument()
  })
})

describe('AppLayout enrollment badge', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('renders the pending enrollment badge for admins', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 0 })
    mock.onGet('/enrollment-requests').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 6 })

    renderAppLayout(adminAuth)

    expect(await screen.findAllByLabelText('6 enrollment requests need review')).toHaveLength(2)
  })

  it('keeps enrollment badge hidden when no requests are pending', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/work-queue').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 0 })
    mock.onGet('/enrollment-requests').reply(200, { items: [], page: 1, pageSize: 1, totalCount: 0 })

    renderAppLayout(adminAuth)

    await waitFor(() => expect(mock!.history.get.some((request) => request.url === '/enrollment-requests')).toBe(true))
    expect(screen.queryByLabelText(/enrollment requests need review/)).not.toBeInTheDocument()
  })
})

describe('RouteTransition', () => {
  it('wraps route content with the transition class', () => {
    render(
      <MemoryRouter initialEntries={['/app/dashboard']}>
        <RouteTransition>
          <div>Dashboard content</div>
        </RouteTransition>
      </MemoryRouter>,
    )

    expect(screen.getByText('Dashboard content').parentElement).toHaveClass('route-transition')
  })
})

function renderAppLayout(authValue = auth) {
  return renderWithProviders(
    <MemoryRouter initialEntries={['/app/dashboard']}>
      <Routes>
        <Route path="/app" element={<AppLayout />}>
          <Route path="dashboard" element={<div>Dashboard content</div>} />
          <Route path="work-queue" element={<div>Work queue content</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
    authValue,
  )
}

function renderWithProviders(children: ReactNode, authValue = auth) {
  return render(
    <AuthContext.Provider value={authValue}>
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>{children}</QueryClientProvider>
    </AuthContext.Provider>,
  )
}
