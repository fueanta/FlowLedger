import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { getMyPreferences, updateMyPreferences } from '../../api/preferences'
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

export function useDataTableState(defaults: Partial<DataTableState> = {}) {
  const [searchParams, setSearchParams] = useSearchParams()
  const queryClient = useQueryClient()
  const preferencesQuery = useQuery({
    queryKey: ['preferences', 'me'],
    queryFn: getMyPreferences,
    staleTime: 60_000,
  })
  const updatePreferencesMutation = useMutation({
    mutationFn: updateMyPreferences,
    onSuccess: (preference) => queryClient.setQueryData(['preferences', 'me'], preference),
  })

  const state = readDataTableState(searchParams, {
    ...defaults,
    pageSize: defaults.pageSize ?? preferencesQuery.data?.rowsPerPage ?? 25,
  })

  function update(next: Partial<DataTableState>) {
    setSearchParams(writeDataTableState(searchParams, next))
  }

  return {
    state,
    setPage: (page: number) => update({ page }),
    setSearch: (search: string) => update({ search, page: 1 }),
    setSort: (sortBy: string, sortDirection: SortDirection) => update({ sortBy, sortDirection, page: 1 }),
    setPageSize: (pageSize: number) => {
      update({ pageSize, page: 1 })
      if (preferencesQuery.data && preferencesQuery.data.rowsPerPage !== pageSize) {
        updatePreferencesMutation.mutate({ ...preferencesQuery.data, rowsPerPage: pageSize })
      }
    },
  }
}
