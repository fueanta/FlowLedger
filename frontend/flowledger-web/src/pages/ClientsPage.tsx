import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { QueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { Archive, Building2, Edit, Mail, Phone, Plus, Save, X } from 'lucide-react'
import type { ReactNode } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { archiveClient, createClient, exportClients, getClients, updateClient, type UpdateClientPayload } from '../api/clients'
import { useAuth } from '../auth/useAuth'
import { DataTable } from '../components/data-table/DataTable'
import { DataTableExportButton } from '../components/data-table/DataTableExportButton'
import { DataTablePageSizeSelect } from '../components/data-table/DataTablePageSizeSelect'
import { DataTableSearch } from '../components/data-table/DataTableSearch'
import { DataTableSortableHeader } from '../components/data-table/DataTableSortableHeader'
import { DataTableToolbar } from '../components/data-table/DataTableToolbar'
import { useDataTableState } from '../components/data-table/dataTableState'
import { PageHeader } from '../components/PageHeader'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Dialog } from '../components/ui/dialog'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { clientFormSchema, defaultClientFormValues, type ClientFormValues } from '../features/clientForm'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate } from '../lib/format'
import { canArchiveClient, canCreateClient, canEditClient } from '../lib/permissions'
import type { Client, ClientStatus } from '../types'

const clientStatuses: (ClientStatus | '')[] = ['', 'Active', 'Inactive', 'Archived']

