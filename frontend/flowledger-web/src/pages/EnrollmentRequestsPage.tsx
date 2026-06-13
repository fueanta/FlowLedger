import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { CheckCircle2, XCircle } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { approveEnrollmentRequest, getEnrollmentRequests, rejectEnrollmentRequest } from '../api/enrollment'
import { ActionDialog } from '../components/ActionDialog'
import { DataTable } from '../components/data-table/DataTable'
import { DataTablePageSizeSelect } from '../components/data-table/DataTablePageSizeSelect'
import { DataTableSearch } from '../components/data-table/DataTableSearch'
import { DataTableSortableHeader } from '../components/data-table/DataTableSortableHeader'
import { DataTableToolbar } from '../components/data-table/DataTableToolbar'
import { useDataTableState } from '../components/data-table/dataTableState'
import { PageHeader } from '../components/PageHeader'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Select } from '../components/ui/select'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate } from '../lib/format'
import type { EnrollmentRequest, EnrollmentRequestStatus, Role } from '../types'

const statuses: (EnrollmentRequestStatus | '')[] = ['', 'Pending', 'Approved', 'Rejected']
const roles: (Role | '')[] = ['', 'Sales', 'Accounts', 'Manager', 'Admin']

export function EnrollmentRequestsPage() {
  const queryClient = useQueryClient()
  const { state, setPage, setSearch, setSort, setPageSize } = useDataTableState({ sortBy: 'createdAtUtc', sortDirection: 'desc' })
  const [status, setStatus] = useState<EnrollmentRequestStatus | ''>('Pending')
  const [requestedRole, setRequestedRole] = useState<Role | ''>('')
  const [rejectTarget, setRejectTarget] = useState<EnrollmentRequest | null>(null)
  const listParams = { search: state.search, status, requestedRole, sortBy: state.sortBy, sortDirection: state.sortDirection }

  const enrollmentsQuery = useQuery({
    queryKey: ['enrollment-requests', { ...listParams, page: state.page, pageSize: state.pageSize }],
    queryFn: () => getEnrollmentRequests({ ...listParams, page: state.page, pageSize: state.pageSize }),
  })

  useEffect(() => {
    if (enrollmentsQuery.isSuccess) {
      void queryClient.invalidateQueries({ queryKey: ['enrollment-nav-count'] })
    }
  }, [enrollmentsQuery.dataUpdatedAt, enrollmentsQuery.isSuccess, queryClient])

  const approveMutation = useMutation({
    mutationFn: ({ id, role }: { id: string; role: Role }) => approveEnrollmentRequest(id, role),
    onSuccess: async () => {
      toast.success('Enrollment approved.')
      await queryClient.invalidateQueries({ queryKey: ['enrollment-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['enrollment-nav-count'] })
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
      await queryClient.invalidateQueries({ queryKey: ['enrollment-nav-count'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Enrollment could not be rejected.')),
  })

  const columns = useMemo<ColumnDef<EnrollmentRequest>[]>(
    () => [
      {
        accessorKey: 'fullName',
        header: () => <DataTableSortableHeader label="Name" column="fullName" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <span className="font-semibold text-slate-950">{row.original.fullName}</span>,
      },
      {
        accessorKey: 'email',
        header: () => <DataTableSortableHeader label="Email" column="email" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
      },
      {
        accessorKey: 'requestedRole',
        header: () => <DataTableSortableHeader label="Requested Role" column="requestedRole" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
      },
      {
        accessorKey: 'status',
        header: () => <DataTableSortableHeader label="Status" column="status" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <EnrollmentStatusBadge status={row.original.status} />,
      },
      {
        accessorKey: 'createdAtUtc',
        header: () => <DataTableSortableHeader label="Created" column="createdAtUtc" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatDate(row.original.createdAtUtc),
      },
      {
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: ({ row }) =>
          row.original.status === 'Pending' ? (
            <div className="flex justify-end gap-2">
              <Button size="sm" onClick={() => approveMutation.mutate({ id: row.original.id, role: row.original.requestedRole })}>
                <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                Approve
              </Button>
              <Button variant="destructive" size="sm" onClick={() => setRejectTarget(row.original)}>
                <XCircle className="h-4 w-4" aria-hidden="true" />
                Reject
              </Button>
            </div>
          ) : null,
      },
    ],
    [approveMutation, setSort, state.sortBy, state.sortDirection],
  )

  return (
    <>
      <PageHeader title="Enrollment Requests" description="Review registration requests and create users after Admin approval." />

      <DataTableToolbar>
        <DataTableSearch value={state.search} onChange={setSearch} />
        <FilterSelect
          label="Status"
          value={status}
          onChange={(value) => {
            setStatus(value as EnrollmentRequestStatus | '')
            setPage(1)
          }}
          options={statuses.map((item) => ({ value: item, label: item || 'All statuses' }))}
        />
        <FilterSelect
          label="Requested Role"
          value={requestedRole}
          onChange={(value) => {
            setRequestedRole(value as Role | '')
            setPage(1)
          }}
          options={roles.map((item) => ({ value: item, label: item || 'All roles' }))}
        />
        <DataTablePageSizeSelect value={state.pageSize} onChange={setPageSize} />
      </DataTableToolbar>

      <DataTable
        data={enrollmentsQuery.data?.items ?? []}
        columns={columns}
        page={state.page}
        pageSize={state.pageSize}
        totalCount={enrollmentsQuery.data?.totalCount ?? 0}
        loading={enrollmentsQuery.isLoading}
        error={enrollmentsQuery.isError}
        emptyMessage="No enrollment requests found."
        onPageChange={setPage}
      />

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

function FilterSelect({ label, value, options, onChange }: { label: string; value: string; options: { value: string; label: string }[]; onChange: (value: string) => void }) {
  const id = `enrollment-${label.toLowerCase().replace(/\s+/g, '-')}`
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium text-slate-900" htmlFor={id}>
        {label}
      </label>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value)}>
        {options.map((option) => (
          <option key={option.value || 'all'} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
    </div>
  )
}

function EnrollmentStatusBadge({ status }: { status: EnrollmentRequestStatus }) {
  const variant = status === 'Approved' ? 'success' : status === 'Rejected' ? 'destructive' : 'warning'
  return <Badge variant={variant}>{status}</Badge>
}
