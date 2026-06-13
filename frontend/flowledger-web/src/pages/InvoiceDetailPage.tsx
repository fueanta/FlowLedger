import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { downloadInvoicePdf, getInvoice, markInvoicePaid } from '../api/invoices'
import { PageHeader } from '../components/PageHeader'
import { ErrorState } from '../components/StateViews'
import { StatusBadge } from '../components/StatusBadge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { Skeleton } from '../components/ui/skeleton'
import { useAuth } from '../auth/useAuth'
import { getApiErrorMessage } from '../lib/apiClient'
import { formatDate, formatMoney } from '../lib/format'
import { canMarkInvoicePaid } from '../lib/permissions'

export function InvoiceDetailPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const invoiceQuery = useQuery({
    queryKey: ['invoice', id],
    queryFn: () => getInvoice(id ?? ''),
    enabled: Boolean(id),
  })
  const markPaidMutation = useMutation({
    mutationFn: markInvoicePaid,
    onSuccess: async () => {
      toast.success('Invoice marked as paid.')
      await queryClient.invalidateQueries({ queryKey: ['invoice', id] })
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['billing-request'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] })
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Invoice could not be marked as paid.')),
  })
  const downloadPdfMutation = useMutation({
    mutationFn: ({ invoiceId, invoiceNumber }: { invoiceId: string; invoiceNumber: string }) => downloadInvoicePdf(invoiceId, invoiceNumber),
    onError: (error) => toast.error(getApiErrorMessage(error, 'Invoice PDF could not be downloaded.')),
  })

  if (invoiceQuery.isLoading) {
    return <InvoiceDetailSkeleton />
  }

  if (invoiceQuery.isError || !invoiceQuery.data) {
    return (
      <>
        <PageHeader title="Invoice" description="Printable invoice detail." />
        <ErrorState message="Invoice could not be loaded." onRetry={() => void invoiceQuery.refetch()} />
      </>
    )
  }

  const invoice = invoiceQuery.data
  const canPay = user ? canMarkInvoicePaid(user.role, invoice.status) : false

  return (
    <>
      <div className="print:hidden">
        <PageHeader
          title={invoice.invoiceNumber}
          description={`${invoice.customer.name} · ${formatMoney(invoice.totalAmount)}`}
          actions={
            <>
              <Button variant="outline" onClick={() => window.print()}>
                <Printer className="h-4 w-4" aria-hidden="true" />
                Print
              </Button>
              <Button variant="outline" onClick={() => downloadPdfMutation.mutate({ invoiceId: invoice.id, invoiceNumber: invoice.invoiceNumber })} disabled={downloadPdfMutation.isPending}>
                <Download className="h-4 w-4" aria-hidden="true" />
                Download PDF
              </Button>
              {canPay ? (
                <Button onClick={() => markPaidMutation.mutate(invoice.id)} disabled={markPaidMutation.isPending}>
                  Mark Paid
                </Button>
              ) : null}
            </>
          }
        />
      </div>

      <Card className="mx-auto max-w-4xl bg-white print:border-0 print:shadow-none">
        <CardContent className="p-8 print:p-0">
          <div className="flex flex-col gap-6 border-b border-slate-200 pb-6 md:flex-row md:items-start md:justify-between">
            <div>
              <p className="text-sm font-semibold uppercase text-blue-950">FlowLedger</p>
              <h2 className="mt-2 text-3xl font-bold text-slate-950">{invoice.invoiceNumber}</h2>
              <p className="mt-2 text-sm text-slate-600">
                Request{' '}
                <Link className="font-semibold text-blue-950 underline" to={`/app/requests/${invoice.billingRequest.id}`}>
                  {invoice.billingRequest.requestNumber}
                </Link>
              </p>
            </div>
            <StatusBadge status={invoice.status} />
          </div>

          <div className="grid gap-6 border-b border-slate-200 py-6 md:grid-cols-2">
            <div>
              <p className="text-sm font-semibold text-slate-950">Bill To</p>
              <p className="mt-2 text-sm text-slate-900">{invoice.customer.name}</p>
              <p className="text-sm text-slate-600">{invoice.customer.contactEmail}</p>
              <p className="mt-2 text-sm leading-6 text-slate-600">{invoice.customer.billingAddress}</p>
            </div>
            <dl className="space-y-2 text-sm">
              <DetailRow label="Issued" value={formatDate(invoice.issuedAtUtc)} />
              <DetailRow label="VAT rate" value={`${invoice.vatPercentage}%`} />
              <DetailRow label="Due period" value={`${invoice.dueDays} days`} />
              <DetailRow label="Due" value={formatDate(invoice.dueAtUtc)} />
              <DetailRow label="Paid" value={formatDate(invoice.paidAtUtc)} />
              <DetailRow label="Request status" value={invoice.billingRequest.status} />
            </dl>
          </div>

          <div className="py-6">
            <div className="rounded-md border border-slate-200">
              <div className="grid grid-cols-[1fr_140px] border-b border-slate-200 bg-slate-50 px-4 py-3 text-sm font-semibold text-slate-600">
                <span>Description</span>
                <span className="text-right">Amount</span>
              </div>
              <div className="grid grid-cols-[1fr_140px] px-4 py-4 text-sm">
                <span>{invoice.billingRequest.title}</span>
                <span className="text-right">{formatMoney(invoice.subtotalAmount)}</span>
              </div>
            </div>
          </div>

          <dl className="ml-auto max-w-sm space-y-3 text-sm">
            <DetailRow label="Subtotal" value={formatMoney(invoice.subtotalAmount)} />
            <DetailRow label="VAT" value={formatMoney(invoice.vatAmount)} />
            <DetailRow label="Total" value={formatMoney(invoice.totalAmount)} strong />
          </dl>
        </CardContent>
      </Card>
    </>
  )
}

