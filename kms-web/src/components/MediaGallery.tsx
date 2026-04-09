import type { MediaItemDto } from '../types/api'
import { useState } from 'react'

interface MediaGalleryProps {
  items: MediaItemDto[]
  onDelete?: (item: MediaItemDto) => void
  onUpdate?: (item: MediaItemDto, updates: { title?: string; description?: string; altText?: string }) => void
  deletingId?: string | null
  updatingId?: string | null
}

export function MediaGallery({ items, onDelete, onUpdate, deletingId, updatingId }: MediaGalleryProps) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [altText, setAltText] = useState('')

  const startEdit = (item: MediaItemDto) => {
    setEditingId(item.id)
    setTitle(item.title ?? '')
    setDescription(item.description ?? '')
    setAltText(item.altText ?? '')
  }

  const cancelEdit = () => {
    setEditingId(null)
    setTitle('')
    setDescription('')
    setAltText('')
  }

  if (items.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-gray-300 p-6 text-sm text-gray-500">
        ยังไม่มีไฟล์ในคอลเลกชันนี้
      </div>
    )
  }

  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
      {items.map((item) => {
        const isImage = item.contentType?.startsWith('image/')

        return (
          <div key={item.id} className="rounded-xl border border-gray-200 bg-white p-3 shadow-sm">
            <div className="mb-3 overflow-hidden rounded-lg bg-gray-100">
              {isImage ? (
                <img
                  src={item.thumbnailUrl ?? item.url}
                  alt={item.altText ?? item.originalFileName}
                  className="h-36 w-full object-cover"
                />
              ) : (
                <div className="flex h-36 items-center justify-center text-4xl text-gray-400">📎</div>
              )}
            </div>

            <div className="space-y-1">
              <p className="truncate text-sm font-medium text-gray-900">{item.originalFileName}</p>
              <p className="text-xs text-gray-500">{item.collectionName ?? 'uncategorized'}</p>
              <p className="text-xs text-gray-500">{Math.ceil(item.fileSize / 1024)} KB</p>
            </div>

            {editingId === item.id ? (
              <div className="mt-3 space-y-2">
                <input
                  type="text"
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Title"
                  className="w-full rounded-md border border-gray-300 px-2 py-1 text-xs"
                />
                <input
                  type="text"
                  value={altText}
                  onChange={(event) => setAltText(event.target.value)}
                  placeholder="Alt text"
                  className="w-full rounded-md border border-gray-300 px-2 py-1 text-xs"
                />
                <textarea
                  rows={2}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                  placeholder="Description"
                  className="w-full rounded-md border border-gray-300 px-2 py-1 text-xs"
                />
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => onUpdate?.(item, { title, altText, description })}
                    disabled={updatingId === item.id}
                    className="rounded-md border border-blue-200 px-2 py-1 text-xs font-medium text-blue-600 hover:bg-blue-50 disabled:opacity-50"
                  >
                    {updatingId === item.id ? 'Saving...' : 'Save'}
                  </button>
                  <button
                    type="button"
                    onClick={cancelEdit}
                    className="rounded-md border border-gray-200 px-2 py-1 text-xs text-gray-600 hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            ) : (
              <div className="mt-2 space-y-1">
                {item.title && <p className="truncate text-xs text-gray-700">Title: {item.title}</p>}
                {item.altText && <p className="truncate text-xs text-gray-700">Alt: {item.altText}</p>}
                {item.description && <p className="line-clamp-2 text-xs text-gray-600">{item.description}</p>}
              </div>
            )}

            <div className="mt-3 flex items-center justify-between gap-2">
              <a
                href={item.url}
                target="_blank"
                rel="noreferrer"
                className="text-xs font-medium text-blue-600 hover:underline"
              >
                Open
              </a>
              <div className="flex items-center gap-2">
                {onUpdate && editingId !== item.id && (
                  <button
                    type="button"
                    onClick={() => startEdit(item)}
                    className="rounded-md border border-blue-200 px-2.5 py-1 text-xs font-medium text-blue-600 hover:bg-blue-50"
                  >
                    Edit
                  </button>
                )}
                {onDelete && (
                  <button
                    type="button"
                    onClick={() => onDelete(item)}
                    disabled={deletingId === item.id}
                    className="rounded-md border border-red-200 px-2.5 py-1 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                  >
                    {deletingId === item.id ? 'Deleting...' : 'Delete'}
                  </button>
                )}
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}
