import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Eye, Search, ThumbsUp, XCircle } from 'lucide-react'
import { useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { approveBillingRequest, getWorkQueue, rejectBillingRequest } from '../api/billingRequests'
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
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDateTime, formatMoney } from '../lib/format'
import { canApproveRequest, canRejectRequest } from '../lib/permissions'
import type { BillingRequestListItem, WorkflowQueue } from '../types'

const queueOptions: (WorkflowQueue | '')[] = ['', 'Sales', 'Accounts', 'Manager']

export function MyWorkQueuePage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search)
  const [queue, setQueue] = useState<WorkflowQueue | ''>('')
  const [rejectTarget, setRejectTarget] = useState<BillingRequestListItem | null>(null)

  const queueQuery = useQuery({
    queryKey: ['work-queue', { search: deferredSearch, queue }],
    queryFn: () =>
      getWorkQueue({
        search: deferredSearch,
        queue: user?.role === 'Admin' ? queue : '',
        sortBy: 'updatedAtUtc',
        sortDirection: 'desc',
        pageSize: 50,
      }),
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => approveBillingRequest(id),
    onSuccess: async () => {
      toast.success('Request approved.')
      await invalidateWorkflowQueries(queryClient)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Request approval failed.')),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => rejectBillingRequest(id, reason),
    onSuccess: async () => {
      toast.success('Request rejected.')
      setRejectTarget(null)
      await invalidateWorkflowQueries(queryClient)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Request rejection failed.')),
  })

  const items = useMemo(() => queueQuery.data?.items ?? [], [queueQuery.data?.items])

  return (
    <>
      <PageHeader title="My Work Queue" description="Requests currently waiting for your role or team." />

      <Card className="mb-4">
        <CardContent className="grid gap-4 p-4 md:grid-cols-3">
          <div className="space-y-2 md:col-span-2">
            <Label htmlFor="work-queue-search">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-slate-500" aria-hidden="true" />
              <Input id="work-queue-search" className="pl-9" value={search} onChange={(event) => setSearch(event.target.value)} />
            </div>
          </div>
          {user?.role === 'Admin' ? (
            <div className="space-y-2">
              <Label htmlFor="work-queue-filter">Queue</Label>
              <Select id="work-queue-filter" value={queue} onChange={(event) => setQueue(event.target.value as WorkflowQueue | '')}>
                {queueOptions.map((item) => (
                  <option key={item || 'all'} value={item}>
                    {item || 'All active queues'}
                  </option>
                ))}
              </Select>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {queueQuery.isLoading ? <LoadingBlock /> : null}
      {queueQuery.isError ? <ErrorState message="Work queue could not be loaded." onRetry={() => void queueQuery.refetch()} /> : null}
      {!queueQuery.isLoading && !queueQuery.isError && items.length === 0 ? (
        <EmptyState title="No queued work" message="There are no active requests waiting for this queue." />
      ) : null}
      {items.length > 0 ? (
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
                  <TableHead>Assigned</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((request) => (
                  <TableRow key={request.id}>
                    <TableCell className="font-semibold text-slate-950">{request.requestNumber}</TableCell>
                    <TableCell>{request.title}</TableCell>
                    <TableCell>{request.customerName}</TableCell>
                    <TableCell>
                      <StatusBadge status={request.status} />
                    </TableCell>
                    <TableCell>{request.assignedQueue}</TableCell>
                    <TableCell>{formatMoney(request.totalAmount)}</TableCell>
                    <TableCell>{formatDateTime(request.assignedAtUtc)}</TableCell>
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
        description={rejectTarget ? `Reject ${rejectTarget.requestNumber}.` : ''}
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

async function invalidateWorkflowQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: ['work-queue'] })
  await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
  await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
  await queryClient.invalidateQueries({ queryKey: ['invoices'] })
}
