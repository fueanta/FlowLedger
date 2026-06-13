import { formatDateTime } from '../lib/format'
import type { AuditLog, RecentActivity } from '../types'

type TimelineItem = AuditLog | RecentActivity

export function AuditTimeline({ items }: { items: TimelineItem[] }) {
  if (items.length === 0) {
    return <p className="text-sm text-slate-600">No activity recorded yet.</p>
  }

  return (
    <ol className="space-y-4">
      {items.map((item) => (
        <li key={item.id} className="relative pl-6">
          <span className="absolute left-0 top-1.5 h-2.5 w-2.5 rounded-full bg-blue-950" aria-hidden="true" />
          <div className="space-y-1">
            <p className="text-sm font-semibold text-slate-950">{item.message}</p>
            <p className="text-xs text-slate-600">
              {getActorName(item)} · {item.actionType} · {formatDateTime(item.createdAtUtc)}
            </p>
          </div>
        </li>
      ))}
    </ol>
  )
}

function getActorName(item: TimelineItem) {
  if ('actor' in item) {
    return item.actor.fullName
  }

  return item.actorName
}