function InvoiceDetailSkeleton() {
  return (
    <>
      <div className="print:hidden">
        <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          <div className="w-full max-w-3xl">
            <Skeleton className="h-8 w-56 max-w-full" />
            <Skeleton className="mt-2 h-5 w-72 max-w-full" />
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Skeleton className="h-10 w-24" />
            <Skeleton className="h-10 w-36" />
            <Skeleton className="h-10 w-28" />
          </div>
        </div>
      </div>

      <Card className="mx-auto max-w-4xl bg-white print:border-0 print:shadow-none" aria-label="Invoice detail loading skeleton">
        <CardContent className="p-8 print:p-0">
          <div className="flex flex-col gap-6 border-b border-slate-200 pb-6 md:flex-row md:items-start md:justify-between">
            <div className="w-full max-w-md">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="mt-2 h-9 w-56 max-w-full" />
              <Skeleton className="mt-2 h-5 w-48 max-w-full" />
            </div>
            <Skeleton className="h-6 w-20 rounded-full" />
          </div>

          <div className="grid gap-6 border-b border-slate-200 py-6 md:grid-cols-2">
            <div>
              <Skeleton className="h-5 w-16" />
              <Skeleton className="mt-2 h-5 w-48 max-w-full" />
              <Skeleton className="mt-1 h-5 w-56 max-w-full" />
              <Skeleton className="mt-2 h-5 w-full" />
              <Skeleton className="mt-1 h-5 w-3/4" />
            </div>
            <dl className="space-y-2 text-sm">
              {Array.from({ length: 6 }).map((_, index) => (
                <SkeletonDetailRow key={index} />
              ))}
            </dl>
          </div>

          <div className="py-6">
            <div className="rounded-md border border-slate-200">
              <div className="grid grid-cols-[1fr_140px] border-b border-slate-200 bg-slate-50 px-4 py-3">
                <Skeleton className="h-5 w-24" />
                <div className="flex justify-end">
                  <Skeleton className="h-5 w-20" />
                </div>
              </div>
              <div className="grid grid-cols-[1fr_140px] px-4 py-4">
                <Skeleton className="h-5 w-full max-w-md" />
                <div className="flex justify-end">
                  <Skeleton className="h-5 w-24" />
                </div>
              </div>
            </div>
          </div>

          <dl className="ml-auto max-w-sm space-y-3 text-sm">
            {Array.from({ length: 3 }).map((_, index) => (
              <SkeletonDetailRow key={index} strong={index === 2} />
            ))}
          </dl>
        </CardContent>
      </Card>
    </>
  )
}

function SkeletonDetailRow({ strong = false }: { strong?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <Skeleton className={strong ? 'h-5 w-20' : 'h-5 w-24'} />
      <Skeleton className={strong ? 'h-5 w-28' : 'h-5 w-32'} />
    </div>
  )
}

function DetailRow({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className={strong ? 'font-semibold text-slate-950' : 'text-slate-600'}>{label}</dt>
      <dd className={strong ? 'font-semibold text-slate-950' : 'text-slate-900'}>{value}</dd>
    </div>
  )
}
