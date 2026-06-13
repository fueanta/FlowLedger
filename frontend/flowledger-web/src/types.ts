export type Role = 'Sales' | 'Accounts' | 'Manager' | 'Admin'

export type BillingRequestStatus =
  | 'Draft'
  | 'Submitted'
  | 'AccountsReview'
  | 'ManagerApproval'
  | 'Approved'
  | 'Rejected'
  | 'InvoiceGenerated'
  | 'Paid'
  | 'Cancelled'

export type InvoiceStatus = 'Draft' | 'Issued' | 'Paid' | 'Cancelled'

export type User = {
  id: string
  fullName: string
  email: string
  role: Role
}

export type LoginResponse = {
  accessToken: string
  user: User
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type Customer = {
  id: string
  name: string
  contactEmail: string
  phone: string
  billingAddress: string
}

export type DashboardSummary = {
  totalRequests: number
  pendingAccountsReview: number
  pendingManagerApproval: number
  approvedThisMonth: number
  totalInvoiceAmount: number
  paidInvoiceAmount: number
  rejectedCount: number
  averageApprovalHours: number
  statusBreakdown: { status: BillingRequestStatus; count: number }[]
  monthlyInvoiceTrend: { month: string; amount: number }[]
  agingBuckets: { label: string; count: number }[]
  recentActivity: RecentActivity[]
}

export type RecentActivity = {
  id: string
  billingRequestId: string
  requestNumber: string
  actorName: string
  actionType: string
  message: string
  createdAtUtc: string
}

export type BillingRequestListItem = {
  id: string
  requestNumber: string
  title: string
  customerName: string
  status: BillingRequestStatus
  totalAmount: number
  createdAtUtc: string
  updatedAtUtc: string
}

export type UserSummary = {
  id: string
  fullName: string
  email: string
  role: Role
}

export type BillingRequestLineItem = {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type BillingRequestComment = {
  id: string
  author: UserSummary
  body: string
  createdAtUtc: string
}

export type AuditLog = {
  id: string
  actor: UserSummary
  actionType: string
  message: string
  createdAtUtc: string
}

export type BillingRequestInvoice = {
  id: string
  invoiceNumber: string
  status: InvoiceStatus
  totalAmount: number
}

export type BillingRequestDetail = {
  id: string
  requestNumber: string
  title: string
  description: string
  status: BillingRequestStatus
  customer: Pick<Customer, 'id' | 'name' | 'contactEmail'>
  createdBy: UserSummary
  assignedTo: UserSummary | null
  subtotalAmount: number
  vatAmount: number
  totalAmount: number
  submittedAtUtc: string | null
  approvedAtUtc: string | null
  rejectedAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
  lineItems: BillingRequestLineItem[]
  comments: BillingRequestComment[]
  auditLogs: AuditLog[]
  invoice: BillingRequestInvoice | null
  availableActions: string[]
}

export type InvoiceListItem = {
  id: string
  invoiceNumber: string
  billingRequestNumber: string
  customerName: string
  status: InvoiceStatus
  totalAmount: number
  issuedAtUtc: string
  dueAtUtc: string
  paidAtUtc: string | null
}

export type InvoiceDetail = {
  id: string
  invoiceNumber: string
  status: InvoiceStatus
  subtotalAmount: number
  vatAmount: number
  totalAmount: number
  issuedAtUtc: string
  dueAtUtc: string
  paidAtUtc: string | null
  customer: Pick<Customer, 'id' | 'name' | 'contactEmail' | 'billingAddress'>
  billingRequest: {
    id: string
    requestNumber: string
    title: string
    status: BillingRequestStatus
  }
}
