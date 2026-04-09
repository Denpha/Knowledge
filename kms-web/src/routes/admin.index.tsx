import { createFileRoute, Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend, AreaChart, Area,
} from 'recharts'
import { api } from '../services/api'

export const Route = createFileRoute('/admin/')({
  component: AdminDashboardPage,
})

const PIE_COLORS = ['#6366f1','#10b981','#f59e0b','#ef4444','#8b5cf6','#06b6d4','#f97316','#84cc16']

const STATUS_COLORS: Record<string, string> = {
  Published: '#10b981',
  Draft:     '#6366f1',
  Review:    '#f59e0b',
  Archived:  '#94a3b8',
  Rejected:  '#ef4444',
}

function fmt(n: number) { return new Intl.NumberFormat('th-TH').format(n) }

function StatCard({ label, value, sub, color }: {
  label: string; value: string | number; sub?: string; color: string
}) {
  return (
    <article className={`rounded-2xl p-5 text-white shadow-sm bg-gradient-to-br ${color}`}>
      <p className="text-xs font-medium uppercase tracking-wide opacity-80">{label}</p>
      <p className="mt-2 text-3xl font-bold">{typeof value === 'number' ? fmt(value) : value}</p>
      {sub && <p className="mt-1 text-xs opacity-75">{sub}</p>}
    </article>
  )
}

