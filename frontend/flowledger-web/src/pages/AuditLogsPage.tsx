import { useQuery } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { useMemo, useState } from 'react'
import { getAuditLogs } from '../api/auditLogs'
import { DataTable } from '../components/data-table/DataTable'
import { DataTablePageSizeSelect } from '../components/data-table/DataTablePageSizeSelect'
import { DataTableSearch } from '../components/data-table/DataTableSearch'
import { DataTableSortableHeader } from '../components/data-table/DataTableSortableHeader'
import { DataTableToolbar } from '../components/data-table/DataTableToolbar'
import { useDataTableState } from '../components/data-table/dataTableState'
import { PageHeader } from '../components/PageHeader'
import { Badge } from '../components/ui/badge'
import { Select } from '../components/ui/select'
import { formatDateTime } from '../lib/format'
import type { AuditLogListItem } from '../types'

const entityTypes = ['', 'BillingRequest', 'Invoice', 'User', 'EnrollmentRequest']

export function AuditLogsPage() {
  const { state, setPage, setSearch, setSort, setPageSize } = useDataTableState({ sortBy: 'createdAtUtc', sortDirection: 'desc' })
  const [entityType, setEntityType] = useState('')
  const listParams = { search: state.search, entityType, sortBy: state.sortBy, sortDirection: state.sortDirection }
  const auditLogsQuery = useQuery({
    queryKey: ['audit-logs', { ...listParams, page: state.page, pageSize: state.pageSize }],
    queryFn: () => getAuditLogs({ ...listParams, page: state.page, pageSize: state.pageSize }),
  })

  const columns = useMemo<ColumnDef<AuditLogListItem>[]>(
    () => [
      {
        accessorKey: 'createdAtUtc',
        header: () => <DataTableSortableHeader label="Created" column="createdAtUtc" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => formatDateTime(row.original.createdAtUtc),
      },
      {
        accessorKey: 'entityType',
        header: () => <DataTableSortableHeader label="Entity" column="entityType" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => (
          <div>
            <p className="font-semibold text-slate-950">{row.original.entityType}</p>
            <p className="text-sm text-slate-600">{row.original.entityNumber ?? row.original.entityId}</p>
          </div>
        ),
      },
      {
        accessorKey: 'actorDisplayName',
        header: () => <DataTableSortableHeader label="Actor" column="actorDisplayName" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
      },
      {
        accessorKey: 'actionType',
        header: () => <DataTableSortableHeader label="Action" column="actionType" sortBy={state.sortBy} sortDirection={state.sortDirection} onSort={setSort} />,
        cell: ({ row }) => <Badge variant="outline">{row.original.actionType}</Badge>,
      },
      { accessorKey: 'message', header: 'Message' },
      {
        id: 'status',
        header: 'Status Change',
        cell: ({ row }) => (row.original.beforeStatus || row.original.afterStatus ? `${row.original.beforeStatus ?? '-'} -> ${row.original.afterStatus ?? '-'}` : '-'),
      },
    ],
    [setSort, state.sortBy, state.sortDirection],
  )

  return (
    <>
      <PageHeader title="Audit Logs" description="Workflow and administration activity recorded by the system." />

      <DataTableToolbar>
        <DataTableSearch value={state.search} onChange={setSearch} />
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-900" htmlFor="audit-entity-type">
            Entity
          </label>
          <Select
            id="audit-entity-type"
            value={entityType}
            onChange={(event) => {
              setEntityType(event.target.value)
              setPage(1)
            }}
          >
            {entityTypes.map((item) => (
              <option key={item || 'all'} value={item}>
                {item || 'All entities'}
              </option>
            ))}
          </Select>
        </div>
        <DataTablePageSizeSelect value={state.pageSize} onChange={setPageSize} />
      </DataTableToolbar>

      <DataTable
        data={auditLogsQuery.data?.items ?? []}
        columns={columns}
        page={state.page}
        pageSize={state.pageSize}
        totalCount={auditLogsQuery.data?.totalCount ?? 0}
        loading={auditLogsQuery.isLoading}
        error={auditLogsQuery.isError}
        emptyMessage="No audit logs found."
        onPageChange={setPage}
      />
    </>
  )
}
