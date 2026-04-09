import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RichTextEditor } from '../components/RichTextEditor'
import { AiWritingPanel } from '../components/AiWritingPanel'
import { MediaUpload } from '../components/MediaUpload'
import { MediaGallery } from '../components/MediaGallery'
import { useArticle, useUpdateArticle } from '../hooks/useArticles'
import { useAutosave } from '../hooks/useAutosave'
import { api } from '../services/api'
import type { MediaItemDto } from '../types/api'

export const Route = createFileRoute('/articles/$id/edit')({
  component: EditArticlePage,
})

function EditArticlePage() {
  const { id } = Route.useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { data, isLoading, isError } = useArticle(id)
  const updateArticleMutation = useUpdateArticle()
  const [tagInput, setTagInput] = useState('')
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    title: '',
    titleEn: '',
    content: '',
    contentEn: '',
    summary: '',
    summaryEn: '',
    keywordsEn: '',
    status: 'draft' as 'draft' | 'under_review' | 'published' | 'archived',
    visibility: 'internal' as 'public' | 'internal' | 'restricted',
    categoryId: '',
    tags: [] as string[],
  })
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([])

  const { clear: clearDraft } = useAutosave(
    `edit-${id}`,
    {
      title: formData.title,
      content: formData.content,
      summary: formData.summary,
      savedAt: new Date().toISOString(),
    },
    !!data?.success,
  )

  const { data: categoriesResponse } = useQuery({
    queryKey: ['categories', 'edit-form'],
    queryFn: () => api.getCategories(),
  })
  const { data: tagsResponse } = useQuery({
    queryKey: ['tags', 'edit-form'],
    queryFn: () => api.getTags(),
  })

  const refreshMediaMutation = useMutation({
    mutationFn: () => api.getMediaByEntity('article', id),
    onSuccess: (response) => {
      if (response.success && response.data) {
        setMediaItems(response.data)
      }
    },
  })

  const deleteMediaMutation = useMutation({
    mutationFn: (mediaId: string) => api.deleteMediaItem(mediaId),
    onSuccess: (_, mediaId) => {
      setMediaItems((current) => current.filter((item) => item.id !== mediaId))
    },
    onSettled: () => {
      setDeletingId(null)
    },
  })

  const updateMediaMutation = useMutation({
    mutationFn: ({
      mediaId,
      title,
      description,
      altText,
    }: {
      mediaId: string
      title?: string
      description?: string
      altText?: string
    }) =>
      api.updateMediaMetadata(mediaId, {
        title: title || undefined,
        description: description || undefined,
        altText: altText || undefined,
      }),
    onSuccess: (response, variables) => {
      if (response.success && response.data) {
        setMediaItems((current) =>
          current.map((item) => (item.id === variables.mediaId ? response.data! : item)),
        )
      }
    },
    onSettled: () => {
      setUpdatingId(null)
    },
  })

  useEffect(() => {
    if (!data?.success || !data.data) return

    const article = data.data
    setFormData({
      title: article.title,
      titleEn: article.titleEn ?? '',
      content: article.content,
      contentEn: article.contentEn ?? '',
      summary: article.summary ?? '',
      summaryEn: article.summaryEn ?? '',
      keywordsEn: article.keywordsEn ?? '',
      status: article.status,
      visibility: article.visibility,
      categoryId: article.categoryId ?? '',
      tags: article.tags?.map((tag) => tag.name) ?? [],
    })
    setMediaItems(article.mediaItems ?? [])
  }, [data])

  const categories = categoriesResponse?.data?.items ?? []
  const availableTags = tagsResponse?.data?.items ?? []
  const coverItems = useMemo(() => mediaItems.filter((item) => item.collectionName === 'cover'), [mediaItems])
  const attachmentItems = useMemo(() => mediaItems.filter((item) => item.collectionName === 'attachments'), [mediaItems])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  const handleAddTag = () => {
    const value = tagInput.trim()
    if (!value) return

    const matchingTag = availableTags.find((tag) => tag.name.toLowerCase() === value.toLowerCase())
    if (!matchingTag) {
      alert('Tag not found in the system. Please create it from admin before using it.')
      return
    }

    if (formData.tags.includes(matchingTag.name)) return

    setFormData((prev) => ({ ...prev, tags: [...prev.tags, matchingTag.name] }))
    setTagInput('')
  }

  const handleRemoveTag = (tagToRemove: string) => {
    setFormData((prev) => ({ ...prev, tags: prev.tags.filter((tag) => tag !== tagToRemove) }))
  }

  const textToHtml = (text: string) => {
    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')

    return escaped
      .split(/\n{2,}/)
      .map((paragraph) => `<p>${paragraph.replace(/\n/g, '<br/>')}</p>`)
      .join('')
  }

  const handleDeleteMedia = (item: MediaItemDto) => {
    setDeletingId(item.id)
    deleteMediaMutation.mutate(item.id)
  }

  const handleSubmit = (nextStatus?: typeof formData.status) => {
    updateArticleMutation.mutate(
      {
        id,
        data: {
          title: formData.title,
          titleEn: formData.titleEn || undefined,
          content: formData.content,
          contentEn: formData.contentEn || undefined,
          summary: formData.summary || undefined,
          summaryEn: formData.summaryEn || undefined,
          keywordsEn: formData.keywordsEn || undefined,
          status: nextStatus ?? formData.status,
          visibility: formData.visibility,
          categoryId: formData.categoryId || undefined,
          tagIds: availableTags
            .filter((tag) => formData.tags.includes(tag.name))
            .map((tag) => tag.id),
        },
      },
      {
        onSuccess: (response) => {
          if (response.success) {
            clearDraft()
            queryClient.invalidateQueries({ queryKey: ['article', id] })
            navigate({ to: '/articles/$id', params: { id } })
          }
        },
      },
    )
  }

  if (isLoading) {
    return <div className="flex min-h-screen items-center justify-center bg-gray-50 text-gray-500">กำลังโหลด...</div>
  }

  if (isError || !data?.success || !data.data) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <div className="text-center">
          <p className="text-lg text-red-600">ไม่พบบทความ</p>
          <Link to="/articles" className="mt-3 inline-block text-blue-600 hover:underline">กลับสู่รายการบทความ</Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
            <span className="text-sm text-gray-500">Edit Article</span>
            <span className="ml-4 text-xs text-gray-400">บันทึกอัตโนมัติ</span>
          </div>
          <nav className="flex items-center gap-4">
            <Link to="/articles/$id" params={{ id }} className="text-gray-700 hover:text-gray-900">Preview</Link>
            <Link to="/articles" className="text-gray-700 hover:text-gray-900">Articles</Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-7xl py-6 sm:px-6 lg:px-8">
        <div className="grid gap-6 px-4 sm:px-0 lg:grid-cols-[minmax(0,2fr)_minmax(320px,1fr)]">
          <div className="space-y-6">
            <div className="overflow-hidden rounded-lg bg-white shadow">
              <div className="border-b px-6 py-4">
                <h2 className="text-2xl font-bold text-gray-900">Edit Knowledge Article</h2>
                <p className="mt-1 text-sm text-gray-600">อัปเดตเนื้อหาและจัดการไฟล์แนบของบทความนี้</p>
              </div>

              <div className="space-y-8 p-6">
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <div className="space-y-4">
                    <div>
                      <label htmlFor="title" className="mb-1 block text-sm font-medium text-gray-700">Title (Thai)</label>
                      <input id="title" name="title" value={formData.title} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2" />
                    </div>
                    <div>
                      <label htmlFor="titleEn" className="mb-1 block text-sm font-medium text-gray-700">Title (English)</label>
                      <input id="titleEn" name="titleEn" value={formData.titleEn} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2" />
                    </div>
                    <div>
                      <label htmlFor="summary" className="mb-1 block text-sm font-medium text-gray-700">Summary (Thai)</label>
                      <textarea id="summary" name="summary" rows={4} value={formData.summary} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2" />
                    </div>
                    <div>
                      <label htmlFor="summaryEn" className="mb-1 block text-sm font-medium text-gray-700">Summary (English)</label>
                      <textarea id="summaryEn" name="summaryEn" rows={4} value={formData.summaryEn} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2" />
                    </div>
                  </div>

                  <div className="space-y-4">
                    <div>
                      <label htmlFor="status" className="mb-1 block text-sm font-medium text-gray-700">Status</label>
                      <select id="status" name="status" value={formData.status} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2">
                        <option value="draft">Draft</option>
                        <option value="under_review">Under Review</option>
                        <option value="published">Published</option>
                        <option value="archived">Archived</option>
                      </select>
                    </div>
                    <div>
                      <label htmlFor="visibility" className="mb-1 block text-sm font-medium text-gray-700">Visibility</label>
                      <select id="visibility" name="visibility" value={formData.visibility} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2">
                        <option value="public">Public</option>
                        <option value="internal">Internal</option>
                        <option value="restricted">Restricted</option>
                      </select>
                    </div>
                    <div>
                      <label htmlFor="categoryId" className="mb-1 block text-sm font-medium text-gray-700">Category</label>
                      <select id="categoryId" name="categoryId" value={formData.categoryId} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2">
                        <option value="">Select category</option>
                        {categories.map((category) => (
                          <option key={category.id} value={category.id}>{category.name}</option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label htmlFor="keywordsEn" className="mb-1 block text-sm font-medium text-gray-700">Keywords (English)</label>
                      <input id="keywordsEn" name="keywordsEn" value={formData.keywordsEn} onChange={handleChange} className="w-full rounded-md border border-gray-300 px-3 py-2" />
                    </div>
                    <div>
                      <label className="mb-1 block text-sm font-medium text-gray-700">Tags</label>
                      <div className="flex">
                        <input
                          type="text"
                          value={tagInput}
                          onChange={(event) => setTagInput(event.target.value)}
                          onKeyDown={(event) => {
                            if (event.key === 'Enter') {
                              event.preventDefault()
                              handleAddTag()
                            }
                          }}
                          className="flex-1 rounded-l-md border border-gray-300 px-3 py-2"
                        />
                        <button type="button" onClick={handleAddTag} className="rounded-r-md border border-l-0 border-gray-300 bg-gray-100 px-4 py-2 text-sm">Add</button>
                      </div>
                      {formData.tags.length > 0 && (
                        <div className="mt-2 flex flex-wrap gap-2">
                          {formData.tags.map((tag) => (
                            <span key={tag} className="inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-sm text-blue-800">
                              {tag}
                              <button type="button" onClick={() => handleRemoveTag(tag)} className="ml-2 text-blue-600">×</button>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">Content (Thai)</label>
                    <RichTextEditor value={formData.content} onChange={(content) => setFormData((prev) => ({ ...prev, content }))} placeholder="Write your article content in Thai..." />
                  </div>
                  <div>
                    <label className="mb-1 block text-sm font-medium text-gray-700">Content (English)</label>
                    <RichTextEditor value={formData.contentEn} onChange={(contentEn) => setFormData((prev) => ({ ...prev, contentEn }))} placeholder="Write your article content in English..." />
                  </div>
                </div>

                <AiWritingPanel
                  title={formData.title}
                  content={formData.content}
                  contentEn={formData.contentEn}
                  onApplyDraft={(nextContent) => setFormData((prev) => ({ ...prev, content: textToHtml(nextContent) }))}
                  onApplyImproved={(nextContent) => setFormData((prev) => ({ ...prev, content: textToHtml(nextContent) }))}
                  onApplyTranslated={(nextContentEn) => setFormData((prev) => ({ ...prev, contentEn: textToHtml(nextContentEn) }))}
                  onApplyTags={(suggestedTags) => {
                    setFormData((prev) => {
                      const merged = [...prev.tags]
                      for (const rawTag of suggestedTags) {
                        const tag = rawTag.trim()
                        if (tag && !merged.includes(tag)) merged.push(tag)
                      }
                      return { ...prev, tags: merged }
                    })
                  }}
                />
              </div>
            </div>
          </div>

          <aside className="space-y-6">
            <div className="rounded-lg bg-white p-6 shadow">
              <h3 className="text-lg font-semibold text-gray-900">Media Management</h3>
              <p className="mt-1 text-sm text-gray-600">จัดการรูปปกและไฟล์แนบของบทความแบบแยกคอลเลกชัน</p>

              <div className="mt-5 space-y-6">
                <div>
                  <p className="mb-2 text-sm font-medium text-gray-700">Cover image</p>
                  <MediaUpload
                    entityType="article"
                    entityId={id}
                    collection="cover"
                    multiple={false}
                    accept="image/*"
                    onUploaded={(item) => {
                      setMediaItems((current) => [item, ...current.filter((media) => media.collectionName !== 'cover')])
                      refreshMediaMutation.mutate()
                    }}
                  />
                  <div className="mt-3">
                    <MediaGallery
                      items={coverItems}
                      deletingId={deletingId}
                      updatingId={updatingId}
                      onDelete={handleDeleteMedia}
                      onUpdate={(item, updates) => {
                        setUpdatingId(item.id)
                        updateMediaMutation.mutate({
                          mediaId: item.id,
                          title: updates.title,
                          description: updates.description,
                          altText: updates.altText,
                        })
                      }}
                    />
                  </div>
                </div>

                <div>
                  <p className="mb-2 text-sm font-medium text-gray-700">Attachments</p>
                  <MediaUpload
                    entityType="article"
                    entityId={id}
                    collection="attachments"
                    onUploaded={(item) => {
                      setMediaItems((current) => [item, ...current])
                      refreshMediaMutation.mutate()
                    }}
                  />
                  <div className="mt-3">
                    <MediaGallery
                      items={attachmentItems}
                      deletingId={deletingId}
                      updatingId={updatingId}
                      onDelete={handleDeleteMedia}
                      onUpdate={(item, updates) => {
                        setUpdatingId(item.id)
                        updateMediaMutation.mutate({
                          mediaId: item.id,
                          title: updates.title,
                          description: updates.description,
                          altText: updates.altText,
                        })
                      }}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="rounded-lg bg-white p-6 shadow">
              <div className="flex flex-col gap-3">
                <button
                  type="button"
                  onClick={() => handleSubmit()}
                  disabled={updateArticleMutation.isPending}
                  className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
                >
                  {updateArticleMutation.isPending ? 'Saving...' : 'Save Changes'}
                </button>
                <button
                  type="button"
                  onClick={() => handleSubmit('under_review')}
                  disabled={updateArticleMutation.isPending}
                  className="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                >
                  Submit for Review
                </button>
                <Link to="/articles/$id" params={{ id }} className="rounded-lg border border-gray-300 px-4 py-2 text-center text-sm font-medium text-gray-700 hover:bg-gray-50">
                  Cancel
                </Link>
              </div>
            </div>
          </aside>
        </div>
      </main>
    </div>
  )
}