export function ClientsPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const { state, setPage, setSearch, setSort, setPageSize } = useDataTableState({ sortBy: 'companyName', sortDirection: 'asc' })
  const [status, setStatus] = useState<ClientStatus | ''>('')
  const [formTarget, setFormTarget] = useState<Client | 'new' | null>(null)
  const [archiveTarget, setArchiveTarget] = useState<Client | null>(null)
  const listParams = { search: state.search, status, sortBy: state.sortBy as 'companyName' | 'status' | 'createdAtUtc' | 'updatedAtUtc', sortDirection: state.sortDirection }

  const clientsQuery = useQuery({
    queryKey: ['clients', { ...listParams, page: state.page, pageSize: state.pageSize }],
    queryFn: () => getClients({ ...listParams, page: state.page, pageSize: state.pageSize }),
  })

  const canCreate = user ? canCreateClient(user.role) : false
  const canEdit = user ? canEditClient(user.role) : false
  const canArchive = user ? canArchiveClient(user.role) : false

  const exportMutation = useMutation({
    mutationFn: () => exportClients(listParams),
    onError: (error) => toast.error(getApiErrorMessage(error, 'Client export failed.')),
  })

  const createMutation = useMutation({
    mutationFn: createClient,
    onSuccess: async () => {
      toast.success('Client created.')
      setFormTarget(null)
      await invalidateClients(queryClient)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Client could not be created.')),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: UpdateClientPayload }) => updateClient(id, values),
    onSuccess: async () => {
      toast.success('Client updated.')
      setFormTarget(null)
      await invalidateClients(queryClient)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Client could not be updated.')),
  })

  const archiveMutation = useMutation({
    mutationFn: archiveClient,
    onSuccess: async () => {
      toast.success('Client archived.')
      setArchiveTarget(null)
      await invalidateClients(queryClient)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Client could not be archived.')),
  })

  const columns = useMemo<ColumnDef<Client>[]>(
    () => [
      {
        accessorKey: 'companyName',
        header: () => <DataTableSortableHeader label="Company" column="companyName" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => (
          <div className="flex items-start gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-blue-50 text-blue-950">
              <Building2 className="h-4 w-4" aria-hidden="true" />
            </div>
            <div>
              <p className="font-semibold text-slate-950">{row.original.companyName}</p>
              <p className="mt-1 max-w-80 text-sm text-slate-600">{row.original.address}</p>
            </div>
          </div>
        ),
      },
      {
        id: 'contact',
        header: 'Contact',
        cell: ({ row }) => (
          <div className="space-y-1 text-sm text-slate-700">
            <p className="font-medium text-slate-950">{row.original.contactPerson}</p>
            <p className="flex items-center gap-2">
              <Mail className="h-4 w-4 text-slate-500" aria-hidden="true" />
              {row.original.email}
            </p>
            {row.original.phone ? (
              <p className="flex items-center gap-2">
                <Phone className="h-4 w-4 text-slate-500" aria-hidden="true" />
                {row.original.phone}
              </p>
            ) : null}
          </div>
        ),
      },
      {
        accessorKey: 'status',
        header: () => <DataTableSortableHeader label="Status" column="status" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <ClientStatusBadge status={row.original.status} />,
      },
      { accessorKey: 'taxIdentifier', header: 'Tax ID', cell: ({ row }) => row.original.taxIdentifier || 'None' },
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
            {canEdit && row.original.status !== 'Archived' ? (
              <Button variant="outline" size="sm" onClick={() => setFormTarget(row.original)}>
                <Edit className="h-4 w-4" aria-hidden="true" />
                Edit
              </Button>
            ) : null}
            {canArchive && row.original.status !== 'Archived' ? (
              <Button variant="destructive" size="sm" onClick={() => setArchiveTarget(row.original)}>
                <Archive className="h-4 w-4" aria-hidden="true" />
                Archive
              </Button>
            ) : null}
          </div>
        ),
      },
    ],
    [canArchive, canEdit, setSort, state.sortBy, state.sortDirection],
  )

  return (
    <>
      <PageHeader
        title="Clients"
        description="Manage invoice recipients, billing contacts, tax references, and active billing eligibility."
        actions={
          canCreate ? (
            <Button onClick={() => setFormTarget('new')}>
              <Plus className="h-4 w-4" aria-hidden="true" />
              New Client
            </Button>
          ) : null
        }
      />

      <DataTableToolbar actions={<DataTableExportButton onExport={() => exportMutation.mutate()} disabled={exportMutation.isPending} />}>
        <DataTableSearch value={state.search} onChange={setSearch} label="Search company, email, or contact" />
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-900" htmlFor="client-status">
            Status
          </label>
          <Select
            id="client-status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value as ClientStatus | '')
              setPage(1)
            }}
          >
            {clientStatuses.map((item) => (
              <option key={item || 'all'} value={item}>
                {item || 'All statuses'}
              </option>
            ))}
          </Select>
        </div>
        <DataTablePageSizeSelect value={state.pageSize} onChange={setPageSize} />
      </DataTableToolbar>

      <DataTable
        data={clientsQuery.data?.items ?? []}
        columns={columns}
        page={state.page}
        pageSize={state.pageSize}
        totalCount={clientsQuery.data?.totalCount ?? 0}
        loading={clientsQuery.isLoading}
        error={clientsQuery.isError}
        emptyMessage="No clients found."
        onPageChange={setPage}
      />

      <ClientFormDialog
        target={formTarget}
        busy={createMutation.isPending || updateMutation.isPending}
        canEditStatus={formTarget !== 'new'}
        onClose={() => setFormTarget(null)}
        onSubmit={(values) => {
          if (formTarget === 'new') {
            createMutation.mutate(values)
          } else if (formTarget) {
            updateMutation.mutate({ id: formTarget.id, values })
          }
        }}
      />

      <Dialog
        open={Boolean(archiveTarget)}
        title="Archive client"
        description={archiveTarget ? `${archiveTarget.companyName} will no longer be selectable for new billing requests.` : ''}
        onClose={() => setArchiveTarget(null)}
      >
        <div className="flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={() => setArchiveTarget(null)}>
            <X className="h-4 w-4" aria-hidden="true" />
            Cancel
          </Button>
          <Button variant="destructive" disabled={archiveMutation.isPending} onClick={() => archiveTarget && archiveMutation.mutate(archiveTarget.id)}>
            <Archive className="h-4 w-4" aria-hidden="true" />
            {archiveMutation.isPending ? 'Archiving...' : 'Archive'}
          </Button>
        </div>
      </Dialog>
    </>
  )
}

