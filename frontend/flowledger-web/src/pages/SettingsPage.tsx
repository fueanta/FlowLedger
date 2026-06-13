import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Save, Settings2 } from 'lucide-react'
import type { ReactNode } from 'react'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { getSettings, updateSettings } from '../api/settings'
import { useAuth } from '../auth/useAuth'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingBlock } from '../components/StateViews'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { settingsFormSchema, type SettingsFormValues } from '../features/settingsForm'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatMoney } from '../lib/format'

const demoUsers = [
  { role: 'Sales', email: 'sales@flowledger.local', passwordSource: 'SeedUsers__SalesPassword' },
  { role: 'Accounts', email: 'accounts@flowledger.local', passwordSource: 'SeedUsers__AccountsPassword' },
  { role: 'Manager', email: 'manager@flowledger.local', passwordSource: 'SeedUsers__ManagerPassword' },
  { role: 'Admin', email: 'admin@flowledger.local', passwordSource: 'SeedUsers__AdminPassword' },
]

export function SettingsPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const settingsQuery = useQuery({ queryKey: ['settings'], queryFn: getSettings })
  const isAdmin = user?.role === 'Admin'
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SettingsFormValues>({
    resolver: zodResolver(settingsFormSchema),
    defaultValues: {
      vatPercentage: 15,
      managerApprovalThreshold: 100000,
      invoiceDueDays: 30,
    },
  })

  useEffect(() => {
    if (settingsQuery.data) {
      reset(settingsQuery.data)
    }
  }, [reset, settingsQuery.data])

  const updateMutation = useMutation({
    mutationFn: updateSettings,
    onSuccess: async () => {
      toast.success('Settings updated.')
      await queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Settings could not be updated.')),
  })

  return (
    <>
      <PageHeader
        title="Settings"
        description="Configure billing workflow defaults. Non-admin roles can review current settings."
      />

      {settingsQuery.isLoading ? <LoadingBlock /> : null}
      {settingsQuery.isError ? <ErrorState message="Settings could not be loaded." onRetry={() => void settingsQuery.refetch()} /> : null}

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings2 className="h-5 w-5 text-blue-950" aria-hidden="true" />
              Billing Settings
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form className="grid gap-4 md:grid-cols-3" noValidate onSubmit={handleSubmit((values) => updateMutation.mutate(values))}>
              <SettingField label="VAT Percentage" id="vatPercentage" suffix="%" error={errors.vatPercentage?.message}>
                <Input
                  id="vatPercentage"
                  type="number"
                  step="0.01"
                  min="0"
                  max="30"
                  disabled={!isAdmin}
                  {...register('vatPercentage', { valueAsNumber: true })}
                />
              </SettingField>
              <SettingField label="Manager Threshold" id="managerApprovalThreshold" error={errors.managerApprovalThreshold?.message}>
                <Input
                  id="managerApprovalThreshold"
                  type="number"
                  min="1"
                  disabled={!isAdmin}
                  {...register('managerApprovalThreshold', { valueAsNumber: true })}
                />
              </SettingField>
              <SettingField label="Invoice Due Days" id="invoiceDueDays" suffix="days" error={errors.invoiceDueDays?.message}>
                <Input
                  id="invoiceDueDays"
                  type="number"
                  min="1"
                  max="365"
                  disabled={!isAdmin}
                  {...register('invoiceDueDays', { valueAsNumber: true })}
                />
              </SettingField>
              <div className="flex items-center gap-3 md:col-span-3">
                {isAdmin ? (
                  <Button type="submit" disabled={updateMutation.isPending}>
                    <Save className="h-4 w-4" aria-hidden="true" />
                    {updateMutation.isPending ? 'Saving...' : 'Save Settings'}
                  </Button>
                ) : (
                  <Badge variant="outline">Read only</Badge>
                )}
              </div>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Current Rule Impact</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-700">
            <p>
              New billing request totals use the current VAT rate. Existing request totals and existing invoice values stay unchanged.
            </p>
            <p>
              Accounts approvals route to Manager when total amount is above{' '}
              <strong className="text-slate-950">{formatMoney(settingsQuery.data?.managerApprovalThreshold ?? 0)}</strong>.
            </p>
            <p>New invoices use the due-day value at generation time; already issued invoices keep their original due dates.</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Architecture Summary</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-700">
            <p>Frontend is a React SPA using role-aware routes, TanStack Query, Axios, React Hook Form, Zod, Tailwind, and Recharts.</p>
            <p>Backend controllers call application services. Services own workflow permissions, settings, and status transitions. EF Core persists SQL Server data.</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Demo Access</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Role</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Password Source</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {demoUsers.map((demoUser) => (
                    <TableRow key={demoUser.role}>
                      <TableCell className="font-semibold text-slate-950">{demoUser.role}</TableCell>
                      <TableCell>{demoUser.email}</TableCell>
                      <TableCell>
                        <code className="rounded bg-slate-100 px-2 py-1 text-xs text-slate-900">{demoUser.passwordSource}</code>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>
    </>
  )
}

function SettingField({
  label,
  id,
  suffix,
  error,
  children,
}: {
  label: string
  id: string
  suffix?: string
  error?: string
  children: ReactNode
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-3">
        <Label htmlFor={id}>{label}</Label>
        {suffix ? <span className="text-xs font-medium text-slate-500">{suffix}</span> : null}
      </div>
      {children}
      {error ? (
        <p className="text-sm text-red-700" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  )
}
