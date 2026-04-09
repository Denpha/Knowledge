import type { ApiResponse, LoginRequest, RegisterRequest, AuthResponse, ArticleDto, CreateArticleDto, UpdateArticleDto, SearchParams, PaginatedResult, CommentDto, CreateCommentDto, UpdateCommentDto, ArticleReactionResult, ProfileUserDto, CategoryDto, CreateCategoryRequest, UpdateCategoryRequest, AdminUserDto, AdminRoleDto, CreateAdminRoleRequest, UpdateAdminRoleRequest, UpdateAdminUserRequest, MediaItemDto, TagDto, UpdateProfileRequest, UpdateMediaMetadataRequest, MediaSearchQuery, UpdateUserLockoutRequest, ResetUserPasswordRequest, ResetUserPasswordResponse, AuditLogDto, AuditLogSearchParams, AuditSummaryDto, AdminSystemSettingDto, UpsertAdminSystemSettingRequest, AdminSettingPolicyDto, LineWebhookTelemetryDto, EvaluateRagBatchRequest, EvaluateRagCompareProfilesRequest, RagBenchmarkSummaryResponse, RagProfileComparisonResponse, RagBenchmarkHistoryItem, RagBenchmarkHistoryAnalytics, DashboardData, TwoFactorSetupDto, TwoFactorPendingResponse, StorageStatsDto, StorageFileListDto } from '../types/api'

const API_BASE_URL = typeof window !== 'undefined' && window.location.hostname !== 'localhost'
  ? `http://${window.location.hostname}:5000/api`
  : 'http://localhost:5000/api'

class ApiClient {
  private token: string | null = null
  private refreshToken: string | null = null
  private tokenExpiresAt: Date | null = null

