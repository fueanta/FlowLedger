import type { BillingRequestStatus, InvoiceStatus, Role } from '../types'

export function canCreateRequest(role: Role) {
  return role === 'Sales' || role === 'Admin'
}

export function canApproveRequest(role: Role, status: BillingRequestStatus) {
  if (role === 'Admin') {
    return status === 'AccountsReview' || status === 'ManagerApproval'
  }

  if (role === 'Accounts') {
    return status === 'AccountsReview'
  }

  if (role === 'Manager') {
    return status === 'ManagerApproval'
  }

  return false
}

export function canRejectRequest(role: Role, status: BillingRequestStatus) {
  return canApproveRequest(role, status)
}

export function canSubmitRequest(role: Role, status: BillingRequestStatus) {
  return (role === 'Sales' || role === 'Admin') && (status === 'Draft' || status === 'Rejected')
}

export function canUpdateRequest(role: Role, status: BillingRequestStatus) {
  return (role === 'Sales' || role === 'Admin') && (status === 'Draft' || status === 'Rejected')
}

export function canMarkInvoicePaid(role: Role, invoiceStatus: InvoiceStatus) {
  return (role === 'Accounts' || role === 'Admin') && invoiceStatus === 'Issued'
}

export function canCreateClient(role: Role) {
  return role === 'Sales' || role === 'Accounts' || role === 'Admin'
}

export function canEditClient(role: Role) {
  return role === 'Accounts' || role === 'Admin'
}

export function canArchiveClient(role: Role) {
  return role === 'Admin'
}
