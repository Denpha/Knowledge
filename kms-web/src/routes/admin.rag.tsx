import { createFileRoute } from '@tanstack/react-router'
import { useMemo, useState, useEffect } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '../services/api'
import type {
  EvaluateRagCompareProfilesRequest,
  RagBenchmarkCaseRequest,
  RagBenchmarkHistoryItem,
  RagProfileComparisonResponse,
} from '../types/api'

export const Route = createFileRoute('/admin/rag')({
  component: AdminRagBenchmarkPage,
})

const defaultCases = JSON.stringify(
  [
    {
      caseId: 'admission-001',
      question: 'ขั้นตอนยื่นขอเอกสารรับรองนักศึกษามีอะไรบ้าง',
      expectedKeywords: ['เอกสาร', 'คำร้อง', 'ระยะเวลา'],
    },
    {
      caseId: 'it-helpdesk-001',
      question: 'หากลืมรหัสผ่านระบบมหาวิทยาลัยต้องทำอย่างไร',
      expectedKeywords: ['รีเซ็ตรหัสผ่าน', 'ยืนยันตัวตน', 'เจ้าหน้าที่'],
    },
  ],
  null,
  2,
)

const HISTORY_STORAGE_KEY = 'rag_benchmark_compare_history_v1'
const ALL_PROFILES = ['default', 'balanced', 'strict'] as const
type PromptProfile = (typeof ALL_PROFILES)[number]
type SharedProfileFilter = PromptProfile | 'all'

type RagHistoryItem = {
  id: string
  createdAt: string
  profiles: string[]
  topK: number
  maxContextChars: number
  semanticThreshold: number
  cases?: RagBenchmarkCaseRequest[]
  totalCases: number
  bestProfile?: string
  bestPassRate?: number
  payload: RagProfileComparisonResponse
}

function buildCasesFromPayload(payload: RagProfileComparisonResponse): RagBenchmarkCaseRequest[] {
  const firstProfile = payload.profiles[0]
  if (!firstProfile) {
    return []
  }

  return firstProfile.caseResults.map((item, index) => ({
    caseId: item.caseId || `case-${index + 1}`,
    question: item.question,
    expectedKeywords: [],
  }))
}

function downloadTextFile(fileName: string, content: string, mimeType: string) {
  const blob = new Blob([content], { type: mimeType })
  const objectUrl = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = objectUrl
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
  URL.revokeObjectURL(objectUrl)
}

function toCsvValue(value: string | number | boolean) {
  const text = String(value)
  if (text.includes(',') || text.includes('"') || text.includes('\n')) {
    return `"${text.replace(/"/g, '""')}"`
  }
  return text
}