type ClientFormDialogProps = {
  target: Client | 'new' | null
  busy: boolean
  canEditStatus: boolean
  onClose: () => void
  onSubmit: (values: UpdateClientPayload) => void
}

function ClientFormDialog({ target, busy, canEditStatus, onClose, onSubmit }: ClientFormDialogProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ClientFormValues>({
    resolver: zodResolver(clientFormSchema),
    defaultValues: defaultClientFormValues,
  })

  useEffect(() => {
    if (!target) {
      return
    }

    if (target === 'new') {
      reset(defaultClientFormValues)
      return
    }

    reset({
      companyName: target.companyName,
      contactPerson: target.contactPerson,
      email: target.email,
      phone: target.phone,
      address: target.address,
      taxIdentifier: target.taxIdentifier,
      status: target.status === 'Archived' ? 'Inactive' : target.status,
    })
  }, [reset, target])

  return (
    <Dialog
      open={Boolean(target)}
      title={target === 'new' ? 'New client' : 'Edit client'}
      description="Client status controls whether the company can be used on new billing requests."
      onClose={onClose}
    >
      <form
        className="grid max-h-[70vh] gap-4 overflow-y-auto pr-1"
        noValidate
        onSubmit={handleSubmit((values) => onSubmit(values))}
      >
        <Field label="Company Name" id="client-company-name" error={errors.companyName?.message}>
          <Input id="client-company-name" {...register('companyName')} aria-invalid={Boolean(errors.companyName)} />
        </Field>
        <Field label="Contact Person" id="client-contact-person" error={errors.contactPerson?.message}>
          <Input id="client-contact-person" {...register('contactPerson')} aria-invalid={Boolean(errors.contactPerson)} />
        </Field>
        <Field label="Email" id="client-email" error={errors.email?.message}>
          <Input id="client-email" type="email" {...register('email')} aria-invalid={Boolean(errors.email)} />
        </Field>
        <Field label="Phone" id="client-phone" error={errors.phone?.message}>
          <Input id="client-phone" {...register('phone')} aria-invalid={Boolean(errors.phone)} />
        </Field>
        <Field label="Address" id="client-address" error={errors.address?.message}>
          <Input id="client-address" {...register('address')} aria-invalid={Boolean(errors.address)} />
        </Field>
        <Field label="Tax Identifier" id="client-tax-id" error={errors.taxIdentifier?.message}>
          <Input id="client-tax-id" {...register('taxIdentifier')} aria-invalid={Boolean(errors.taxIdentifier)} />
        </Field>
        {canEditStatus ? (
          <Field label="Status" id="client-edit-status" error={errors.status?.message}>
            <Select id="client-edit-status" {...register('status')} aria-invalid={Boolean(errors.status)}>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
            </Select>
          </Field>
        ) : (
          <input type="hidden" value="Active" {...register('status')} />
        )}
        <div className="flex flex-col gap-3 pt-2 sm:flex-row sm:justify-end">
          <Button type="button" variant="outline" onClick={onClose}>
            <X className="h-4 w-4" aria-hidden="true" />
            Cancel
          </Button>
          <Button type="submit" disabled={busy}>
            <Save className="h-4 w-4" aria-hidden="true" />
            {busy ? 'Saving...' : 'Save Client'}
          </Button>
        </div>
      </form>
    </Dialog>
  )
}

function Field({ label, id, error, children }: { label: string; id: string; error?: string; children: ReactNode }) {
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {error ? (
        <p className="text-sm text-red-700" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  )
}

function ClientStatusBadge({ status }: { status: ClientStatus }) {
  const variant = status === 'Active' ? 'success' : status === 'Inactive' ? 'warning' : 'outline'
  return <Badge variant={variant}>{status}</Badge>
}

async function invalidateClients(queryClient: QueryClient) {
  await queryClient.invalidateQueries({ queryKey: ['clients'] })
  await queryClient.invalidateQueries({ queryKey: ['customers'] })
}
