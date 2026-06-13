import { Badge } from './ui/badge'
import { formatStatus } from '../lib/format'
import type { BillingRequestStatus, InvoiceStatus } from '../types'

type StatusBadgeProps = {
  status: BillingRequestStatus | InvoiceStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const variant =
    status === 'Paid' || status === 'InvoiceGenerated' || status === 'Approved'
      ? 'success'
      : status === 'AccountsReview' || status === 'ManagerApproval' || status === 'Issued'
        ? 'warning'
        : status === 'Rejected' || status === 'Cancelled'
          ? 'destructive'
          : 'secondary'

  return <Badge variant={variant}>{formatStatus(status)}</Badge>
}