  constructor() {
    if (typeof window !== 'undefined') {
      this.token = localStorage.getItem('auth_token')
      this.refreshToken = localStorage.getItem('refresh_token')
      const exp = localStorage.getItem('token_expires_at')
      this.tokenExpiresAt = exp ? new Date(exp) : null
    }
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
    const url = `${API_BASE_URL}${endpoint}`

    const headers = new Headers(options.headers)
    if (!headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    if (this.token) {
      headers.set('Authorization', `Bearer ${this.token}`)
    }

    // Auto-refresh token if expired
    if (this.token && this.isTokenExpired() && this.refreshToken) {
      await this.refreshAccessToken()
      if (this.token) {
        headers.set('Authorization', `Bearer ${this.token}`)
      }
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      })

      if (!response.ok) {
        if (response.status === 401) {
          // Try to refresh token once
          if (this.refreshToken) {
            const refreshed = await this.refreshAccessToken()
            if (refreshed) {
              // Retry original request with new token
              headers.set('Authorization', `Bearer ${this.token}`)
              const retryResponse = await fetch(url, { ...options, headers })
              if (retryResponse.ok) {
                const retryText = await retryResponse.text()
                if (!retryText) return { success: true, data: undefined }
                const retryParsed = JSON.parse(retryText)
                if (typeof retryParsed === 'object' && retryParsed !== null && 'success' in retryParsed) {
                  return retryParsed as ApiResponse<T>
                }
                return { success: true, data: retryParsed as T }
              }
            }
          }
          this.clearToken()
        }

        const errorText = await response.text()
        let errorMessage = `HTTP error! status: ${response.status}`
        if (errorText) {
          try {
            const parsedError = JSON.parse(errorText)
            if (typeof parsedError === 'string') {
              errorMessage = parsedError
            } else if (parsedError?.message) {
              errorMessage = parsedError.message
            } else {
              errorMessage = errorText
            }
          } catch {
            errorMessage = errorText
          }
        }

        throw new Error(errorMessage)
      }

      if (response.status === 204) {
        return { success: true, data: undefined }
      }

      const text = await response.text()
      if (!text) {
        return { success: true, data: undefined }
      }

      const parsed = JSON.parse(text)
      if (typeof parsed === 'object' && parsed !== null && 'success' in parsed) {
        return parsed as ApiResponse<T>
      }

      return {
        success: true,
        data: parsed as T,
      }
    } catch (error) {
      console.error('API request failed:', error)
      throw error
    }
  }

  setToken(token: string, expiresAt?: string, refreshToken?: string) {
    this.token = token
    if (typeof window !== 'undefined') {
      localStorage.setItem('auth_token', token)
      if (expiresAt) {
        this.tokenExpiresAt = new Date(expiresAt)
        localStorage.setItem('token_expires_at', expiresAt)
      }
      if (refreshToken) {
        this.refreshToken = refreshToken
        localStorage.setItem('refresh_token', refreshToken)
      }
    }
  }

  clearToken() {
    this.token = null
    this.refreshToken = null
    this.tokenExpiresAt = null
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('token_expires_at')
    }
  }

  private isTokenExpired(): boolean {
    if (!this.tokenExpiresAt) return false
    return new Date() >= new Date(this.tokenExpiresAt.getTime() - 60_000) // 1 min buffer
  }

  async refreshAccessToken(): Promise<boolean> {
    if (!this.refreshToken) return false
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: this.refreshToken }),
      })
      if (!response.ok) {
        this.clearToken()
        return false
      }
      const data = await response.json()
      const authData = data?.data ?? data
      if (authData?.token) {
        this.setToken(authData.token, authData.expiresAt, authData.refreshToken)
        return true
      }
      return false
    } catch {
      this.clearToken()
      return false
    }
  }

  isAuthenticated(): boolean {
    return !!this.token
  }

  // Auth endpoints
  async login(data: LoginRequest): Promise<ApiResponse<AuthResponse | TwoFactorPendingResponse>> {
    const response = await this.request<AuthResponse | TwoFactorPendingResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({
        username: data.email,
        password: data.password,
      }),
    })
    
    if (response.success && response.data && 'token' in response.data && response.data.token) {
      this.setToken(response.data.token, response.data.expiresAt, response.data.refreshToken)
    }
    
    return response
  }

  async register(data: RegisterRequest): Promise<ApiResponse<AuthResponse>> {
    const response = await this.request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    
    if (response.success && response.data?.token) {
      this.setToken(response.data.token, response.data.expiresAt, response.data.refreshToken)
    }
    
    return response
  }

  async logout(): Promise<ApiResponse<void>> {
    const response = await this.request<void>('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken: this.refreshToken }),
    })
    this.clearToken()
    return response
  }

  async getCurrentUser(): Promise<ApiResponse<ProfileUserDto>> {
    return this.request<ProfileUserDto>('/auth/profile')
  }

  async updateProfile(data: UpdateProfileRequest): Promise<ApiResponse<ProfileUserDto>> {
    return this.request<ProfileUserDto>('/auth/profile', {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  // Article endpoints
  async getArticles(params?: SearchParams): Promise<ApiResponse<PaginatedResult<ArticleDto>>> {
    const queryString = params ? '?' + new URLSearchParams(params as any).toString() : ''
    return this.request<PaginatedResult<ArticleDto>>(`/articles${queryString}`)
  }

  async getArticle(id: string): Promise<ApiResponse<ArticleDto>> {
    return this.request<ArticleDto>(`/articles/${id}`)
  }

  async createArticle(data: CreateArticleDto): Promise<ApiResponse<ArticleDto>> {
    return this.request<ArticleDto>('/articles', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async updateArticle(id: string, data: UpdateArticleDto): Promise<ApiResponse<ArticleDto>> {
    return this.request<ArticleDto>(`/articles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async deleteArticle(id: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/articles/${id}`, {
      method: 'DELETE',
    })
  }

  async publishArticle(id: string): Promise<ApiResponse<ArticleDto>> {
    return this.request<ArticleDto>(`/articles/${id}/publish`, {
      method: 'POST',
    })
  }

  async archiveArticle(id: string): Promise<ApiResponse<ArticleDto>> {
    return this.request<ArticleDto>(`/articles/${id}/archive`, {
      method: 'POST',
    })
  }

  // Category endpoints
  async getCategories(): Promise<ApiResponse<PaginatedResult<CategoryDto>>> {
    return this.request<PaginatedResult<CategoryDto>>('/categories')
  }

  async createCategory(data: CreateCategoryRequest): Promise<ApiResponse<CategoryDto>> {
    return this.request<CategoryDto>('/categories', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async updateCategory(id: string, data: UpdateCategoryRequest): Promise<ApiResponse<CategoryDto>> {
    return this.request<CategoryDto>(`/categories/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async deleteCategory(id: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/categories/${id}`, {
      method: 'DELETE',
    })
  }

  async getAdminUsers(): Promise<ApiResponse<AdminUserDto[]>> {
    return this.request<AdminUserDto[]>('/admin/users')
  }

  async getAdminRoles(): Promise<ApiResponse<AdminRoleDto[]>> {
    return this.request<AdminRoleDto[]>('/admin/roles')
  }

  async createAdminRole(data: CreateAdminRoleRequest): Promise<ApiResponse<AdminRoleDto>> {
    return this.request<AdminRoleDto>('/admin/roles', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async updateAdminRole(id: string, data: UpdateAdminRoleRequest): Promise<ApiResponse<AdminRoleDto>> {
    return this.request<AdminRoleDto>(`/admin/roles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async deleteAdminRole(id: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/admin/roles/${id}`, { method: 'DELETE' })
  }

  async updateAdminUser(id: string, data: UpdateAdminUserRequest): Promise<ApiResponse<AdminUserDto>> {
    return this.request<AdminUserDto>(`/admin/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async lockAdminUser(id: string, data?: UpdateUserLockoutRequest): Promise<ApiResponse<AdminUserDto>> {
    return this.request<AdminUserDto>(`/admin/users/${id}/lock`, {
      method: 'POST',
      body: JSON.stringify(data ?? {}),
    })
  }

  async unlockAdminUser(id: string): Promise<ApiResponse<AdminUserDto>> {
    return this.request<AdminUserDto>(`/admin/users/${id}/unlock`, {
      method: 'POST',
      body: JSON.stringify({}),
    })
  }

  async resetAdminUserPassword(id: string, data?: ResetUserPasswordRequest): Promise<ApiResponse<ResetUserPasswordResponse>> {
    return this.request<ResetUserPasswordResponse>(`/admin/users/${id}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(data ?? {}),
    })
  }

  async getAdminSettings(): Promise<ApiResponse<AdminSystemSettingDto[]>> {
    return this.request<AdminSystemSettingDto[]>('/admin/settings')
  }

  async getAdminDashboard(): Promise<ApiResponse<DashboardData>> {
    return this.request<DashboardData>('/admin/dashboard')
  }

  // ── Storage (MinIO Admin) ────────────────────────────────────────────────
  async getStorageStats(): Promise<ApiResponse<StorageStatsDto>> {
    return this.request<StorageStatsDto>('/admin/storage/stats')
  }

  async listStorageFiles(params?: {
    prefix?: string
    ext?: string
    page?: number
    pageSize?: number
  }): Promise<ApiResponse<StorageFileListDto>> {
    const q = new URLSearchParams()
    if (params?.prefix) q.set('prefix', params.prefix)
    if (params?.ext) q.set('ext', params.ext)
    if (params?.page) q.set('page', String(params.page))
    if (params?.pageSize) q.set('pageSize', String(params.pageSize))
    return this.request<StorageFileListDto>(`/admin/storage/files?${q}`)
  }

  async getPresignedUrl(key: string, expiryMinutes = 60): Promise<ApiResponse<string>> {
    return this.request<string>(`/admin/storage/presigned?key=${encodeURIComponent(key)}&expiryMinutes=${expiryMinutes}`)
  }

  async deleteStorageFiles(keys: string[]): Promise<ApiResponse<void>> {
    return this.request<void>('/admin/storage/files', {
      method: 'DELETE',
      body: JSON.stringify({ keys }),
    })
  }

  // ── Export ───────────────────────────────────────────────────────────────
  async exportArticlePdf(articleId: string): Promise<void> {
    const url = `${API_BASE_URL}/export/article/${articleId}/pdf`
    const headers: Record<string, string> = { 'Content-Type': 'application/json' }
    if (this.token) headers['Authorization'] = `Bearer ${this.token}`
    const response = await fetch(url, { headers })
    if (!response.ok) throw new Error('ไม่สามารถส่งออก PDF ได้')
    const blob = await response.blob()
    const disposition = response.headers.get('Content-Disposition') ?? ''
    const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
    const filename = match ? match[1].replace(/['"]/g, '') : `KMS_article_${articleId}.pdf`
    const objUrl = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = objUrl; a.download = filename; a.click()
    URL.revokeObjectURL(objUrl)
  }

  // ── 2FA ──────────────────────────────────────────────────────────────────
  async getTwoFactorSetup(): Promise<ApiResponse<TwoFactorSetupDto>> {
    return this.request<TwoFactorSetupDto>('/auth/2fa/setup')
  }

  async enableTwoFactor(code: string): Promise<ApiResponse<{ message: string }>> {
    return this.request<{ message: string }>('/auth/2fa/enable', {
      method: 'POST',
      body: JSON.stringify({ code }),
    })
  }

  async disableTwoFactor(code: string): Promise<ApiResponse<{ message: string }>> {
    return this.request<{ message: string }>('/auth/2fa/disable', {
      method: 'POST',
      body: JSON.stringify({ code }),
    })
  }

  async verifyTwoFactor(tempToken: string, code: string): Promise<ApiResponse<AuthResponse>> {
    return this.request<AuthResponse>('/auth/2fa/verify', {
      method: 'POST',
      body: JSON.stringify({ tempToken, code }),
    })
  }

  async getAdminSettingPolicies(): Promise<ApiResponse<AdminSettingPolicyDto[]>> {
    return this.request<AdminSettingPolicyDto[]>('/admin/settings/policies')
  }

  async getLineWebhookTelemetry(): Promise<ApiResponse<LineWebhookTelemetryDto>> {
    return this.request<LineWebhookTelemetryDto>('/line/webhook/telemetry')
  }

  async upsertAdminSetting(key: string, data: UpsertAdminSystemSettingRequest): Promise<ApiResponse<AdminSystemSettingDto>> {
    return this.request<AdminSystemSettingDto>(`/admin/settings/${encodeURIComponent(key)}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  // Tag endpoints
  async getTags(): Promise<ApiResponse<PaginatedResult<TagDto>>> {
    return this.request<PaginatedResult<TagDto>>('/tags')
  }

  // Search endpoints
  async searchArticles(query: string): Promise<ApiResponse<ArticleDto[]>> {
    return this.request<ArticleDto[]>(`/search/articles?query=${encodeURIComponent(query)}`)
  }

  async semanticSearch(query: string): Promise<ApiResponse<ArticleDto[]>> {
    return this.request<ArticleDto[]>(`/search/semantic?query=${encodeURIComponent(query)}`)
  }

  // Media endpoints
  async uploadMedia(file: File, collection: string, modelType: string, modelId: string): Promise<ApiResponse<any>> {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('collectionName', collection)
    formData.append('entityType', modelType)
    if (modelId) {
      formData.append('entityId', modelId)
    }

    const response = await fetch(`${API_BASE_URL}/media/upload`, {
      method: 'POST',
      headers: {
        'Authorization': this.token ? `Bearer ${this.token}` : '',
      },
      body: formData,
    })

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    return response.json()
  }

  async getMediaByEntity(entityType: string, entityId: string): Promise<ApiResponse<MediaItemDto[]>> {
    return this.request<MediaItemDto[]>(`/media/entity/${entityType}/${entityId}`)
  }

  async getMedia(params?: MediaSearchQuery): Promise<ApiResponse<PaginatedResult<MediaItemDto>>> {
    const query = new URLSearchParams()
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          query.set(key, String(value))
        }
      })
    }

    const suffix = query.toString() ? `?${query.toString()}` : ''
    return this.request<PaginatedResult<MediaItemDto>>(`/media${suffix}`)
  }

  async deleteMediaItem(id: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/media/${id}`, {
      method: 'DELETE',
    })
  }

  async updateMediaMetadata(id: string, data: UpdateMediaMetadataRequest): Promise<ApiResponse<MediaItemDto>> {
    return this.request<MediaItemDto>(`/media/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  // AI endpoints
  async aiGenerateDraft(topic: string, language: string = 'th'): Promise<ApiResponse<{ content: string }>> {
    return this.request<{ content: string }>('/ai/draft', {
      method: 'POST',
      body: JSON.stringify({ topic, language }),
    })
  }

  async aiImproveText(text: string, improvementType: string): Promise<ApiResponse<{ content: string }>> {
    return this.request<{ content: string }>('/ai/improve', {
      method: 'POST',
      body: JSON.stringify({ text, improvementType }),
    })
  }

  async aiTranslate(text: string, targetLanguage: string = 'en'): Promise<ApiResponse<{ content: string }>> {
    return this.request<{ content: string }>('/ai/translate', {
      method: 'POST',
      body: JSON.stringify({ text, targetLanguage }),
    })
  }

  async aiSuggestTags(text: string): Promise<ApiResponse<{ tags: string[] }>> {
    return this.request<{ tags: string[] }>('/ai/tags', {
      method: 'POST',
      body: JSON.stringify({ content: text }),
    })
  }

  async evaluateRagBatch(data: EvaluateRagBatchRequest): Promise<ApiResponse<RagBenchmarkSummaryResponse>> {
    return this.request<RagBenchmarkSummaryResponse>('/ai/evaluate-batch', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async evaluateRagCompareProfiles(data: EvaluateRagCompareProfilesRequest): Promise<ApiResponse<RagProfileComparisonResponse>> {
    return this.request<RagProfileComparisonResponse>('/ai/evaluate-compare', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  }

  async getRagBenchmarkHistory(): Promise<ApiResponse<RagBenchmarkHistoryItem[]>> {
    return this.request<RagBenchmarkHistoryItem[]>('/ai/benchmark-history')
  }

  async clearRagBenchmarkHistory(): Promise<ApiResponse<string>> {
    return this.request<string>('/ai/benchmark-history', {
      method: 'DELETE',
    })
  }

  async getRagBenchmarkHistoryAnalytics(): Promise<ApiResponse<RagBenchmarkHistoryAnalytics>> {
    return this.request<RagBenchmarkHistoryAnalytics>('/ai/benchmark-history/analytics')
  }

  // Comment endpoints
  async getComments(articleId: string, pageNumber = 1, pageSize = 20): Promise<ApiResponse<PaginatedResult<CommentDto>>> {
    return this.request<PaginatedResult<CommentDto>>(
      `/articles/${articleId}/comments?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    )
  }

  async createComment(articleId: string, data: CreateCommentDto): Promise<ApiResponse<CommentDto>> {
    return this.request<CommentDto>(`/articles/${articleId}/comments`, {
      method: 'POST',
      body: JSON.stringify({ ...data, articleId }),
    })
  }

  async updateComment(articleId: string, commentId: string, data: UpdateCommentDto): Promise<ApiResponse<CommentDto>> {
    return this.request<CommentDto>(`/articles/${articleId}/comments/${commentId}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  async deleteComment(articleId: string, commentId: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/articles/${articleId}/comments/${commentId}`, {
      method: 'DELETE',
    })
  }

  async likeComment(articleId: string, commentId: string): Promise<ApiResponse<CommentDto>> {
    return this.request<CommentDto>(`/articles/${articleId}/comments/${commentId}/like`, {
      method: 'POST',
    })
  }

  // Reaction endpoints
  async reactToArticle(articleId: string, reactionType: 'Like' | 'Bookmark' | 'Share'): Promise<ApiResponse<ArticleReactionResult>> {
    return this.request<ArticleReactionResult>(`/articles/${articleId}/react`, {
      method: 'POST',
      body: JSON.stringify({ reactionType }),
    })
  }

  async getMyReaction(articleId: string): Promise<ApiResponse<ArticleReactionResult>> {
    return this.request<ArticleReactionResult>(`/articles/${articleId}/my-reaction`)
  }

  async getAuditLogs(params?: AuditLogSearchParams): Promise<ApiResponse<PaginatedResult<AuditLogDto>>> {
    const query = new URLSearchParams()
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          query.set(key, String(value))
        }
      })
    }

    const suffix = query.toString() ? `?${query.toString()}` : ''
    return this.request<PaginatedResult<AuditLogDto>>(`/auditlogs${suffix}`)
  }

  async getAuditSummary(fromDate?: string, toDate?: string): Promise<ApiResponse<AuditSummaryDto>> {
    const query = new URLSearchParams()
    if (fromDate) {
      query.set('fromDate', fromDate)
    }
    if (toDate) {
      query.set('toDate', toDate)
    }

    const suffix = query.toString() ? `?${query.toString()}` : ''
    return this.request<AuditSummaryDto>(`/auditlogs/summary${suffix}`)
  }

  async exportAuditLogsCsv(params?: AuditLogSearchParams): Promise<Blob> {
    const query = new URLSearchParams()
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          query.set(key, String(value))
        }
      })
    }

    const suffix = query.toString() ? `?${query.toString()}` : ''
    const response = await fetch(`${API_BASE_URL}/auditlogs/export/csv${suffix}`, {
      method: 'GET',
      headers: {
        Authorization: this.token ? `Bearer ${this.token}` : '',
      },
    })

    if (!response.ok) {
      if (response.status === 401) {
        this.clearToken()
      }
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    return response.blob()
  }
}

export const api = new ApiClient()