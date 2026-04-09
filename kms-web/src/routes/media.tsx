import { createFileRoute, Link } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { api } from '../services/api'

export const Route = createFileRoute('/media')({
  component: MediaLibraryPage,
})

function MediaLibraryPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [collectionName, setCollectionName] = useState('')
  const [mediaType, setMediaType] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(24)
  const [selectedIds, setSelectedIds] = useState<string[]>([])

  const { data: profileResponse } = useQuery({
    queryKey: ['profile', 'library-user'],
    queryFn: () => api.getCurrentUser(),
    enabled: api.isAuthenticated(),
  })

  const userId = profileResponse?.data?.id

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['media-library', userId, search, collectionName, mediaType, pageNumber, pageSize],
    queryFn: () =>
      api.getMedia({
        uploaderId: userId,
        search: search || undefined,
        collectionName: collectionName || undefined,
        mediaType: mediaType || undefined,
        pageNumber,
        pageSize,
      }),
    enabled: !!userId,
  })

  const bulkDelete = useMutation({
    mutationFn: async (ids: string[]) => {
      await Promise.all(ids.map((id) => api.deleteMediaItem(id)))
    },
    onSuccess: () => {
      setSelectedIds([])
      queryClient.invalidateQueries({ queryKey: ['media-library'] })
    },
  })

  const items = data?.data?.items ?? []
  const totalCount = data?.data?.totalCount ?? 0
  const totalPages = Math.max(1, data?.data?.totalPages ?? 1)

  const groupedByCollection = useMemo(() => {
    const map = new Map<string, typeof items>()
    for (const item of items) {
      const key = item.collectionName || 'uncategorized'
      map.set(key, [...(map.get(key) ?? []), item])
    }
    return Array.from(map.entries())
  }, [items])

  const allSelectedOnPage = items.length > 0 && items.every((item) => selectedIds.includes(item.id))

  const toggleSelectOne = (id: string) => {
    setSelectedIds((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]))
  }

  const toggleSelectPage = () => {
    setSelectedIds((current) => {
      if (allSelectedOnPage) {
        return current.filter((id) => !items.some((item) => item.id === id))
      }

      const next = new Set(current)
      items.forEach((item) => next.add(item.id))
      return Array.from(next)
    })
  }

  const applyPreset = (preset: 'images' | 'docs' | 'avatars' | 'recent') => {
    if (preset === 'images') {
      setMediaType('Image')
      setCollectionName('')
      setSearch('')
    }
    if (preset === 'docs') {
      setMediaType('Document')
      setCollectionName('attachments')
      setSearch('')
    }
    if (preset === 'avatars') {
      setCollectionName('avatar')
      setMediaType('Image')
      setSearch('')
    }
    if (preset === 'recent') {
      setSearch('')
      setCollectionName('')
      setMediaType('')
    }
    setPageNumber(1)
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
            <span className="text-sm text-gray-500">Media Library</span>
          </div>
          <nav className="flex items-center gap-4">
            <Link to="/profile" className="text-gray-700 hover:text-gray-900">Profile</Link>
            <Link to="/articles" className="text-gray-700 hover:text-gray-900">Articles</Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-6xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
          <h2 className="text-xl font-semibold text-gray-900">My Media</h2>
          <p className="mt-1 text-sm text-gray-600">ค้นหาและกรองไฟล์ที่คุณอัปโหลดไว้ทั้งหมด</p>

          <div className="mt-3 flex flex-wrap gap-2 text-xs">
            <button type="button" onClick={() => applyPreset('images')} className="rounded-full border border-gray-300 px-3 py-1 hover:bg-gray-50">Images</button>
            <button type="button" onClick={() => applyPreset('docs')} className="rounded-full border border-gray-300 px-3 py-1 hover:bg-gray-50">Docs Attachments</button>
            <button type="button" onClick={() => applyPreset('avatars')} className="rounded-full border border-gray-300 px-3 py-1 hover:bg-gray-50">Avatars</button>
            <button type="button" onClick={() => applyPreset('recent')} className="rounded-full border border-gray-300 px-3 py-1 hover:bg-gray-50">Reset Filters</button>
          </div>

          <div className="mt-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_200px_200px_auto]">
            <input
              type="text"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPageNumber(1)
              }}
              placeholder="ค้นหาจากชื่อไฟล์/title/description"
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            />
            <select
              value={collectionName}
              onChange={(event) => {
                setCollectionName(event.target.value)
                setPageNumber(1)
              }}
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            >
              <option value="">All collections</option>
              <option value="avatar">avatar</option>
              <option value="cover">cover</option>
              <option value="attachments">attachments</option>
            </select>
            <select
              value={mediaType}
              onChange={(event) => {
                setMediaType(event.target.value)
                setPageNumber(1)
              }}
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            >
              <option value="">All media types</option>
              <option value="Image">Image</option>
              <option value="Document">Document</option>
              <option value="Video">Video</option>
              <option value="Audio">Audio</option>
            </select>
            <button
              type="button"
              onClick={() => refetch()}
              className="rounded-xl bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800"
            >
              Refresh
            </button>
          </div>

          <div className="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-gray-600">
            <div className="flex items-center gap-2">
              <span>{totalCount} items</span>
              <span>•</span>
              <span>{selectedIds.length} selected</span>
            </div>
            <div className="flex items-center gap-2">
              <label className="text-xs text-gray-500">Page size</label>
              <select
                value={String(pageSize)}
                onChange={(event) => {
                  setPageSize(Number(event.target.value))
                  setPageNumber(1)
                }}
                className="rounded-lg border border-gray-300 px-2 py-1 text-sm"
              >
                <option value="12">12</option>
                <option value="24">24</option>
                <option value="48">48</option>
              </select>
            </div>
          </div>
        </section>

        <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
          {isLoading && <p className="text-sm text-gray-500">กำลังโหลดไฟล์...</p>}
          {isError && <p className="text-sm text-red-600">โหลด media library ไม่สำเร็จ</p>}
          {!isLoading && !isError && items.length === 0 && (
            <p className="text-sm text-gray-500">ไม่พบไฟล์ตามเงื่อนไขที่เลือก</p>
          )}

          {!isLoading && !isError && groupedByCollection.length > 0 && (
            <div className="space-y-6">
              <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-200 bg-gray-50 px-4 py-3">
                <label className="flex items-center gap-2 text-sm text-gray-700">
                  <input type="checkbox" checked={allSelectedOnPage} onChange={toggleSelectPage} />
                  Select all items on page
                </label>
                <button
                  type="button"
                  onClick={() => bulkDelete.mutate(selectedIds)}
                  disabled={selectedIds.length === 0 || bulkDelete.isPending}
                  className="rounded-lg border border-red-300 px-4 py-2 text-sm font-medium text-red-700 hover:bg-red-50 disabled:opacity-50"
                >
                  Delete selected
                </button>
              </div>

              {groupedByCollection.map(([collection, collectionItems]) => (
                <div key={collection}>
                  <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-500">{collection}</h3>
                  <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                    {collectionItems.map((item) => (
                      <div key={item.id} className="rounded-xl border border-gray-200 bg-white p-3">
                        <label className="mb-2 flex items-center gap-2 text-xs text-gray-600">
                          <input
                            type="checkbox"
                            checked={selectedIds.includes(item.id)}
                            onChange={() => toggleSelectOne(item.id)}
                          />
                          Select
                        </label>
                        <div className="mb-3 overflow-hidden rounded-lg bg-gray-100">
                          {item.contentType?.startsWith('image/') ? (
                            <img src={item.thumbnailUrl ?? item.url} alt={item.altText ?? item.originalFileName} className="h-32 w-full object-cover" />
                          ) : (
                            <div className="flex h-32 items-center justify-center text-4xl text-gray-400">📎</div>
                          )}
                        </div>
                        <p className="truncate text-sm font-medium text-gray-900">{item.title || item.originalFileName}</p>
                        {item.description && <p className="mt-1 line-clamp-2 text-xs text-gray-600">{item.description}</p>}
                        <div className="mt-2 flex items-center justify-between text-xs text-gray-500">
                          <span>{new Date(item.createdAt).toLocaleDateString('th-TH')}</span>
                          <a href={item.url} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">Open</a>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}

              <div className="flex items-center justify-between border-t border-gray-100 pt-4 text-sm">
                <p className="text-gray-600">Page {pageNumber} of {totalPages}</p>
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
            </div>
          )}
        </section>
      </main>
    </div>
  )
}
