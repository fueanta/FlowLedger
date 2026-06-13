import type { ReactNode } from 'react'
import { useLocation } from 'react-router-dom'

type RouteTransitionProps = {
  children: ReactNode
}

export function RouteTransition({ children }: RouteTransitionProps) {
  const location = useLocation()

  return (
    <div key={location.pathname} className="route-transition min-w-0">
      {children}
    </div>
  )
}
