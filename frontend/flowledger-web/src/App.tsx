import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Navigate, Outlet, RouterProvider, createBrowserRouter } from 'react-router-dom'
import { Toaster } from 'sonner'
import { LoginPage } from './auth/LoginPage'
import { RegisterPage } from './auth/RegisterPage'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { AuthProvider } from './auth/AuthContext'
import { AppLayout } from './layout/AppLayout'
import { ClientsPage } from './pages/ClientsPage'
import { DashboardPage } from './pages/DashboardPage'
import { EnrollmentRequestsPage } from './pages/EnrollmentRequestsPage'
import { InvoiceDetailPage } from './pages/InvoiceDetailPage'
import { InvoiceListPage } from './pages/InvoiceListPage'
import { MyWorkQueuePage } from './pages/MyWorkQueuePage'
import { RequestDetailPage } from './pages/RequestDetailPage'
import { RequestFormPage } from './pages/RequestFormPage'
import { RequestListPage } from './pages/RequestListPage'
import { SettingsPage } from './pages/SettingsPage'
import { UsersPage } from './pages/UsersPage'

const queryClient = new QueryClient()

const router = createBrowserRouter([
  {
    element: <RootProviders />,
    children: [
      { path: '/', element: <Navigate to="/app/dashboard" replace /> },
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
      {
        path: '/app',
        element: (
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Navigate to="/app/dashboard" replace /> },
          { path: 'dashboard', element: <DashboardPage /> },
          { path: 'work-queue', element: <MyWorkQueuePage /> },
          { path: 'requests', element: <RequestListPage /> },
          { path: 'requests/new', element: <RequestFormPage /> },
          { path: 'requests/:id/edit', element: <RequestFormPage /> },
          { path: 'requests/:id', element: <RequestDetailPage /> },
          { path: 'invoices', element: <InvoiceListPage /> },
          { path: 'invoices/:id', element: <InvoiceDetailPage /> },
          { path: 'clients', element: <ClientsPage /> },
          { path: 'customers', element: <Navigate to="/app/clients" replace /> },
          { path: 'enrollment-requests', element: <EnrollmentRequestsPage /> },
          { path: 'users', element: <UsersPage /> },
          { path: 'settings', element: <SettingsPage /> },
        ],
      },
    ],
  },
])

function RootProviders() {
  return (
    <AuthProvider>
      <Outlet />
      <Toaster richColors position="top-right" />
    </AuthProvider>
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}

export default App
