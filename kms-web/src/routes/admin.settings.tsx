import { createFileRoute } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { api } from '../services/api'
import type { AdminSettingPolicyDto, AdminSystemSettingDto, LineWebhookTelemetryDto } from '../types/api'

export const Route = createFileRoute('/admin/settings')({
  component: AdminSettingsPage,
})

const LOCKED_KEYS = new Set([
  'system.version',
  'database.provider',
  'jwt.issuer',
  'jwt.audience',
])

const PROTECTED_PREFIXES = ['security.', 'jwt.', 'auth.']
const SETTING_KEY_PATTERN = /^[a-zA-Z0-9._:-]+$/

function isProtectedKey(key: string) {
  return PROTECTED_PREFIXES.some((prefix) => key.toLowerCase().startsWith(prefix))
}

function isSensitiveKey(key: string) {
  const lower = key.toLowerCase()
  return lower.includes('secret') || lower.includes('token') || lower.includes('password') || lower.endsWith('.key')
}

function validateValueByPolicy(value: string | undefined, policy?: AdminSettingPolicyDto): string | null {
  if (!policy || !value || !value.trim()) {
    return null
  }

  const trimmed = value.trim()

  if (policy.valueType === 'bool') {
    if (trimmed !== 'true' && trimmed !== 'false') {
      return 'Value must be true or false'
    }
    return null
  }

  if (policy.valueType === 'int') {
    const parsed = Number(trimmed)
    if (!Number.isInteger(parsed)) {
      return 'Value must be an integer'
    }
    if (policy.minValue !== undefined && parsed < policy.minValue) {
      return `Value must be >= ${policy.minValue}`
    }
    if (policy.maxValue !== undefined && parsed > policy.maxValue) {
      return `Value must be <= ${policy.maxValue}`
    }
    return null
  }

  if (policy.valueType === 'url') {
    try {
      const url = new URL(trimmed)
      if (!url.protocol.startsWith('http')) {
        return 'URL must start with http/https'
      }
      return null
    } catch {
      return 'Value must be a valid absolute URL'
    }
  }

  if (policy.valueType === 'json') {
    try {
      JSON.parse(trimmed)
      return null
    } catch {
      return 'Value must be valid JSON'
    }
  }

  return null
}

function AdminSettingsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [newKey, setNewKey] = useState('')
  const [newValue, setNewValue] = useState('')
  const [newDescription, setNewDescription] = useState('')
  const [newGroup, setNewGroup] = useState('general')
  const [newIsEncrypted, setNewIsEncrypted] = useState(false)
  const [drafts, setDrafts] = useState<Record<string, AdminSystemSettingDto>>({})
  const [formError, setFormError] = useState('')

  const settingsQuery = useQuery({
    queryKey: ['admin', 'settings'],
    queryFn: () => api.getAdminSettings(),
    enabled: api.isAuthenticated(),
  })

  const policiesQuery = useQuery({
    queryKey: ['admin', 'settings', 'policies'],
    queryFn: () => api.getAdminSettingPolicies(),
    enabled: api.isAuthenticated(),
  })

  const lineTelemetryQuery = useQuery({
    queryKey: ['admin', 'line', 'telemetry'],
    queryFn: () => api.getLineWebhookTelemetry(),
    enabled: api.isAuthenticated(),
    refetchInterval: 30_000,
  })

  const upsertSetting = useMutation({
    mutationFn: ({ key, value, description, group, isEncrypted }: {
      key: string
      value?: string
      description?: string
      group?: string
      isEncrypted?: boolean
    }) =>
      api.upsertAdminSetting(key, {
        value,
        description,
        group,
        isEncrypted,
      }),
    onSuccess: () => {
      setFormError('')
      queryClient.invalidateQueries({ queryKey: ['admin', 'settings'] })
    },
    onError: (error) => {
      setFormError(error instanceof Error ? error.message : 'Failed to save setting')
    },
  })

  const settings = settingsQuery.data?.data ?? []
  const policies = policiesQuery.data?.data ?? []
  const telemetry = lineTelemetryQuery.data?.data
  const policiesByKey = useMemo(() => new Map(policies.map((policy) => [policy.key, policy])), [policies])

  const telemetryHealth = useMemo(() => {
    if (!telemetry || telemetry.processedEvents <= 0) {
      return {
        level: 'unknown' as const,
        label: 'No recent traffic',
        detail: 'ยังไม่มี event ในช่วงเวลาวัดล่าสุด',
      }
    }

    const processed = Math.max(telemetry.processedEvents, 1)
    const duplicateRatio = telemetry.duplicateEvents / processed
    const rateLimitedRatio = telemetry.rateLimitedEvents / processed
    const failureRatio = (telemetry.replyFailures + telemetry.processingErrors) / processed

    if (failureRatio > 0.1 || rateLimitedRatio > 0.25) {
      return {
        level: 'critical' as const,
        label: 'Critical',
        detail: 'อัตรา error/rate-limit สูงเกินเกณฑ์ที่ควรรับได้',
      }
    }

    if (failureRatio > 0.03 || rateLimitedRatio > 0.1 || duplicateRatio > 0.3) {
      return {
        level: 'warning' as const,
        label: 'Warning',
        detail: 'มีสัญญาณผิดปกติ ควรติดตามและปรับ threshold',
      }
    }

    return {
      level: 'good' as const,
      label: 'Healthy',
      detail: 'ค่า error และ pressure อยู่ในช่วงปกติ',
    }
  }, [telemetry])

  const telemetryCards: Array<{ key: keyof LineWebhookTelemetryDto; label: string }> = [
    { key: 'processedEvents', label: 'Processed' },
    { key: 'duplicateEvents', label: 'Duplicates' },
    { key: 'rateLimitedEvents', label: 'Rate Limited' },
    { key: 'replyFailures', label: 'Reply Failures' },
    { key: 'processingErrors', label: 'Processing Errors' },
  ]

  const filteredSettings = useMemo(() => {
    return settings.filter((setting) => {
      const query = search.toLowerCase()
      return (
        setting.key.toLowerCase().includes(query) ||
        (setting.group ?? '').toLowerCase().includes(query) ||
        (setting.description ?? '').toLowerCase().includes(query)
      )
    })
  }, [search, settings])

  const grouped = useMemo(() => {
    const groups: Record<string, AdminSystemSettingDto[]> = {}
    for (const setting of filteredSettings) {
      const groupName = setting.group || 'ungrouped'
      if (!groups[groupName]) {
        groups[groupName] = []
      }
      groups[groupName].push(setting)
    }

    Object.values(groups).forEach((items) => {
      items.sort((a, b) => a.key.localeCompare(b.key))
    })

    return Object.entries(groups).sort((a, b) => a[0].localeCompare(b[0]))
  }, [filteredSettings])

  const getDraft = (setting: AdminSystemSettingDto) => {
    return drafts[setting.key] ?? setting
  }

  const isCreateValid = newKey.trim().length > 0
    && SETTING_KEY_PATTERN.test(newKey.trim())
    && !isProtectedKey(newKey.trim())
    && !LOCKED_KEYS.has(newKey.trim())
    && (!isSensitiveKey(newKey.trim()) || newIsEncrypted)

  const updateDraft = (key: string, updater: (current: AdminSystemSettingDto) => AdminSystemSettingDto) => {
    setDrafts((current) => {
      const base = current[key] ?? settings.find((item) => item.key === key)
      if (!base) {
        return current
      }

      return {
        ...current,
        [key]: updater(base),
      }
    })
  }

  return (
    <div className="space-y-6">
      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">LINE Observability</h2>
            <p className="mt-2 text-sm text-gray-600">ภาพรวม telemetry ล่าสุดของ webhook สำหรับติดตาม SLO เบื้องต้น</p>
          </div>
          <button
            type="button"
            onClick={() => lineTelemetryQuery.refetch()}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50"
          >
            Refresh
          </button>
        </div>

        {lineTelemetryQuery.isLoading && <p className="mt-4 text-sm text-gray-500">Loading LINE telemetry...</p>}
        {lineTelemetryQuery.isError && <p className="mt-4 text-sm text-red-600">Failed to load LINE telemetry.</p>}

        {telemetry && (
          <>
            <div className="mt-4 flex items-center gap-2">
              <span
                className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${
                  telemetryHealth.level === 'good'
                    ? 'bg-emerald-100 text-emerald-800'
                    : telemetryHealth.level === 'warning'
                      ? 'bg-amber-100 text-amber-800'
                      : telemetryHealth.level === 'critical'
                        ? 'bg-rose-100 text-rose-800'
                        : 'bg-gray-100 text-gray-700'
                }`}
              >
                {telemetryHealth.label}
              </span>
              <span className="text-xs text-gray-500">Window: {telemetry.windowSeconds}s</span>
            </div>
            <p className="mt-2 text-xs text-gray-600">{telemetryHealth.detail}</p>

            <div className="mt-4 grid gap-3 md:grid-cols-5">
              {telemetryCards.map((card) => (
                <div key={card.key} className="rounded-xl border border-gray-200 p-3">
                  <p className="text-xs uppercase tracking-wide text-gray-500">{card.label}</p>
                  <p className="mt-1 text-xl font-semibold text-gray-900">{telemetry[card.key]}</p>
                </div>
              ))}
            </div>
          </>
        )}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <h2 className="text-xl font-semibold text-gray-900">System Settings</h2>
        <p className="mt-2 text-sm text-gray-600">จัดการค่าคอนฟิกของระบบผ่านคีย์-แวลู พร้อม grouping และการค้นหา</p>
        <p className="mt-2 text-xs text-amber-700">
          Governance: protected prefixes (`security.`, `jwt.`, `auth.`) cannot be created here, and sensitive keys require encrypted flag.
        </p>

        <div className="mt-4">
          <input
            type="text"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search by key, group, description"
            className="w-full rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
        </div>
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <h3 className="text-lg font-semibold text-gray-900">Create New Setting</h3>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          <input
            type="text"
            value={newKey}
            onChange={(event) => setNewKey(event.target.value)}
            placeholder="Key"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <input
            type="text"
            value={newGroup}
            onChange={(event) => setNewGroup(event.target.value)}
            placeholder="Group"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <textarea
            value={newValue}
            onChange={(event) => setNewValue(event.target.value)}
            placeholder="Value"
            rows={3}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <textarea
            value={newDescription}
            onChange={(event) => setNewDescription(event.target.value)}
            placeholder="Description"
            rows={3}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
        </div>

        <div className="mt-3 flex items-center justify-between">
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={newIsEncrypted}
              onChange={(event) => setNewIsEncrypted(event.target.checked)}
            />
            Mark as encrypted
          </label>

          <button
            type="button"
            disabled={!isCreateValid || upsertSetting.isPending}
            onClick={() => {
              const key = newKey.trim()
              upsertSetting.mutate({
                key,
                value: newValue,
                description: newDescription || undefined,
                group: newGroup || undefined,
                isEncrypted: newIsEncrypted,
              })

              setNewKey('')
              setNewValue('')
              setNewDescription('')
              setNewGroup('general')
              setNewIsEncrypted(false)
            }}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            Save New Setting
          </button>
        </div>

        {!!newKey.trim() && !SETTING_KEY_PATTERN.test(newKey.trim()) && (
          <p className="mt-2 text-xs text-red-600">Key contains invalid characters. Allowed: letters, numbers, . _ : -</p>
        )}
        {!!newKey.trim() && isProtectedKey(newKey.trim()) && (
          <p className="mt-2 text-xs text-red-600">Protected key prefix is not allowed for create in admin UI.</p>
        )}
        {!!newKey.trim() && isSensitiveKey(newKey.trim()) && !newIsEncrypted && (
          <p className="mt-2 text-xs text-red-600">Sensitive key must be marked as encrypted.</p>
        )}
        {formError && <p className="mt-2 text-xs text-red-600">{formError}</p>}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        {settingsQuery.isLoading && <p className="text-sm text-gray-500">Loading settings...</p>}
        {settingsQuery.isError && <p className="text-sm text-red-600">Failed to load settings.</p>}

        {!settingsQuery.isLoading && !settingsQuery.isError && grouped.length === 0 && (
          <p className="text-sm text-gray-500">No settings found.</p>
        )}

        {!settingsQuery.isLoading && !settingsQuery.isError && grouped.length > 0 && (
          <div className="space-y-6">
            {grouped.map(([groupName, items]) => (
              <div key={groupName} className="space-y-3">
                <h4 className="text-sm font-semibold uppercase tracking-wide text-gray-500">{groupName}</h4>
                {items.map((setting) => {
                  const draft = getDraft(setting)
                  const policy = policiesByKey.get(setting.key)
                  const isLocked = LOCKED_KEYS.has(setting.key) || !!policy?.isLocked
                  const requiresEncrypted = isSensitiveKey(setting.key)
                    || !!policy?.requiresEncrypted
                  const policyError = validateValueByPolicy(draft.value, policy)
                  const saveBlocked = isLocked || (requiresEncrypted && !draft.isEncrypted) || !!policyError
                  return (
                    <div key={setting.id} className="rounded-xl border border-gray-200 p-4">
                      <div className="mb-3 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <p className="font-mono text-sm font-medium text-gray-900">{setting.key}</p>
                          {isLocked && <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase text-amber-800">Locked</span>}
                          {requiresEncrypted && <span className="rounded-full bg-indigo-100 px-2 py-0.5 text-[10px] font-semibold uppercase text-indigo-800">Sensitive</span>}
                          {policy?.valueType && <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase text-emerald-800">{policy.valueType}</span>}
                        </div>
                        <p className="text-xs text-gray-500">{setting.updatedAt ? new Date(setting.updatedAt).toLocaleString() : '-'}</p>
                      </div>
                      {policy?.description && (
                        <p className="mb-3 text-xs text-gray-500">{policy.description}</p>
                      )}

                      <div className="grid gap-3 md:grid-cols-2">
                        <input
                          type="text"
                          value={draft.group ?? ''}
                          onChange={(event) => updateDraft(setting.key, (current) => ({ ...current, group: event.target.value }))}
                          placeholder="Group"
                          className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
                        />
                        <input
                          type="text"
                          value={draft.description ?? ''}
                          onChange={(event) => updateDraft(setting.key, (current) => ({ ...current, description: event.target.value }))}
                          placeholder="Description"
                          className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
                        />
                        <textarea
                          value={draft.value ?? ''}
                          onChange={(event) => updateDraft(setting.key, (current) => ({ ...current, value: event.target.value }))}
                          placeholder="Value"
                          rows={3}
                          className="md:col-span-2 rounded-lg border border-gray-300 px-3 py-2 text-sm"
                        />
                      </div>

                      <div className="mt-3 flex items-center justify-between">
                        <label className="flex items-center gap-2 text-sm text-gray-700">
                          <input
                            type="checkbox"
                            checked={draft.isEncrypted}
                            onChange={(event) => updateDraft(setting.key, (current) => ({ ...current, isEncrypted: event.target.checked }))}
                          />
                          Encrypted
                        </label>

                        <button
                          type="button"
                          onClick={() => upsertSetting.mutate({
                            key: setting.key,
                            value: draft.value,
                            description: draft.description,
                            group: draft.group,
                            isEncrypted: draft.isEncrypted,
                          })}
                          disabled={upsertSetting.isPending || saveBlocked}
                          className="rounded-lg border border-blue-300 px-4 py-2 text-sm font-medium text-blue-700 hover:bg-blue-50 disabled:opacity-50"
                        >
                          Save
                        </button>
                      </div>
                      {isLocked && (
                        <p className="mt-2 text-xs text-amber-700">This key is locked by policy and cannot be updated from admin UI.</p>
                      )}
                      {!isLocked && requiresEncrypted && !draft.isEncrypted && (
                        <p className="mt-2 text-xs text-red-600">Sensitive key requires encrypted flag before saving.</p>
                      )}
                      {!isLocked && policyError && (
                        <p className="mt-2 text-xs text-red-600">{policyError}</p>
                      )}
                    </div>
                  )
                })}
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
