import type { ReactNode } from 'react'

export function DataTableToolbar({ children, actions }: { children: ReactNode; actions?: ReactNode }) {
  return (
    <div className="grid gap-4 p-4 md:grid-cols-[1fr_auto] md:items-end">
      <div className="grid gap-4 md:grid-cols-3">{children}</div>
      {actions ? <div className="flex justify-start md:justify-end">{actions}</div> : null}
    </div>
  )
}
