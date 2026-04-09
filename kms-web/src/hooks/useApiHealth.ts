import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'

const HEALTH_URL =
  typeof window !== 'undefined' && window.location.hostname !== 'localhost'
    ? `http://${window.location.hostname}:5000/health`
    : 'http://localhost:5000/health'

const CHECK_INTERVAL = 30_000 // 30 seconds

export function useApiHealth() {
  const [online, setOnline] = useState(true)
  const toastIdRef = useRef<string | number | null>(null)
  const offlineSinceRef = useRef<boolean>(false)

  useEffect(() => {
    let intervalId: ReturnType<typeof setInterval>

    async function check() {
      try {
        const res = await fetch(HEALTH_URL, { signal: AbortSignal.timeout(5000) })
        if (res.ok) {
          if (offlineSinceRef.current) {
            // Was offline → now back online
            if (toastIdRef.current !== null) {
              toast.dismiss(toastIdRef.current)
              toastIdRef.current = null
            }
            toast.success('เชื่อมต่อ API สำเร็จแล้ว', { duration: 4000 })
            offlineSinceRef.current = false
          }
          setOnline(true)
        } else {
          throw new Error('unhealthy')
        }
      } catch {
        if (!offlineSinceRef.current) {
          // First time going offline
          offlineSinceRef.current = true
          toastIdRef.current = toast.error('ไม่สามารถเชื่อมต่อ API ได้', {
            description: 'กำลังพยายามเชื่อมต่อใหม่...',
            duration: Infinity,
          })
        }
        setOnline(false)
      }
    }

    check()
    intervalId = setInterval(check, CHECK_INTERVAL)
    return () => clearInterval(intervalId)
  }, [])

  return online
}
