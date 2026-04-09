import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../services/api'
import type { StorageFileDto } from '../types/api'

export const Route = createFileRoute('/admin/storage')({
  component: AdminStoragePage,
})

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getFileIcon(ext: string): string {
  const e = ext.replace('.', '').toLowerCase()
  if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg'].includes(e)) return '🖼️'
  if (['mp4', 'avi', 'mov', 'webm'].includes(e)) return '🎬'
  if (['mp3', 'wav', 'ogg'].includes(e)) return '🎵'
  if (['pdf'].includes(e)) return '📄'
  if (['docx', 'doc'].includes(e)) return '📝'
  if (['xlsx', 'xls'].includes(e)) return '📊'
  if (['zip', 'rar', '7z'].includes(e)) return '📦'
  return '📁'
}

function isImage(ext: string): boolean {
  return ['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg'].includes(ext.replace('.', '').toLowerCase())
}

// ─── Page ─────────────────────────────────────────────────────────────────────

function AdminStoragePage() {
  const queryClient = useQueryClient()

  // ── Filters ─────────────────────────────────────────────────────────────────
  const [prefix, setPrefix] = useState('')
  const [extFilter, setExtFilter] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 50

  // ── Selection ───────────────────────────────────────────────────────────────
  const [selected, setSelected] = useState<Set<string>>(new Set())

  // ── Preview modal ────────────────────────────────────────────────────────────
  const [previewFile, setPreviewFile] = useState<StorageFileDto | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)

  // ── Queries ─────────────────────────────────────────────────────────────────
  const statsQuery = useQuery({
    queryKey: ['storage', 'stats'],
    queryFn: () => api.getStorageStats(),
  })

  const filesQuery = useQuery({
    queryKey: ['storage', 'files', prefix, extFilter, page],
    queryFn: () => api.listStorageFiles({ prefix: prefix || undefined, ext: extFilter || undefined, page, pageSize }),
  })

  // ── Mutations ────────────────────────────────────────────────────────────────
  const deleteMutation = useMutation({
    mutationFn: (keys: string[]) => api.deleteStorageFiles(keys),
    onSuccess: () => {
      setSelected(new Set())
      queryClient.invalidateQueries({ queryKey: ['storage'] })
    },
  })

  const presignMutation = useMutation({
    mutationFn: (key: string) => api.getPresignedUrl(key, 60),
    onSuccess: (res, _key) => {
      if (res.data) {
        setPreviewUrl(res.data)
      }
    },
  })

  const stats = statsQuery.data?.data
  const fileList = filesQuery.data?.data

  // ── Selection helpers ────────────────────────────────────────────────────────
  const toggleSelect = (key: string) => {
    setSelected(prev => {
      const next = new Set(prev)
      next.has(key) ? next.delete(key) : next.add(key)
      return next
    })
  }

  const toggleSelectAll = () => {
    if (!fileList) return
    if (selected.size === fileList.files.length) {
      setSelected(new Set())
    } else {
      setSelected(new Set(fileList.files.map(f => f.key)))
    }
  }

  const handlePreview = (file: StorageFileDto) => {
    setPreviewFile(file)
    setPreviewUrl(null)
    presignMutation.mutate(file.key)
  }

  const topTypes = stats
    ? Object.entries(stats.filesByType)
        .sort((a, b) => b[1] - a[1])
        .slice(0, 5)
    : []

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Storage Management</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">จัดการไฟล์ใน MinIO bucket</p>
        </div>
        <span className="text-xs bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-200 px-3 py-1 rounded-full font-mono">
          {stats?.bucketName ?? '…'}
        </span>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          label="ไฟล์ทั้งหมด"
          value={stats ? stats.totalFiles.toLocaleString() : '…'}
          icon="📁"
          loading={statsQuery.isLoading}
        />
        <StatCard
          label="พื้นที่ใช้งาน"
          value={stats?.totalSizeFormatted ?? '…'}
          icon="💾"
          loading={statsQuery.isLoading}
        />
        <StatCard
          label="ประเภทไฟล์"
          value={stats ? Object.keys(stats.filesByType).length.toString() : '…'}
          icon="🗂️"
          loading={statsQuery.isLoading}
        />
        <StatCard
          label="ไฟล์รูปภาพ"
          value={stats ? (
            (stats.filesByType['.jpg'] ?? 0) +
            (stats.filesByType['.jpeg'] ?? 0) +
            (stats.filesByType['.png'] ?? 0) +
            (stats.filesByType['.webp'] ?? 0)
          ).toLocaleString() : '…'}
          icon="🖼️"
          loading={statsQuery.isLoading}
        />
      </div>

      {/* Type breakdown */}
      {topTypes.length > 0 && (
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
          <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">ประเภทไฟล์ยอดนิยม</h2>
          <div className="flex flex-wrap gap-2">
            {topTypes.map(([ext, count]) => (
              <button
                key={ext}
                onClick={() => { setExtFilter(extFilter === ext.replace('.', '') ? '' : ext.replace('.', '')); setPage(1) }}
                className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium transition-colors ${
                  extFilter === ext.replace('.', '')
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-blue-100 dark:hover:bg-blue-900'
                }`}
              >
                {getFileIcon(ext)} {ext} <span className="opacity-70">({count})</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* File Browser */}
      <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
        {/* Toolbar */}
        <div className="flex flex-wrap items-center gap-3 p-4 border-b border-gray-200 dark:border-gray-700">
          <input
            type="text"
            placeholder="ค้นหาด้วย prefix (เช่น articles/)"
            value={prefix}
            onChange={e => { setPrefix(e.target.value); setPage(1) }}
            className="flex-1 min-w-48 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="text"
            placeholder="นามสกุลไฟล์ เช่น jpg"
            value={extFilter}
            onChange={e => { setExtFilter(e.target.value.replace('.', '')); setPage(1) }}
            className="w-40 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {selected.size > 0 && (
            <button
              onClick={() => {
                if (confirm(`ลบ ${selected.size} ไฟล์ที่เลือก?`)) {
                  deleteMutation.mutate(Array.from(selected))
                }
              }}
              disabled={deleteMutation.isPending}
              className="px-4 py-2 text-sm bg-red-600 hover:bg-red-700 text-white rounded-lg transition-colors disabled:opacity-50"
            >
              🗑️ ลบ {selected.size} ไฟล์
            </button>
          )}
          <span className="text-xs text-gray-500 dark:text-gray-400 ml-auto">
            {fileList ? `${fileList.totalCount.toLocaleString()} ไฟล์` : ''}
          </span>
        </div>

        {/* Table */}
        {filesQuery.isLoading ? (
          <div className="p-12 text-center text-gray-400">กำลังโหลด…</div>
        ) : filesQuery.isError ? (
          <div className="p-12 text-center text-red-500">ไม่สามารถโหลดรายการไฟล์ได้</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 dark:bg-gray-700/50 text-gray-600 dark:text-gray-400 text-xs uppercase">
                <tr>
                  <th className="w-10 px-4 py-3">
                    <input
                      type="checkbox"
                      checked={!!fileList && selected.size === fileList.files.length && fileList.files.length > 0}
                      onChange={toggleSelectAll}
                      className="rounded"
                    />
                  </th>
                  <th className="px-4 py-3 text-left">ชื่อไฟล์</th>
                  <th className="px-4 py-3 text-right">ขนาด</th>
                  <th className="px-4 py-3 text-left">ประเภท</th>
                  <th className="px-4 py-3 text-left">วันที่</th>
                  <th className="px-4 py-3 text-center">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                {fileList?.files.map(file => (
                  <tr
                    key={file.key}
                    className={`hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors ${
                      selected.has(file.key) ? 'bg-blue-50 dark:bg-blue-900/20' : ''
                    }`}
                  >
                    <td className="px-4 py-3">
                      <input
                        type="checkbox"
                        checked={selected.has(file.key)}
                        onChange={() => toggleSelect(file.key)}
                        className="rounded"
                      />
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className="text-lg">{getFileIcon(file.extension)}</span>
                        <div className="min-w-0">
                          <p className="font-medium text-gray-900 dark:text-gray-100 truncate max-w-xs" title={file.key}>
                            {file.key.split('/').pop()}
                          </p>
                          <p className="text-xs text-gray-400 truncate max-w-xs" title={file.key}>
                            {file.key}
                          </p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right text-gray-600 dark:text-gray-400 tabular-nums">
                      {file.sizeFormatted}
                    </td>
                    <td className="px-4 py-3">
                      <span className="inline-block px-2 py-0.5 rounded text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 font-mono">
                        {file.extension || 'unknown'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs whitespace-nowrap">
                      {file.lastModified ? new Date(file.lastModified).toLocaleString('th-TH') : '-'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => handlePreview(file)}
                          className="text-xs px-2 py-1 rounded bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 hover:bg-blue-200 dark:hover:bg-blue-900 transition-colors"
                        >
                          👁️ Preview
                        </button>
                        <a
                          href={file.publicUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-xs px-2 py-1 rounded bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
                        >
                          ↗️ เปิด
                        </a>
                        <button
                          onClick={() => {
                            if (confirm(`ลบไฟล์ "${file.key}"?`)) {
                              deleteMutation.mutate([file.key])
                            }
                          }}
                          className="text-xs px-2 py-1 rounded bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300 hover:bg-red-200 dark:hover:bg-red-900 transition-colors"
                        >
                          🗑️
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {fileList?.files.length === 0 && (
                  <tr>
                    <td colSpan={6} className="py-16 text-center text-gray-400">ไม่พบไฟล์</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {fileList && fileList.totalCount > pageSize && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 dark:border-gray-700 text-sm text-gray-600 dark:text-gray-400">
            <span>หน้า {page} / {Math.ceil(fileList.totalCount / pageSize)}</span>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage(p => p - 1)}
                className="px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-40 hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                ← ก่อนหน้า
              </button>
              <button
                disabled={!fileList.hasMore}
                onClick={() => setPage(p => p + 1)}
                className="px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-40 hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                ถัดไป →
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Preview Modal */}
      {previewFile && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4"
          onClick={() => { setPreviewFile(null); setPreviewUrl(null) }}
        >
          <div
            className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl max-w-3xl w-full max-h-[90vh] overflow-auto"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
              <div className="min-w-0">
                <p className="font-semibold text-gray-900 dark:text-gray-100 truncate">
                  {previewFile.key.split('/').pop()}
                </p>
                <p className="text-xs text-gray-400 mt-0.5">{previewFile.sizeFormatted} · {previewFile.contentType}</p>
              </div>
              <button
                onClick={() => { setPreviewFile(null); setPreviewUrl(null) }}
                className="ml-4 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 text-2xl leading-none"
              >
                ×
              </button>
            </div>
            <div className="p-4">
              {presignMutation.isPending && (
                <div className="py-16 text-center text-gray-400">กำลังโหลด…</div>
              )}
              {previewUrl && isImage(previewFile.extension) && (
                <img
                  src={previewUrl}
                  alt={previewFile.key}
                  className="max-w-full max-h-[60vh] mx-auto rounded-lg object-contain"
                />
              )}
              {previewUrl && !isImage(previewFile.extension) && (
                <div className="text-center py-12">
                  <div className="text-6xl mb-4">{getFileIcon(previewFile.extension)}</div>
                  <p className="text-gray-600 dark:text-gray-400 mb-4">{previewFile.key}</p>
                  <a
                    href={previewUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-block px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                  >
                    ↗️ เปิดไฟล์
                  </a>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Stat Card ────────────────────────────────────────────────────────────────

function StatCard({ label, value, icon, loading }: {
  label: string
  value: string
  icon: string
  loading?: boolean
}) {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
      <div className="flex items-center gap-3">
        <span className="text-2xl">{icon}</span>
        <div>
          <p className="text-xs text-gray-500 dark:text-gray-400">{label}</p>
          <p className={`text-xl font-bold text-gray-900 dark:text-white ${loading ? 'animate-pulse text-gray-300' : ''}`}>
            {value}
          </p>
        </div>
      </div>
    </div>
  )
}
