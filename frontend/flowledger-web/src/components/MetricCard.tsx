import type { LucideIcon } from 'lucide-react'
import { Card, CardContent } from './ui/card'
import { Badge } from './ui/badge'

type MetricCardProps = {
  label: string
  value: string
  hint?: string
  scope?: 'Period' | 'Current'
  icon: LucideIcon
}

export function MetricCard({ label, value, hint, scope, icon: Icon }: MetricCardProps) {
  return (
    <Card>
      <CardContent className="p-5">
        <div className="flex items-start justify-between gap-4">
          <div>
            {scope ? (
              <Badge variant={scope === 'Period' ? 'secondary' : 'outline'} className="mb-3">
                {scope === 'Period' ? 'Period filtered' : 'Current state'}
              </Badge>
            ) : null}
            <p className="text-sm font-medium text-slate-600">{label}</p>
            <p className="mt-2 text-2xl font-bold text-slate-950">{value}</p>
            {hint ? <p className="mt-1 text-xs text-slate-500">{hint}</p> : null}
          </div>
          <div className="flex h-10 w-10 items-center justify-center rounded-md bg-blue-50 text-blue-950">
            <Icon className="h-5 w-5" aria-hidden="true" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
