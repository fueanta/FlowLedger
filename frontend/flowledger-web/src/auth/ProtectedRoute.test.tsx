import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type AuthContextValue } from './authContextValue'
import { RoleRoute } from './ProtectedRoute'

const baseAuth: AuthContextValue = {
  user: {
    id: '11111111-1111-1111-1111-111111111111',
    fullName: 'Sarah Sales',
    email: 'sales@flowledger.local',
    role: 'Sales',
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

describe('RoleRoute', () => {
  it('redirects users without the required role', async () => {
    renderWithRoleRoute(baseAuth)

    expect(await screen.findByText('Dashboard')).toBeInTheDocument()
  })

  it('renders children for an allowed role', async () => {
    renderWithRoleRoute({ ...baseAuth, user: { ...baseAuth.user!, role: 'Admin' } })

    expect(await screen.findByText('Admin surface')).toBeInTheDocument()
  })
})

function renderWithRoleRoute(auth: AuthContextValue) {
  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={['/app/users']}>
        <Routes>
          <Route
            path="/app/users"
            element={
              <RoleRoute roles={['Admin']}>
                <div>Admin surface</div>
              </RoleRoute>
            }
          />
          <Route path="/app/dashboard" element={<div>Dashboard</div>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}
