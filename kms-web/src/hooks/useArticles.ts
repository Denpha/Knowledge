import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../services/api'
import type { CreateArticleDto, UpdateArticleDto, SearchParams } from '../types/api'

export const useArticles = (params?: SearchParams) => {
  return useQuery({
    queryKey: ['articles', params],
    queryFn: () => api.getArticles(params),
  })
}

export const useArticle = (id: string) => {
  return useQuery({
    queryKey: ['article', id],
    queryFn: () => api.getArticle(id),
    enabled: !!id,
  })
}

export const useCreateArticle = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateArticleDto) => api.createArticle(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['articles'] })
    },
  })
}

export const useUpdateArticle = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateArticleDto }) => 
      api.updateArticle(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['articles'] })
      queryClient.invalidateQueries({ queryKey: ['article', variables.id] })
    },
  })
}

export const useDeleteArticle = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.deleteArticle(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['articles'] })
    },
  })
}

export const usePublishArticle = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.publishArticle(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['articles'] })
      queryClient.invalidateQueries({ queryKey: ['article', id] })
    },
  })
}

export const useArchiveArticle = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.archiveArticle(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['articles'] })
      queryClient.invalidateQueries({ queryKey: ['article', id] })
    },
  })
}

export const useSearchArticles = (query: string) => {
  return useQuery({
    queryKey: ['search', query],
    queryFn: () => api.searchArticles(query),
    enabled: !!query,
  })
}

export const useSemanticSearch = (query: string) => {
  return useQuery({
    queryKey: ['semantic-search', query],
    queryFn: () => api.semanticSearch(query),
    enabled: !!query,
  })
}

export const useMyReaction = (articleId: string, enabled: boolean) => {
  return useQuery({
    queryKey: ['my-reaction', articleId],
    queryFn: () => api.getMyReaction(articleId),
    enabled: enabled && !!articleId,
  })
}

export const useReactToArticle = (articleId: string) => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (reactionType: 'Like' | 'Bookmark' | 'Share') =>
      api.reactToArticle(articleId, reactionType),
    onSuccess: (data) => {
      if (data.success && data.data) {
        queryClient.setQueryData(['my-reaction', articleId], data)
      }
    },
  })
}