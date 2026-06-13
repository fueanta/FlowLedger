export type SortDirection = 'asc' | 'desc'

export type DataTableState = {
  page: number
  pageSize: number
  search: string
  sortBy: string
  sortDirection: SortDirection
}

export const pageSizeOptions = [10, 25, 50, 100] as const