function AdminDashboardPage() {
  const { data: me } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: () => api.getCurrentUser(),
    enabled: api.isAuthenticated(),
  })

  const { data: dashRes, isLoading, isError } = useQuery({
    queryKey: ['admin', 'dashboard'],
    queryFn: () => api.getAdminDashboard(),
    enabled: api.isAuthenticated(),
    refetchInterval: 60_000,
  })

  const d = dashRes?.data
  const s = d?.stats

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="rounded-2xl bg-white dark:bg-gray-800 p-6 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
        <p className="text-sm font-medium uppercase tracking-wide text-indigo-600 dark:text-indigo-400">Dashboard Analytics</p>
        <h2 className="mt-1 text-2xl font-semibold text-gray-900 dark:text-white">
          สวัสดี, {me?.data?.fullName ?? me?.data?.username ?? 'Administrator'} 👋
        </h2>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">ภาพรวมระบบ KMS — อัปเดตทุก 60 วินาที</p>
        {isLoading && <p className="mt-2 text-xs text-amber-600 dark:text-amber-400 animate-pulse">⏳ กำลังโหลดข้อมูล…</p>}
        {isError  && <p className="mt-2 text-xs text-red-600 dark:text-red-400">⚠️ ไม่สามารถดึงข้อมูล dashboard ได้</p>}
      </section>

      {/* Stats cards */}
      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="บทความทั้งหมด"      value={s?.totalArticles ?? 0}
          sub={`เผยแพร่ ${fmt(s?.publishedArticles ?? 0)} · Draft ${fmt(s?.draftArticles ?? 0)}`}
          color="from-indigo-700 to-indigo-500" />
        <StatCard label="ยอดเข้าชม"          value={s?.totalViews ?? 0}
          sub="ทุก Views ในระบบ"               color="from-emerald-700 to-emerald-500" />
        <StatCard label="ผู้ใช้งาน"           value={s?.totalUsers ?? 0}
          sub={`Active ${fmt(s?.activeUsers ?? 0)} คน`} color="from-slate-800 to-slate-600" />
        <StatCard label="หมวดหมู่ / คอมเมนต์" value={`${s?.totalCategories ?? 0} / ${s?.totalComments ?? 0}`}
          sub="Categories · Comments"          color="from-amber-600 to-amber-400" />
      </section>

      {/* Charts row 1 */}
      <section className="grid gap-4 xl:grid-cols-2">
        <div className="rounded-2xl bg-white dark:bg-gray-800 p-5 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
          <h3 className="mb-4 text-base font-semibold text-gray-900 dark:text-white">บทความใหม่ (6 เดือนล่าสุด)</h3>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={d?.articlesPerMonth ?? []} margin={{ top: 4, right: 4, left: -10, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" opacity={0.3} />
              <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#9ca3af' }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: '#9ca3af' }} />
              <Tooltip contentStyle={{ background: '#1f2937', border: 'none', borderRadius: 8, color: '#f9fafb' }} />
              <Bar dataKey="count" name="บทความ" fill="#6366f1" radius={[4,4,0,0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="rounded-2xl bg-white dark:bg-gray-800 p-5 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
          <h3 className="mb-4 text-base font-semibold text-gray-900 dark:text-white">ยอดเข้าชม (6 เดือนล่าสุด)</h3>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={d?.viewsPerMonth ?? []} margin={{ top: 4, right: 4, left: -10, bottom: 0 }}>
              <defs>
                <linearGradient id="viewsGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%"  stopColor="#10b981" stopOpacity={0.35} />
                  <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" opacity={0.3} />
              <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#9ca3af' }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: '#9ca3af' }} />
              <Tooltip contentStyle={{ background: '#1f2937', border: 'none', borderRadius: 8, color: '#f9fafb' }} />
              <Area type="monotone" dataKey="count" name="Views" stroke="#10b981" fill="url(#viewsGrad)" strokeWidth={2} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </section>

      {/* Charts row 2 */}
      <section className="grid gap-4 xl:grid-cols-2">
        <div className="rounded-2xl bg-white dark:bg-gray-800 p-5 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
          <h3 className="mb-4 text-base font-semibold text-gray-900 dark:text-white">บทความตามหมวดหมู่ (Published)</h3>
          <ResponsiveContainer width="100%" height={240}>
            <PieChart>
              <Pie data={d?.categoryBreakdown ?? []} dataKey="count" nameKey="name"
                cx="50%" cy="50%" outerRadius={85}
                label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                labelLine={false}>
                {(d?.categoryBreakdown ?? []).map((_, i) => (
                  <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                ))}
              </Pie>
              <Tooltip contentStyle={{ background: '#1f2937', border: 'none', borderRadius: 8, color: '#f9fafb' }} />
              <Legend iconSize={10} wrapperStyle={{ fontSize: 11, color: '#9ca3af' }} />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="rounded-2xl bg-white dark:bg-gray-800 p-5 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
          <h3 className="mb-4 text-base font-semibold text-gray-900 dark:text-white">สถานะบทความ</h3>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={d?.statusBreakdown ?? []} layout="vertical"
              margin={{ top: 0, right: 16, left: 20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" opacity={0.3} horizontal={false} />
              <XAxis type="number" allowDecimals={false} tick={{ fontSize: 11, fill: '#9ca3af' }} />
              <YAxis type="category" dataKey="name" tick={{ fontSize: 12, fill: '#9ca3af' }} width={72} />
              <Tooltip contentStyle={{ background: '#1f2937', border: 'none', borderRadius: 8, color: '#f9fafb' }} />
              <Bar dataKey="count" name="บทความ" radius={[0,4,4,0]}>
                {(d?.statusBreakdown ?? []).map((item, i) => (
                  <Cell key={i} fill={STATUS_COLORS[item.name] ?? PIE_COLORS[i % PIE_COLORS.length]} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </section>

      {/* Tables row */}
      <section className="grid gap-4 xl:grid-cols-2">
        {/* Top articles */}
        <div className="rounded-2xl bg-white dark:bg-gray-800 p-5 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
          <h3 className="mb-4 text-base font-semibold text-gray-900 dark:text-white">🏆 บทความยอดนิยม</h3>
          <div className="space-y-2">
            {(d?.topArticles ?? []).length === 0 && (
              <p className="text-sm text-gray-400 dark:text-gray-500">ยังไม่มีข้อมูล</p>
            )}
            {(d?.topArticles ?? []).map((a, i) => (
              <Link key={a.id} to="/articles/$id" params={{ id: a.id }}
                className="flex items-center gap-3 rounded-xl px-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-indigo-100 dark:bg-indigo-900 text-xs font-bold text-indigo-600 dark:text-indigo-300">
                  {i + 1}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-gray-800 dark:text-gray-100">{a.title}</p>
                  <p className="text-xs text-gray-400 dark:text-gray-500">{a.category}</p>
                </div>
                <div className="text-right text-xs text-gray-500 dark:text-gray-400 shrink-0">
                  <p>👁 {fmt(a.viewCount)}</p>
                  <p>❤️ {fmt(a.likeCount)}</p>
                </div>
              </Link>
            ))}
          </div>
        </div>

        {/* Recent articles */}
        <div className="rounded-2xl bg-white dark:bg-gray-800 p-5 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
          <h3 className="mb-4 text-base font-semibold text-gray-900 dark:text-white">🕒 บทความล่าสุด</h3>
          <div className="space-y-2">
            {(d?.recentArticles ?? []).length === 0 && (
              <p className="text-sm text-gray-400 dark:text-gray-500">ยังไม่มีข้อมูล</p>
            )}
            {(d?.recentArticles ?? []).map((a) => (
              <div key={a.id}
                className="flex items-center gap-3 rounded-xl px-3 py-2.5 bg-gray-50 dark:bg-gray-700/50">
                <span className="shrink-0 rounded-full px-2 py-0.5 text-xs font-medium"
                  style={{
                    background: (STATUS_COLORS[a.status] ?? '#6366f1') + '22',
                    color: STATUS_COLORS[a.status] ?? '#6366f1',
                  }}>
                  {a.status}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-gray-800 dark:text-gray-100">{a.title}</p>
                  <p className="text-xs text-gray-400 dark:text-gray-500">{a.author}</p>
                </div>
                <p className="shrink-0 text-xs text-gray-400 dark:text-gray-500">
                  {new Date(a.createdAt).toLocaleDateString('th-TH', { day: '2-digit', month: 'short' })}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Quick nav */}
      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {([
          { to: '/admin/users'      as const, label: 'จัดการผู้ใช้', icon: '👥' },
          { to: '/admin/categories' as const, label: 'หมวดหมู่',     icon: '🗂️' },
          { to: '/admin/audit'      as const, label: 'Audit Log',    icon: '🔍' },
          { to: '/admin/settings'   as const, label: 'ตั้งค่าระบบ', icon: '⚙️' },
        ]).map(card => (
          <Link key={card.to} to={card.to}
            className="flex items-center gap-3 rounded-2xl bg-white dark:bg-gray-800 p-4 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700 transition hover:-translate-y-0.5 hover:shadow-md">
            <span className="text-2xl">{card.icon}</span>
            <span className="text-sm font-medium text-gray-800 dark:text-gray-100">{card.label}</span>
          </Link>
        ))}
      </section>
    </div>
  )
}
