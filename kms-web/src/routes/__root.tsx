import { createRootRoute, Outlet } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools'
import { Toaster } from 'sonner'
import { useApiHealth } from '../hooks/useApiHealth'

function RootLayout() {
  useApiHealth()
  return (
    <>
      <Outlet />
      <Toaster
        position="bottom-right"
        richColors
        toastOptions={{
          style: { fontFamily: "'Kanit', sans-serif" },
        }}
      />
      <TanStackRouterDevtools />
    </>
  )
}

export const Route = createRootRoute({
  component: RootLayout,
})