import { AlertCircle, Inbox, RefreshCcw } from 'lucide-react'
import { Button } from './ui/button'
import { Card, CardContent } from './ui/card'
import { Skeleton } from './ui/skeleton'

export function LoadingBlock() {
  return (
    <div className="space-y-3">
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-28 w-full" />
      <Skeleton className="h-28 w-full" />
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <Card>
      <CardContent className="flex flex-col items-start gap-3 p-6">
        <AlertCircle className="h-6 w-6 text-red-700" aria-hidden="true" />
        <div>
          <p className="font-semibold text-slate-950">Something went wrong</p>
          <p className="mt-1 text-sm text-slate-600">{message}</p>
        </div>
        {onRetry ? (
          <Button variant="outline" onClick={onRetry}>
            <RefreshCcw className="h-4 w-4" aria-hidden="true" />
            Retry
          </Button>
        ) : null}
      </CardContent>
    </Card>
  )
}

export function EmptyState({ title, message }: { title: string; message: string }) {
  return (
    <Card>
      <CardContent className="flex flex-col items-start gap-3 p-6">
        <Inbox className="h-6 w-6 text-slate-500" aria-hidden="true" />
        <div>
          <p className="font-semibold text-slate-950">{title}</p>
          <p className="mt-1 text-sm text-slate-600">{message}</p>
        </div>
      </CardContent>
    </Card>
  )
}
