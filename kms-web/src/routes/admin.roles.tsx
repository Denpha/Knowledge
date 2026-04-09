import { createFileRoute } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../services/api'
import type { CreateAdminRoleRequest, UpdateAdminRoleRequest } from '../types/api'

export const Route = createFileRoute('/admin/roles')({
  component: AdminRolesPage,
})

function AdminRolesPage() {
  const queryClient = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<{ name: string; description: string }>({ name: '', description: '' })
  const [error, setError] = useState<string | null>(null)

  const { data: rolesResponse, isLoading } = useQuery({
    queryKey: ['admin', 'roles'],
    queryFn: () => api.getAdminRoles(),
    enabled: api.isAuthenticated(),
  })

  const createRole = useMutation({
    mutationFn: (data: CreateAdminRoleRequest) => api.createAdminRole(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      setShowCreate(false)
      setForm({ name: '', description: '' })
      setError(null)
    },
    onError: (err: unknown) => {
      setError(err instanceof Error ? err.message : 'Failed to create role')
    },
  })

  const updateRole = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAdminRoleRequest }) => api.updateAdminRole(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
      setEditingId(null)
      setForm({ name: '', description: '' })
      setError(null)
    },
    onError: (err: unknown) => {
      setError(err instanceof Error ? err.message : 'Failed to update role')
    },
  })

  const deleteRole = useMutation({
    mutationFn: (id: string) => api.deleteAdminRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] })
    },
    onError: (err: unknown) => {
      setError(err instanceof Error ? err.message : 'Failed to delete role')
    },
  })

  const roles = rolesResponse?.data ?? []

  const startEdit = (id: string, name: string, description?: string) => {
    setEditingId(id)
    setForm({ name, description: description ?? '' })
    setShowCreate(false)
    setError(null)
  }

  const cancelEdit = () => {
    setEditingId(null)
    setForm({ name: '', description: '' })
    setError(null)
  }

  const handleSubmitCreate = () => {
    if (!form.name.trim()) { setError('Role name is required'); return }
    createRole.mutate({ name: form.name.trim(), description: form.description.trim() || undefined })
  }

  const handleSubmitUpdate = () => {
    if (!editingId) return
    if (!form.name.trim()) { setError('Role name is required'); return }
    updateRole.mutate({ id: editingId, data: { name: form.name.trim(), description: form.description.trim() || undefined } })
  }

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Role Management</h1>
        {!showCreate && !editingId && (
          <button
            onClick={() => { setShowCreate(true); setForm({ name: '', description: '' }); setError(null) }}
            className="px-4 py-2 bg-brand-500 text-white rounded-lg text-sm font-medium hover:bg-brand-600 transition"
          >
            + New Role
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm dark:bg-red-900/20 dark:border-red-800 dark:text-red-400">
          {error}
        </div>
      )}

      {(showCreate || editingId) && (
        <div className="mb-6 p-4 border border-gray-200 dark:border-gray-700 rounded-xl bg-white dark:bg-gray-800 shadow-sm">
          <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
            {showCreate ? 'Create New Role' : 'Edit Role'}
          </h2>
          <div className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Role Name *</label>
              <input
                type="text"
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="e.g. Moderator"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-sm bg-white dark:bg-gray-700 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Description</label>
              <input
                type="text"
                value={form.description}
                onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                placeholder="Optional description"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-sm bg-white dark:bg-gray-700 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
            <div className="flex gap-2 pt-1">
              <button
                onClick={showCreate ? handleSubmitCreate : handleSubmitUpdate}
                disabled={createRole.isPending || updateRole.isPending}
                className="px-4 py-2 bg-brand-500 text-white rounded-lg text-sm font-medium hover:bg-brand-600 disabled:opacity-50 transition"
              >
                {createRole.isPending || updateRole.isPending ? 'Saving...' : 'Save'}
              </button>
              <button
                onClick={cancelEdit}
                className="px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg text-sm font-medium hover:bg-gray-200 dark:hover:bg-gray-600 transition"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Loading roles...</div>
      ) : (
        <div className="space-y-3">
          {roles.map(role => (
            <div
              key={role.id}
              className="flex items-center justify-between p-4 border border-gray-200 dark:border-gray-700 rounded-xl bg-white dark:bg-gray-800 shadow-sm"
            >
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium text-gray-900 dark:text-white">{role.name}</span>
                  <span className="text-xs px-2 py-0.5 bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400 rounded-full">
                    {role.userCount} user{role.userCount !== 1 ? 's' : ''}
                  </span>
                </div>
                {role.description && (
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{role.description}</p>
                )}
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => startEdit(role.id, role.name, role.description)}
                  className="px-3 py-1.5 text-xs bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition"
                >
                  Edit
                </button>
                <button
                  onClick={() => {
                    if (role.userCount > 0) { setError(`Cannot delete '${role.name}' — ${role.userCount} user(s) assigned`); return }
                    if (confirm(`Delete role '${role.name}'?`)) deleteRole.mutate(role.id)
                  }}
                  disabled={deleteRole.isPending}
                  className="px-3 py-1.5 text-xs bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-lg hover:bg-red-100 dark:hover:bg-red-900/40 disabled:opacity-50 transition"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
          {roles.length === 0 && (
            <div className="text-center py-12 text-gray-400 dark:text-gray-500">No roles found</div>
          )}
        </div>
      )}
    </div>
  )
}
