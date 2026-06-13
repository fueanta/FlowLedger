import { useQuery } from '@tanstack/react-query'
import { getWorkQueue } from '../api/billingRequests'
import { Badge } from '../components/ui/badge'
import { useAuth } from '../auth/useAuth'

export function WorkQueueNavBadge() {
  const { user } = useAuth()
  const queueCountQuery = useQuery({
    queryKey: ['work-queue-nav-count', user?.id, user?.role],
    queryFn: () => getWorkQueue({ pageSize: 1 }),
    enabled: Boolean(user),
    staleTime: 30_000,
  })
  const count = queueCountQuery.data?.totalCount ?? 0

  if (count <= 0) {
    return null
  }

  return (
    <Badge variant="destructive" className="ml-auto min-w-5 justify-center px-1.5" aria-label={`${count} work items need attention`}>
      {count > 99 ? '99+' : count}
    </Badge>
  )
}
