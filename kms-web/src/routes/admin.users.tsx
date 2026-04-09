import { createFileRoute } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../services/api'
import type { UpdateAdminUserRequest } from '../types/api'

export const Route = createFileRoute('/admin/users')({
  component: AdminUsersPage,
})

function AdminUsersPage() {
  const queryClient = useQueryClient()
  const [drafts, setDrafts] = useState<Record<string, UpdateAdminUserRequest>>({})
  const [searchText, setSearchText] = useState('')
  const [roleFilter, setRoleFilter] = useState('all')
  const [passwordDrafts, setPasswordDrafts] = useState<Record<string, string>>({})
  const [temporaryPasswords, setTemporaryPasswords] = useState<Record<string, string>>({})

  const { data: usersResponse, isLoading: isUsersLoading, isError: isUsersError } = useQuery({
    queryKey: ['admin', 'users'],
    queryFn: () => api.getAdminUsers(),
    enabled: api.isAuthenticated(),
  })

  const { data: rolesResponse } = useQuery({
    queryKey: ['admin', 'roles'],
    queryFn: () => api.getAdminRoles(),
    enabled: api.isAuthenticated(),
  })

  const updateUser = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAdminUserRequest }) => api.updateAdminUser(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
  })

  const lockUser = useMutation({
    mutationFn: (id: string) => api.lockAdminUser(id, { lockMinutes: 1440 }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
  })

  const unlockUser = useMutation({
    mutationFn: (id: string) => api.unlockAdminUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
  })

  const resetPassword = useMutation({
    mutationFn: ({ id, newPassword }: { id: string; newPassword?: string }) =>
      api.resetAdminUserPassword(id, { newPassword }),
    onSuccess: (response) => {
      const payload = response.data
      if (payload?.userId && payload.temporaryPassword) {
        setTemporaryPasswords((current) => ({
          ...current,
          [payload.userId]: payload.temporaryPassword,
        }))
        setPasswordDrafts((current) => ({
          ...current,
          [payload.userId]: '',
        }))
      }
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
  })

  const users = usersResponse?.data ?? []
  const roles = rolesResponse?.data ?? []
  const filteredUsers = users.filter((user) => {
    const matchesSearch = [user.fullNameTh, user.email, user.username]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(searchText.toLowerCase()))

    const matchesRole = roleFilter === 'all' || user.roleNames.includes(roleFilter)
    return matchesSearch && matchesRole
  })

  const getDraft = (userId: string, roleIds: string[], isActive: boolean): UpdateAdminUserRequest => {
    return drafts[userId] ?? { roleIds, isActive }
  }

  const updateDraft = (userId: string, next: UpdateAdminUserRequest) => {
    setDrafts((current) => ({
      ...current,
      [userId]: next,
    }))
  }

  const toggleRole = (userId: string, roleId: string, currentDraft: UpdateAdminUserRequest) => {
    const currentRoleIds = currentDraft.roleIds ?? []
    const nextRoleIds = currentRoleIds.includes(roleId)
      ? currentRoleIds.filter((id) => id !== roleId)
      : [...currentRoleIds, roleId]

    updateDraft(userId, {
      ...currentDraft,
      roleIds: nextRoleIds,
    })
  }

  return (
    <div className="space-y-6">
      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <h2 className="text-xl font-semibold text-gray-900">Users</h2>
        <p className="mt-2 text-sm text-gray-600">
          หน้านี้เชื่อมกับข้อมูลผู้ใช้และ roles จริงแล้ว สามารถปรับสถานะ active และมอบหมาย role ให้ผู้ใช้แต่ละคนได้
        </p>
        <div className="mt-4 grid gap-3 md:grid-cols-[minmax(0,1fr)_220px]">
          <input
            type="text"
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
            placeholder="ค้นหาจากชื่อ, username หรือ email"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <select
            value={roleFilter}
            onChange={(event) => setRoleFilter(event.target.value)}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          >
            <option value="all">All roles</option>
            {roles.map((role) => (
              <option key={role.id} value={role.name}>{role.name}</option>
            ))}
          </select>
        </div>
      </section>

      <section className="rounded-2xl bg-white shadow-sm ring-1 ring-gray-100">
        {isUsersLoading && <div className="p-6 text-sm text-gray-500">กำลังโหลดผู้ใช้...</div>}
        {isUsersError && <div className="p-6 text-sm text-red-600">โหลดรายการผู้ใช้ไม่สำเร็จ</div>}
        {!isUsersLoading && !isUsersError && (
          <div className="divide-y divide-gray-100">
            {filteredUsers.map((user) => {
              const draft = getDraft(user.id, user.roleIds, user.isActive)
              return (
                <div key={user.id} className="p-6">
                  <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900">{user.fullNameTh}</h3>
                      <p className="text-sm text-gray-600">{user.email}</p>
                      <div className="mt-2 flex flex-wrap gap-2 text-xs text-gray-500">
                        <span>{user.username}</span>
                        <span>•</span>
                        <span>{user.articleCount} articles</span>
                        <span>•</span>
                        <span>{user.roleNames.join(', ') || 'No roles'}</span>
                        <span>•</span>
                        <span>{user.isLockedOut ? 'Locked' : 'Unlocked'}</span>
                        {user.isLockedOut && user.lockoutEnd && (
                          <>
                            <span>•</span>
                            <span>until {new Date(user.lockoutEnd).toLocaleString()}</span>
                          </>
                        )}
                      </div>
                    </div>

                    <div className="w-full max-w-xl space-y-4">
                      <label className="flex items-center gap-3 text-sm text-gray-700">
                        <input
                          type="checkbox"
                          checked={draft.isActive ?? false}
                          onChange={(event) => updateDraft(user.id, {
                            ...draft,
                            isActive: event.target.checked,
                          })}
                        />
                        Active account
                      </label>

                      <div className="grid gap-2 md:grid-cols-2">
                        {roles.map((role) => (
                          <label key={role.id} className="flex items-center gap-3 rounded-xl border border-gray-200 px-3 py-2 text-sm text-gray-700">
                            <input
                              type="checkbox"
                              checked={(draft.roleIds ?? []).includes(role.id)}
                              onChange={() => toggleRole(user.id, role.id, draft)}
                            />
                            <span>{role.name}</span>
                          </label>
                        ))}
                      </div>

                      <button
                        type="button"
                        onClick={() => updateUser.mutate({ id: user.id, data: draft })}
                        disabled={updateUser.isPending}
                        className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
                      >
                        Save user changes
                      </button>

                      <div className="grid gap-2 md:grid-cols-2">
                        <button
                          type="button"
                          onClick={() => lockUser.mutate(user.id)}
                          disabled={lockUser.isPending || user.isLockedOut}
                          className="rounded-lg border border-amber-300 px-4 py-2 text-sm font-medium text-amber-700 hover:bg-amber-50 disabled:opacity-50"
                        >
                          Lock 24h
                        </button>
                        <button
                          type="button"
                          onClick={() => unlockUser.mutate(user.id)}
                          disabled={unlockUser.isPending || !user.isLockedOut}
                          className="rounded-lg border border-emerald-300 px-4 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-50 disabled:opacity-50"
                        >
                          Unlock
                        </button>
                      </div>

                      <div className="rounded-xl border border-gray-200 p-3">
                        <p className="mb-2 text-xs font-medium uppercase tracking-wide text-gray-500">Reset Password</p>
                        <div className="grid gap-2 md:grid-cols-[1fr_auto]">
                          <input
                            type="text"
                            value={passwordDrafts[user.id] ?? ''}
                            onChange={(event) => setPasswordDrafts((current) => ({
                              ...current,
                              [user.id]: event.target.value,
                            }))}
                            placeholder="Leave empty to auto-generate"
                            className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
                          />
                          <button
                            type="button"
                            onClick={() => resetPassword.mutate({ id: user.id, newPassword: (passwordDrafts[user.id] ?? '').trim() || undefined })}
                            disabled={resetPassword.isPending}
                            className="rounded-lg border border-indigo-300 px-4 py-2 text-sm font-medium text-indigo-700 hover:bg-indigo-50 disabled:opacity-50"
                          >
                            Reset
                          </button>
                        </div>
                        {temporaryPasswords[user.id] && (
                          <p className="mt-2 text-xs text-indigo-700">
                            Temporary password: <span className="font-semibold">{temporaryPasswords[user.id]}</span>
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              )
            })}
            {filteredUsers.length === 0 && (
              <div className="p-6 text-sm text-gray-500">ไม่พบผู้ใช้ตามเงื่อนไขที่ค้นหา</div>
            )}
          </div>
        )}
      </section>
    </div>
  )
}
