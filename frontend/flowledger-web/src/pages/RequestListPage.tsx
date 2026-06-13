import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Eye, Plus, Search, ThumbsUp, XCircle } from 'lucide-react'
import { useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { approveBillingRequest, getBillingRequests, rejectBillingRequest } from '../api/billingRequests'
import { getCustomers } from '../api/customers'
import { ActionDialog } from '../components/ActionDialog'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingBlock } from '../components/StateViews'
import { StatusBadge } from '../components/StatusBadge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { useAuth } from '../auth/useAuth'
import { formatDate, formatMoney } from '../lib/format'
import { canApproveRequest, canCreateRequest, canRejectRequest } from '../lib/permissions'
import type { BillingRequestListItem, BillingRequestStatus } from '../types'
import { toast } from 'sonner'
import { getApiErrorMessage } from '../lib/apiClient'

const requestStatuses: (BillingRequestStatus | '')[] = [
  '',
  'Draft',
  'AccountsReview',
  'ManagerApproval',
  'Rejected',
  'InvoiceGenerated',
  'Paid',
  'Cancelled',
]

export function RequestListPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search)
  const [status, setStatus] = useState<BillingRequestStatus | ''>(defaultStatus(user?.role))
  const [customerId, setCustomerId] = useState('')
  const [assignedToMe, setAssignedToMe] = useState(false)
  const [createdByMe, setCreatedByMe] = useState(user?.role === 'Sales')
  const [rejectTarget, setRejectTarget] = useState<BillingRequestListItem | null>(null)

  const requestsQuery = useQuery({
    queryKey: ['billing-requests', { status, customerId, search: deferredSearch, assignedToMe, createdByMe }],
    queryFn: () =>
      getBillingRequests({
        status,
        customerId,
        search: deferredSearch,
        assignedToMe,
        createdByMe,
        pageSize: 50,
      }),
  })
  const customersQuery = useQuery({ queryKey: ['customers'], queryFn: getCustomers })

  const approveMutation = useMutation({
    mutationFn: (id: string) => approveBillingRequest(id),
    onSuccess: async () => {
      toast.success('Request approved.')
      await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Request approval failed.')),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => rejectBillingRequest(id, reason),
    onSuccess: async () => {
      toast.success('Request rejected.')
      setRejectTarget(null)
      await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Request rejection failed.')),
  })

  const requests = useMemo(() => requestsQuery.data?.items ?? [], [requestsQuery.data?.items])

  return (
    <>
      <PageHeader
        title="Billing Requests"
        description="Filter, review, approve, reject, or open request details based on current workflow status."
        actions={
          user && canCreateRequest(user.role) ? (
            <Button asChild>
              <Link to="/app/requests/new">
                <Plus className="h-4 w-4" aria-hidden="true" />
                New Request
              </Link>
            </Button>
          ) : null
        }
      />

      <Card className="mb-4">
        <CardContent className="grid gap-4 p-4 md:grid-cols-2 xl:grid-cols-5">
          <div className="space-y-2 xl:col-span-2">
            <Label htmlFor="request-search">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-slate-500" aria-hidden="true" />
              <Input id="request-search" className="pl-9" value={search} onChange={(event) => setSearch(event.target.value)} />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="request-status">Status</Label>
            <Select id="request-status" value={status} onChange={(event) => setStatus(event.target.value as BillingRequestStatus | '')}>
              {requestStatuses.map((item) => (
                <option key={item || 'all'} value={item}>
                  {item || 'All statuses'}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="request-customer">Client</Label>
            <Select id="request-customer" value={customerId} onChange={(event) => setCustomerId(event.target.value)}>
              <option value="">All active clients</option>
              {customersQuery.data?.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name}
                </option>
              ))}
            </Select>
          </div>
          <div className="flex items-end gap-4">
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input type="checkbox" checked={assignedToMe} onChange={(event) => setAssignedToMe(event.target.checked)} />
              Assigned to me
            </label>
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input type="checkbox" checked={createdByMe} onChange={(event) => setCreatedByMe(event.target.checked)} />
              Created by me
            </label>
          </div>
        </CardContent>
      </Card>

      {requestsQuery.isLoading ? <LoadingBlock /> : null}
      {requestsQuery.isError ? <ErrorState message="Billing requests could not be loaded." onRetry={() => void requestsQuery.refetch()} /> : null}
      {!requestsQuery.isLoading && !requestsQuery.isError && requests.length === 0 ? (
        <EmptyState title="No billing requests found" message="Adjust filters or create a new request if your role allows it." />
      ) : null}
      {requests.length > 0 ? (
        <Card>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Request No</TableHead>
                  <TableHead>Title</TableHead>
                  <TableHead>Client</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Queue</TableHead>
                  <TableHead>Amount</TableHead>
                  <TableHead>Updated</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {requests.map((request) => (
                  <TableRow key={request.id}>
                    <TableCell className="font-semibold text-slate-950">{request.requestNumber}</TableCell>
                    <TableCell>{request.title}</TableCell>
                    <TableCell>{request.customerName}</TableCell>
                    <TableCell>
                      <StatusBadge status={request.status} />
                    </TableCell>
                    <TableCell>{request.assignedQueue === 'None' ? '-' : request.assignedQueue}</TableCell>
                    <TableCell>{formatMoney(request.totalAmount)}</TableCell>
                    <TableCell>{formatDate(request.updatedAtUtc)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-2">
                        <Button asChild variant="outline" size="sm">
                          <Link to={`/app/requests/${request.id}`}>
                            <Eye className="h-4 w-4" aria-hidden="true" />
                            View
                          </Link>
                        </Button>
                        {user && canApproveRequest(user.role, request.status) ? (
                          <Button size="sm" onClick={() => approveMutation.mutate(request.id)} disabled={approveMutation.isPending}>
                            <ThumbsUp className="h-4 w-4" aria-hidden="true" />
                            Approve
                          </Button>
                        ) : null}
                        {user && canRejectRequest(user.role, request.status) ? (
                          <Button variant="destructive" size="sm" onClick={() => setRejectTarget(request)}>
                            <XCircle className="h-4 w-4" aria-hidden="true" />
                            Reject
                          </Button>
                        ) : null}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </Card>
      ) : null}

      <ActionDialog
        open={Boolean(rejectTarget)}
        title="Reject request"
        description={rejectTarget ? `Reject ${rejectTarget.requestNumber}. Sales can revise and resubmit it.` : ''}
        label="Reason"
        confirmLabel="Reject"
        destructive
        required
        busy={rejectMutation.isPending}
        onClose={() => setRejectTarget(null)}
        onConfirm={(reason) => {
          if (rejectTarget) {
            rejectMutation.mutate({ id: rejectTarget.id, reason })
          }
        }}
      />
    </>
  )
}

function defaultStatus(role: string | undefined): BillingRequestStatus | '' {
  if (role === 'Accounts') {
    return 'AccountsReview'
  }

  if (role === 'Manager') {
    return 'ManagerApproval'
  }

  return ''
}
