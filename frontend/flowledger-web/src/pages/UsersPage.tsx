import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, Search, XCircle } from 'lucide-react'
import { useDeferredValue, useState } from 'react'
import { toast } from 'sonner'
import { activateUser, deactivateUser, getUsers, updateUserRole } from '../api/users'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingBlock } from '../components/StateViews'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { useAuth } from '../auth/useAuth'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate } from '../lib/format'
import type { Role, UserStatus } from '../types'

const roles: (Role | '')[] = ['', 'Sales', 'Accounts', 'Manager', 'Admin']
const statuses: (UserStatus | '')[] = ['', 'Active', 'Inactive']

export function UsersPage() {
  const { user: currentUser } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search)
  const [role, setRole] = useState<Role | ''>('')
  const [status, setStatus] = useState<UserStatus | ''>('')
  const usersQuery = useQuery({
    queryKey: ['users', { search: deferredSearch, role, status }],
    queryFn: () => getUsers({ search: deferredSearch, role, status, pageSize: 100 }),
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

  const rows = usersQuery.data?.items ?? []

  return (
    <>
      <PageHeader title="Users" description="Admin user directory with role and activation controls." />
      <Card className="mb-4">
        <CardContent className="grid gap-4 p-4 md:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="user-search">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-slate-500" aria-hidden="true" />
              <Input id="user-search" className="pl-9" value={search} onChange={(event) => setSearch(event.target.value)} />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="user-role">Role</Label>
            <Select id="user-role" value={role} onChange={(event) => setRole(event.target.value as Role | '')}>
              {roles.map((item) => (
                <option key={item || 'all'} value={item}>
                  {item || 'All roles'}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="user-status">Status</Label>
            <Select id="user-status" value={status} onChange={(event) => setStatus(event.target.value as UserStatus | '')}>
              {statuses.map((item) => (
                <option key={item || 'all'} value={item}>
                  {item || 'All statuses'}
                </option>
              ))}
            </Select>
          </div>
        </CardContent>
      </Card>

      {usersQuery.isLoading ? <LoadingBlock /> : null}
      {usersQuery.isError ? <ErrorState message="Users could not be loaded." onRetry={() => void usersQuery.refetch()} /> : null}
      {!usersQuery.isLoading && !usersQuery.isError && rows.length === 0 ? (
        <EmptyState title="No users found" message="Approved enrollment requests create active users." />
      ) : null}
      {rows.length > 0 ? (
        <Card>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Last Login</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell className="font-semibold text-slate-950">{user.fullName}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>
                      <Select
                        value={user.role}
                        disabled={roleMutation.isPending}
                        onChange={(event) => roleMutation.mutate({ id: user.id, nextRole: event.target.value as Role })}
                        aria-label={`Change role for ${user.fullName}`}
                      >
                        <option value="Sales">Sales</option>
                        <option value="Accounts">Accounts</option>
                        <option value="Manager">Manager</option>
                        <option value="Admin">Admin</option>
                      </Select>
                    </TableCell>
                    <TableCell>
                      <UserStatusBadge status={user.status} />
                    </TableCell>
                    <TableCell>{formatDate(user.lastLoginAtUtc)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-2">
                        {user.status === 'Active' ? (
                          <Button
                            variant="destructive"
                            size="sm"
                            disabled={user.id === currentUser?.id || statusMutation.isPending}
                            onClick={() => statusMutation.mutate({ id: user.id, nextStatus: 'Inactive' })}
                          >
                            <XCircle className="h-4 w-4" aria-hidden="true" />
                            Deactivate
                          </Button>
                        ) : (
                          <Button size="sm" disabled={statusMutation.isPending} onClick={() => statusMutation.mutate({ id: user.id, nextStatus: 'Active' })}>
                            <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                            Activate
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </Card>
      ) : null}
    </>
  )
}

function UserStatusBadge({ status }: { status: UserStatus }) {
  return <Badge variant={status === 'Active' ? 'success' : 'warning'}>{status}</Badge>
}
