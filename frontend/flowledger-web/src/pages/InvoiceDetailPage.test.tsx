import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import MockAdapter from 'axios-mock-adapter'
import { afterEach, describe, expect, it } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext } from '../auth/authContextValue'
import { apiClient } from '../lib/apiClient'
import type { AuthContextValue } from '../auth/authContextValue'
import { InvoiceDetailPage } from './InvoiceDetailPage'

let mock: MockAdapter | undefined

const accountsAuth: AuthContextValue = {
  user: {
    id: '22222222-2222-2222-2222-222222222222',
    fullName: 'Alex Accounts',
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

describe('InvoiceDetailPage', () => {
  afterEach(() => {
    mock?.restore()
    mock = undefined
  })

  it('shows print, pdf download, and payment actions for issued invoices', async () => {
    mock = new MockAdapter(apiClient)
    mock.onGet('/invoices/11111111-1111-1111-1111-111111111111').reply(200, {
      id: '11111111-1111-1111-1111-111111111111',
      invoiceNumber: 'INV-2026-0001',
      status: 'Issued',
      subtotalAmount: 52000,
      vatPercentage: 15,
      vatAmount: 7800,
      totalAmount: 59800,
      issuedAtUtc: '2026-06-01T00:00:00Z',
      dueDays: 30,
      dueAtUtc: '2026-07-01T00:00:00Z',
      paidAtUtc: null,
      customer: {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'BluePeak Systems',
        contactEmail: 'billing@bluepeak.example',
        billingAddress: 'House 10, Road 4, Dhaka',
      },
      billingRequest: {
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        requestNumber: 'BR-2026-0010',
        title: 'BluePeak subscription billing',
        status: 'InvoiceGenerated',
      },
    })

    render(
      <AuthContext.Provider value={accountsAuth}>
        <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
          <MemoryRouter initialEntries={['/app/invoices/11111111-1111-1111-1111-111111111111']}>
            <Routes>
              <Route path="/app/invoices/:id" element={<InvoiceDetailPage />} />
            </Routes>
          </MemoryRouter>
        </QueryClientProvider>
      </AuthContext.Provider>,
    )

    expect((await screen.findAllByRole('heading', { name: 'INV-2026-0001' })).length).toBeGreaterThan(0)
    expect(screen.getByRole('button', { name: 'Print' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Download PDF' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Mark Paid' })).toBeInTheDocument()
    expect(screen.getByText('VAT rate')).toBeInTheDocument()
    expect(screen.getByText('Due period')).toBeInTheDocument()
  })
})
