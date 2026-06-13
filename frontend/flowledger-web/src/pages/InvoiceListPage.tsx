import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { Eye } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { getCustomers } from '../api/customers'
import { exportInvoices, getInvoices, markInvoicePaid } from '../api/invoices'
import { useAuth } from '../auth/useAuth'
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
import { canMarkInvoicePaid } from '../lib/permissions'
import type { InvoiceListItem, InvoiceStatus } from '../types'

const invoiceStatuses: (InvoiceStatus | '')[] = ['', 'Issued', 'Paid', 'Cancelled']

export function InvoiceListPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const { state, setPage, setSearch, setSort, setPageSize } = useDataTableState({ sortBy: 'issuedAtUtc', sortDirection: 'desc' })
  const [status, setStatus] = useState<InvoiceStatus | ''>('')
  const [customerId, setCustomerId] = useState('')
  const listParams = { status, customerId, search: state.search, sortBy: state.sortBy, sortDirection: state.sortDirection }

  const invoicesQuery = useQuery({
    queryKey: ['invoices', { ...listParams, page: state.page, pageSize: state.pageSize }],
    queryFn: () => getInvoices({ ...listParams, page: state.page, pageSize: state.pageSize }),
  })
  const customersQuery = useQuery({ queryKey: ['customers'], queryFn: getCustomers })

  const exportMutation = useMutation({
    mutationFn: () => exportInvoices(listParams),
    onError: (error) => toast.error(getApiErrorMessage(error, 'Invoice export failed.')),
  })

  const markPaidMutation = useMutation({
    mutationFn: markInvoicePaid,
    onSuccess: async () => {
      toast.success('Invoice marked as paid.')
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Invoice could not be marked as paid.')),
  })

  const columns = useMemo<ColumnDef<InvoiceListItem>[]>(
    () => [
      {
        accessorKey: 'invoiceNumber',
        header: () => <DataTableSortableHeader label="Invoice No" column="invoiceNumber" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <span className="font-semibold text-slate-950">{row.original.invoiceNumber}</span>,
      },
      { accessorKey: 'billingRequestNumber', header: 'Request No' },
      {
        accessorKey: 'customerName',
        header: () => <DataTableSortableHeader label="Client" column="clientName" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
      },
      {
        accessorKey: 'status',
        header: () => <DataTableSortableHeader label="Status" column="status" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <StatusBadge status={row.original.status} />,
      },
      {
        accessorKey: 'totalAmount',
        header: () => <DataTableSortableHeader label="Amount" column="amount" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatMoney(row.original.totalAmount),
      },
      {
        accessorKey: 'issuedAtUtc',
        header: () => <DataTableSortableHeader label="Issued" column="issuedAtUtc" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatDate(row.original.issuedAtUtc),
      },
      {
        accessorKey: 'dueAtUtc',
        header: () => <DataTableSortableHeader label="Due" column="dueAtUtc" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatDate(row.original.dueAtUtc),
      },
      {
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: ({ row }) => (
          <div className="flex justify-end gap-2">
            <Button asChild variant="outline" size="sm">
              <Link to={`/app/invoices/${row.original.id}`}>
                <Eye className="h-4 w-4" aria-hidden="true" />
                View
              </Link>
            </Button>
            {user && canMarkInvoicePaid(user.role, row.original.status) ? (
              <Button size="sm" onClick={() => markPaidMutation.mutate(row.original.id)} disabled={markPaidMutation.isPending}>
                Mark Paid
              </Button>
            ) : null}
          </div>
        ),
      },
    ],
    [markPaidMutation, setSort, state.sortBy, state.sortDirection, user],
  )

  return (
    <>
      <PageHeader title="Invoices" description="Review issued and paid invoices generated by approved billing requests." />

      <DataTableToolbar actions={<DataTableExportButton onExport={() => exportMutation.mutate()} disabled={exportMutation.isPending} />}>
        <DataTableSearch value={state.search} onChange={setSearch} />
        <FilterSelect
          label="Status"
          value={status}
          onChange={(value) => {
            setStatus(value as InvoiceStatus | '')
            setPage(1)
          }}
          options={invoiceStatuses.map((item) => ({ value: item, label: item || 'All statuses' }))}
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
        <DataTablePageSizeSelect value={state.pageSize} onChange={setPageSize} />
      </DataTableToolbar>

      <DataTable
        data={invoicesQuery.data?.items ?? []}
        columns={columns}
        page={state.page}
        pageSize={state.pageSize}
        totalCount={invoicesQuery.data?.totalCount ?? 0}
        loading={invoicesQuery.isLoading}
        error={invoicesQuery.isError}
        emptyMessage="No invoices found."
        onPageChange={setPage}
      />
    </>
  )
}

function FilterSelect({ label, value, options, onChange }: { label: string; value: string; options: { value: string; label: string }[]; onChange: (value: string) => void }) {
  const id = `invoice-${label.toLowerCase().replace(/\s+/g, '-')}`
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
