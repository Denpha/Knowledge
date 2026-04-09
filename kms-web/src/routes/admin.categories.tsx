import { createFileRoute } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { api } from '../services/api'
import type { CategoryDto } from '../types/api'

export const Route = createFileRoute('/admin/categories')({
  component: AdminCategoriesPage,
})

const MAX_TREE_DEPTH = 4

function AdminCategoriesPage() {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [parentId, setParentId] = useState('')
  const [order, setOrder] = useState(0)
  const [isActive, setIsActive] = useState(true)
  const [editingCategory, setEditingCategory] = useState<CategoryDto | null>(null)
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editParentId, setEditParentId] = useState('')
  const [editOrder, setEditOrder] = useState(0)
  const [editIsActive, setEditIsActive] = useState(true)
  const [orderDrafts, setOrderDrafts] = useState<Record<string, number>>({})
  const [parentDrafts, setParentDrafts] = useState<Record<string, string>>({})
  const [dragCategoryId, setDragCategoryId] = useState<string | null>(null)
  const [dropTargetCategoryId, setDropTargetCategoryId] = useState<string | null>(null)
  const [dragHint, setDragHint] = useState('')

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: () => api.getCategories(),
  })

  const createCategory = useMutation({
    mutationFn: () => api.createCategory({
      name,
      description: description || undefined,
      parentId: parentId || undefined,
      order,
      isActive,
    }),
    onSuccess: () => {
      setName('')
      setDescription('')
      setParentId('')
      setOrder(0)
      setIsActive(true)
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] })
    },
  })

  const deleteCategory = useMutation({
    mutationFn: (id: string) => api.deleteCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] })
    },
  })

  const updateCategory = useMutation({
    mutationFn: ({ id, name, description, parentId, order, isActive }: {
      id: string
      name: string
      description: string
      parentId?: string
      order: number
      isActive: boolean
    }) =>
      api.updateCategory(id, {
        name,
        description: description || undefined,
        parentId,
        order,
        isActive,
      }),
    onSuccess: () => {
      setEditingCategory(null)
      setEditName('')
      setEditDescription('')
      setEditParentId('')
      setEditOrder(0)
      setEditIsActive(true)
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] })
    },
  })

  const bulkSaveOrder = useMutation({
    mutationFn: async () => {
      const pending = categories
        .map((category) => {
          const nextOrder = orderDrafts[category.id] ?? category.order
          const nextParentRaw = parentDrafts[category.id]
          const nextParentId = nextParentRaw === undefined
            ? category.parentId
            : nextParentRaw || undefined

          return {
            category,
            nextOrder,
            nextParentId,
          }
        })
        .filter((entry) => (
          entry.nextOrder !== (entry.category.order ?? 0)
          || (entry.nextParentId ?? '') !== (entry.category.parentId ?? '')
        ))

      await Promise.all(
        pending.map((entry) =>
          api.updateCategory(entry.category.id, {
            name: entry.category.name,
            description: entry.category.description || undefined,
            parentId: entry.nextParentId,
            order: entry.nextOrder,
            isActive: entry.category.isActive,
          }),
        ),
      )
    },
    onSuccess: () => {
      setOrderDrafts({})
      setParentDrafts({})
      setDragHint('')
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] })
    },
  })

  const categories = data?.data?.items ?? []
  const categoriesById = new Map(categories.map((category) => [category.id, category]))

  const getEffectiveParentId = (category: CategoryDto) => {
    return parentDrafts[category.id] ?? (category.parentId ?? '')
  }

  const getEffectiveOrder = (category: CategoryDto) => {
    return orderDrafts[category.id] ?? category.order ?? 0
  }

  const pendingOrderChanges = useMemo(() => {
    return categories.filter((category) => {
      const nextOrder = orderDrafts[category.id]
      const nextParent = parentDrafts[category.id]
      const parentChanged = nextParent !== undefined && nextParent !== (category.parentId ?? '')
      return (nextOrder !== undefined && nextOrder !== (category.order ?? 0)) || parentChanged
    }).length
  }, [categories, orderDrafts, parentDrafts])

  const getDepthByCategoryId = (categoryId: string) => {
    let depth = 0
    let currentId: string | undefined = categoryId
    const visited = new Set<string>()

    while (currentId) {
      const current = categoriesById.get(currentId)
      if (!current) {
        break
      }

      const parentId = getEffectiveParentId(current)
      if (!parentId) {
        break
      }

      if (visited.has(parentId)) {
        break
      }

      visited.add(parentId)
      depth += 1
      currentId = parentId

      if (depth > 12) {
        break
      }
    }

    return depth
  }

  const getSubtreeHeight = (rootCategoryId: string) => {
    const childrenByParent = new Map<string, CategoryDto[]>()
    for (const category of categories) {
      const parentId = getEffectiveParentId(category)
      if (!parentId) {
        continue
      }
      if (!childrenByParent.has(parentId)) {
        childrenByParent.set(parentId, [])
      }
      childrenByParent.get(parentId)!.push(category)
    }

    const walk = (id: string): number => {
      const children = childrenByParent.get(id) ?? []
      if (children.length === 0) {
        return 0
      }

      let maxChildHeight = 0
      for (const child of children) {
        maxChildHeight = Math.max(maxChildHeight, 1 + walk(child.id))
      }
      return maxChildHeight
    }

    return walk(rootCategoryId)
  }

  const canMoveToParent = (sourceCategoryId: string, newParentId: string) => {
    if (!newParentId) {
      return true
    }

    if (sourceCategoryId === newParentId) {
      return false
    }

    let cursor = newParentId
    const visited = new Set<string>()

    while (cursor) {
      if (cursor === sourceCategoryId) {
        return false
      }
      if (visited.has(cursor)) {
        return false
      }
      visited.add(cursor)

      const parent = categoriesById.get(cursor)
      if (!parent) {
        break
      }
      cursor = getEffectiveParentId(parent)
    }

    const targetDepth = getDepthByCategoryId(newParentId)
    const subtreeHeight = getSubtreeHeight(sourceCategoryId)
    return targetDepth + 1 + subtreeHeight <= MAX_TREE_DEPTH
  }

  const getDepth = (category: CategoryDto) => {
    return getDepthByCategoryId(category.id)
  }

  const sortedCategories = [...categories].sort((a, b) => {
    if (getEffectiveParentId(a) !== getEffectiveParentId(b)) {
      return getEffectiveParentId(a).localeCompare(getEffectiveParentId(b))
    }
    if (getEffectiveOrder(a) !== getEffectiveOrder(b)) {
      return getEffectiveOrder(a) - getEffectiveOrder(b)
    }
    return a.name.localeCompare(b.name)
  })

  const moveCategoryInSibling = (categoryId: string, direction: 'up' | 'down') => {
    const target = categoriesById.get(categoryId)
    if (!target) {
      return
    }

    const siblings = categories
      .filter((category) => getEffectiveParentId(category) === getEffectiveParentId(target))
      .sort((a, b) => {
        if (getEffectiveOrder(a) !== getEffectiveOrder(b)) {
          return getEffectiveOrder(a) - getEffectiveOrder(b)
        }
        return a.name.localeCompare(b.name)
      })

    const currentIndex = siblings.findIndex((item) => item.id === categoryId)
    if (currentIndex < 0) {
      return
    }

    const nextIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1
    if (nextIndex < 0 || nextIndex >= siblings.length) {
      return
    }

    const reordered = [...siblings]
    const temp = reordered[currentIndex]
    reordered[currentIndex] = reordered[nextIndex]
    reordered[nextIndex] = temp

    setOrderDrafts((current) => {
      const next = { ...current }
      reordered.forEach((item, index) => {
        next[item.id] = (index + 1) * 10
      })
      return next
    })
  }

  const moveCategoryByDrop = (sourceCategoryId: string, targetCategoryId: string) => {
    if (sourceCategoryId === targetCategoryId) {
      return
    }

    const source = categoriesById.get(sourceCategoryId)
    const target = categoriesById.get(targetCategoryId)
    if (!source || !target) {
      return
    }

    if ((source.parentId ?? '') !== (target.parentId ?? '')) {
      return
    }

    const siblings = categories
      .filter((category) => getEffectiveParentId(category) === getEffectiveParentId(source))
      .sort((a, b) => {
        if (getEffectiveOrder(a) !== getEffectiveOrder(b)) {
          return getEffectiveOrder(a) - getEffectiveOrder(b)
        }
        return a.name.localeCompare(b.name)
      })

    const sourceIndex = siblings.findIndex((item) => item.id === sourceCategoryId)
    const targetIndex = siblings.findIndex((item) => item.id === targetCategoryId)
    if (sourceIndex < 0 || targetIndex < 0) {
      return
    }

    const reordered = [...siblings]
    const [moved] = reordered.splice(sourceIndex, 1)
    reordered.splice(targetIndex, 0, moved)

    setOrderDrafts((current) => {
      const next = { ...current }
      reordered.forEach((item, index) => {
        next[item.id] = (index + 1) * 10
      })
      return next
    })
  }

  const moveCategoryToParent = (sourceCategoryId: string, newParentId: string) => {
    const source = categoriesById.get(sourceCategoryId)
    if (!source) {
      return
    }

    const normalizedParentId = newParentId
    if (!canMoveToParent(sourceCategoryId, normalizedParentId)) {
      setDragHint('Cannot move category here (loop/depth constraint).')
      return
    }

    const currentParentId = getEffectiveParentId(source)
    if (currentParentId === normalizedParentId) {
      return
    }

    const newSiblings = categories
      .filter((category) => category.id !== sourceCategoryId && getEffectiveParentId(category) === normalizedParentId)

    const maxOrder = newSiblings.reduce((max, category) => Math.max(max, getEffectiveOrder(category)), 0)

    setParentDrafts((current) => ({
      ...current,
      [sourceCategoryId]: normalizedParentId,
    }))

    setOrderDrafts((current) => ({
      ...current,
      [sourceCategoryId]: maxOrder + 10,
    }))

    setDragHint('')
  }

  return (
    <div className="space-y-6">
      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <h2 className="text-xl font-semibold text-gray-900">Categories</h2>
        <p className="mt-2 text-sm text-gray-600">
          หน้านี้ดึงข้อมูลหมวดหมู่จริงจาก API แล้ว และเพิ่มการสร้าง/ลบหมวดหมู่พื้นฐานสำหรับงาน admin รอบแรก
        </p>
        <p className="mt-2 text-xs text-gray-500">
          Tip: ลากวางการ์ดเพื่อ reorder ใน parent เดียวกัน หรือวางบนปุ่ม "Drop as child" เพื่อย้ายข้าม parent
        </p>
        {dragHint && <p className="mt-2 text-xs text-red-600">{dragHint}</p>}
        <div className="mt-4 flex flex-wrap items-center gap-3 text-sm">
          <span className="rounded-full bg-slate-100 px-3 py-1 font-medium text-slate-700">
            Pending order changes: {pendingOrderChanges}
          </span>
          <button
            type="button"
            onClick={() => bulkSaveOrder.mutate()}
            disabled={pendingOrderChanges === 0 || bulkSaveOrder.isPending}
            className="rounded-lg bg-slate-900 px-4 py-2 font-medium text-white hover:bg-slate-800 disabled:opacity-50"
          >
            Save all order changes
          </button>
          <button
            type="button"
            onClick={() => {
              setOrderDrafts({})
              setParentDrafts({})
              setDragHint('')
            }}
            disabled={pendingOrderChanges === 0 || bulkSaveOrder.isPending}
            className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
          >
            Reset draft changes
          </button>
        </div>
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        <div className="grid gap-4 md:grid-cols-2">
          <input
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="ชื่อหมวดหมู่"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <input
            type="text"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="คำอธิบายหมวดหมู่"
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          />
          <select
            value={parentId}
            onChange={(event) => setParentId(event.target.value)}
            className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
          >
            <option value="">No parent (root)</option>
            {sortedCategories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
          <div className="grid gap-3 sm:grid-cols-[140px_1fr_auto] sm:items-center">
            <input
              type="number"
              min={0}
              value={order}
              onChange={(event) => setOrder(Number(event.target.value) || 0)}
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            />
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(event) => setIsActive(event.target.checked)}
              />
              Active
            </label>
            <button
              type="button"
              onClick={() => createCategory.mutate()}
              disabled={!name.trim() || createCategory.isPending}
              className="rounded-xl bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50"
            >
              Add category
            </button>
          </div>
        </div>
      </section>

      {editingCategory && (
        <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
          <div className="mb-4 flex items-center justify-between gap-4">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">Edit category</h3>
              <p className="text-sm text-gray-600">ปรับชื่อหรือคำอธิบายของหมวดหมู่ที่เลือก</p>
            </div>
            <button
              type="button"
              onClick={() => setEditingCategory(null)}
              className="text-sm text-gray-500 hover:text-gray-700"
            >
              Close
            </button>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <input
              type="text"
              value={editName}
              onChange={(event) => setEditName(event.target.value)}
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            />
            <input
              type="text"
              value={editDescription}
              onChange={(event) => setEditDescription(event.target.value)}
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            />
            <select
              value={editParentId}
              onChange={(event) => setEditParentId(event.target.value)}
              className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
            >
              <option value="">No parent (root)</option>
              {sortedCategories
                .filter((category) => category.id !== editingCategory.id)
                .map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
            </select>
            <div className="grid gap-3 sm:grid-cols-[140px_1fr_auto] sm:items-center">
              <input
                type="number"
                min={0}
                value={editOrder}
                onChange={(event) => setEditOrder(Number(event.target.value) || 0)}
                className="rounded-xl border border-gray-300 px-4 py-2 text-sm"
              />
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={editIsActive}
                  onChange={(event) => setEditIsActive(event.target.checked)}
                />
                Active
              </label>
              <button
                type="button"
                onClick={() => updateCategory.mutate({
                  id: editingCategory.id,
                  name: editName,
                  description: editDescription,
                  parentId: editParentId || undefined,
                  order: editOrder,
                  isActive: editIsActive,
                })}
                disabled={!editName.trim() || updateCategory.isPending}
                className="rounded-xl bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
              >
                Save
              </button>
            </div>
          </div>
        </section>
      )}

      <section className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-gray-100">
        {isLoading && <p className="text-sm text-gray-500">กำลังโหลดหมวดหมู่...</p>}
        {isError && <p className="text-sm text-red-600">โหลดหมวดหมู่ไม่สำเร็จ</p>}
        {!isLoading && !isError && categories.length === 0 && (
          <p className="text-sm text-gray-500">ยังไม่มีข้อมูลหมวดหมู่</p>
        )}
        {!isLoading && !isError && categories.length > 0 && (
          <div className="space-y-3">
            <div
              className="rounded-xl border border-dashed border-gray-300 bg-gray-50 p-3 text-xs text-gray-600"
              onDragOver={(event) => {
                event.preventDefault()
                setDropTargetCategoryId('__root__')
              }}
              onDrop={(event) => {
                event.preventDefault()
                if (dragCategoryId) {
                  moveCategoryToParent(dragCategoryId, '')
                }
                setDragCategoryId(null)
                setDropTargetCategoryId(null)
              }}
            >
              Drop here to move dragged category to Root level
            </div>
            {sortedCategories.map((category) => (
              <div
                key={category.id}
                className={`rounded-xl border p-4 ${dropTargetCategoryId === category.id ? 'border-blue-400 bg-blue-50/40' : 'border-gray-200'}`}
                style={{ marginLeft: `${Math.min(getDepth(category) * 20, 80)}px` }}
                draggable
                onDragStart={() => {
                  setDragCategoryId(category.id)
                }}
                onDragOver={(event) => {
                  event.preventDefault()
                  setDropTargetCategoryId(category.id)
                }}
                onDrop={(event) => {
                  event.preventDefault()
                  if (dragCategoryId) {
                    moveCategoryByDrop(dragCategoryId, category.id)
                  }
                  setDragCategoryId(null)
                  setDropTargetCategoryId(null)
                }}
                onDragEnd={() => {
                  setDragCategoryId(null)
                  setDropTargetCategoryId(null)
                }}
              >
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h3 className="font-medium text-gray-900">{category.name}</h3>
                    {category.description && (
                      <p className="mt-1 text-sm text-gray-600">{category.description}</p>
                    )}
                  </div>
                  <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-700">
                    {category.articleCount ?? 0} articles
                  </span>
                </div>
                <div className="mt-4 flex items-center justify-between text-xs text-gray-500">
                  <span>
                    Parent: {getEffectiveParentId(category) || 'Root'}{parentDrafts[category.id] !== undefined && parentDrafts[category.id] !== (category.parentId ?? '') ? ' (draft)' : ''} | Order: {getEffectiveOrder(category)}{orderDrafts[category.id] !== undefined && orderDrafts[category.id] !== (category.order ?? 0) ? ' (draft)' : ''} | {category.isActive ? 'Active' : 'Inactive'} | Subcategories: {category.subCategoryCount ?? 0}
                  </span>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onDragOver={(event) => event.preventDefault()}
                      onDrop={(event) => {
                        event.preventDefault()
                        if (dragCategoryId) {
                          moveCategoryToParent(dragCategoryId, category.id)
                        }
                        setDragCategoryId(null)
                        setDropTargetCategoryId(null)
                      }}
                      className="rounded-lg border border-violet-200 px-3 py-1.5 text-violet-700 hover:bg-violet-50"
                    >
                      Drop as child
                    </button>
                    <button
                      type="button"
                      onClick={() => moveCategoryInSibling(category.id, 'up')}
                      className="rounded-lg border border-gray-200 px-3 py-1.5 text-gray-600 hover:bg-gray-50"
                    >
                      Up
                    </button>
                    <button
                      type="button"
                      onClick={() => moveCategoryInSibling(category.id, 'down')}
                      className="rounded-lg border border-gray-200 px-3 py-1.5 text-gray-600 hover:bg-gray-50"
                    >
                      Down
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setEditingCategory(category)
                        setEditName(category.name)
                        setEditDescription(category.description ?? '')
                        setEditParentId(category.parentId ?? '')
                        setEditOrder(category.order ?? 0)
                        setEditIsActive(category.isActive)
                      }}
                      className="rounded-lg border border-blue-200 px-3 py-1.5 text-blue-600 hover:bg-blue-50"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteCategory.mutate(category.id)}
                      disabled={deleteCategory.isPending}
                      className="rounded-lg border border-red-200 px-3 py-1.5 text-red-600 hover:bg-red-50 disabled:opacity-50"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
