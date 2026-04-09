import { useEffect, useRef, useState } from 'react'
import { api } from '../services/api'

interface AiWritingPanelProps {
  title: string
  content: string
  contentEn?: string
  onApplyDraft: (content: string) => void
  onApplyImproved: (content: string) => void
  onApplyTranslated: (contentEn: string) => void
  onApplyTags: (tags: string[]) => void
}

const IMPROVEMENT_OPTIONS = ['Grammar', 'Concise', 'Formal', 'Expand', 'Simplify'] as const

type ImprovementOption = (typeof IMPROVEMENT_OPTIONS)[number]

export function AiWritingPanel({
  title,
  content,
  contentEn,
  onApplyDraft,
  onApplyImproved,
  onApplyTranslated,
  onApplyTags,
}: AiWritingPanelProps) {
  const [isLoadingDraft, setIsLoadingDraft] = useState(false)
  const [isLoadingImprove, setIsLoadingImprove] = useState(false)
  const [isLoadingTranslate, setIsLoadingTranslate] = useState(false)
  const [isLoadingTags, setIsLoadingTags] = useState(false)
  const [isStreamingDraft, setIsStreamingDraft] = useState(false)
  const [improvementType, setImprovementType] = useState<ImprovementOption>('Grammar')
  const [streamedDraft, setStreamedDraft] = useState('')
  const [lastMessage, setLastMessage] = useState<string | null>(null)
  const [lastError, setLastError] = useState<string | null>(null)
  const streamRef = useRef<EventSource | null>(null)

  const toPlainText = (html: string) =>
    html
      .replace(/<br\s*\/?\s*>/gi, '\n')
      .replace(/<\/p>/gi, '\n\n')
      .replace(/<[^>]*>/g, '')
      .replace(/&nbsp;/g, ' ')
      .replace(/&amp;/g, '&')
      .replace(/&lt;/g, '<')
      .replace(/&gt;/g, '>')
      .trim()

  const runGenerateDraft = async () => {
    if (!title.trim()) {
      setLastError('กรุณาใส่หัวข้อบทความก่อนใช้ Generate Draft')
      return
    }

    setLastError(null)
    setLastMessage(null)
    setIsLoadingDraft(true)

    try {
      const response = await api.aiGenerateDraft(title, 'th')
      if (!response.success || !response.data?.content) {
        throw new Error(response.message ?? 'Generate draft failed')
      }

      onApplyDraft(response.data.content)
      setLastMessage('สร้าง Draft สำเร็จ และแทนที่เนื้อหาใน editor แล้ว')
    } catch (error: any) {
      setLastError(error.message ?? 'Generate draft failed')
    } finally {
      setIsLoadingDraft(false)
    }
  }

  const runImproveText = async () => {
    const plain = toPlainText(content)
    if (!plain) {
      setLastError('กรุณาใส่เนื้อหาภาษาไทยก่อนใช้ Improve Text')
      return
    }

    setLastError(null)
    setLastMessage(null)
    setIsLoadingImprove(true)

    try {
      const response = await api.aiImproveText(plain, improvementType)
      if (!response.success || !response.data?.content) {
        throw new Error(response.message ?? 'Improve text failed')
      }

      onApplyImproved(response.data.content)
      setLastMessage(`ปรับปรุงข้อความสำเร็จ (${improvementType}) และอัปเดตใน editor แล้ว`)
    } catch (error: any) {
      setLastError(error.message ?? 'Improve text failed')
    } finally {
      setIsLoadingImprove(false)
    }
  }

  const runTranslate = async () => {
    const source = toPlainText(content)
    if (!source) {
      setLastError('กรุณาใส่เนื้อหาภาษาไทยก่อนใช้ Auto Translate')
      return
    }

    setLastError(null)
    setLastMessage(null)
    setIsLoadingTranslate(true)

    try {
      const response = await api.aiTranslate(source, 'en')
      if (!response.success || !response.data?.content) {
        throw new Error(response.message ?? 'Translate failed')
      }

      onApplyTranslated(response.data.content)
      setLastMessage('แปลเป็นภาษาอังกฤษสำเร็จ และอัปเดตช่อง Content (English) แล้ว')
    } catch (error: any) {
      setLastError(error.message ?? 'Translate failed')
    } finally {
      setIsLoadingTranslate(false)
    }
  }

  const runSuggestTags = async () => {
    const source = (contentEn ? toPlainText(contentEn) : '') || toPlainText(content) || title.trim()
    if (!source) {
      setLastError('กรุณาใส่หัวข้อหรือเนื้อหาก่อนใช้ Suggest Tags')
      return
    }

    setLastError(null)
    setLastMessage(null)
    setIsLoadingTags(true)

    try {
      const response = await api.aiSuggestTags(source)
      if (!response.success || !response.data?.tags?.length) {
        throw new Error(response.message ?? 'Suggest tags failed')
      }

      onApplyTags(response.data.tags)
      setLastMessage(`แนะนำแท็กสำเร็จ (${response.data.tags.length} รายการ)`)
    } catch (error: any) {
      setLastError(error.message ?? 'Suggest tags failed')
    } finally {
      setIsLoadingTags(false)
    }
  }

  const stopStreaming = () => {
    streamRef.current?.close()
    streamRef.current = null
    setIsStreamingDraft(false)
  }

  useEffect(() => {
    return () => {
      streamRef.current?.close()
    }
  }, [])

  const runStreamDraft = () => {
    if (!title.trim()) {
      setLastError('กรุณาใส่หัวข้อบทความก่อนใช้ Stream Draft')
      return
    }

    const token = localStorage.getItem('auth_token')
    if (!token) {
      setLastError('กรุณาเข้าสู่ระบบก่อนใช้งาน Stream Draft')
      return
    }

    stopStreaming()
    setLastError(null)
    setLastMessage(null)
    setStreamedDraft('')
    setIsStreamingDraft(true)

    const prompt = `Generate a comprehensive Thai knowledge article draft about: ${title}`
    const url = `http://localhost:5000/api/ai/stream?prompt=${encodeURIComponent(prompt)}&access_token=${encodeURIComponent(token)}`
    const es = new EventSource(url)
    streamRef.current = es

    es.addEventListener('chunk', (event) => {
      const chunk = (event as MessageEvent).data?.toString?.() ?? ''
      setStreamedDraft((prev) => prev + chunk.replace(/\\n/g, '\n'))
    })

    es.addEventListener('done', () => {
      stopStreaming()
      setLastMessage('Stream draft เสร็จแล้ว กด Apply Streamed Draft เพื่อใส่ลง editor')
    })

    es.addEventListener('error', () => {
      stopStreaming()
      setLastError('Streaming interrupted or failed')
    })
  }

  return (
    <div className="bg-gray-50 p-4 rounded-lg border">
      <div className="flex items-center justify-between gap-3 mb-3">
        <h3 className="text-sm font-semibold text-gray-800">AI Writing Assistant</h3>
        <span className="text-xs text-gray-500">OpenRouter / Xiaomi fallback</span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <button
          type="button"
          onClick={runGenerateDraft}
          disabled={isLoadingDraft}
          className="px-3 py-2 bg-white border border-gray-300 rounded-md text-sm hover:bg-gray-100 disabled:opacity-60"
        >
          {isLoadingDraft ? 'Generating...' : 'Generate Draft'}
        </button>

        <div className="flex gap-2">
          <button
            type="button"
            onClick={runStreamDraft}
            disabled={isStreamingDraft}
            className="flex-1 px-3 py-2 bg-white border border-gray-300 rounded-md text-sm hover:bg-gray-100 disabled:opacity-60"
          >
            {isStreamingDraft ? 'Streaming...' : 'Stream Draft'}
          </button>
          {isStreamingDraft && (
            <button
              type="button"
              onClick={stopStreaming}
              className="px-3 py-2 bg-red-50 border border-red-200 text-red-700 rounded-md text-sm hover:bg-red-100"
            >
              Stop
            </button>
          )}
        </div>

        <div className="flex gap-2">
          <select
            value={improvementType}
            onChange={(e) => setImprovementType(e.target.value as ImprovementOption)}
            className="flex-1 px-2 py-2 text-sm border border-gray-300 rounded-md bg-white"
          >
            {IMPROVEMENT_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={runImproveText}
            disabled={isLoadingImprove}
            className="px-3 py-2 bg-white border border-gray-300 rounded-md text-sm hover:bg-gray-100 disabled:opacity-60"
          >
            {isLoadingImprove ? 'Improving...' : 'Improve'}
          </button>
        </div>

        <button
          type="button"
          onClick={runTranslate}
          disabled={isLoadingTranslate}
          className="px-3 py-2 bg-white border border-gray-300 rounded-md text-sm hover:bg-gray-100 disabled:opacity-60"
        >
          {isLoadingTranslate ? 'Translating...' : 'Auto Translate TH -> EN'}
        </button>

        <button
          type="button"
          onClick={runSuggestTags}
          disabled={isLoadingTags}
          className="px-3 py-2 bg-white border border-gray-300 rounded-md text-sm hover:bg-gray-100 disabled:opacity-60"
        >
          {isLoadingTags ? 'Suggesting...' : 'Suggest Tags'}
        </button>
      </div>

      {streamedDraft && (
        <div className="mt-3 rounded-md border border-blue-200 bg-blue-50 p-3">
          <div className="flex items-center justify-between gap-2 mb-2">
            <p className="text-xs font-medium text-blue-900">Stream Preview</p>
            <button
              type="button"
              onClick={() => onApplyDraft(streamedDraft)}
              className="px-2 py-1 text-xs bg-white border border-blue-300 rounded hover:bg-blue-100"
            >
              Apply Streamed Draft
            </button>
          </div>
          <pre className="text-xs text-blue-900 whitespace-pre-wrap max-h-44 overflow-y-auto">{streamedDraft}</pre>
        </div>
      )}

      {lastMessage && <p className="text-xs text-green-700 mt-3">{lastMessage}</p>}
      {lastError && <p className="text-xs text-red-600 mt-3">{lastError}</p>}
    </div>
  )
}
