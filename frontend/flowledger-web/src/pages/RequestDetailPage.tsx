import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, MessageSquare, Pencil, ReceiptText, Send, XCircle } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { addBillingRequestComment, approveBillingRequest, getBillingRequest, rejectBillingRequest, submitBillingRequest } from '../api/billingRequests'
import { markInvoicePaid } from '../api/invoices'
import { ActionDialog } from '../components/ActionDialog'
import { AuditTimeline } from '../components/AuditTimeline'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState } from '../components/StateViews'
import { StatusBadge } from '../components/StatusBadge'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Skeleton } from '../components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { useAuth } from '../auth/useAuth'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDateTime, formatMoney } from '../lib/format'
import { canMarkInvoicePaid } from '../lib/permissions'
import { useState } from 'react'

type DialogMode = 'approve' | 'reject' | 'comment' | null

export function RequestDetailPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [dialogMode, setDialogMode] = useState<DialogMode>(null)
  const requestQuery = useQuery({
    queryKey: ['billing-request', id],
    queryFn: () => getBillingRequest(id ?? ''),
    enabled: Boolean(id),
  })

  const workflowMutation = useMutation({
    mutationFn: async ({ action, value }: { action: Exclude<DialogMode, null> | 'submit' | 'markPaid'; value?: string }) => {
      if (!id) {
        return
      }

      if (action === 'submit') {
        await submitBillingRequest(id)
      } else if (action === 'approve') {
        await approveBillingRequest(id, value)
      } else if (action === 'reject') {
        await rejectBillingRequest(id, value ?? '')
      } else if (action === 'comment') {
        await addBillingRequestComment(id, value ?? '')
      } else if (action === 'markPaid' && requestQuery.data?.invoice) {
        await markInvoicePaid(requestQuery.data.invoice.id)
      }
    },
    onSuccess: async (_, variables) => {
      const message =
        variables.action === 'submit'
          ? 'Request submitted to Accounts.'
          : variables.action === 'approve'
            ? 'Request approved.'
            : variables.action === 'reject'
              ? 'Request rejected.'
              : variables.action === 'markPaid'
                ? 'Invoice marked as paid.'
                : 'Comment added.'
      toast.success(message)
      setDialogMode(null)
      await queryClient.invalidateQueries({ queryKey: ['billing-request', id] })
      await queryClient.invalidateQueries({ queryKey: ['work-queue'] })
      await queryClient.invalidateQueries({ queryKey: ['work-queue-nav-count'] })
      await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Workflow action failed.')),
  })

  if (requestQuery.isLoading) {
    return <RequestDetailSkeleton />
  }

  if (requestQuery.isError || !requestQuery.data) {
    return (
      <>
        <PageHeader title="Billing Request" description="Review request detail and workflow history." />
        <ErrorState message="Billing request could not be loaded." onRetry={() => void requestQuery.refetch()} />
      </>
    )
  }

  const request = requestQuery.data
  const canUpdate = request.availableActions.includes('Update')
  const canSubmit = request.availableActions.includes('Submit')
  const canApprove = request.availableActions.includes('Approve')
  const canReject = request.availableActions.includes('Reject')
  const canComment = request.availableActions.includes('Comment')
  const canPayInvoice = user && request.invoice ? canMarkInvoicePaid(user.role, request.invoice.status) : false

  return (
    <>
      <PageHeader
        title={`${request.requestNumber}: ${request.title}`}
        description={`${request.customer.name} · ${formatMoney(request.totalAmount)}`}
        actions={
          <>
            <StatusBadge status={request.status} />
            {canUpdate ? (
              <Button asChild variant="outline">
                <Link to={`/app/requests/${request.id}/edit`}>
                  <Pencil className="h-4 w-4" aria-hidden="true" />
                  {request.status === 'Rejected' ? 'Edit and Resubmit' : 'Edit Draft'}
                </Link>
              </Button>
            ) : null}
          </>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Request Details</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2">
              <Info label="Client" value={request.customer.name} />
              <Info label="Client Email" value={request.customer.contactEmail} />
              <Info label="Created By" value={`${request.createdBy.fullName} (${request.createdBy.role})`} />
              <Info label="Assigned To" value={request.assignedTo ? `${request.assignedTo.fullName} (${request.assignedTo.role})` : '-'} />
              <Info label="Current Queue" value={request.assignedQueue === 'None' ? 'No active queue' : request.assignedQueue} />
              <Info label="Assigned At" value={formatDateTime(request.assignedAtUtc)} />
              <Info label="Submitted" value={formatDateTime(request.submittedAtUtc)} />
              <Info label="Last Workflow Action" value={formatDateTime(request.lastWorkflowActionAtUtc)} />
              <div className="md:col-span-2">
                <p className="text-sm font-medium text-slate-600">Description</p>
                <p className="mt-1 text-sm leading-6 text-slate-900">{request.description || '-'}</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Line Items</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Description</TableHead>
                      <TableHead>Quantity</TableHead>
                      <TableHead>Unit Price</TableHead>
                      <TableHead>Line Total</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {request.lineItems.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>{item.description}</TableCell>
                        <TableCell>{item.quantity}</TableCell>
                        <TableCell>{formatMoney(item.unitPrice)}</TableCell>
                        <TableCell>{formatMoney(item.lineTotal)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              <div className="mt-4 flex justify-end">
                <dl className="w-full max-w-sm space-y-2 text-sm">
                  <AmountRow label="Subtotal" value={request.subtotalAmount} />
                  <AmountRow label="VAT" value={request.vatAmount} />
                  <AmountRow label="Total" value={request.totalAmount} strong />
                </dl>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Comments</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {request.comments.length === 0 ? (
                <EmptyState title="No comments yet" message="Add context for reviewers or approvers." />
              ) : (
                request.comments.map((comment) => (
                  <div key={comment.id} className="rounded-md border border-slate-200 p-4">
                    <p className="text-sm leading-6 text-slate-900">{comment.body}</p>
                    <p className="mt-2 text-xs text-slate-500">
                      {comment.author.fullName} · {formatDateTime(comment.createdAtUtc)}
                    </p>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {canSubmit ? (
                <Button className="w-full" onClick={() => workflowMutation.mutate({ action: 'submit' })} disabled={workflowMutation.isPending}>
                  <Send className="h-4 w-4" aria-hidden="true" />
                  Submit to Accounts
                </Button>
              ) : null}
              {canApprove ? (
                <Button className="w-full" onClick={() => setDialogMode('approve')}>
                  <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                  {request.status === 'ManagerApproval' ? 'Approve High-Value Request' : 'Approve Request'}
                </Button>
              ) : null}
              {canReject ? (
                <Button variant="destructive" className="w-full" onClick={() => setDialogMode('reject')}>
                  <XCircle className="h-4 w-4" aria-hidden="true" />
                  Reject
                </Button>
              ) : null}
              {canComment ? (
                <Button variant="outline" className="w-full" onClick={() => setDialogMode('comment')}>
                  <MessageSquare className="h-4 w-4" aria-hidden="true" />
                  Add Comment
                </Button>
              ) : null}
              {request.invoice ? (
                <Button asChild variant="outline" className="w-full">
                  <Link to={`/app/invoices/${request.invoice.id}`}>
                    <ReceiptText className="h-4 w-4" aria-hidden="true" />
                    View Invoice
                  </Link>
                </Button>
              ) : null}
              {canPayInvoice ? (
                <Button variant="secondary" className="w-full" onClick={() => workflowMutation.mutate({ action: 'markPaid' })}>
                  Mark Invoice Paid
                </Button>
              ) : null}
              {!canSubmit && !canApprove && !canReject && !canComment && !request.invoice ? (
                <p className="text-sm text-slate-600">No workflow actions are currently available.</p>
              ) : null}
            </CardContent>
          </Card>

          {request.invoice ? (
            <Card>
              <CardHeader>
                <CardTitle>Invoice</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                <Info label="Invoice No" value={request.invoice.invoiceNumber} />
                <div className="flex items-center justify-between gap-3">
                  <span className="text-slate-600">Status</span>
                  <StatusBadge status={request.invoice.status} />
                </div>
                <AmountRow label="Total" value={request.invoice.totalAmount} strong />
              </CardContent>
            </Card>
          ) : null}

          <Card>
            <CardHeader>
              <CardTitle>Audit Timeline</CardTitle>
            </CardHeader>
            <CardContent>
              <AuditTimeline items={request.auditLogs} />
            </CardContent>
          </Card>
        </div>
      </div>

      <ActionDialog
        open={dialogMode === 'approve'}
        title="Approve request"
        description="Approval will either generate an invoice or move high-value work to Manager approval."
        label="Comment"
        confirmLabel="Approve"
        busy={workflowMutation.isPending}
        onClose={() => setDialogMode(null)}
        onConfirm={(value) => workflowMutation.mutate({ action: 'approve', value })}
      />
      <ActionDialog
        open={dialogMode === 'reject'}
        title="Reject request"
        description="The request returns to Sales for revision and resubmission."
        label="Reason"
        confirmLabel="Reject"
        destructive
        required
        busy={workflowMutation.isPending}
        onClose={() => setDialogMode(null)}
        onConfirm={(value) => workflowMutation.mutate({ action: 'reject', value })}
      />
      <ActionDialog
        open={dialogMode === 'comment'}
        title="Add comment"
        description="Add a note to the request history."
        label="Comment"
        confirmLabel="Add Comment"
        required
        busy={workflowMutation.isPending}
        onClose={() => setDialogMode(null)}
        onConfirm={(value) => workflowMutation.mutate({ action: 'comment', value })}
      />
    </>
  )
}

function RequestDetailSkeleton() {
  return (
    <>
      <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div className="w-full max-w-3xl">
          <Skeleton className="h-8 w-full max-w-2xl" />
          <Skeleton className="mt-2 h-5 w-80 max-w-full" />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Skeleton className="h-6 w-28 rounded-full" />
          <Skeleton className="h-10 w-40" />
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1fr_360px]" aria-label="Request detail loading skeleton">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-36" />
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2">
              {Array.from({ length: 8 }).map((_, index) => (
                <InfoSkeleton key={index} />
              ))}
              <div className="md:col-span-2">
                <Skeleton className="h-5 w-28" />
                <Skeleton className="mt-1 h-5 w-full" />
                <Skeleton className="mt-1 h-5 w-2/3" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-24" />
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>
                        <Skeleton className="h-4 w-28" />
                      </TableHead>
                      <TableHead>
                        <Skeleton className="h-4 w-20" />
                      </TableHead>
                      <TableHead>
                        <Skeleton className="h-4 w-24" />
                      </TableHead>
                      <TableHead>
                        <Skeleton className="h-4 w-24" />
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {Array.from({ length: 3 }).map((_, index) => (
                      <TableRow key={index}>
                        <TableCell>
                          <Skeleton className="h-5 w-full max-w-md" />
                        </TableCell>
                        <TableCell>
                          <Skeleton className="h-5 w-16" />
                        </TableCell>
                        <TableCell>
                          <Skeleton className="h-5 w-24" />
                        </TableCell>
                        <TableCell>
                          <Skeleton className="h-5 w-24" />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              <div className="mt-4 flex justify-end">
                <dl className="w-full max-w-sm space-y-2 text-sm">
                  {Array.from({ length: 3 }).map((_, index) => (
                    <SkeletonAmountRow key={index} strong={index === 2} />
                  ))}
                </dl>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-24" />
            </CardHeader>
            <CardContent className="space-y-4">
              {Array.from({ length: 2 }).map((_, index) => (
                <div key={index} className="rounded-md border border-slate-200 p-4">
                  <Skeleton className="h-5 w-full" />
                  <Skeleton className="mt-1 h-5 w-4/5" />
                  <Skeleton className="mt-2 h-4 w-56 max-w-full" />
                </div>
              ))}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-20" />
            </CardHeader>
            <CardContent className="space-y-3">
              {Array.from({ length: 4 }).map((_, index) => (
                <Skeleton key={index} className="h-10 w-full" />
              ))}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-20" />
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <InfoSkeleton />
              <div className="flex items-center justify-between gap-3">
                <Skeleton className="h-5 w-16" />
                <Skeleton className="h-6 w-20 rounded-full" />
              </div>
              <SkeletonAmountRow strong />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {Array.from({ length: 4 }).map((_, index) => (
                  <div key={index} className="relative pl-6">
                    <Skeleton className="absolute left-0 top-1.5 h-2.5 w-2.5 rounded-full" />
                    <div className="space-y-2">
                      <Skeleton className="h-5 w-full" />
                      <Skeleton className="h-4 w-3/4" />
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </>
  )
}

function InfoSkeleton() {
  return (
    <div>
      <Skeleton className="h-5 w-28" />
      <Skeleton className="mt-1 h-5 w-40 max-w-full" />
    </div>
  )
}

function SkeletonAmountRow({ strong = false }: { strong?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <Skeleton className={strong ? 'h-5 w-20' : 'h-5 w-24'} />
      <Skeleton className={strong ? 'h-5 w-28' : 'h-5 w-24'} />
    </div>
  )
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-sm font-medium text-slate-600">{label}</p>
      <p className="mt-1 text-sm text-slate-950">{value}</p>
    </div>
  )
}

function AmountRow({ label, value, strong = false }: { label: string; value: number; strong?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className={strong ? 'font-semibold text-slate-950' : 'text-slate-600'}>{label}</dt>
      <dd className={strong ? 'font-semibold text-slate-950' : 'text-slate-900'}>{formatMoney(value)}</dd>
    </div>
  )
}
