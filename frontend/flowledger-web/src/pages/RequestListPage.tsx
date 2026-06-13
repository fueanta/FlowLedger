import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { Eye, Plus, ThumbsUp, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { approveBillingRequest, exportBillingRequests, getBillingRequests, rejectBillingRequest } from '../api/billingRequests'
import { getCustomers } from '../api/customers'
import { useAuth } from '../auth/useAuth'
import { ActionDialog } from '../components/ActionDialog'
import { DataTable } from '../components/data-table/DataTable'
import { DataTableExportButton } from '../components/data-table/DataTableExportButton'
import { DataTablePageSizeSelect } from '../components/data-table/DataTablePageSizeSelect'
import { DataTableSearch } from '../components/data-table/DataTableSearch'
import { DataTableSortableHeader } from '../components/data-table/DataTableSortableHeader'
import { DataTableToolbar } from '../components/data-table/DataTableToolbar'
import { useDataTableState } from '../components/data-table/dataTableState'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { Button } from '../components/ui/button'
import { Select } from '../components/ui/select'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate, formatMoney } from '../lib/format'
import { canApproveRequest, canCreateRequest, canRejectRequest } from '../lib/permissions'
import type { BillingRequestListItem, BillingRequestStatus } from '../types'

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
  const { state, setPage, setSearch, setSort, setPageSize } = useDataTableState({ sortBy: 'createdAtUtc', sortDirection: 'desc' })
  const [status, setStatus] = useState<BillingRequestStatus | ''>(defaultStatus(user?.role))
  const [customerId, setCustomerId] = useState('')
  const [assignedToMe, setAssignedToMe] = useState(false)
  const [createdByMe, setCreatedByMe] = useState(user?.role === 'Sales')
  const [rejectTarget, setRejectTarget] = useState<BillingRequestListItem | null>(null)

  const listParams = { status, customerId, search: state.search, assignedToMe, createdByMe, sortBy: state.sortBy, sortDirection: state.sortDirection }
  const requestsQuery = useQuery({
    queryKey: ['billing-requests', { ...listParams, page: state.page, pageSize: state.pageSize }],
    queryFn: () => getBillingRequests({ ...listParams, page: state.page, pageSize: state.pageSize }),
  })
  const customersQuery = useQuery({ queryKey: ['customers'], queryFn: getCustomers })

  const exportMutation = useMutation({
    mutationFn: () => exportBillingRequests(listParams),
    onError: (error) => toast.error(getApiErrorMessage(error, 'Billing request export failed.')),
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

  const columns = useMemo<ColumnDef<BillingRequestListItem>[]>(
    () => [
      {
        accessorKey: 'requestNumber',
        header: () => <DataTableSortableHeader label="Request No" column="requestNumber" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <span className="font-semibold text-slate-950">{row.original.requestNumber}</span>,
      },
      { accessorKey: 'title', header: 'Title' },
      {
        accessorKey: 'customerName',
        header: () => <DataTableSortableHeader label="Client" column="clientName" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
      },
      {
        accessorKey: 'status',
        header: () => <DataTableSortableHeader label="Status" column="status" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <StatusBadge status={row.original.status} />,
      },
      { accessorKey: 'assignedQueue', header: 'Queue', cell: ({ row }) => (row.original.assignedQueue === 'None' ? '-' : row.original.assignedQueue) },
      {
        accessorKey: 'totalAmount',
        header: () => <DataTableSortableHeader label="Amount" column="amount" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatMoney(row.original.totalAmount),
      },
      {
        accessorKey: 'updatedAtUtc',
        header: () => <DataTableSortableHeader label="Updated" column="updatedAtUtc" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatDate(row.original.updatedAtUtc),
      },
      {
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: ({ row }) => (
          <div className="flex justify-end gap-2">
            <Button asChild variant="outline" size="sm">
              <Link to={`/app/requests/${row.original.id}`}>
                <Eye className="h-4 w-4" aria-hidden="true" />
                View
              </Link>
            </Button>
            {user && canApproveRequest(user.role, row.original.status) ? (
              <Button size="sm" onClick={() => approveMutation.mutate(row.original.id)} disabled={approveMutation.isPending}>
                <ThumbsUp className="h-4 w-4" aria-hidden="true" />
                Approve
              </Button>
            ) : null}
            {user && canRejectRequest(user.role, row.original.status) ? (
              <Button variant="destructive" size="sm" onClick={() => setRejectTarget(row.original)}>
                <XCircle className="h-4 w-4" aria-hidden="true" />
                Reject
              </Button>
            ) : null}
          </div>
        ),
      },
    ],
    [approveMutation, setSort, state.sortBy, state.sortDirection, user],
  )

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

      <DataTableToolbar actions={<DataTableExportButton onExport={() => exportMutation.mutate()} disabled={exportMutation.isPending} />}>
        <DataTableSearch value={state.search} onChange={setSearch} />
        <FilterSelect
          label="Status"
          value={status}
          onChange={(value) => {
            setStatus(value as BillingRequestStatus | '')
            setPage(1)
          }}
          options={requestStatuses.map((item) => ({ value: item, label: item || 'All statuses' }))}
        />
        <FilterSelect
          label="Client"
          value={customerId}
          onChange={(value) => {
            setCustomerId(value)
            setPage(1)
          }}
          options={[{ value: '', label: 'All active clients' }, ...(customersQuery.data ?? []).map((customer) => ({ value: customer.id, label: customer.name }))]}
        />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={assignedToMe}
            onChange={(event) => {
              setAssignedToMe(event.target.checked)
              setPage(1)
            }}
          />
          Assigned to me
        </label>
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={createdByMe}
            onChange={(event) => {
              setCreatedByMe(event.target.checked)
              setPage(1)
            }}
          />
          Created by me
        </label>
        <DataTablePageSizeSelect value={state.pageSize} onChange={setPageSize} />
      </DataTableToolbar>

      <DataTable
        data={requestsQuery.data?.items ?? []}
        columns={columns}
        page={state.page}
        pageSize={state.pageSize}
        totalCount={requestsQuery.data?.totalCount ?? 0}
        loading={requestsQuery.isLoading}
        error={requestsQuery.isError}
        emptyMessage="No billing requests found."
        onPageChange={setPage}
      />

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

function FilterSelect({ label, value, options, onChange }: { label: string; value: string; options: { value: string; label: string }[]; onChange: (value: string) => void }) {
  const id = `request-${label.toLowerCase().replace(/\s+/g, '-')}`
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

function defaultStatus(role: string | undefined): BillingRequestStatus | '' {
  if (role === 'Accounts') {
    return 'AccountsReview'
  }

  if (role === 'Manager') {
    return 'ManagerApproval'
  }

  return ''
}

async function invalidateWorkflowQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
  await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
  await queryClient.invalidateQueries({ queryKey: ['invoices'] })
}
