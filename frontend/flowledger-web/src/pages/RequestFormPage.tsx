import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Save, Send, Trash2 } from 'lucide-react'
import { useEffect } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { createBillingRequest, getBillingRequest, submitBillingRequest, updateBillingRequest } from '../api/billingRequests'
import { getCustomers } from '../api/customers'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingBlock } from '../components/StateViews'
import { Button } from '../components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { Select } from '../components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'
import { Textarea } from '../components/ui/textarea'
import { useAuth } from '../auth/useAuth'
import { billingRequestFormSchema, calculateRequestTotals, type BillingRequestFormValues } from '../features/billingRequestForm'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatMoney } from '../lib/format'
import { canCreateRequest } from '../lib/permissions'

const defaultValues: BillingRequestFormValues = {
  customerId: '',
  title: '',
  description: '',
  lineItems: [{ description: '', quantity: 1, unitPrice: 1 }],
}

export function RequestFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const { user } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const customersQuery = useQuery({ queryKey: ['customers'], queryFn: getCustomers })
  const detailQuery = useQuery({
    queryKey: ['billing-request', id],
    queryFn: () => getBillingRequest(id ?? ''),
    enabled: isEdit,
  })
  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BillingRequestFormValues>({
    resolver: zodResolver(billingRequestFormSchema),
    defaultValues,
  })
  const { fields, append, remove } = useFieldArray({ control, name: 'lineItems' })
  const watchedLineItems = useWatch({ control, name: 'lineItems' }) ?? []
  const totals = calculateRequestTotals(watchedLineItems)

  useEffect(() => {
    if (detailQuery.data) {
      reset({
        customerId: detailQuery.data.customer.id,
        title: detailQuery.data.title,
        description: detailQuery.data.description,
        lineItems: detailQuery.data.lineItems.map((item) => ({
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
        })),
      })
    }
  }, [detailQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: async ({ values, submitAfterSave }: { values: BillingRequestFormValues; submitAfterSave: boolean }) => {
      const payload = toPayload(values)
      const requestId = id ?? (await createBillingRequest(payload))

      if (id) {
        await updateBillingRequest(id, payload)
      }

      if (submitAfterSave) {
        await submitBillingRequest(requestId)
      }

      return requestId
    },
    onSuccess: async (requestId, variables) => {
      toast.success(variables.submitAfterSave ? 'Request submitted to Accounts.' : 'Request saved as draft.')
      await queryClient.invalidateQueries({ queryKey: ['billing-requests'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
      navigate(`/app/requests/${requestId}`)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Billing request could not be saved.')),
  })

  if (!user || !canCreateRequest(user.role)) {
    return (
      <>
        <PageHeader title="New Billing Request" description="Sales and Admin users can create billing requests." />
        <ErrorState message="Your role cannot create or edit billing requests." />
      </>
    )
  }

  if (isEdit && detailQuery.isLoading) {
    return (
      <>
        <PageHeader title="Edit Billing Request" description="Revise a draft or rejected request before resubmitting." />
        <LoadingBlock />
      </>
    )
  }

  if (isEdit && detailQuery.isError) {
    return (
      <>
        <PageHeader title="Edit Billing Request" description="Revise a draft or rejected request before resubmitting." />
        <ErrorState message="Billing request could not be loaded for editing." onRetry={() => void detailQuery.refetch()} />
      </>
    )
  }

  return (
    <>
      <PageHeader
        title={isEdit ? 'Edit Billing Request' : 'New Billing Request'}
        description="Add customer, description, and billable line items. Totals include 15% VAT."
      />

      <form className="grid gap-6 xl:grid-cols-[1fr_320px]" noValidate>
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Request Details</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="customerId">Customer</Label>
                <Select id="customerId" {...register('customerId')} aria-invalid={Boolean(errors.customerId)}>
                  <option value="">Select customer</option>
                  {customersQuery.data?.map((customer) => (
                    <option key={customer.id} value={customer.id}>
                      {customer.name}
                    </option>
                  ))}
                </Select>
                {errors.customerId ? <p className="text-sm text-red-700">{errors.customerId.message}</p> : null}
              </div>
              <div className="space-y-2">
                <Label htmlFor="title">Title</Label>
                <Input id="title" {...register('title')} aria-invalid={Boolean(errors.title)} />
                {errors.title ? <p className="text-sm text-red-700">{errors.title.message}</p> : null}
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="description">Description</Label>
                <Textarea id="description" {...register('description')} aria-invalid={Boolean(errors.description)} />
                {errors.description ? <p className="text-sm text-red-700">{errors.description.message}</p> : null}
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
                      <TableHead className="w-28">Quantity</TableHead>
                      <TableHead className="w-40">Unit Price</TableHead>
                      <TableHead className="w-36">Line Total</TableHead>
                      <TableHead className="w-16" />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {fields.map((field, index) => (
                      <TableRow key={field.id}>
                        <TableCell>
                          <Input
                            aria-label={`Line item ${index + 1} description`}
                            {...register(`lineItems.${index}.description`)}
                            aria-invalid={Boolean(errors.lineItems?.[index]?.description)}
                          />
                          {errors.lineItems?.[index]?.description ? (
                            <p className="mt-1 text-xs text-red-700">{errors.lineItems[index]?.description?.message}</p>
                          ) : null}
                        </TableCell>
                        <TableCell>
                          <Input
                            type="number"
                            min="1"
                            {...register(`lineItems.${index}.quantity`, { valueAsNumber: true })}
                            aria-label={`Line item ${index + 1} quantity`}
                          />
                        </TableCell>
                        <TableCell>
                          <Input
                            type="number"
                            min="1"
                            step="0.01"
                            {...register(`lineItems.${index}.unitPrice`, { valueAsNumber: true })}
                            aria-label={`Line item ${index + 1} unit price`}
                          />
                        </TableCell>
                        <TableCell>{formatMoney(Number(watchedLineItems[index]?.quantity || 0) * Number(watchedLineItems[index]?.unitPrice || 0))}</TableCell>
                        <TableCell>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            onClick={() => remove(index)}
                            aria-label={`Remove line item ${index + 1}`}
                            disabled={fields.length === 1}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              {errors.lineItems?.root?.message ? <p className="mt-2 text-sm text-red-700">{errors.lineItems.root.message}</p> : null}
              <Button type="button" variant="outline" className="mt-4" onClick={() => append({ description: '', quantity: 1, unitPrice: 1 })}>
                <Plus className="h-4 w-4" aria-hidden="true" />
                Add line item
              </Button>
            </CardContent>
          </Card>
        </div>

        <Card className="h-fit">
          <CardHeader>
            <CardTitle>Totals</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-3 text-sm">
              <div className="flex justify-between gap-4">
                <span className="text-slate-600">Subtotal</span>
                <strong>{formatMoney(totals.subtotal)}</strong>
              </div>
              <div className="flex justify-between gap-4">
                <span className="text-slate-600">VAT 15%</span>
                <strong>{formatMoney(totals.vat)}</strong>
              </div>
              <div className="flex justify-between gap-4 border-t border-slate-200 pt-3 text-base">
                <span className="font-semibold text-slate-950">Total</span>
                <strong>{formatMoney(totals.total)}</strong>
              </div>
            </div>
            <div className="space-y-2">
              <Button
                type="button"
                className="w-full"
                disabled={saveMutation.isPending}
                onClick={handleSubmit((values) => saveMutation.mutate({ values, submitAfterSave: false }))}
              >
                <Save className="h-4 w-4" aria-hidden="true" />
                {saveMutation.isPending ? 'Saving...' : 'Save Draft'}
              </Button>
              <Button
                type="button"
                variant="secondary"
                className="w-full"
                disabled={saveMutation.isPending}
                onClick={handleSubmit((values) => saveMutation.mutate({ values, submitAfterSave: true }))}
              >
                <Send className="h-4 w-4" aria-hidden="true" />
                {isEdit ? 'Save and Resubmit' : 'Save and Submit'}
              </Button>
            </div>
          </CardContent>
        </Card>
      </form>
    </>
  )
}

function toPayload(values: BillingRequestFormValues) {
  return {
    customerId: values.customerId,
    title: values.title.trim(),
    description: values.description.trim(),
    lineItems: values.lineItems.map((item) => ({
      description: item.description.trim(),
      quantity: Number(item.quantity),
      unitPrice: Number(item.unitPrice),
    })),
  }
}
