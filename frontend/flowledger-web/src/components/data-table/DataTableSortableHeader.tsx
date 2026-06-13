import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { Button } from '../ui/button'
import type { SortDirection } from './dataTableTypes'

export function DataTableSortableHeader({
  label,
  column,
  sortBy,
  sortDirection,
  onSort,
}: {
  label: string
  column: string
  sortBy: string
  sortDirection: SortDirection
  onSort: (column: string, direction: SortDirection) => void
}) {
  const active = sortBy === column
  const nextDirection: SortDirection = active && sortDirection === 'asc' ? 'desc' : 'asc'
  const Icon = !active ? ChevronsUpDown : sortDirection === 'asc' ? ArrowUp : ArrowDown

  return (
    <Button
      type="button"
      variant="ghost"
      className="-ml-3 h-8 px-2"
      aria-sort={active ? (sortDirection === 'asc' ? 'ascending' : 'descending') : 'none'}
      onClick={() => onSort(column, nextDirection)}
    >
      {label}
      <Icon className="h-4 w-4" aria-hidden="true" />
    </Button>
  )
}
