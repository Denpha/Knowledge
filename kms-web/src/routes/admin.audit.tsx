import { createFileRoute } from '@tanstack/react-router'
import { useMemo, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '../services/api'

export const Route = createFileRoute('/admin/audit')({
  component: AdminAuditPage,
})

function toInputDate(date: Date): string {
  return date.toISOString().slice(0, 10)
}

function toIsoRangeStart(dateInput: string): string {
  return `${dateInput}T00:00:00.000Z`
}

function toIsoRangeEnd(dateInput: string): string {
  return `${dateInput}T23:59:59.999Z`
}

function addDays(baseDate: Date, days: number): Date {
  const next = new Date(baseDate)
  next.setDate(next.getDate() + days)
  return next
}

function diffDaysInclusive(from: string, to: string): number {
  const fromTime = new Date(`${from}T00:00:00.000Z`).getTime()
  const toTime = new Date(`${to}T00:00:00.000Z`).getTime()
  const msPerDay = 24 * 60 * 60 * 1000
  return Math.max(1, Math.floor((toTime - fromTime) / msPerDay) + 1)
}

function AdminAuditPage() {
  const today = new Date()
  const sevenDaysAgo = new Date()
  sevenDaysAgo.setDate(today.getDate() - 6)

  const [search, setSearch] = useState('')
  const [entityName, setEntityName] = useState('')
  const [action, setAction] = useState('')
  const [fromDate, setFromDate] = useState(toInputDate(sevenDaysAgo))
  const [toDate, setToDate] = useState(toInputDate(today))
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const periodDays = diffDaysInclusive(fromDate, toDate)

  const previousPeriodStart = toInputDate(addDays(new Date(`${fromDate}T00:00:00.000Z`), -periodDays))
  const previousPeriodEnd = toInputDate(addDays(new Date(`${fromDate}T00:00:00.000Z`), -1))

  const applyPreset = (days: number) => {
    const end = new Date()
    const start = addDays(end, -(days - 1))
    setFromDate(toInputDate(start))
    setToDate(toInputDate(end))
    setPageNumber(1)
  }

  const auditLogsQuery = useQuery({
    queryKey: ['admin', 'auditlogs', search, entityName, action, fromDate, toDate, pageNumber, pageSize],
    queryFn: () => api.getAuditLogs({
      search: search || undefined,
      entityName: entityName || undefined,
      action: action || undefined,
      fromDate: fromDate ? toIsoRangeStart(fromDate) : undefined,
      toDate: toDate ? toIsoRangeEnd(toDate) : undefined,
      pageNumber,
      pageSize,
      sortBy: 'CreatedAt',
      sortDescending: true,
    }),
    enabled: api.isAuthenticated(),
  })

  const auditSummaryQuery = useQuery({
    queryKey: ['admin', 'auditlogs', 'summary', fromDate, toDate],
    queryFn: () => api.getAuditSummary(
      fromDate ? toIsoRangeStart(fromDate) : undefined,
      toDate ? toIsoRangeEnd(toDate) : undefined,
    ),
    enabled: api.isAuthenticated(),
  })

  const previousSummaryQuery = useQuery({
    queryKey: ['admin', 'auditlogs', 'summary', 'previous', previousPeriodStart, previousPeriodEnd],
    queryFn: () => api.getAuditSummary(
      toIsoRangeStart(previousPeriodStart),
      toIsoRangeEnd(previousPeriodEnd),
    ),
    enabled: api.isAuthenticated(),
  })

  const exportCsv = useMutation({
    mutationFn: async () => {
      const blob = await api.exportAuditLogsCsv({
        search: search || undefined,
        entityName: entityName || undefined,
        action: action || undefined,
        fromDate: fromDate ? toIsoRangeStart(fromDate) : undefined,
        toDate: toDate ? toIsoRangeEnd(toDate) : undefined,
      })

      const fileUrl = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = fileUrl
      anchor.download = `audit-logs-${fromDate}-to-${toDate}.csv`
      document.body.appendChild(anchor)
      anchor.click()
      document.body.removeChild(anchor)
      URL.revokeObjectURL(fileUrl)
    },
  })

  const logs = auditLogsQuery.data?.data?.items ?? []
  const totalCount = auditLogsQuery.data?.data?.totalCount ?? 0
  const totalPages = auditLogsQuery.data?.data?.totalPages ?? 1

  const topActions = useMemo(() => {
    const entries = Object.entries(auditSummaryQuery.data?.data?.actionsByType ?? {})
    return entries.sort((a, b) => b[1] - a[1]).slice(0, 5)
  }, [auditSummaryQuery.data?.data?.actionsByType])

  const topEntities = useMemo(() => {
    const entries = Object.entries(auditSummaryQuery.data?.data?.actionsByEntity ?? {})
    return entries.sort((a, b) => b[1] - a[1]).slice(0, 5)
  }, [auditSummaryQuery.data?.data?.actionsByEntity])

  const topUsers = useMemo(() => {
    const entries = Object.entries(auditSummaryQuery.data?.data?.actionsByUser ?? {})
    return entries.sort((a, b) => b[1] - a[1]).slice(0, 5)
  }, [auditSummaryQuery.data?.data?.actionsByUser])

  const dailyActivity = useMemo(() => {
    const entries = Object.entries(auditSummaryQuery.data?.data?.dailyActivity ?? {})
    return entries
      .sort(([a], [b]) => new Date(a).getTime() - new Date(b).getTime())
      .slice(-7)
  }, [auditSummaryQuery.data?.data?.dailyActivity])

  const anomalyDays = useMemo(() => {
    if (dailyActivity.length < 3) {
      return new Set<string>()
    }

    const counts = dailyActivity.map(([, count]) => count)
    const average = counts.reduce((sum, value) => sum + value, 0) / counts.length
    const variance = counts.reduce((sum, value) => sum + ((value - average) ** 2), 0) / counts.length
    const stdDev = Math.sqrt(variance)

    if (stdDev < 0.01) {
      return new Set<string>()
    }

    return new Set(dailyActivity
      .filter(([, count]) => (count - average) / stdDev >= 1.2)
      .map(([day]) => day))
  }, [dailyActivity])

  const maxDailyCount = useMemo(() => {
    return Math.max(1, ...dailyActivity.map(([, count]) => count))
  }, [dailyActivity])

  const currentTotal = auditSummaryQuery.data?.data?.totalActions ?? 0
  const previousTotal = previousSummaryQuery.data?.data?.totalActions ?? 0
  const delta = currentTotal - previousTotal
  const deltaPct = previousTotal > 0 ? (delta / previousTotal) * 100 : null

  const insightRows = useMemo(() => {
    const rows: { label: string; actionLabel: string; onApply: () => void }[] = []

    if (topActions.length > 0) {
      rows.push({
        label: `Most frequent action: ${topActions[0][0]}`,
        actionLabel: 'Filter action',
        onApply: () => {
          setAction(topActions[0][0])
          setPageNumber(1)
        },
      })
    }

    if (topEntities.length > 0) {
      rows.push({
        label: `Most active entity: ${topEntities[0][0]}`,
        actionLabel: 'Filter entity',
        onApply: () => {
          setEntityName(topEntities[0][0])
          setPageNumber(1)
        },
      })
    }

    if (anomalyDays.size > 0) {
      const latestAnomaly = [...dailyActivity].reverse().find(([day]) => anomalyDays.has(day))
      if (latestAnomaly) {
        rows.push({
          label: `Latest anomaly day: ${new Date(latestAnomaly[0]).toLocaleDateString()} (${latestAnomaly[1]} actions)`,
          actionLabel: 'Focus this day',
          onApply: () => {
            const day = toInputDate(new Date(latestAnomaly[0]))
            setFromDate(day)
            setToDate(day)
            setPageNumber(1)
          },
        })
      }
    }

    return rows
  }, [anomalyDays, dailyActivity, topActions, topEntities])

  return (
    <div className="space-y-6">
      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <h2 className="text-xl font-semibold text-gray-900">Audit Dashboard</h2>
        <p className="mt-2 text-sm text-gray-600">ติดตามกิจกรรมระบบย้อนหลัง พร้อมกรองตาม entity, action และช่วงเวลา</p>

        <div className="mt-4 grid gap-3 md:grid-cols-3">
          <input
            type="text"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPageNumber(1)
            }}
            placeholder="Search keyword"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <input
            type="text"
            value={entityName}
            onChange={(event) => {
              setEntityName(event.target.value)
              setPageNumber(1)
            }}
            placeholder="Entity (e.g. KnowledgeArticle)"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <input
            type="text"
            value={action}
            onChange={(event) => {
              setAction(event.target.value)
              setPageNumber(1)
            }}
            placeholder="Action (e.g. Create/Update/Delete)"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <input
            type="date"
            value={fromDate}
            onChange={(event) => {
              setFromDate(event.target.value)
              setPageNumber(1)
            }}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <input
            type="date"
            value={toDate}
            onChange={(event) => {
              setToDate(event.target.value)
              setPageNumber(1)
            }}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <select
            value={String(pageSize)}
            onChange={(event) => {
              setPageSize(Number(event.target.value))
              setPageNumber(1)
            }}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          >
            <option value="10">10 rows</option>
            <option value="20">20 rows</option>
            <option value="50">50 rows</option>
          </select>
        </div>

        <div className="mt-3 flex flex-wrap items-center gap-2 text-xs">
          <span className="text-gray-500">Quick range:</span>
          <button
            type="button"
            onClick={() => applyPreset(1)}
            className="rounded-full border border-gray-300 px-3 py-1 text-gray-700 hover:bg-gray-50"
          >
            Today
          </button>
          <button
            type="button"
            onClick={() => applyPreset(7)}
            className="rounded-full border border-gray-300 px-3 py-1 text-gray-700 hover:bg-gray-50"
          >
            Last 7 days
          </button>
          <button
            type="button"
            onClick={() => applyPreset(30)}
            className="rounded-full border border-gray-300 px-3 py-1 text-gray-700 hover:bg-gray-50"
          >
            Last 30 days
          </button>
        </div>

        <div className="mt-3 flex justify-end">
          <button
            type="button"
            onClick={() => exportCsv.mutate()}
            disabled={exportCsv.isPending}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
          >
            Export CSV
          </button>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <article className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
          <p className="text-sm text-gray-500">Total actions</p>
          <p className="mt-2 text-3xl font-semibold text-gray-900">{currentTotal}</p>
          <p className="mt-2 text-xs text-gray-500">from selected date range</p>
          <p className={`mt-1 text-xs font-medium ${delta >= 0 ? 'text-emerald-700' : 'text-rose-700'}`}>
            {delta >= 0 ? '+' : ''}{delta} vs previous {periodDays} day(s)
            {deltaPct !== null ? ` (${deltaPct >= 0 ? '+' : ''}${deltaPct.toFixed(1)}%)` : ''}
          </p>
        </article>

        <article className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
          <p className="text-sm font-medium text-gray-700">Top actions</p>
          <div className="mt-3 space-y-2 text-sm text-gray-600">
            {topActions.length > 0 ? topActions.map(([name, count]) => (
              <div key={name} className="flex items-center justify-between">
                <span>{name}</span>
                <span className="font-medium text-gray-900">{count}</span>
              </div>
            )) : <p className="text-gray-500">No data</p>}
          </div>
        </article>

        <article className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
          <p className="text-sm font-medium text-gray-700">Top entities</p>
          <div className="mt-3 space-y-2 text-sm text-gray-600">
            {topEntities.length > 0 ? topEntities.map(([name, count]) => (
              <div key={name} className="flex items-center justify-between">
                <span>{name}</span>
                <span className="font-medium text-gray-900">{count}</span>
              </div>
            )) : <p className="text-gray-500">No data</p>}
          </div>
        </article>

        <article className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
          <p className="text-sm font-medium text-gray-700">Top users</p>
          <div className="mt-3 space-y-2 text-sm text-gray-600">
            {topUsers.length > 0 ? topUsers.map(([name, count]) => (
              <div key={name} className="flex items-center justify-between">
                <span>{name}</span>
                <span className="font-medium text-gray-900">{count}</span>
              </div>
            )) : <p className="text-gray-500">No data</p>}
          </div>
        </article>

        <article className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
          <p className="text-sm font-medium text-gray-700">Daily activity (7d)</p>
          <div className="mt-3 space-y-2 text-sm text-gray-600">
            {dailyActivity.length > 0 ? dailyActivity.map(([day, count]) => (
              <div key={day} className="flex items-center justify-between">
                <span className="flex items-center gap-2">
                  <span>{new Date(day).toLocaleDateString()}</span>
                  {anomalyDays.has(day) && (
                    <span className="rounded-full bg-rose-100 px-2 py-0.5 text-[10px] font-semibold uppercase text-rose-700">
                      Spike
                    </span>
                  )}
                </span>
                <span className="font-medium text-gray-900">{count}</span>
              </div>
            )) : <p className="text-gray-500">No data</p>}
          </div>
        </article>
      </section>

      <section className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
        <p className="text-sm font-medium text-gray-700">Activity Trend (7d)</p>
        <div className="mt-4 grid grid-cols-7 gap-2">
          {dailyActivity.map(([day, count]) => {
            const ratio = count / maxDailyCount
            const height = Math.max(8, Math.round(ratio * 96))
            return (
              <div key={day} className="flex flex-col items-center gap-2">
                <div className="flex h-28 w-full items-end rounded-md bg-slate-50 px-1">
                  <div
                    className={`w-full rounded-sm ${anomalyDays.has(day) ? 'bg-rose-400' : 'bg-slate-700'}`}
                    style={{ height: `${height}px` }}
                    title={`${new Date(day).toLocaleDateString()} • ${count}`}
                  />
                </div>
                <div className="text-[10px] text-gray-500">{new Date(day).toLocaleDateString(undefined, { month: 'numeric', day: 'numeric' })}</div>
              </div>
            )
          })}
        </div>
      </section>

      <section className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-gray-100">
        <p className="text-sm font-medium text-gray-700">Insight Actions</p>
        <div className="mt-3 space-y-2">
          {insightRows.length === 0 && <p className="text-sm text-gray-500">No actionable insight in this range.</p>}
          {insightRows.map((row) => (
            <div key={row.label} className="flex items-center justify-between gap-3 rounded-lg border border-gray-200 px-3 py-2">
              <p className="text-sm text-gray-700">{row.label}</p>
              <button
                type="button"
                onClick={row.onApply}
                className="rounded-md border border-indigo-300 px-3 py-1.5 text-xs font-medium text-indigo-700 hover:bg-indigo-50"
              >
                {row.actionLabel}
              </button>
            </div>
          ))}
        </div>
      </section>

      <section className="rounded-2xl bg-white shadow-sm ring-1 ring-gray-100">
        {auditLogsQuery.isLoading && <div className="p-6 text-sm text-gray-500">Loading audit logs...</div>}
        {auditLogsQuery.isError && <div className="p-6 text-sm text-red-600">Failed to load audit logs.</div>}

        {!auditLogsQuery.isLoading && !auditLogsQuery.isError && (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200 text-sm">
                <thead className="bg-gray-50 text-left text-xs uppercase tracking-wide text-gray-500">
                  <tr>
                    <th className="px-4 py-3">Time</th>
                    <th className="px-4 py-3">User</th>
                    <th className="px-4 py-3">Entity</th>
                    <th className="px-4 py-3">Action</th>
                    <th className="px-4 py-3">IP</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {logs.map((log) => (
                    <tr key={log.id}>
                      <td className="px-4 py-3 text-gray-700">{new Date(log.createdAt).toLocaleString()}</td>
                      <td className="px-4 py-3 text-gray-700">{log.userName || '-'}</td>
                      <td className="px-4 py-3 text-gray-700">{log.entityName}</td>
                      <td className="px-4 py-3">
                        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-700">{log.action}</span>
                      </td>
                      <td className="px-4 py-3 text-gray-700">{log.ipAddress || '-'}</td>
                    </tr>
                  ))}
                  {logs.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-4 py-8 text-center text-sm text-gray-500">No audit logs found for current filters.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            <div className="flex items-center justify-between border-t border-gray-100 px-4 py-3 text-sm">
              <p className="text-gray-600">{totalCount} records • page {pageNumber} of {Math.max(totalPages, 1)}</p>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  disabled={pageNumber <= 1}
                  onClick={() => setPageNumber((current) => Math.max(current - 1, 1))}
                  className="rounded-lg border border-gray-300 px-3 py-1.5 text-gray-700 disabled:opacity-50"
                >
                  Previous
                </button>
                <button
                  type="button"
                  disabled={pageNumber >= totalPages}
                  onClick={() => setPageNumber((current) => current + 1)}
                  className="rounded-lg border border-gray-300 px-3 py-1.5 text-gray-700 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </section>
    </div>
  )
}
