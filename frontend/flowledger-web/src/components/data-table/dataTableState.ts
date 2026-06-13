import type { DataTableState, SortDirection } from './dataTableTypes'

export function readDataTableState(searchParams: URLSearchParams, defaults?: Partial<DataTableState>): DataTableState {
  return {
    page: positiveInt(searchParams.get('page'), defaults?.page ?? 1),
    pageSize: allowedPageSize(searchParams.get('pageSize'), defaults?.pageSize ?? 25),
    search: searchParams.get('search') ?? defaults?.search ?? '',
    sortBy: searchParams.get('sortBy') ?? defaults?.sortBy ?? 'createdAtUtc',
    sortDirection: sortDirection(searchParams.get('sortDirection'), defaults?.sortDirection ?? 'desc'),
  }
}

export function writeDataTableState(current: URLSearchParams, next: Partial<DataTableState>) {
  const params = new URLSearchParams(current)
  for (const [key, value] of Object.entries(next)) {
    if (value === undefined || value === null || value === '') {
      params.delete(key)
    } else {
      params.set(key, String(value))
    }
  }
  return params
}

function positiveInt(value: string | null, fallback: number) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed >= 1 ? parsed : fallback
}

function allowedPageSize(value: string | null, fallback: number) {
  const parsed = Number(value)
  return [10, 25, 50, 100].includes(parsed) ? parsed : fallback
}

function sortDirection(value: string | null, fallback: SortDirection): SortDirection {
  return value === 'asc' || value === 'desc' ? value : fallback
}
