import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../services/api'
import type { CreateCommentDto, UpdateCommentDto } from '../types/api'

export const useComments = (articleId: string, pageNumber = 1, pageSize = 20) => {
  return useQuery({
    queryKey: ['comments', articleId, pageNumber],
    queryFn: () => api.getComments(articleId, pageNumber, pageSize),
    enabled: !!articleId,
  })
}

export const useCreateComment = (articleId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCommentDto) => api.createComment(articleId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', articleId] })
      queryClient.invalidateQueries({ queryKey: ['article', articleId] })
    },
  })
}

export const useUpdateComment = (articleId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ commentId, data }: { commentId: string; data: UpdateCommentDto }) =>
      api.updateComment(articleId, commentId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', articleId] })
    },
  })
}

export const useDeleteComment = (articleId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (commentId: string) => api.deleteComment(articleId, commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', articleId] })
      queryClient.invalidateQueries({ queryKey: ['article', articleId] })
    },
  })
}

export const useLikeComment = (articleId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (commentId: string) => api.likeComment(articleId, commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', articleId] })
    },
  })
}
