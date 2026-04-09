import { createFileRoute, Link } from '@tanstack/react-router'
import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../services/api'
import { MediaUpload } from '../components/MediaUpload'
import { MediaGallery } from '../components/MediaGallery'
import type { MediaItemDto } from '../types/api'

export const Route = createFileRoute('/profile')({
  component: ProfilePage,
})

// ── 2FA Section ───────────────────────────────────────────────────────────────
function TwoFactorSection() {
  const [step, setStep] = useState<'idle' | 'setup' | 'confirm' | 'disable'>('idle')
  const [setup, setSetup] = useState<{ secret: string; qrCodeUri: string; isEnabled: boolean } | null>(null)
  const [code, setCode] = useState('')
  const [msg, setMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [loading, setLoading] = useState(false)

  const { data: setupData, refetch } = useQuery({
    queryKey: ['2fa', 'status'],
    queryFn: () => api.getTwoFactorSetup(),
    enabled: api.isAuthenticated(),
  })

  const isEnabled = setupData?.data?.isEnabled ?? false

  const handleSetup = async () => {
    setLoading(true); setMsg(null)
    const res = await api.getTwoFactorSetup()
    setLoading(false)
    if (res.success && res.data) { setSetup(res.data); setStep('setup') }
    else setMsg({ type: 'error', text: 'ไม่สามารถโหลดข้อมูล 2FA ได้' })
  }

  const handleEnable = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setMsg(null)
    const res = await api.enableTwoFactor(code)
    setLoading(false)
    if (res.success) {
      setMsg({ type: 'success', text: '✅ เปิดใช้งาน 2FA สำเร็จ!' })
      setStep('idle'); setCode(''); refetch()
    } else {
      setMsg({ type: 'error', text: res.message || 'รหัสไม่ถูกต้อง' })
    }
  }

  const handleDisable = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setMsg(null)
    const res = await api.disableTwoFactor(code)
    setLoading(false)
    if (res.success) {
      setMsg({ type: 'success', text: '✅ ปิดใช้งาน 2FA สำเร็จ' })
      setStep('idle'); setCode(''); refetch()
    } else {
      setMsg({ type: 'error', text: res.message || 'รหัสไม่ถูกต้อง' })
    }
  }

  return (
    <section className="rounded-2xl bg-white dark:bg-gray-800 p-6 shadow-sm ring-1 ring-gray-100 dark:ring-gray-700">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-base font-semibold text-gray-900 dark:text-white">🔐 Two-Factor Authentication (2FA)</h3>
          <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
            เพิ่มความปลอดภัยด้วย TOTP (Google Authenticator, Authy)
          </p>
        </div>
        <span className={`rounded-full px-3 py-1 text-xs font-semibold ${isEnabled ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'}`}>
          {isEnabled ? 'เปิดใช้งาน' : 'ปิดอยู่'}
        </span>
      </div>

      {msg && (
        <div className={`mt-3 rounded-lg p-3 text-sm ${msg.type === 'success' ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/20 dark:text-emerald-400' : 'bg-red-50 text-red-600 dark:bg-red-900/20 dark:text-red-400'}`}>
          {msg.text}
        </div>
      )}

      {step === 'idle' && (
        <div className="mt-4 flex gap-3">
          {!isEnabled ? (
            <button onClick={handleSetup} disabled={loading}
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50">
              {loading ? 'กำลังโหลด…' : 'ตั้งค่า 2FA'}
            </button>
          ) : (
            <button onClick={() => { setStep('disable'); setMsg(null) }}
              className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700">
              ปิดใช้งาน 2FA
            </button>
          )}
        </div>
      )}

      {step === 'setup' && setup && (
        <div className="mt-4 space-y-4">
          <div className="rounded-xl bg-gray-50 dark:bg-gray-700/50 p-4">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">1. สแกน QR Code ด้วย Authenticator App</p>
            <div className="flex justify-center">
              <img
                src={`https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(setup.qrCodeUri)}`}
                alt="QR Code"
                className="rounded-lg border border-gray-200 dark:border-gray-600"
              />
            </div>
            <p className="mt-3 text-xs text-gray-500 dark:text-gray-400 text-center">
              หรือกรอก Secret Key: <code className="rounded bg-gray-200 dark:bg-gray-600 px-1 font-mono text-xs">{setup.secret}</code>
            </p>
          </div>
          <form onSubmit={handleEnable} className="space-y-3">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">2. กรอกรหัส 6 หลักเพื่อยืนยัน</p>
            <input type="text" inputMode="numeric" pattern="[0-9]{6}" maxLength={6}
              placeholder="000000" value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
              className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2 text-center font-mono text-xl tracking-widest text-gray-900 dark:text-white focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
              autoFocus
            />
            <div className="flex gap-2">
              <button type="submit" disabled={loading || code.length !== 6}
                className="flex-1 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50">
                {loading ? 'กำลังยืนยัน…' : 'เปิดใช้งาน 2FA'}
              </button>
              <button type="button" onClick={() => { setStep('idle'); setCode(''); setMsg(null) }}
                className="rounded-lg border border-gray-300 dark:border-gray-600 px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">
                ยกเลิก
              </button>
            </div>
          </form>
        </div>
      )}

      {step === 'disable' && (
        <form onSubmit={handleDisable} className="mt-4 space-y-3">
          <p className="text-sm text-gray-600 dark:text-gray-400">กรอกรหัส OTP ปัจจุบันเพื่อยืนยันการปิด 2FA</p>
          <input type="text" inputMode="numeric" pattern="[0-9]{6}" maxLength={6}
            placeholder="000000" value={code}
            onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
            className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2 text-center font-mono text-xl tracking-widest text-gray-900 dark:text-white focus:border-red-500 focus:outline-none"
            autoFocus
          />
          <div className="flex gap-2">
            <button type="submit" disabled={loading || code.length !== 6}
              className="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50">
              {loading ? 'กำลังปิด…' : 'ยืนยันปิด 2FA'}
            </button>
            <button type="button" onClick={() => { setStep('idle'); setCode(''); setMsg(null) }}
              className="rounded-lg border border-gray-300 dark:border-gray-600 px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">
              ยกเลิก
            </button>
          </div>
        </form>
      )}
    </section>
  )
}

