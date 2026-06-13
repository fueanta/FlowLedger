import { useQuery } from '@tanstack/react-query'
import { Building2, Mail, Phone } from 'lucide-react'
import { getCustomers } from '../api/customers'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingBlock } from '../components/StateViews'
import { Card, CardContent } from '../components/ui/card'

export function CustomersPage() {
  const customersQuery = useQuery({ queryKey: ['customers'], queryFn: getCustomers })

  return (
    <>
      <PageHeader title="Customers" description="Read-only customer directory used when creating billing requests." />

      {customersQuery.isLoading ? <LoadingBlock /> : null}
      {customersQuery.isError ? <ErrorState message="Customers could not be loaded." onRetry={() => void customersQuery.refetch()} /> : null}
      {!customersQuery.isLoading && !customersQuery.isError && customersQuery.data?.length === 0 ? (
        <EmptyState title="No customers found" message="Seeded customers should appear after database initialization." />
      ) : null}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" aria-label="Customer list">
        {customersQuery.data?.map((customer) => (
          <Card key={customer.id}>
            <CardContent className="space-y-4 p-5">
              <div className="flex items-start gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-md bg-blue-50 text-blue-950">
                  <Building2 className="h-5 w-5" aria-hidden="true" />
                </div>
                <div>
                  <h2 className="font-semibold text-slate-950">{customer.name}</h2>
                  <p className="mt-1 text-sm leading-6 text-slate-600">{customer.billingAddress}</p>
                </div>
              </div>
              <div className="space-y-2 text-sm text-slate-700">
                <p className="flex items-center gap-2">
                  <Mail className="h-4 w-4 text-slate-500" aria-hidden="true" />
                  {customer.contactEmail}
                </p>
                <p className="flex items-center gap-2">
                  <Phone className="h-4 w-4 text-slate-500" aria-hidden="true" />
                  {customer.phone}
                </p>
              </div>
            </CardContent>
          </Card>
        ))}
      </section>
    </>
  )
}
