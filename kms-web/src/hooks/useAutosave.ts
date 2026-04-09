import { useEffect, useRef, useCallback } from 'react'

const AUTOSAVE_INTERVAL_MS = 30_000 // 30 seconds
const STORAGE_KEY_PREFIX = 'kms_draft_'

export interface AutosaveData {
  title: string
  content: string
  summary?: string
  savedAt: string
}

export function useAutosave(key: string, data: AutosaveData, enabled = true) {
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const lastSavedRef = useRef<string>('')

  const save = useCallback(() => {
    const json = JSON.stringify(data)
    if (json === lastSavedRef.current) return // no change
    localStorage.setItem(`${STORAGE_KEY_PREFIX}${key}`, json)
    lastSavedRef.current = json
  }, [key, data])

  const load = useCallback((): AutosaveData | null => {
    try {
      const raw = localStorage.getItem(`${STORAGE_KEY_PREFIX}${key}`)
      return raw ? JSON.parse(raw) : null
    } catch {
      return null
    }
  }, [key])

  const clear = useCallback(() => {
    localStorage.removeItem(`${STORAGE_KEY_PREFIX}${key}`)
    lastSavedRef.current = ''
  }, [key])

  useEffect(() => {
    if (!enabled) return
    timerRef.current = setInterval(save, AUTOSAVE_INTERVAL_MS)
    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [save, enabled])

  // Save on unmount
  useEffect(() => {
    return () => { if (enabled) save() }
  }, [save, enabled])

  return { save, load, clear }
}