function AdminRagBenchmarkPage() {
  const [promptProfiles, setPromptProfiles] = useState<PromptProfile[]>(['default', 'balanced', 'strict'])
  const [topK, setTopK] = useState(5)
  const [maxContextChars, setMaxContextChars] = useState(5000)
  const [semanticThreshold, setSemanticThreshold] = useState(0.65)
  const [casesJson, setCasesJson] = useState(defaultCases)
  const [parseError, setParseError] = useState('')
  const [statusMessage, setStatusMessage] = useState('')
  const [history, setHistory] = useState<RagHistoryItem[]>([])
  const [comparisonResult, setComparisonResult] = useState<RagProfileComparisonResponse | null>(null)
  const [sharedSearch, setSharedSearch] = useState('')
  const [sharedProfileFilter, setSharedProfileFilter] = useState<SharedProfileFilter>('all')
  const [sharedDateFrom, setSharedDateFrom] = useState('')
  const [sharedDateTo, setSharedDateTo] = useState('')
  const [sharedMinCases, setSharedMinCases] = useState(0)

  const sharedHistoryQuery = useQuery({
    queryKey: ['admin', 'rag', 'benchmark-history'],
    queryFn: () => api.getRagBenchmarkHistory(),
    enabled: api.isAuthenticated(),
  })

  const sharedHistoryAnalyticsQuery = useQuery({
    queryKey: ['admin', 'rag', 'benchmark-history', 'analytics'],
    queryFn: () => api.getRagBenchmarkHistoryAnalytics(),
    enabled: api.isAuthenticated(),
  })

  const clearSharedHistoryMutation = useMutation({
    mutationFn: () => api.clearRagBenchmarkHistory(),
    onSuccess: () => {
      sharedHistoryQuery.refetch()
      sharedHistoryAnalyticsQuery.refetch()
    },
  })

  useEffect(() => {
    try {
      const raw = localStorage.getItem(HISTORY_STORAGE_KEY)
      if (!raw) {
        return
      }

      const parsed = JSON.parse(raw) as RagHistoryItem[]
      if (Array.isArray(parsed)) {
        setHistory(parsed.slice(0, 20))
      }
    } catch {
      setHistory([])
    }
  }, [])

  const parsedCases = useMemo(() => {
    try {
      const parsed = JSON.parse(casesJson) as RagBenchmarkCaseRequest[]
      if (!Array.isArray(parsed)) {
        return null
      }

      const normalized = parsed
        .map((item, index) => ({
          caseId: item.caseId || `case-${index + 1}`,
          question: (item.question || '').trim(),
          expectedKeywords: Array.isArray(item.expectedKeywords)
            ? item.expectedKeywords.map((k) => String(k).trim()).filter(Boolean)
            : [],
        }))
        .filter((item) => item.question.length > 0)

      return normalized
    } catch {
      return null
    }
  }, [casesJson])

  const compareMutation = useMutation({
    mutationFn: (payload: EvaluateRagCompareProfilesRequest) => api.evaluateRagCompareProfiles(payload),
    onSuccess: (response) => {
      if (!response.success || !response.data) {
        return
      }

      setComparisonResult(response.data)
      setStatusMessage('')

      const best = response.data.profiles[0]
      const entry: RagHistoryItem = {
        id: `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`,
        createdAt: new Date().toISOString(),
        profiles: [...promptProfiles],
        topK,
        maxContextChars,
        semanticThreshold,
        cases: parsedCases ?? undefined,
        totalCases: response.data.totalCases,
        bestProfile: best?.promptProfileUsed,
        bestPassRate: best?.passRate,
        payload: response.data,
      }

      setHistory((current) => {
        const next = [entry, ...current].slice(0, 20)
        localStorage.setItem(HISTORY_STORAGE_KEY, JSON.stringify(next))
        return next
      })
    },
    onError: (error) => {
      setParseError(error instanceof Error ? error.message : 'Failed to run benchmark')
      setStatusMessage('')
    },
  })

  const runCompare = () => {
    setParseError('')
    setStatusMessage('')

    if (!parsedCases || parsedCases.length === 0) {
      setParseError('Cases JSON is invalid or empty.')
      return
    }

    if (promptProfiles.length === 0) {
      setParseError('Select at least one prompt profile.')
      return
    }

    compareMutation.mutate({
      profiles: promptProfiles,
      topK,
      maxContextChars,
      semanticThreshold,
      cases: parsedCases,
    })
  }

  const profileResults = comparisonResult?.profiles ?? []

  const loadHistoryItem = (item: RagHistoryItem) => {
    setPromptProfiles(item.profiles.filter((x): x is PromptProfile => ALL_PROFILES.includes(x as PromptProfile)))
    setTopK(item.topK)
    setMaxContextChars(item.maxContextChars)
    setSemanticThreshold(item.semanticThreshold)
    setComparisonResult(item.payload)

    const cases = item.cases && item.cases.length > 0 ? item.cases : buildCasesFromPayload(item.payload)
    if (cases.length > 0) {
      setCasesJson(JSON.stringify(cases, null, 2))
      setStatusMessage(item.cases && item.cases.length > 0
        ? 'Loaded run from local history with full benchmark inputs.'
        : 'Loaded run from local history. Expected keywords are not stored in older history and were reset to empty arrays.')
    } else {
      setStatusMessage('Loaded run from local history.')
    }
    setParseError('')
  }

  const loadSharedHistoryItem = (item: RagBenchmarkHistoryItem) => {
    const inputProfiles = item.input?.profiles ?? item.profiles
    const profiles = inputProfiles.filter((x): x is PromptProfile => ALL_PROFILES.includes(x as PromptProfile))
    if (profiles.length > 0) {
      setPromptProfiles(profiles)
    }

    if (item.input) {
      setTopK(Math.max(1, Math.min(10, item.input.topK || 5)))
      setMaxContextChars(Math.max(800, Math.min(12000, item.input.maxContextChars || 5000)))
      setSemanticThreshold(Math.max(0, Math.min(1, item.input.semanticThreshold || 0.65)))
    }

    setComparisonResult(item.payload)
    const cases = item.input?.cases && item.input.cases.length > 0
      ? item.input.cases
      : buildCasesFromPayload(item.payload)
    if (cases.length > 0) {
      setCasesJson(JSON.stringify(cases, null, 2))
    }

    setStatusMessage(item.input
      ? 'Loaded run from shared history with full benchmark inputs.'
      : 'Loaded run from shared history. Older history entries may not include full benchmark inputs.')
    setParseError('')
  }

  const clearHistory = () => {
    localStorage.removeItem(HISTORY_STORAGE_KEY)
    setHistory([])
  }

  const exportJson = () => {
    const payload = comparisonResult
    if (!payload) {
      return
    }

    downloadTextFile(
      `rag-compare-${new Date().toISOString().replace(/[:.]/g, '-')}.json`,
      JSON.stringify(payload, null, 2),
      'application/json',
    )
  }

  const sharedHistoryItems = sharedHistoryQuery.data?.data ?? []

  const sharedProfileOptions = useMemo(() => {
    const known = new Set<PromptProfile>()
    sharedHistoryItems.forEach((item) => {
      item.profiles.forEach((profile) => {
        if (ALL_PROFILES.includes(profile as PromptProfile)) {
          known.add(profile as PromptProfile)
        }
      })
    })

    return Array.from(known)
  }, [sharedHistoryItems])

  const filteredSharedHistory = useMemo(() => {
    return sharedHistoryItems.filter((item) => {
      if (sharedProfileFilter !== 'all' && !item.profiles.includes(sharedProfileFilter)) {
        return false
      }

      if (item.totalCases < sharedMinCases) {
        return false
      }

      if (sharedDateFrom) {
        const from = new Date(`${sharedDateFrom}T00:00:00Z`)
        if (new Date(item.createdAtUtc) < from) {
          return false
        }
      }

      if (sharedDateTo) {
        const to = new Date(`${sharedDateTo}T23:59:59Z`)
        if (new Date(item.createdAtUtc) > to) {
          return false
        }
      }

      if (!sharedSearch.trim()) {
        return true
      }

      const needle = sharedSearch.trim().toLowerCase()
      const haystack = [
        item.id,
        item.bestProfile ?? '',
        item.profiles.join(' '),
      ].join(' ').toLowerCase()

      return haystack.includes(needle)
    })
  }, [sharedHistoryItems, sharedProfileFilter, sharedMinCases, sharedDateFrom, sharedDateTo, sharedSearch])

  const exportCsv = () => {
    if (profileResults.length === 0) {
      return
    }

    const header = [
      'profile',
      'caseId',
      'question',
      'answerCoverage',
      'contextCoverage',
      'passed',
      'missingKeywordsInAnswer',
    ].join(',')

    const rows = profileResults.flatMap((profile) =>
      profile.caseResults.map((item) => [
        toCsvValue(profile.promptProfileUsed),
        toCsvValue(item.caseId),
        toCsvValue(item.question),
        toCsvValue((item.answerKeywordCoverage * 100).toFixed(2)),
        toCsvValue((item.contextKeywordCoverage * 100).toFixed(2)),
        toCsvValue(item.passed),
        toCsvValue(item.missingKeywordsInAnswer.join('|')),
      ].join(',')),
    )

    downloadTextFile(
      `rag-compare-${new Date().toISOString().replace(/[:.]/g, '-')}.csv`,
      [header, ...rows].join('\n'),
      'text/csv;charset=utf-8',
    )
  }

  return (
    <div className="space-y-6">
      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <h2 className="text-xl font-semibold text-gray-900">RAG Benchmark Runner</h2>
        <p className="mt-2 text-sm text-gray-600">รัน benchmark และ compare prompt profiles เพื่อหากลยุทธ์ตอบคำถามที่เหมาะสมที่สุด</p>

        <div className="mt-4 grid gap-4 md:grid-cols-4">
          <label className="text-sm text-gray-700">
            <span className="mb-1 block">Top K</span>
            <input
              type="number"
              value={topK}
              min={1}
              max={10}
              onChange={(event) => setTopK(Math.max(1, Math.min(10, Number(event.target.value) || 5)))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
            />
          </label>
          <label className="text-sm text-gray-700">
            <span className="mb-1 block">Max Context Chars</span>
            <input
              type="number"
              value={maxContextChars}
              min={800}
              max={12000}
              onChange={(event) => setMaxContextChars(Math.max(800, Math.min(12000, Number(event.target.value) || 5000)))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
            />
          </label>
          <label className="text-sm text-gray-700">
            <span className="mb-1 block">Semantic Threshold</span>
            <input
              type="number"
              value={semanticThreshold}
              step={0.01}
              min={0}
              max={1}
              onChange={(event) => setSemanticThreshold(Math.max(0, Math.min(1, Number(event.target.value) || 0.65)))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
            />
          </label>
          <div className="text-sm text-gray-700">
            <span className="mb-1 block">Profiles</span>
            <div className="flex flex-wrap gap-2">
              {ALL_PROFILES.map((profile) => {
                const checked = promptProfiles.includes(profile)
                return (
                  <label key={profile} className="inline-flex items-center gap-2 rounded-lg border border-gray-200 px-2 py-1 text-xs">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(event) => {
                        setPromptProfiles((current) => {
                          if (event.target.checked) {
                            return [...current, profile]
                          }
                          return current.filter((p) => p !== profile)
                        })
                      }}
                    />
                    {profile}
                  </label>
                )
              })}
            </div>
          </div>
        </div>

        <div className="mt-4">
          <p className="mb-2 text-sm font-medium text-gray-700">Benchmark Cases (JSON Array)</p>
          <textarea
            value={casesJson}
            onChange={(event) => setCasesJson(event.target.value)}
            rows={16}
            className="w-full rounded-xl border border-gray-300 p-3 font-mono text-xs"
          />
        </div>

        {parseError && <p className="mt-2 text-sm text-red-600">{parseError}</p>}
        {!parseError && statusMessage && <p className="mt-2 text-sm text-sky-700">{statusMessage}</p>}

        <div className="mt-4 flex items-center gap-3">
          <button
            type="button"
            onClick={runCompare}
            disabled={compareMutation.isPending}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {compareMutation.isPending ? 'Running...' : 'Run Profile Compare'}
          </button>
          <span className="text-xs text-gray-500">Runs: /api/ai/evaluate-compare</span>
        </div>
      </section>

      {profileResults.length > 0 && (
        <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">Profile Leaderboard</h3>
              <p className="mt-1 text-xs text-gray-500">Sorted by pass rate then average answer coverage</p>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={exportJson}
                className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50"
              >
                Export JSON
              </button>
              <button
                type="button"
                onClick={exportCsv}
                className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50"
              >
                Export CSV
              </button>
            </div>
          </div>

          <div className="mt-4 grid gap-3 md:grid-cols-3">
            {profileResults.map((result) => (
              <div key={result.promptProfileUsed} className="rounded-xl border border-gray-200 p-4">
                <p className="text-sm font-semibold uppercase tracking-wide text-gray-700">{result.promptProfileUsed}</p>
                <p className="mt-2 text-sm text-gray-600">Pass: {(result.passRate * 100).toFixed(1)}%</p>
                <p className="text-sm text-gray-600">Avg Answer Coverage: {(result.averageAnswerCoverage * 100).toFixed(1)}%</p>
                <p className="text-sm text-gray-600">Avg Context Coverage: {(result.averageContextCoverage * 100).toFixed(1)}%</p>
                <p className="text-sm text-gray-600">Cases: {result.passedCases}/{result.totalCases}</p>
              </div>
            ))}
          </div>

          <div className="mt-6 space-y-4">
            {profileResults.map((result) => (
              <div key={`${result.promptProfileUsed}-cases`}>
                <h4 className="text-sm font-semibold text-gray-800">{result.promptProfileUsed} cases</h4>
                <div className="mt-2 overflow-x-auto rounded-xl border border-gray-200">
                  <table className="min-w-full divide-y divide-gray-200 text-sm">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-3 py-2 text-left font-medium text-gray-600">Case</th>
                        <th className="px-3 py-2 text-left font-medium text-gray-600">Answer Coverage</th>
                        <th className="px-3 py-2 text-left font-medium text-gray-600">Context Coverage</th>
                        <th className="px-3 py-2 text-left font-medium text-gray-600">Status</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100 bg-white">
                      {result.caseResults.map((item) => (
                        <tr key={`${result.promptProfileUsed}-${item.caseId}`}>
                          <td className="px-3 py-2 text-gray-800">{item.caseId}</td>
                          <td className="px-3 py-2 text-gray-700">{(item.answerKeywordCoverage * 100).toFixed(1)}%</td>
                          <td className="px-3 py-2 text-gray-700">{(item.contextKeywordCoverage * 100).toFixed(1)}%</td>
                          <td className="px-3 py-2">
                            <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${item.passed ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-700'}`}>
                              {item.passed ? 'Pass' : 'Fail'}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Recent Benchmark History</h3>
            <p className="mt-1 text-xs text-gray-500">Stored locally in browser (last 20 runs)</p>
          </div>
          <button
            type="button"
            onClick={clearHistory}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50"
            disabled={history.length === 0}
          >
            Clear History
          </button>
        </div>

        {history.length === 0 && (
          <p className="mt-3 text-sm text-gray-500">No saved benchmark runs yet.</p>
        )}

        {history.length > 0 && (
          <div className="mt-3 overflow-x-auto rounded-xl border border-gray-200">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Run Time</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Profiles</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Cases</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Best</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 bg-white">
                {history.map((item) => (
                  <tr key={item.id}>
                    <td className="px-3 py-2 text-gray-800">{new Date(item.createdAt).toLocaleString()}</td>
                    <td className="px-3 py-2 text-gray-700">{item.profiles.join(', ')}</td>
                    <td className="px-3 py-2 text-gray-700">{item.totalCases}</td>
                    <td className="px-3 py-2 text-gray-700">
                      {item.bestProfile ? `${item.bestProfile} (${((item.bestPassRate ?? 0) * 100).toFixed(1)}%)` : '-'}
                    </td>
                    <td className="px-3 py-2">
                      <button
                        type="button"
                        onClick={() => loadHistoryItem(item)}
                        className="rounded-md border border-gray-300 px-2 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50"
                      >
                        Reuse Params
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Shared Benchmark History (Backend)</h3>
            <p className="mt-1 text-xs text-gray-500">Persisted by API for admin users across sessions</p>
          </div>
          <button
            type="button"
            onClick={() => clearSharedHistoryMutation.mutate()}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50"
            disabled={clearSharedHistoryMutation.isPending || (sharedHistoryQuery.data?.data?.length ?? 0) === 0}
          >
            Clear Shared History
          </button>
        </div>

        {sharedHistoryQuery.isLoading && <p className="mt-3 text-sm text-gray-500">Loading shared history...</p>}
        {sharedHistoryQuery.isError && <p className="mt-3 text-sm text-red-600">Failed to load shared history.</p>}

        {!sharedHistoryQuery.isLoading && !sharedHistoryQuery.isError && sharedHistoryItems.length === 0 && (
          <p className="mt-3 text-sm text-gray-500">No shared benchmark history yet.</p>
        )}

        {sharedHistoryItems.length > 0 && (
          <>
            <div className="mt-3 grid gap-3 rounded-xl border border-gray-200 bg-gray-50 p-3 md:grid-cols-5">
              <label className="text-xs text-gray-700">
                <span className="mb-1 block">Search</span>
                <input
                  type="text"
                  value={sharedSearch}
                  onChange={(event) => setSharedSearch(event.target.value)}
                  placeholder="run id / profile"
                  className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-xs"
                />
              </label>
              <label className="text-xs text-gray-700">
                <span className="mb-1 block">Profile</span>
                <select
                  value={sharedProfileFilter}
                  onChange={(event) => setSharedProfileFilter(event.target.value as SharedProfileFilter)}
                  className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-xs"
                >
                  <option value="all">all</option>
                  {sharedProfileOptions.map((profile) => (
                    <option key={profile} value={profile}>
                      {profile}
                    </option>
                  ))}
                </select>
              </label>
              <label className="text-xs text-gray-700">
                <span className="mb-1 block">Date From (UTC)</span>
                <input
                  type="date"
                  value={sharedDateFrom}
                  onChange={(event) => setSharedDateFrom(event.target.value)}
                  className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-xs"
                />
              </label>
              <label className="text-xs text-gray-700">
                <span className="mb-1 block">Date To (UTC)</span>
                <input
                  type="date"
                  value={sharedDateTo}
                  onChange={(event) => setSharedDateTo(event.target.value)}
                  className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-xs"
                />
              </label>
              <label className="text-xs text-gray-700">
                <span className="mb-1 block">Min Cases</span>
                <input
                  type="number"
                  min={0}
                  value={sharedMinCases}
                  onChange={(event) => setSharedMinCases(Math.max(0, Number(event.target.value) || 0))}
                  className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-xs"
                />
              </label>
            </div>

            <p className="mt-2 text-xs text-gray-500">
              Showing {filteredSharedHistory.length} of {sharedHistoryItems.length} runs.
            </p>

            <div className="mt-3 overflow-x-auto rounded-xl border border-gray-200">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Run Time (UTC)</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Profiles</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Cases</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Best</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 bg-white">
                {filteredSharedHistory.map((item) => (
                  <tr key={item.id}>
                    <td className="px-3 py-2 text-gray-800">{new Date(item.createdAtUtc).toLocaleString()}</td>
                    <td className="px-3 py-2 text-gray-700">{item.profiles.join(', ')}</td>
                    <td className="px-3 py-2 text-gray-700">{item.totalCases}</td>
                    <td className="px-3 py-2 text-gray-700">
                      {item.bestProfile ? `${item.bestProfile} (${((item.bestPassRate ?? 0) * 100).toFixed(1)}%)` : '-'}
                    </td>
                    <td className="px-3 py-2">
                      <button
                        type="button"
                        onClick={() => loadSharedHistoryItem(item)}
                        className="rounded-md border border-gray-300 px-2 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50"
                      >
                        Load Run
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            </div>

            {filteredSharedHistory.length === 0 && (
              <p className="mt-2 text-sm text-gray-500">No runs match current filters.</p>
            )}
          </>
        )}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">Benchmark History Analytics</h3>
          <p className="mt-1 text-xs text-gray-500">Trend baseline + drift flag + profile stability scoring from shared history</p>
        </div>

        {sharedHistoryAnalyticsQuery.isLoading && <p className="mt-3 text-sm text-gray-500">Loading analytics...</p>}
        {sharedHistoryAnalyticsQuery.isError && <p className="mt-3 text-sm text-red-600">Failed to load analytics.</p>}

        {!sharedHistoryAnalyticsQuery.isLoading && !sharedHistoryAnalyticsQuery.isError && (
          <>
            <div className="mt-3 grid gap-3 md:grid-cols-4">
              <div className="rounded-xl border border-gray-200 p-3">
                <p className="text-xs text-gray-500">Total Runs</p>
                <p className="mt-1 text-lg font-semibold text-gray-900">{sharedHistoryAnalyticsQuery.data?.data?.totalRuns ?? 0}</p>
              </div>
              <div className="rounded-xl border border-gray-200 p-3">
                <p className="text-xs text-gray-500">Recent Window</p>
                <p className="mt-1 text-lg font-semibold text-gray-900">{sharedHistoryAnalyticsQuery.data?.data?.windowSizeRecent ?? 0}</p>
              </div>
              <div className="rounded-xl border border-gray-200 p-3">
                <p className="text-xs text-gray-500">Baseline Window</p>
                <p className="mt-1 text-lg font-semibold text-gray-900">{sharedHistoryAnalyticsQuery.data?.data?.windowSizeBaseline ?? 0}</p>
              </div>
              <div className="rounded-xl border border-gray-200 p-3">
                <p className="text-xs text-gray-500">Latest Run (UTC)</p>
                <p className="mt-1 text-sm font-semibold text-gray-900">
                  {sharedHistoryAnalyticsQuery.data?.data?.latestRunAtUtc
                    ? new Date(sharedHistoryAnalyticsQuery.data.data.latestRunAtUtc).toLocaleString()
                    : '-'}
                </p>
              </div>
            </div>

            {(sharedHistoryAnalyticsQuery.data?.data?.profiles?.length ?? 0) > 0 && (
              <div className="mt-4 overflow-x-auto rounded-xl border border-gray-200">
                <table className="min-w-full divide-y divide-gray-200 text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Profile</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Samples</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Avg Pass</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Stability</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Recent</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Baseline</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Drift</th>
                      <th className="px-3 py-2 text-left font-medium text-gray-600">Flag</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 bg-white">
                    {(sharedHistoryAnalyticsQuery.data?.data?.profiles ?? []).map((item) => (
                      <tr key={item.profile}>
                        <td className="px-3 py-2 text-gray-800">{item.profile}</td>
                        <td className="px-3 py-2 text-gray-700">{item.sampleCount}</td>
                        <td className="px-3 py-2 text-gray-700">{(item.averagePassRate * 100).toFixed(1)}%</td>
                        <td className="px-3 py-2 text-gray-700">{(item.stabilityScore * 100).toFixed(1)}%</td>
                        <td className="px-3 py-2 text-gray-700">{(item.recentAveragePassRate * 100).toFixed(1)}%</td>
                        <td className="px-3 py-2 text-gray-700">{(item.baselineAveragePassRate * 100).toFixed(1)}%</td>
                        <td className={`px-3 py-2 ${item.drift >= 0 ? 'text-emerald-700' : 'text-rose-700'}`}>
                          {(item.drift >= 0 ? '+' : '')}{(item.drift * 100).toFixed(1)}%
                        </td>
                        <td className="px-3 py-2">
                          <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${item.driftFlag ? 'bg-amber-100 text-amber-800' : 'bg-gray-100 text-gray-700'}`}>
                            {item.driftFlag ? 'Watch' : 'Stable'}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {(sharedHistoryAnalyticsQuery.data?.data?.profiles?.length ?? 0) === 0 && (
              <p className="mt-3 text-sm text-gray-500">No analytics yet. Run and persist more shared benchmark history first.</p>
            )}
          </>
        )}
      </section>
    </div>
  )
}
