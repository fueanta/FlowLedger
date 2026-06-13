import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { CheckCircle2, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import { activateUser, deactivateUser, getUsers, updateUserRole } from '../api/users'
import { useAuth } from '../auth/useAuth'
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
import type { Role, User, UserStatus } from '../types'

const roles: (Role | '')[] = ['', 'Sales', 'Accounts', 'Manager', 'Admin']
const statuses: (UserStatus | '')[] = ['', 'Active', 'Inactive']

export function UsersPage() {
  const { user: currentUser } = useAuth()
  const queryClient = useQueryClient()
  const { state, setPage, setSearch, setSort, setPageSize } = useDataTableState({ sortBy: 'fullName', sortDirection: 'asc' })
  const [role, setRole] = useState<Role | ''>('')
  const [status, setStatus] = useState<UserStatus | ''>('')
  const listParams = { search: state.search, role, status, sortBy: state.sortBy, sortDirection: state.sortDirection }

  const usersQuery = useQuery({
    queryKey: ['users', { ...listParams, page: state.page, pageSize: state.pageSize }],
    queryFn: () => getUsers({ ...listParams, page: state.page, pageSize: state.pageSize }),
  })

  const roleMutation = useMutation({
    mutationFn: ({ id, nextRole }: { id: string; nextRole: Role }) => updateUserRole(id, nextRole),
    onSuccess: async () => {
      toast.success('User role updated.')
      await queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'User role could not be updated.')),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, nextStatus }: { id: string; nextStatus: UserStatus }) => (nextStatus === 'Active' ? activateUser(id) : deactivateUser(id)),
    onSuccess: async () => {
      toast.success('User status updated.')
      await queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'User status could not be updated.')),
  })

  const columns = useMemo<ColumnDef<User>[]>(
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
        accessorKey: 'role',
        header: () => <DataTableSortableHeader label="Role" column="role" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => (
          <Select
            value={row.original.role}
            disabled={roleMutation.isPending}
            onChange={(event) => roleMutation.mutate({ id: row.original.id, nextRole: event.target.value as Role })}
            aria-label={`Change role for ${row.original.fullName}`}
          >
            <option value="Sales">Sales</option>
            <option value="Accounts">Accounts</option>
            <option value="Manager">Manager</option>
            <option value="Admin">Admin</option>
          </Select>
        ),
      },
      {
        accessorKey: 'status',
        header: () => <DataTableSortableHeader label="Status" column="status" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <UserStatusBadge status={row.original.status} />,
      },
      {
        accessorKey: 'lastLoginAtUtc',
        header: 'Last Login',
        cell: ({ row }) => formatDate(row.original.lastLoginAtUtc),
      },
      {
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: ({ row }) => (
          <div className="flex justify-end gap-2">
            {row.original.status === 'Active' ? (
              <Button
                variant="destructive"
                size="sm"
                disabled={row.original.id === currentUser?.id || statusMutation.isPending}
                onClick={() => statusMutation.mutate({ id: row.original.id, nextStatus: 'Inactive' })}
              >
                <XCircle className="h-4 w-4" aria-hidden="true" />
                Deactivate
              </Button>
            ) : (
              <Button size="sm" disabled={statusMutation.isPending} onClick={() => statusMutation.mutate({ id: row.original.id, nextStatus: 'Active' })}>
                <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                Activate
              </Button>
            )}
          </div>
        ),
      },
    ],
    [currentUser?.id, roleMutation, setSort, state.sortBy, state.sortDirection, statusMutation],
  )

  return (
    <>
      <PageHeader title="Users" description="Admin user directory with role and activation controls." />

      <DataTableToolbar>
        <DataTableSearch value={state.search} onChange={setSearch} />
        <FilterSelect
          label="Role"
          value={role}
          onChange={(value) => {
            setRole(value as Role | '')
            setPage(1)
          }}
          options={roles.map((item) => ({ value: item, label: item || 'All roles' }))}
        />
        <FilterSelect
          label="Status"
          value={status}
          onChange={(value) => {
            setStatus(value as UserStatus | '')
            setPage(1)
          }}
          options={statuses.map((item) => ({ value: item, label: item || 'All statuses' }))}
        />
        <DataTablePageSizeSelect value={state.pageSize} onChange={setPageSize} />
      </DataTableToolbar>

      <DataTable
        data={usersQuery.data?.items ?? []}
        columns={columns}
        page={state.page}
        pageSize={state.pageSize}
        totalCount={usersQuery.data?.totalCount ?? 0}
        loading={usersQuery.isLoading}
        error={usersQuery.isError}
        emptyMessage="No users found."
        onPageChange={setPage}
      />
    </>
  )
}

function FilterSelect({ label, value, options, onChange }: { label: string; value: string; options: { value: string; label: string }[]; onChange: (value: string) => void }) {
  const id = `user-${label.toLowerCase().replace(/\s+/g, '-')}`
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

function UserStatusBadge({ status }: { status: UserStatus }) {
  return <Badge variant={status === 'Active' ? 'success' : 'warning'}>{status}</Badge>
}
