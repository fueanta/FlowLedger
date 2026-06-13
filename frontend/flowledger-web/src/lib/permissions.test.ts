import { describe, expect, it } from 'vitest'
import { canApproveRequest, canCreateRequest, canMarkInvoicePaid, canSubmitRequest } from './permissions'

describe('role permissions', () => {
  it('allows Sales and Admin to create billing requests', () => {
    expect(canCreateRequest('Sales')).toBe(true)
    expect(canCreateRequest('Admin')).toBe(true)
    expect(canCreateRequest('Accounts')).toBe(false)
    expect(canCreateRequest('Manager')).toBe(false)
  })

  it('matches approval responsibility to request status', () => {
    expect(canApproveRequest('Accounts', 'AccountsReview')).toBe(true)
    expect(canApproveRequest('Accounts', 'ManagerApproval')).toBe(false)
    expect(canApproveRequest('Manager', 'ManagerApproval')).toBe(true)
    expect(canApproveRequest('Manager', 'AccountsReview')).toBe(false)
    expect(canApproveRequest('Admin', 'AccountsReview')).toBe(true)
    expect(canApproveRequest('Sales', 'AccountsReview')).toBe(false)
  })

  it('allows draft or rejected submission by Sales/Admin only', () => {
    expect(canSubmitRequest('Sales', 'Draft')).toBe(true)
    expect(canSubmitRequest('Sales', 'Rejected')).toBe(true)
    expect(canSubmitRequest('Sales', 'AccountsReview')).toBe(false)
    expect(canSubmitRequest('Accounts', 'Draft')).toBe(false)
  })

  it('allows Accounts/Admin to mark issued invoices paid', () => {
    expect(canMarkInvoicePaid('Accounts', 'Issued')).toBe(true)
    expect(canMarkInvoicePaid('Admin', 'Issued')).toBe(true)
    expect(canMarkInvoicePaid('Manager', 'Issued')).toBe(false)
    expect(canMarkInvoicePaid('Accounts', 'Paid')).toBe(false)
  })
})