function ProfilePage() {
  const queryClient = useQueryClient()
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [faculty, setFaculty] = useState('')
  const [department, setDepartment] = useState('')
  const [position, setPosition] = useState('')
  const [bio, setBio] = useState('')
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([])

  const { data: profileResponse, isLoading, isError } = useQuery({
    queryKey: ['profile', 'me'],
    queryFn: () => api.getCurrentUser(),
    enabled: api.isAuthenticated(),
  })

  const updateProfileMutation = useMutation({
    mutationFn: () => api.updateProfile({
      faculty: faculty || undefined,
      department: department || undefined,
      position: position || undefined,
      bio: bio || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile', 'me'] })
      queryClient.invalidateQueries({ queryKey: ['auth', 'user'] })
    },
  })

  const refreshMediaMutation = useMutation({
    mutationFn: (userId: string) => api.getMediaByEntity('user', userId),
    onSuccess: (response) => {
      if (response.success && response.data) {
        setMediaItems(response.data)
      }
    },
  })

  const deleteMediaMutation = useMutation({
    mutationFn: (id: string) => api.deleteMediaItem(id),
    onSuccess: (_, id) => {
      setMediaItems((current) => current.filter((item) => item.id !== id))
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

  const user = profileResponse?.data

  useEffect(() => {
    if (!user) return

    setFaculty(user.faculty ?? '')
    setDepartment(user.department ?? '')
    setPosition(user.position ?? '')
    setBio(user.bio ?? '')

    refreshMediaMutation.mutate(user.id)
  }, [user])

  const avatarItems = useMemo(
    () => mediaItems.filter((item) => item.collectionName === 'avatar'),
    [mediaItems],
  )
  const historyItems = useMemo(
    () => [...mediaItems].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
    [mediaItems],
  )

  if (!api.isAuthenticated()) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <div className="rounded-xl bg-white p-8 text-center shadow">
          <p className="text-lg font-medium text-gray-900">ต้องเข้าสู่ระบบก่อน</p>
          <Link to="/login" className="mt-3 inline-block text-blue-600 hover:underline">ไปหน้า Login</Link>
        </div>
      </div>
    )
  }

  if (isLoading) {
    return <div className="flex min-h-screen items-center justify-center bg-gray-50 text-gray-500">กำลังโหลดโปรไฟล์...</div>
  }

  if (isError || !user) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <div className="rounded-xl bg-white p-8 text-center shadow">
          <p className="text-lg font-medium text-red-600">โหลดข้อมูลโปรไฟล์ไม่สำเร็จ</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
            <span className="text-sm text-gray-500">My Profile</span>
          </div>
          <nav className="flex items-center gap-4">
            <Link to="/articles" className="text-gray-700 hover:text-gray-900">Articles</Link>
            <Link to="/media" className="text-gray-700 hover:text-gray-900">Media</Link>
            <Link to="/admin" className="text-gray-700 hover:text-gray-900">Admin</Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-6xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
          <h2 className="text-2xl font-semibold text-gray-900">{user.fullName ?? user.username}</h2>
          <p className="mt-1 text-sm text-gray-600">{user.email}</p>
          <div className="mt-3 flex flex-wrap gap-2 text-xs text-gray-500">
            <span>Roles: {user.roles.map((role) => role.name).join(', ') || 'None'}</span>
            <span>•</span>
            <span>{user.articleCount} articles</span>
            <span>•</span>
            <span>{user.isActive ? 'Active' : 'Inactive'}</span>
          </div>
        </section>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1fr)]">
          <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
            <h3 className="text-lg font-semibold text-gray-900">Profile Information</h3>
            <p className="mt-1 text-sm text-gray-600">อัปเดตข้อมูลพื้นฐานของบัญชีผู้ใช้</p>

            <div className="mt-5 grid gap-4">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Faculty</label>
                <input
                  type="text"
                  value={faculty}
                  onChange={(event) => setFaculty(event.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Department</label>
                <input
                  type="text"
                  value={department}
                  onChange={(event) => setDepartment(event.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Position</label>
                <input
                  type="text"
                  value={position}
                  onChange={(event) => setPosition(event.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Bio</label>
                <textarea
                  rows={4}
                  value={bio}
                  onChange={(event) => setBio(event.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
              <div>
                <button
                  type="button"
                  onClick={() => updateProfileMutation.mutate()}
                  disabled={updateProfileMutation.isPending}
                  className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
                >
                  {updateProfileMutation.isPending ? 'Saving...' : 'Save Profile'}
                </button>
              </div>
            </div>
          </section>

          <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
            <h3 className="text-lg font-semibold text-gray-900">Avatar</h3>
            <p className="mt-1 text-sm text-gray-600">อัปโหลดรูปโปรไฟล์ผ่าน media library collection `avatar`</p>

            <div className="mt-4">
              <MediaUpload
                entityType="user"
                entityId={user.id}
                collection="avatar"
                multiple={false}
                accept="image/*"
                onUploaded={(item) => {
                  setMediaItems((current) => [item, ...current.filter((media) => media.collectionName !== 'avatar')])
                  refreshMediaMutation.mutate(user.id)
                }}
              />
            </div>

            <div className="mt-4">
              <MediaGallery
                items={avatarItems}
                deletingId={deletingId}
                updatingId={updatingId}
                onDelete={(item) => {
                  setDeletingId(item.id)
                  deleteMediaMutation.mutate(item.id)
                }}
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
          </section>
        </div>

        <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
          <div className="mb-4 flex items-center justify-between gap-4">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">Media History</h3>
              <p className="mt-1 text-sm text-gray-600">รายการไฟล์ที่คุณอัปโหลดล่าสุด (ทุกคอลเลกชัน)</p>
            </div>
            <Link to="/media" className="text-sm font-medium text-blue-600 hover:underline">Open full media library</Link>
          </div>

          {historyItems.length === 0 ? (
            <p className="text-sm text-gray-500">ยังไม่มีประวัติการอัปโหลด</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">File</th>
                    <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Collection</th>
                    <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Uploaded</th>
                    <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {historyItems.slice(0, 12).map((item) => (
                    <tr key={item.id}>
                      <td className="px-4 py-2 text-sm text-gray-800">{item.title || item.originalFileName}</td>
                      <td className="px-4 py-2 text-sm text-gray-600">{item.collectionName || 'uncategorized'}</td>
                      <td className="px-4 py-2 text-sm text-gray-600">{new Date(item.createdAt).toLocaleString('th-TH')}</td>
                      <td className="px-4 py-2">
                        <a href={item.url} target="_blank" rel="noreferrer" className="text-sm text-blue-600 hover:underline">Open</a>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <TwoFactorSection />
      </main>
    </div>
  )
}
