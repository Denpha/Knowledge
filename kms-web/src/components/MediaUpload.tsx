import { useRef, useState } from 'react'
import { api } from '../services/api'
import type { MediaItemDto } from '../types/api'

interface MediaUploadProps {
  /** The article/entity ID to associate uploads with */
  entityId?: string
  entityType?: string
  /** 'cover' | 'attachments' | 'avatar' */
  collection?: 'cover' | 'attachments' | 'avatar'
  /** Called after a successful upload */
  onUploaded?: (item: MediaItemDto) => void
  /** Max file size in MB (default 10) */
  maxSizeMb?: number
  /** Accepted mime types, e.g. 'image/*,application/pdf' */
  accept?: string
  /** Allow multiple files */
  multiple?: boolean
}

export function MediaUpload({
  entityId,
  entityType = 'article',
  collection = 'attachments',
  onUploaded,
  maxSizeMb = 10,
  accept = 'image/*,application/pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.zip',
  multiple = true,
}: MediaUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [uploads, setUploads] = useState<UploadItem[]>([])

  type UploadItem = {
    id: string
    file: File
    status: 'pending' | 'uploading' | 'done' | 'error'
    progress: number
    result?: MediaItemDto
    error?: string
  }

  const processFiles = async (files: FileList | File[]) => {
    const fileArray = Array.from(files)
    const maxBytes = maxSizeMb * 1024 * 1024

    const newItems: UploadItem[] = fileArray.map((file) => ({
      id: crypto.randomUUID(),
      file,
      status: file.size > maxBytes ? 'error' : 'pending',
      progress: 0,
      error: file.size > maxBytes ? `ไฟล์ใหญ่เกิน ${maxSizeMb}MB` : undefined,
    }))

    setUploads((prev) => [...prev, ...newItems])

    for (const item of newItems) {
      if (item.status === 'error') continue

      setUploads((prev) =>
        prev.map((u) => (u.id === item.id ? { ...u, status: 'uploading', progress: 10 } : u)),
      )

      try {
        const result = await api.uploadMedia(
          item.file,
          collection,
          entityType,
          entityId ?? '',
        )

        if (result.success && result.data) {
          setUploads((prev) =>
            prev.map((u) =>
              u.id === item.id ? { ...u, status: 'done', progress: 100, result: result.data } : u,
            ),
          )
          onUploaded?.(result.data)
        } else {
          throw new Error(result.message ?? 'Upload failed')
        }
      } catch (err: any) {
        setUploads((prev) =>
          prev.map((u) =>
            u.id === item.id ? { ...u, status: 'error', error: err.message ?? 'Upload failed' } : u,
          ),
        )
      }
    }
  }

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    setIsDragging(false)
    if (e.dataTransfer.files.length > 0) {
      processFiles(e.dataTransfer.files)
    }
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      processFiles(e.target.files)
      e.target.value = '' // reset so same file can be re-uploaded
    }
  }

  const removeItem = (id: string) => {
    setUploads((prev) => prev.filter((u) => u.id !== id))
  }

  const fileIcon = (file: File) => {
    if (file.type.startsWith('image/')) return '🖼'
    if (file.type === 'application/pdf') return '📄'
    if (file.type.includes('spreadsheet') || file.name.endsWith('.xlsx') || file.name.endsWith('.xls')) return '📊'
    if (file.type.includes('presentation') || file.name.endsWith('.pptx') || file.name.endsWith('.ppt')) return '📑'
    if (file.type.includes('word') || file.name.endsWith('.docx') || file.name.endsWith('.doc')) return '📝'
    if (file.name.endsWith('.zip')) return '🗜'
    return '📎'
  }

  return (
    <div className="space-y-3">
      {/* Drop Zone */}
      <div
        onDrop={handleDrop}
        onDragOver={(e) => { e.preventDefault(); setIsDragging(true) }}
        onDragLeave={() => setIsDragging(false)}
        onClick={() => inputRef.current?.click()}
        className={`border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors select-none
          ${isDragging
            ? 'border-blue-500 bg-blue-50'
            : 'border-gray-300 hover:border-blue-400 hover:bg-gray-50'
          }`}
      >
        <div className="text-3xl mb-2">📁</div>
        <p className="text-sm font-medium text-gray-700">ลากไฟล์มาวางที่นี่ หรือคลิกเพื่อเลือกไฟล์</p>
        <p className="text-xs text-gray-400 mt-1">
          รองรับ: รูปภาพ, PDF, Office ไม่เกิน {maxSizeMb}MB
        </p>
        <input
          ref={inputRef}
          type="file"
          accept={accept}
          multiple={multiple}
          className="hidden"
          onChange={handleChange}
        />
      </div>

      {/* Upload list */}
      {uploads.length > 0 && (
        <ul className="space-y-2">
          {uploads.map((item) => (
            <li
              key={item.id}
              className={`flex items-center gap-3 rounded-lg border px-3 py-2 text-sm
                ${item.status === 'error' ? 'border-red-200 bg-red-50' : 'border-gray-200 bg-white'}`}
            >
              {/* Icon */}
              <span className="text-xl flex-shrink-0">{fileIcon(item.file)}</span>

              {/* Name + progress */}
              <div className="flex-1 min-w-0">
                <p className="truncate text-gray-800 font-medium">{item.file.name}</p>
                {item.status === 'uploading' && (
                  <div className="mt-1 h-1.5 rounded-full bg-gray-200 overflow-hidden">
                    <div
                      className="h-full bg-blue-500 transition-all"
                      style={{ width: `${item.progress}%` }}
                    />
                  </div>
                )}
                {item.status === 'error' && (
                  <p className="text-red-600 text-xs">{item.error}</p>
                )}
                {item.status === 'done' && (
                  <p className="text-green-600 text-xs">อัปโหลดสำเร็จ</p>
                )}
              </div>

              {/* Status icon */}
              <span className="flex-shrink-0">
                {item.status === 'uploading' && (
                  <span className="text-blue-500 animate-spin inline-block">⟳</span>
                )}
                {item.status === 'done' && <span className="text-green-500">✓</span>}
                {item.status === 'error' && <span className="text-red-500">✕</span>}
              </span>

              {/* Remove */}
              <button
                type="button"
                onClick={(e) => { e.stopPropagation(); removeItem(item.id) }}
                className="flex-shrink-0 text-gray-400 hover:text-gray-600 text-xs px-1"
              >
                ✕
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
