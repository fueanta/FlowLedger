import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, Search, XCircle } from 'lucide-react'
import { useDeferredValue, useState } from 'react'
import { toast } from 'sonner'
import { approveEnrollmentRequest, getEnrollmentRequests, rejectEnrollmentRequest } from '../api/enrollment'
import { ActionDialog } from '../components/ActionDialog'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingBlock } from '../components/StateViews'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate } from '../lib/format'
import type { EnrollmentRequest, EnrollmentRequestStatus, Role } from '../types'

const statuses: (EnrollmentRequestStatus | '')[] = ['', 'Pending', 'Approved', 'Rejected']
const roles: (Role | '')[] = ['', 'Sales', 'Accounts', 'Manager', 'Admin']

export function EnrollmentRequestsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search)
  const [status, setStatus] = useState<EnrollmentRequestStatus | ''>('Pending')
  const [requestedRole, setRequestedRole] = useState<Role | ''>('')
  const [rejectTarget, setRejectTarget] = useState<EnrollmentRequest | null>(null)

  const enrollmentsQuery = useQuery({
    queryKey: ['enrollment-requests', { search: deferredSearch, status, requestedRole }],
    queryFn: () => getEnrollmentRequests({ search: deferredSearch, status, requestedRole, pageSize: 100 }),
  })

  const approveMutation = useMutation({
    mutationFn: ({ id, role }: { id: string; role: Role }) => approveEnrollmentRequest(id, role),
    onSuccess: async () => {
      toast.success('Enrollment approved.')
      await queryClient.invalidateQueries({ queryKey: ['enrollment-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Enrollment could not be approved.')),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => rejectEnrollmentRequest(id, reason),
    onSuccess: async () => {
      toast.success('Enrollment rejected.')
      setRejectTarget(null)
      await queryClient.invalidateQueries({ queryKey: ['enrollment-requests'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Enrollment could not be rejected.')),
  })

  const rows = enrollmentsQuery.data?.items ?? []

  return (
    <>
      <PageHeader title="Enrollment Requests" description="Review registration requests and create users after Admin approval." />
      <Card className="mb-4">
        <CardContent className="grid gap-4 p-4 md:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="enrollment-search">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-slate-500" aria-hidden="true" />
              <Input id="enrollment-search" className="pl-9" value={search} onChange={(event) => setSearch(event.target.value)} />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="enrollment-status">Status</Label>
            <Select id="enrollment-status" value={status} onChange={(event) => setStatus(event.target.value as EnrollmentRequestStatus | '')}>
              {statuses.map((item) => (
                <option key={item || 'all'} value={item}>
                  {item || 'All statuses'}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="enrollment-role">Requested Role</Label>
            <Select id="enrollment-role" value={requestedRole} onChange={(event) => setRequestedRole(event.target.value as Role | '')}>
              {roles.map((item) => (
                <option key={item || 'all'} value={item}>
                  {item || 'All roles'}
                </option>
              ))}
            </Select>
          </div>
        </CardContent>
      </Card>

      {enrollmentsQuery.isLoading ? <LoadingBlock /> : null}
      {enrollmentsQuery.isError ? <ErrorState message="Enrollment requests could not be loaded." onRetry={() => void enrollmentsQuery.refetch()} /> : null}
      {!enrollmentsQuery.isLoading && !enrollmentsQuery.isError && rows.length === 0 ? (
        <EmptyState title="No enrollment requests found" message="New registration requests appear here for Admin review." />
      ) : null}
      {rows.length > 0 ? (
        <Card>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Requested Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((request) => (
                  <TableRow key={request.id}>
                    <TableCell className="font-semibold text-slate-950">{request.fullName}</TableCell>
                    <TableCell>{request.email}</TableCell>
                    <TableCell>{request.requestedRole}</TableCell>
                    <TableCell>
                      <EnrollmentStatusBadge status={request.status} />
                    </TableCell>
                    <TableCell>{formatDate(request.createdAtUtc)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-2">
                        {request.status === 'Pending' ? (
                          <>
                            <Button size="sm" onClick={() => approveMutation.mutate({ id: request.id, role: request.requestedRole })}>
                              <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                              Approve
                            </Button>
                            <Button variant="destructive" size="sm" onClick={() => setRejectTarget(request)}>
                              <XCircle className="h-4 w-4" aria-hidden="true" />
                              Reject
                            </Button>
                          </>
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
        title="Reject enrollment"
        description={rejectTarget ? `Reject ${rejectTarget.email}.` : ''}
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

function EnrollmentStatusBadge({ status }: { status: EnrollmentRequestStatus }) {
  const variant = status === 'Approved' ? 'success' : status === 'Rejected' ? 'destructive' : 'warning'
  return <Badge variant={variant}>{status}</Badge>
}
