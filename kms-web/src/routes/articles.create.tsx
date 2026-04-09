import { createFileRoute, Link } from '@tanstack/react-router'
import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAutosave } from '../hooks/useAutosave'
import { RichTextEditor } from '../components/RichTextEditor'
import { AiWritingPanel } from '../components/AiWritingPanel'
import { MediaUpload } from '../components/MediaUpload'
import { useCreateArticle } from '../hooks/useArticles'
import { api } from '../services/api'
import type { MediaItemDto } from '../types/api'

export const Route = createFileRoute('/articles/create')({
  component: CreateArticlePage,
})

function CreateArticlePage() {
  const [formData, setFormData] = useState({
    title: '',
    titleEn: '',
    content: '<p>Write your article content here...</p>',
    contentEn: '<p>Write your English article content here...</p>',
    summary: '',
    summaryEn: '',
    keywordsEn: '',
    status: 'draft' as 'draft' | 'under_review' | 'published' | 'archived',
    visibility: 'internal' as 'public' | 'internal' | 'restricted',
    categoryId: '',
    tags: [] as string[],
  })

  const [tagInput, setTagInput] = useState('')
  const [createdArticleId, setCreatedArticleId] = useState<string | null>(null)
  const [uploadedMedia, setUploadedMedia] = useState<MediaItemDto[]>([])
  const createArticleMutation = useCreateArticle()
  const { data: categoriesResponse } = useQuery({ queryKey: ['categories', 'create-form'], queryFn: () => api.getCategories() })
  const { data: tagsResponse } = useQuery({ queryKey: ['tags', 'create-form'], queryFn: () => api.getTags() })

  const { load: loadDraft, clear: clearDraft } = useAutosave('new-article', {
    title: formData.title,
    content: formData.content,
    summary: formData.summary,
    savedAt: new Date().toISOString(),
  })

  useEffect(() => {
    const draft = loadDraft()
    if (draft && (draft.title || draft.content)) {
      setFormData((prev) => ({
        ...prev,
        title: draft.title || prev.title,
        content: draft.content || prev.content,
        summary: draft.summary || prev.summary,
      }))
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const categories = categoriesResponse?.data?.items ?? []
  const availableTags = tagsResponse?.data?.items ?? []

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: value }))
  }

  const handleAddTag = () => {
    const normalized = tagInput.trim()
    if (!normalized) return

    const matchingTag = availableTags.find((tag) => tag.name.toLowerCase() === normalized.toLowerCase())
    if (!matchingTag) {
      alert('Tag not found in the system. Please create it from admin before using it.')
      return
    }

    if (!formData.tags.includes(matchingTag.name)) {
      setFormData(prev => ({
        ...prev,
        tags: [...prev.tags, matchingTag.name]
      }))
      setTagInput('')
    }
  }

  const handleRemoveTag = (tagToRemove: string) => {
    setFormData(prev => ({
      ...prev,
      tags: prev.tags.filter(tag => tag !== tagToRemove)
    }))
  }

  const textToHtml = (text: string) => {
    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
    return escaped
      .split(/\n{2,}/)
      .map((p) => `<p>${p.replace(/\n/g, '<br/>')}</p>`)
      .join('')
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    const articleData = {
      title: formData.title,
      titleEn: formData.titleEn || undefined,
      content: formData.content,
      contentEn: formData.contentEn || undefined,
      summary: formData.summary || undefined,
      summaryEn: formData.summaryEn || undefined,
      keywordsEn: formData.keywordsEn || undefined,
      status: formData.status,
      visibility: formData.visibility,
      categoryId: formData.categoryId || undefined,
      tagIds: availableTags
        .filter((tag) => formData.tags.includes(tag.name))
        .map((tag) => tag.id),
    }
    
    createArticleMutation.mutate(articleData, {
      onSuccess: (data) => {
        if (data.success && data.data) {
          clearDraft()
          setCreatedArticleId(data.data.id)
        } else {
          alert(data.message || 'Failed to create article')
        }
      },
      onError: (error) => {
        console.error('Create article error:', error)
        alert('Failed to create article. Please try again.')
      },
    })
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
              <span className="ml-2 text-sm text-gray-500">Create Article</span>
              <span className="ml-4 text-xs text-gray-400">บันทึกอัตโนมัติ</span>
            </div>
            <nav className="flex items-center space-x-4">
              <Link to="/" className="text-gray-700 hover:text-gray-900">Home</Link>
              <Link to="/articles" className="text-blue-600 font-medium">Articles</Link>
            </nav>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">

          {/* Step 2: Uploaded successfully → show media upload panel */}
          {createdArticleId ? (
            <div className="bg-white rounded-lg shadow p-8 space-y-6">
              <div className="flex items-center gap-3 text-green-700">
                <span className="text-2xl">✅</span>
                <h2 className="text-xl font-semibold">บทความถูกสร้างเรียบร้อยแล้ว!</h2>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">อัปโหลดรูปปก / ไฟล์แนบ</h3>
                <div className="mb-4">
                  <p className="text-xs text-gray-500 mb-2 font-medium">รูปปก (Cover Image)</p>
                  <MediaUpload
                    entityType="article"
                    entityId={createdArticleId}
                    collection="cover"
                    onUploaded={(item) => setUploadedMedia((prev) => [item, ...prev])}
                  />
                </div>
                <div>
                  <p className="text-xs text-gray-500 mb-2 font-medium">ไฟล์แนบ (Attachments)</p>
                  <MediaUpload
                    entityType="article"
                    entityId={createdArticleId}
                    collection="attachments"
                    onUploaded={(item) => setUploadedMedia((prev) => [item, ...prev])}
                  />
                </div>
                {uploadedMedia.length > 0 && (
                  <ul className="mt-3 space-y-1">
                    {uploadedMedia.map((m) => (
                      <li key={m.id} className="text-sm text-gray-600">
                        ✓ {m.originalFileName}
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div className="flex gap-3 pt-4 border-t">
                <Link
                  to="/articles/$id"
                  params={{ id: createdArticleId }}
                  className="px-5 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700"
                >
                  ดูบทความ →
                </Link>
                <Link
                  to="/articles"
                  className="px-5 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm font-medium hover:bg-gray-50"
                >
                  กลับสู่รายการบทความ
                </Link>
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <div className="px-6 py-4 border-b">
                <h2 className="text-2xl font-bold text-gray-900">Create New Knowledge Article</h2>
                <p className="text-sm text-gray-600 mt-1">Fill in the details below to create a new knowledge article</p>
              </div>

              <form onSubmit={handleSubmit} className="p-6 space-y-8">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div>
                    <label htmlFor="title" className="block text-sm font-medium text-gray-700 mb-1">
                      Title (Thai) *
                    </label>
                    <input
                      type="text"
                      id="title"
                      name="title"
                      required
                      value={formData.title}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      placeholder="หัวข้อบทความภาษาไทย"
                    />
                  </div>

                  <div>
                    <label htmlFor="titleEn" className="block text-sm font-medium text-gray-700 mb-1">
                      Title (English)
                    </label>
                    <input
                      type="text"
                      id="titleEn"
                      name="titleEn"
                      value={formData.titleEn}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      placeholder="Article title in English"
                    />
                  </div>

                  <div>
                    <label htmlFor="summary" className="block text-sm font-medium text-gray-700 mb-1">
                      Summary (Thai)
                    </label>
                    <textarea
                      id="summary"
                      name="summary"
                      rows={3}
                      value={formData.summary}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      placeholder="สรุปย่อบทความภาษาไทย"
                    />
                  </div>

                  <div>
                    <label htmlFor="summaryEn" className="block text-sm font-medium text-gray-700 mb-1">
                      Summary (English)
                    </label>
                    <textarea
                      id="summaryEn"
                      name="summaryEn"
                      rows={3}
                      value={formData.summaryEn}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      placeholder="Article summary in English"
                    />
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
                      Status
                    </label>
                    <select
                      id="status"
                      name="status"
                      value={formData.status}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                    >
                      <option value="draft">Draft</option>
                      <option value="under_review">Under Review</option>
                      <option value="published">Published</option>
                      <option value="archived">Archived</option>
                    </select>
                  </div>

                  <div>
                    <label htmlFor="visibility" className="block text-sm font-medium text-gray-700 mb-1">
                      Visibility
                    </label>
                    <select
                      id="visibility"
                      name="visibility"
                      value={formData.visibility}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                    >
                      <option value="public">Public</option>
                      <option value="internal">Internal</option>
                      <option value="restricted">Restricted</option>
                    </select>
                  </div>

                  <div>
                    <label htmlFor="categoryId" className="block text-sm font-medium text-gray-700 mb-1">
                      Category
                    </label>
                    <select
                      id="categoryId"
                      name="categoryId"
                      value={formData.categoryId}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                    >
                      <option value="">Select Category</option>
                      {categories.map((category) => (
                        <option key={category.id} value={category.id}>{category.name}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label htmlFor="keywordsEn" className="block text-sm font-medium text-gray-700 mb-1">
                      Keywords (English)
                    </label>
                    <input
                      type="text"
                      id="keywordsEn"
                      name="keywordsEn"
                      value={formData.keywordsEn}
                      onChange={handleChange}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      placeholder="keyword1, keyword2, keyword3"
                    />
                    <p className="text-xs text-gray-500 mt-1">Separate keywords with commas</p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Tags
                    </label>
                    <div className="flex">
                      <input
                        type="text"
                        value={tagInput}
                        onChange={(e) => setTagInput(e.target.value)}
                        onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddTag())}
                        className="flex-1 px-3 py-2 border border-gray-300 rounded-l-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                        placeholder="Add a tag"
                      />
                      <button
                        type="button"
                        onClick={handleAddTag}
                        className="px-4 py-2 bg-gray-200 text-gray-700 hover:bg-gray-300 border border-gray-300 border-l-0 rounded-r-md"
                      >
                        Add
                      </button>
                    </div>
                    {formData.tags.length > 0 && (
                      <div className="mt-2 flex flex-wrap gap-2">
                        {formData.tags.map(tag => (
                          <span
                            key={tag}
                            className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-blue-100 text-blue-800"
                          >
                            {tag}
                            <button
                              type="button"
                              onClick={() => handleRemoveTag(tag)}
                              className="ml-2 text-blue-600 hover:text-blue-800"
                            >
                              ×
                            </button>
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Content (Thai) *
                  </label>
                  <RichTextEditor
                    value={formData.content}
                    onChange={(content) => setFormData(prev => ({ ...prev, content }))}
                    placeholder="Write your article content in Thai..."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Content (English)
                  </label>
                  <RichTextEditor
                    value={formData.contentEn}
                    onChange={(contentEn) => setFormData(prev => ({ ...prev, contentEn }))}
                    placeholder="Write your article content in English..."
                  />
                </div>
              </div>

              <AiWritingPanel
                title={formData.title}
                content={formData.content}
                contentEn={formData.contentEn}
                onApplyDraft={(nextContent) => {
                  setFormData((prev) => ({
                    ...prev,
                    content: textToHtml(nextContent),
                  }))
                }}
                onApplyImproved={(nextContent) => {
                  setFormData((prev) => ({
                    ...prev,
                    content: textToHtml(nextContent),
                  }))
                }}
                onApplyTranslated={(nextContentEn) => {
                  setFormData((prev) => ({
                    ...prev,
                    contentEn: textToHtml(nextContentEn),
                  }))
                }}
                onApplyTags={(suggestedTags) => {
                  setFormData((prev) => {
                    const merged = [...prev.tags]
                    for (const rawTag of suggestedTags) {
                      const tag = rawTag.trim()
                      if (tag && !merged.includes(tag)) {
                        merged.push(tag)
                      }
                    }
                    return { ...prev, tags: merged }
                  })
                }}
              />

              <div className="flex justify-end space-x-4 pt-6 border-t">
                <a
                  href="/articles"
                  className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </a>
                <button
                  type="submit"
                  disabled={createArticleMutation.isPending}
                  className="px-6 py-2 bg-blue-600 text-white rounded-md text-sm font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {createArticleMutation.isPending ? 'Saving...' : 'Save Article'}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setFormData(prev => ({ ...prev, status: 'under_review' }))
                    const articleData = {
                      title: formData.title,
                      titleEn: formData.titleEn || undefined,
                      content: formData.content,
                      contentEn: formData.contentEn || undefined,
                      summary: formData.summary || undefined,
                      summaryEn: formData.summaryEn || undefined,
                      keywordsEn: formData.keywordsEn || undefined,
                      status: 'under_review' as const,
                      visibility: formData.visibility,
                      categoryId: formData.categoryId || undefined,
                      tagIds: availableTags
                        .filter((tag) => formData.tags.includes(tag.name))
                        .map((tag) => tag.id),
                    }
                    
                    createArticleMutation.mutate(articleData, {
                      onSuccess: (data) => {
                        if (data.success && data.data) {
                          clearDraft()
                          setCreatedArticleId(data.data.id)
                        } else {
                          alert(data.message || 'Failed to submit article for review')
                        }
                      },
                    })
                  }}
                  className="px-6 py-2 bg-green-600 text-white rounded-md text-sm font-medium hover:bg-green-700"
                >
                  Submit for Review
                </button>
              </div>
              </form>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}