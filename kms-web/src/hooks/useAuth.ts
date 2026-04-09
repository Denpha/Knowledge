import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../services/api'
import type { LoginRequest, RegisterRequest } from '../types/api'

export const useLogin = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: LoginRequest) => api.login(data),
    onSuccess: (data) => {
      if (data.success) {
        queryClient.setQueryData(['auth', 'user'], data.data)
      }
    },
  })
}

export const useRegister = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: RegisterRequest) => api.register(data),
    onSuccess: (data) => {
      if (data.success) {
        queryClient.setQueryData(['auth', 'user'], data.data)
      }
    },
  })
}

export const useLogout = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => api.logout(),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: ['auth', 'user'] })
    },
  })
}

export const useCurrentUser = () => {
  return useQuery({
    queryKey: ['auth', 'user'],
    queryFn: () => api.getCurrentUser(),
    enabled: api.isAuthenticated(),
    retry: false,
  })
}

export const useIsAuthenticated = () => {
  return api.isAuthenticated()
}