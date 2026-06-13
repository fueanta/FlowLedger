import { BarChart3, Building2, FileText, History, Inbox, LayoutDashboard, LogOut, ReceiptText, Settings, UserPlus, Users } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { Button } from '../components/ui/button'
import { Separator } from '../components/ui/separator'
import { useAuth } from '../auth/useAuth'
import { canCreateRequest } from '../lib/permissions'
import { cn } from '../lib/utils'
import { RouteTransition } from './RouteTransition'
import { WorkQueueNavBadge } from './WorkQueueNavBadge'

const navItems = [
  { to: '/app/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/app/work-queue', label: 'My Work', icon: Inbox },
  { to: '/app/requests', label: 'Requests', icon: FileText },
  { to: '/app/invoices', label: 'Invoices', icon: ReceiptText },
  { to: '/app/clients', label: 'Clients', icon: Building2 },
  { to: '/app/enrollment-requests', label: 'Enrollment', icon: UserPlus, adminOnly: true },
  { to: '/app/users', label: 'Users', icon: Users, adminOnly: true },
  { to: '/app/audit-logs', label: 'Audit Logs', icon: History },
  { to: '/app/settings', label: 'Settings', icon: Settings },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const visibleNavItems = navItems.filter((item) => !item.adminOnly || user?.role === 'Admin')

  return (
    <div className="min-h-svh bg-slate-50">
      <aside className="fixed inset-y-0 left-0 hidden w-64 border-r border-slate-200 bg-white print:hidden lg:block">
        <div className="flex h-full flex-col">
          <div className="p-5">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-md bg-slate-950 text-white">
                <BarChart3 className="h-5 w-5" aria-hidden="true" />
              </div>
              <div>
                <p className="text-lg font-bold leading-6 text-slate-950">FlowLedger</p>
                <p className="text-xs font-medium text-slate-500">Billing workflow</p>
              </div>
            </div>
          </div>
          <nav className="flex-1 space-y-1 px-3" aria-label="Main navigation">
            {visibleNavItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 hover:text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-900',
                    isActive && 'bg-blue-950 text-white hover:bg-blue-950 hover:text-white',
                  )
                }
              >
                <item.icon className="h-4 w-4" aria-hidden="true" />
                <span>{item.label}</span>
                {item.to === '/app/work-queue' ? <WorkQueueNavBadge /> : null}
              </NavLink>
            ))}
          </nav>
          <div className="p-4">
            <Separator className="mb-4" />
            <div className="mb-3 rounded-md border border-slate-200 bg-slate-50 p-3">
              <p className="text-sm font-semibold text-slate-950">{user?.fullName}</p>
              <p className="text-xs text-slate-600">{user?.role}</p>
            </div>
            <Button variant="outline" className="w-full justify-start" onClick={logout}>
              <LogOut className="h-4 w-4" aria-hidden="true" />
              Sign out
            </Button>
          </div>
        </div>
      </aside>

      <div className="min-w-0 print:p-0 lg:pl-64">
        <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/95 px-4 py-3 backdrop-blur print:hidden lg:px-8">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="lg:hidden">
              <p className="text-lg font-bold text-slate-950">FlowLedger</p>
              <p className="text-xs text-slate-600">{user?.fullName}</p>
            </div>
            <nav className="flex w-full min-w-0 max-w-full gap-2 overflow-x-auto [contain:paint] lg:hidden" aria-label="Mobile navigation">
              {visibleNavItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    cn(
                      'flex shrink-0 items-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-900',
                      isActive && 'border-blue-950 bg-blue-950 text-white hover:bg-blue-950',
                    )
                  }
                >
                  <item.icon className="h-4 w-4" aria-hidden="true" />
                  <span>{item.label}</span>
                  {item.to === '/app/work-queue' ? <WorkQueueNavBadge /> : null}
                </NavLink>
              ))}
            </nav>
            <div className="hidden items-center gap-3 md:flex md:justify-end lg:ml-auto">
              {user && canCreateRequest(user.role) ? (
                <Button asChild>
                  <NavLink to="/app/requests/new">New Request</NavLink>
                </Button>
              ) : null}
              <Button variant="outline" onClick={logout}>
                <LogOut className="h-4 w-4" aria-hidden="true" />
                Sign out
              </Button>
            </div>
          </div>
        </header>
        <main id="main-content" className="min-w-0 px-4 py-6 print:p-0 lg:px-8">
          <RouteTransition>
            <Outlet />
          </RouteTransition>
        </main>
      </div>
    </div>
  )
}
