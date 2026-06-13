import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { QueryClient } from '@tanstack/react-query'
import { Archive, Building2, Edit, Mail, Phone, Plus, Save, Search, X } from 'lucide-react'
import type { ReactNode } from 'react'
import { useDeferredValue, useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { archiveClient, createClient, getClients, updateClient, type UpdateClientPayload } from '../api/clients'
import { useAuth } from '../auth/useAuth'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingBlock } from '../components/StateViews'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { Dialog } from '../components/ui/dialog'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { clientFormSchema, defaultClientFormValues, type ClientFormValues } from '../features/clientForm'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate } from '../lib/format'
import { canArchiveClient, canCreateClient, canEditClient } from '../lib/permissions'
import type { Client, ClientStatus } from '../types'

const clientStatuses: (ClientStatus | '')[] = ['', 'Active', 'Inactive', 'Archived']

export function ClientsPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search)
  const [status, setStatus] = useState<ClientStatus | ''>('')
  const [sortBy, setSortBy] = useState<'companyName' | 'status' | 'createdAtUtc' | 'updatedAtUtc'>('companyName')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [formTarget, setFormTarget] = useState<Client | 'new' | null>(null)
  const [archiveTarget, setArchiveTarget] = useState<Client | null>(null)

  const clientsQuery = useQuery({
    queryKey: ['clients', { search: deferredSearch, status, sortBy, sortDirection }],
    queryFn: () => getClients({ search: deferredSearch, status, sortBy, sortDirection, pageSize: 100 }),
  })

  const clients = useMemo(() => clientsQuery.data?.items ?? [], [clientsQuery.data?.items])
  const canCreate = user ? canCreateClient(user.role) : false
  const canEdit = user ? canEditClient(user.role) : false
  const canArchive = user ? canArchiveClient(user.role) : false

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

      <Card className="mb-4">
        <CardContent className="grid gap-4 p-4 md:grid-cols-2 xl:grid-cols-5">
          <div className="space-y-2 xl:col-span-2">
            <Label htmlFor="client-search">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-slate-500" aria-hidden="true" />
              <Input
                id="client-search"
                className="pl-9"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Company, email, or contact"
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="client-status">Status</Label>
            <Select id="client-status" value={status} onChange={(event) => setStatus(event.target.value as ClientStatus | '')}>
              {clientStatuses.map((item) => (
                <option key={item || 'all'} value={item}>
                  {item || 'All statuses'}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="client-sort">Sort</Label>
            <Select id="client-sort" value={sortBy} onChange={(event) => setSortBy(event.target.value as typeof sortBy)}>
              <option value="companyName">Company</option>
              <option value="status">Status</option>
              <option value="createdAtUtc">Created date</option>
              <option value="updatedAtUtc">Updated date</option>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="client-sort-direction">Direction</Label>
            <Select id="client-sort-direction" value={sortDirection} onChange={(event) => setSortDirection(event.target.value as typeof sortDirection)}>
              <option value="asc">Ascending</option>
              <option value="desc">Descending</option>
            </Select>
          </div>
        </CardContent>
      </Card>

      {clientsQuery.isLoading ? <LoadingBlock /> : null}
      {clientsQuery.isError ? <ErrorState message="Clients could not be loaded." onRetry={() => void clientsQuery.refetch()} /> : null}
      {!clientsQuery.isLoading && !clientsQuery.isError && clients.length === 0 ? (
        <EmptyState title="No clients found" message="Adjust filters or create a new client if your role allows it." />
      ) : null}

      {clients.length > 0 ? (
        <Card>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Company</TableHead>
                  <TableHead>Contact</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Tax ID</TableHead>
                  <TableHead>Updated</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {clients.map((client) => (
                  <TableRow key={client.id}>
                    <TableCell>
                      <div className="flex items-start gap-3">
                        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-blue-50 text-blue-950">
                          <Building2 className="h-4 w-4" aria-hidden="true" />
                        </div>
                        <div>
                          <p className="font-semibold text-slate-950">{client.companyName}</p>
                          <p className="mt-1 max-w-80 text-sm text-slate-600">{client.address}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="space-y-1 text-sm text-slate-700">
                        <p className="font-medium text-slate-950">{client.contactPerson}</p>
                        <p className="flex items-center gap-2">
                          <Mail className="h-4 w-4 text-slate-500" aria-hidden="true" />
                          {client.email}
                        </p>
                        {client.phone ? (
                          <p className="flex items-center gap-2">
                            <Phone className="h-4 w-4 text-slate-500" aria-hidden="true" />
                            {client.phone}
                          </p>
                        ) : null}
                      </div>
                    </TableCell>
                    <TableCell>
                      <ClientStatusBadge status={client.status} />
                    </TableCell>
                    <TableCell>{client.taxIdentifier || 'None'}</TableCell>
                    <TableCell>{formatDate(client.updatedAtUtc)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-2">
                        {canEdit && client.status !== 'Archived' ? (
                          <Button variant="outline" size="sm" onClick={() => setFormTarget(client)}>
                            <Edit className="h-4 w-4" aria-hidden="true" />
                            Edit
                          </Button>
                        ) : null}
                        {canArchive && client.status !== 'Archived' ? (
                          <Button variant="destructive" size="sm" onClick={() => setArchiveTarget(client)}>
                            <Archive className="h-4 w-4" aria-hidden="true" />
                            Archive
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
