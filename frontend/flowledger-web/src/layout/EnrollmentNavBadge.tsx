import { useQuery } from '@tanstack/react-query'
import { getEnrollmentRequests } from '../api/enrollment'
import { useAuth } from '../auth/useAuth'
import { Badge } from '../components/ui/badge'

export function EnrollmentNavBadge() {
  const { user } = useAuth()
  const enrollmentCountQuery = useQuery({
    queryKey: ['enrollment-nav-count', user?.id, user?.role],
    queryFn: () => getEnrollmentRequests({ status: 'Pending', pageSize: 1 }),
    enabled: user?.role === 'Admin',
    staleTime: 30_000,
  })
  const count = enrollmentCountQuery.data?.totalCount ?? 0

  if (count <= 0) {
    return null
  }

  return (
    <Badge variant="destructive" className="ml-auto min-w-5 justify-center px-1.5" aria-label={`${count} enrollment requests need review`}>
      {count > 99 ? '99+' : count}
    </Badge>
  )
}
