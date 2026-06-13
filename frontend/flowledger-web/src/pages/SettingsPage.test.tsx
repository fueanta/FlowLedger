import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import MockAdapter from 'axios-mock-adapter'
import { afterEach, describe, expect, it } from 'vitest'
import { AuthContext, type AuthContextValue } from '../auth/authContextValue'
import { apiClient } from '../lib/apiClient'
import { SettingsPage } from './SettingsPage'

let mock: MockAdapter | undefined

const adminAuth: AuthContextValue = {
  user: {
    id: '44444444-4444-4444-4444-444444444444',
    fullName: 'Amina Admin',
    email: 'admin@flowledger.local',
    role: 'Admin',
    status: 'Active',
    isActive: true,
    createdAtUtc: '2026-01-05T09:00:00Z',
    updatedAtUtc: '2026-06-14T09:00:00Z',
    lastLoginAtUtc: null,
  },
  token: 'token',
  isAuthenticated: true,
  login: async () => undefined,
  logout: () => undefined,
}

describe('SettingsPage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('renders billing settings loading state inside the billing card instead of above the section', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/settings').reply(() => new Promise(() => undefined))

    render(
      <AuthContext.Provider value={adminAuth}>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <SettingsPage />
        </QueryClientProvider>
      </AuthContext.Provider>,
    )

    const billingSettingsCard = screen.getByTestId('billing-settings-card')

    expect(within(billingSettingsCard).getByLabelText('Loading billing settings')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading rule impact')).toBeInTheDocument()
    expect(screen.queryByText('Save Settings')).not.toBeInTheDocument()
  })
})
