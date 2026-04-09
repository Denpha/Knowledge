import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { useArticle } from '../hooks/useArticles'
import { useMyReaction, useReactToArticle } from '../hooks/useArticles'
import { useComments, useCreateComment, useDeleteComment, useLikeComment } from '../hooks/useComments'
import { api } from '../services/api'
import { MediaUpload } from '../components/MediaUpload'
import type { MediaItemDto } from '../types/api'

export const Route = createFileRoute('/articles/$id')({
  component: ArticleDetailPage,
})

function ArticleDetailPage() {
  const { id } = Route.useParams()
  const { data, isLoading, isError } = useArticle(id)
  const isAuthenticated = api.isAuthenticated()
  const [commentText, setCommentText] = useState('')
  const [replyTo, setReplyTo] = useState<{ id: string; authorName: string } | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editText, setEditText] = useState('')
  const [uploadedMedia, setUploadedMedia] = useState<MediaItemDto[]>([])

  const { data: commentsData } = useComments(id)
  const createComment = useCreateComment(id)
  const deleteComment = useDeleteComment(id)
  const likeComment = useLikeComment(id)
  const { data: reactionData } = useMyReaction(id, isAuthenticated)
  const reactToArticle = useReactToArticle(id)

  const reaction = reactionData?.data

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-gray-500">กำลังโหลด...</div>
      </div>
    )
  }

  if (isError || !data?.success || !data.data) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-500 text-lg mb-2">ไม่พบบทความ</div>
          <Link to="/articles" className="text-blue-600 hover:underline">
            กลับสู่รายการบทความ
          </Link>
        </div>
      </div>
    )
  }

  const article = data.data
  const allMedia = [...(article.mediaItems ?? []), ...uploadedMedia]

  const coverImage = allMedia.find(m => m.collectionName === 'cover')
  const attachments = allMedia.filter(m => m.collectionName === 'attachments')

  const statusColors: Record<string, string> = {
    draft: 'bg-gray-100 text-gray-800',
    under_review: 'bg-yellow-100 text-yellow-800',
    published: 'bg-green-100 text-green-800',
    archived: 'bg-red-100 text-red-800',
  }
  const statusLabels: Record<string, string> = {
    draft: 'ร่าง',
    under_review: 'รอตรวจสอบ',
    published: 'เผยแพร่แล้ว',
    archived: 'เก็บถาวร',
  }
  const visibilityLabels: Record<string, string> = {
    public: 'สาธารณะ',
    internal: 'ภายในองค์กร',
    restricted: 'จำกัดสิทธิ์',
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '-'
    return new Date(dateStr).toLocaleDateString('th-TH', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
            </div>
            <nav className="flex items-center space-x-4">
              <Link to="/" className="text-gray-700 hover:text-gray-900">Home</Link>
              <Link to="/articles" className="text-gray-700 hover:text-gray-900">Articles</Link>
            </nav>
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto py-6 px-4 sm:px-6 lg:px-8">
        {/* Breadcrumb */}
        <nav className="flex items-center space-x-2 text-sm text-gray-500 mb-6">
          <Link to="/articles" className="hover:text-blue-600">
            บทความ
          </Link>
          <span>/</span>
          <span className="text-gray-900 truncate max-w-xs">{article.title}</span>
        </nav>

        <div className="bg-white rounded-lg shadow overflow-hidden">
          {/* Cover Image */}
          {coverImage && (
            <div className="w-full h-64 overflow-hidden bg-gray-100">
              <img
                src={coverImage.url}
                alt={article.title}
                className="w-full h-full object-cover"
              />
            </div>
          )}

          <div className="p-6 lg:p-8">
            {/* Status & Visibility Badges */}
            <div className="flex items-center gap-2 mb-4">
              <span
                className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                  statusColors[article.status] ?? 'bg-gray-100 text-gray-800'
                }`}
              >
                {statusLabels[article.status] ?? article.status}
              </span>
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                {visibilityLabels[article.visibility] ?? article.visibility}
              </span>
            </div>

            {/* Title */}
            <h2 className="text-3xl font-bold text-gray-900 mb-2">{article.title}</h2>
            {article.titleEn && (
              <p className="text-xl text-gray-500 mb-4">{article.titleEn}</p>
            )}

            {/* Summary */}
            {article.summary && (
              <p className="text-gray-600 text-base italic border-l-4 border-blue-200 pl-4 mb-6">
                {article.summary}
              </p>
            )}

            {/* Meta row */}
            <div className="flex flex-wrap items-center gap-4 text-sm text-gray-500 border-t border-b py-3 mb-6">
              <div>
                <span className="font-medium text-gray-700">ผู้เขียน:</span>{' '}
                {article.createdBy?.fullNameTh ?? '-'}
              </div>
              {article.category && (
                <div>
                  <span className="font-medium text-gray-700">หมวดหมู่:</span>{' '}
                  {article.category.name}
                </div>
              )}
              <div>
                <span className="font-medium text-gray-700">วันที่เผยแพร่:</span>{' '}
                {formatDate(article.publishedAt ?? article.createdAt)}
              </div>
              <div className="flex items-center gap-3 ml-auto">
                <span title="จำนวนเข้าชม">👁 {article.viewCount ?? 0}</span>
                <button
                  title={reaction?.userLiked ? 'ยกเลิกถูกใจ' : 'ถูกใจ'}
                  disabled={!isAuthenticated || reactToArticle.isPending}
                  onClick={() => reactToArticle.mutate('Like')}
                  className={`flex items-center gap-1 text-sm transition-colors ${
                    reaction?.userLiked
                      ? 'text-red-500 font-semibold'
                      : 'text-gray-400 hover:text-red-400'
                  } disabled:opacity-50 disabled:cursor-not-allowed`}
                >
                  {reaction?.userLiked ? '❤️' : '🤍'}{' '}
                  {reaction?.likeCount ?? article.likeCount ?? 0}
                </button>
                <button
                  title={reaction?.userBookmarked ? 'ยกเลิกบุ๊กมาร์ก' : 'บุ๊กมาร์ก'}
                  disabled={!isAuthenticated || reactToArticle.isPending}
                  onClick={() => reactToArticle.mutate('Bookmark')}
                  className={`flex items-center gap-1 text-sm transition-colors ${
                    reaction?.userBookmarked
                      ? 'text-yellow-500 font-semibold'
                      : 'text-gray-400 hover:text-yellow-500'
                  } disabled:opacity-50 disabled:cursor-not-allowed`}
                >
                  {reaction?.userBookmarked ? '🔖' : '📄'}{' '}
                  {reaction?.bookmarkCount ?? article.bookmarkCount ?? 0}
                </button>
                <span title="ความคิดเห็น">💬 {article.commentCount ?? 0}</span>
                <button
                  title="ดาวน์โหลด PDF"
                  onClick={async () => {
                    try { await api.exportArticlePdf(id) }
                    catch { alert('ไม่สามารถส่งออก PDF ได้ กรุณาลองใหม่') }
                  }}
                  className="flex items-center gap-1 text-sm text-gray-400 hover:text-red-600 transition-colors"
                >
                  📄 PDF
                </button>
              </div>
            </div>

            {/* Tags */}
            {article.tags && article.tags.length > 0 && (
              <div className="flex flex-wrap gap-2 mb-6">
                {article.tags.map(tag => (
                  <span
                    key={tag.id}
                    className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-blue-50 text-blue-700 border border-blue-100"
                  >
                    #{tag.name}
                  </span>
                ))}
              </div>
            )}

            {/* Main Content */}
            <div
              className="article-content prose max-w-none text-gray-800 leading-relaxed"
              dangerouslySetInnerHTML={{ __html: article.content }}
            />

            {/* English Content (if present) */}
            {article.contentEn && (
              <details className="mt-8 border rounded-lg p-4 bg-gray-50">
                <summary className="font-medium text-gray-700 cursor-pointer select-none">
                  English Content
                </summary>
                <div
                  className="article-content prose max-w-none text-gray-800 leading-relaxed mt-4"
                  dangerouslySetInnerHTML={{ __html: article.contentEn }}
                />
              </details>
            )}

            {/* Attachment list */}
            {attachments.length > 0 && (
              <div className="mt-8 border-t pt-6">
                <h3 className="text-sm font-semibold text-gray-700 mb-3">ไฟล์แนบ</h3>
                <ul className="space-y-2">
                  {attachments.map(media => (
                      <li key={media.id}>
                        <a
                          href={media.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:underline text-sm"
                        >
                          {media.originalFileName}
                        </a>
                      </li>
                    ))}
                </ul>
              </div>
            )}

            {isAuthenticated && (
              <div className="mt-8 border-t pt-6">
                <h3 className="text-sm font-semibold text-gray-700 mb-3">อัปโหลดไฟล์แนบ</h3>
                <MediaUpload
                  entityType="article"
                  entityId={id}
                  collection="attachments"
                  onUploaded={(item) => {
                    setUploadedMedia((prev) => [item, ...prev])
                  }}
                />
              </div>
            )}

            {/* Bottom actions */}
            <div className="mt-8 pt-6 border-t flex justify-between items-center">
              <Link
                to="/articles"
                className="text-blue-600 hover:text-blue-800 text-sm font-medium"
              >
                ← กลับสู่รายการบทความ
              </Link>
              <Link
                to="/articles/$id/edit"
                params={{ id }}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 text-sm"
              >
                แก้ไขบทความ
              </Link>
            </div>
          </div>
        </div>

        {/* Comments Section */}
        <div className="bg-white rounded-lg shadow mt-6 p-6 lg:p-8">
          <h3 className="text-lg font-semibold text-gray-900 mb-6">
            ความคิดเห็น ({commentsData?.data?.totalCount ?? 0})
          </h3>

          {/* New Comment Form */}
          {isAuthenticated ? (
            <form
              className="mb-8"
              onSubmit={(e) => {
                e.preventDefault()
                if (!commentText.trim()) return
                createComment.mutate(
                  {
                    content: commentText.trim(),
                    articleId: id,
                    parentId: replyTo?.id,
                  },
                  {
                    onSuccess: () => {
                      setCommentText('')
                      setReplyTo(null)
                    },
                  },
                )
              }}
            >
              {replyTo && (
                <div className="flex items-center gap-2 text-sm text-blue-700 bg-blue-50 rounded px-3 py-1.5 mb-2">
                  <span>ตอบกลับ {replyTo.authorName}</span>
                  <button
                    type="button"
                    onClick={() => setReplyTo(null)}
                    className="ml-auto text-gray-400 hover:text-gray-600"
                  >
                    ✕
                  </button>
                </div>
              )}
              <textarea
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                rows={3}
                placeholder={replyTo ? `ตอบกลับ ${replyTo.authorName}...` : 'เขียนความคิดเห็น...'}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                maxLength={2000}
              />
              <div className="flex justify-between items-center mt-2">
                <span className="text-xs text-gray-400">{commentText.length}/2000</span>
                <button
                  type="submit"
                  disabled={!commentText.trim() || createComment.isPending}
                  className="bg-blue-600 text-white px-4 py-1.5 rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {createComment.isPending ? 'กำลังส่ง...' : 'ส่งความคิดเห็น'}
                </button>
              </div>
            </form>
          ) : (
            <div className="mb-8 p-4 bg-gray-50 rounded-lg text-sm text-gray-600 text-center">
              <Link to="/login" className="text-blue-600 hover:underline font-medium">เข้าสู่ระบบ</Link>
              {' '}เพื่อแสดงความคิดเห็น
            </div>
          )}

          {/* Comment List */}
          <div className="space-y-6">
            {commentsData?.data?.items.length === 0 && (
              <div className="text-center text-gray-400 py-8 text-sm">ยังไม่มีความคิดเห็น</div>
            )}
            {commentsData?.data?.items.map((comment) => (
              <div key={comment.id} className="flex gap-3">
                <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-semibold text-sm flex-shrink-0">
                  {comment.authorName.charAt(0).toUpperCase()}
                </div>
                <div className="flex-1">
                  <div className="bg-gray-50 rounded-lg px-4 py-3">
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm font-medium text-gray-900">{comment.authorName}</span>
                      <span className="text-xs text-gray-400">
                        {new Date(comment.createdAt).toLocaleDateString('th-TH')}
                      </span>
                    </div>
                    {editingId === comment.id ? (
                      <div>
                        <textarea
                          value={editText}
                          onChange={(e) => setEditText(e.target.value)}
                          rows={2}
                          className="w-full border border-gray-300 rounded px-2 py-1 text-sm resize-none"
                          maxLength={2000}
                        />
                        <div className="flex gap-2 mt-1">
                          <button
                            className="text-xs text-blue-600 hover:underline"
                            onClick={() => {
                              // TODO: wire up updateComment mutation
                              setEditingId(null)
                            }}
                          >
                            บันทึก
                          </button>
                          <button className="text-xs text-gray-500 hover:underline" onClick={() => setEditingId(null)}>
                            ยกเลิก
                          </button>
                        </div>
                      </div>
                    ) : (
                      <p className="text-sm text-gray-800 whitespace-pre-wrap">{comment.content}</p>
                    )}
                  </div>

                  {/* Comment actions */}
                  <div className="flex items-center gap-4 mt-1 px-1">
                    <button
                      className="text-xs text-gray-400 hover:text-red-500 flex items-center gap-1"
                      onClick={() => likeComment.mutate(comment.id)}
                    >
                      ❤ {comment.likeCount > 0 && comment.likeCount}
                    </button>
                    {isAuthenticated && (
                      <button
                        className="text-xs text-gray-400 hover:text-blue-600"
                        onClick={() => setReplyTo({ id: comment.id, authorName: comment.authorName })}
                      >
                        ตอบกลับ
                      </button>
                    )}
                    {isAuthenticated && comment.authorId === '' /* TODO: compare with current userId */ && (
                      <>
                        <button
                          className="text-xs text-gray-400 hover:text-green-600"
                          onClick={() => { setEditingId(comment.id); setEditText(comment.content) }}
                        >
                          แก้ไข
                        </button>
                        <button
                          className="text-xs text-gray-400 hover:text-red-600"
                          onClick={() => {
                            if (confirm('ลบความคิดเห็นนี้?')) deleteComment.mutate(comment.id)
                          }}
                        >
                          ลบ
                        </button>
                      </>
                    )}
                  </div>

                  {/* Replies */}
                  {comment.replies && comment.replies.length > 0 && (
                    <div className="mt-3 space-y-3 pl-4 border-l-2 border-gray-100">
                      {comment.replies.map((reply) => (
                        <div key={reply.id} className="flex gap-3">
                          <div className="w-7 h-7 rounded-full bg-green-100 flex items-center justify-center text-green-700 font-semibold text-xs flex-shrink-0">
                            {reply.authorName.charAt(0).toUpperCase()}
                          </div>
                          <div className="flex-1 bg-gray-50 rounded-lg px-3 py-2">
                            <div className="flex items-center justify-between mb-1">
                              <span className="text-xs font-medium text-gray-900">{reply.authorName}</span>
                              <span className="text-xs text-gray-400">
                                {new Date(reply.createdAt).toLocaleDateString('th-TH')}
                              </span>
                            </div>
                            <p className="text-sm text-gray-800 whitespace-pre-wrap">{reply.content}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  )
}
