export interface ApiResponse<T> {
  success: boolean
  data?: T
  message?: string
  errors?: Record<string, string[]>
}

export interface PaginatedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalPages: number
  totalCount: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// Auth types
export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  confirmPassword: string
  fullNameTh: string
  fullNameEn?: string
  faculty?: string
  department?: string
  position?: string
  employeeCode?: string
  bio?: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: UserDto
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface ProfileUserDto {
  id: string
  username: string
  email: string
  firstName?: string
  lastName?: string
  fullName?: string
  faculty?: string
  department?: string
  position?: string
  bio?: string
  isActive: boolean
  lastLoginAt?: string
  articleCount: number
  roles: { id?: string; name: string; description?: string; userCount?: number }[]
}

export interface AdminUserDto {
  id: string
  username: string
  email: string
  fullNameTh: string
  fullNameEn?: string
  faculty?: string
  department?: string
  position?: string
  isActive: boolean
  isLockedOut: boolean
  lockoutEnd?: string
  lastLoginAt?: string
  articleCount: number
  roleIds: string[]
  roleNames: string[]
}

export interface AdminRoleDto {
  id: string
  name: string
  description?: string
  userCount: number
}

export interface CreateAdminRoleRequest {
  name: string
  description?: string
}

export interface UpdateAdminRoleRequest {
  name?: string
  description?: string
}

export interface UpdateAdminUserRequest {
  isActive?: boolean
  roleIds?: string[]
}

export interface UpdateUserLockoutRequest {
  lockMinutes?: number
}

export interface ResetUserPasswordRequest {
  newPassword?: string
}

export interface ResetUserPasswordResponse {
  userId: string
  temporaryPassword: string
  updatedUser: AdminUserDto
}

export interface AdminSystemSettingDto {
  id: string
  key: string
  value?: string
  description?: string
  group?: string
  isEncrypted: boolean
  updatedAt?: string
  updatedById?: string
}

export interface UpsertAdminSystemSettingRequest {
  value?: string
  description?: string
  group?: string
  isEncrypted?: boolean
}

export interface AdminSettingPolicyDto {
  key: string
  valueType: 'string' | 'bool' | 'int' | 'json' | 'url'
  isLocked: boolean
  requiresEncrypted: boolean
  minValue?: number
  maxValue?: number
  description?: string
}

export interface AuditLogDto {
  id: string
  createdAt: string
  updatedAt?: string
  entityName: string
  entityId?: string
  action: string
  oldValues?: string
  newValues?: string
  ipAddress?: string
  userAgent?: string
  userId?: string
  userName?: string
}

export interface AuditSummaryDto {
  totalActions: number
  actionsByType: Record<string, number>
  actionsByEntity: Record<string, number>
  actionsByUser: Record<string, number>
  dailyActivity: Record<string, number>
}

export interface AuditLogSearchParams {
  entityName?: string
  entityId?: string
  action?: string
  userId?: string
  fromDate?: string
  toDate?: string
  search?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
}

export interface UpdateProfileRequest {
  firstName?: string
  lastName?: string
  faculty?: string
  department?: string
  position?: string
  bio?: string
}

export interface UpdateMediaMetadataRequest {
  title?: string
  description?: string
  altText?: string
  isPublic?: boolean
}

export interface MediaSearchQuery {
  mediaType?: string
  collectionName?: string
  entityType?: string
  entityId?: string
  uploaderId?: string
  isPublic?: boolean
  search?: string
  pageNumber?: number
  pageSize?: number
}

export interface UserDto {
  id: string
  email: string
  fullNameTh: string
  fullNameEn?: string
  faculty?: string
  department?: string
  position?: string
  employeeCode?: string
  bio?: string
  roles: string[]
}

// Article types
export interface ArticleDto {
  id: string
  title: string
  titleEn?: string
  slug: string
  content: string
  contentEn?: string
  summary?: string
  summaryEn?: string
  keywordsEn?: string
  status: 'draft' | 'under_review' | 'published' | 'archived'
  visibility: 'public' | 'internal' | 'restricted'
  viewCount: number
  likeCount: number
  bookmarkCount: number
  shareCount: number
  commentCount: number
  isPublished: boolean
  publishedAt?: string
  createdAt: string
  updatedAt: string
  createdById: string
  createdBy: UserDto
  categoryId?: string
  category?: CategoryDto
  tags: TagDto[]
  mediaItems: MediaItemDto[]
}

export interface CreateArticleDto {
  title: string
  titleEn?: string
  content: string
  contentEn?: string
  summary?: string
  summaryEn?: string
  keywordsEn?: string
  status?: 'draft' | 'under_review' | 'published' | 'archived'
  visibility?: 'public' | 'internal' | 'restricted'
  categoryId?: string
  tagIds?: string[]
}

export interface UpdateArticleDto {
  title?: string
  titleEn?: string
  content?: string
  contentEn?: string
  summary?: string
  summaryEn?: string
  keywordsEn?: string
  status?: 'draft' | 'under_review' | 'published' | 'archived'
  visibility?: 'public' | 'internal' | 'restricted'
  categoryId?: string
  tagIds?: string[]
}

// Category types
export interface CategoryDto {
  id: string
  name: string
  description?: string
  slug: string
  parentId?: string
  parentName?: string
  order: number
  isActive: boolean
  articleCount: number
  subCategoryCount: number
  createdAt: string
  updatedAt: string
  subCategories?: CategoryDto[]
}

export interface CreateCategoryRequest {
  name: string
  description?: string
  parentId?: string
  order?: number
  isActive?: boolean
}

export interface UpdateCategoryRequest {
  name?: string
  description?: string
  parentId?: string
  order?: number
  isActive?: boolean
}

// Tag types
export interface TagDto {
  id: string
  name: string
  nameEn?: string
  description?: string
  articleCount: number
  createdAt: string
  updatedAt: string
}

// Media types
export interface MediaItemDto {
  id: string
  fileName: string
  originalFileName: string
  filePath: string
  contentType: string
  fileSize: number
  mediaType: string
  title?: string
  description?: string
  altText?: string
  collectionName?: string
  entityType?: string
  entityId?: string
  isPublic: boolean
  width: number
  height: number
  duration: number
  uploaderId: string
  uploaderName: string
  url: string
  thumbnailUrl?: string
  createdAt: string
  updatedAt: string
}

// Comment types
export interface CommentDto {
  id: string
  content: string
  isAnonymous: boolean
  articleId: string
  articleTitle: string
  parentId?: string
  authorId: string
  authorName: string
  authorAvatarUrl?: string
  likeCount: number
  isEdited: boolean
  replies?: CommentDto[]
  createdAt: string
  updatedAt?: string
}

export interface CreateCommentDto {
  content: string
  articleId: string
  parentId?: string
  isAnonymous?: boolean
}

export interface UpdateCommentDto {
  content?: string
  isAnonymous?: boolean
}

// Reaction types
export interface ArticleReactionResult {
  likeCount: number
  bookmarkCount: number
  shareCount: number
  userLiked: boolean
  userBookmarked: boolean
  userShared: boolean
}

export interface LineWebhookTelemetryDto {
  windowSeconds: number
  processedEvents: number
  duplicateEvents: number
  rateLimitedEvents: number
  replyFailures: number
  processingErrors: number
}

// Search types
export interface SearchParams {
  query?: string
  categoryId?: string
  tagIds?: string[]
  status?: string
  visibility?: string
  authorId?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

// AI types
export interface AiWritingRequest {
  prompt: string
  context?: string
  type: 'generate' | 'improve' | 'translate' | 'suggest_tags'
  options?: Record<string, any>
}

export interface AiWritingResponse {
  content: string
  suggestions?: string[]
  tags?: string[]
  metadata?: Record<string, any>
}

export interface RagBenchmarkCaseRequest {
  caseId?: string
  question: string
  expectedKeywords: string[]
}

export interface EvaluateRagBatchRequest {
  promptProfile?: 'default' | 'balanced' | 'strict'
  topK?: number
  maxContextChars?: number
  semanticThreshold?: number
  cases: RagBenchmarkCaseRequest[]
}

export interface RagBenchmarkCaseResult {
  caseId: string
  question: string
  expectedKeywordCount: number
  answerKeywordHitCount: number
  contextKeywordHitCount: number
  answerKeywordCoverage: number
  contextKeywordCoverage: number
  passed: boolean
  missingKeywordsInAnswer: string[]
}

export interface RagBenchmarkSummaryResponse {
  promptProfileUsed: string
  totalCases: number
  passedCases: number
  passRate: number
  averageAnswerCoverage: number
  averageContextCoverage: number
  caseResults: RagBenchmarkCaseResult[]
}

export interface EvaluateRagCompareProfilesRequest {
  profiles?: Array<'default' | 'balanced' | 'strict'>
  topK?: number
  maxContextChars?: number
  semanticThreshold?: number
  cases: RagBenchmarkCaseRequest[]
}

export interface RagProfileComparisonResponse {
  totalCases: number
  profiles: RagBenchmarkSummaryResponse[]
}

export interface RagBenchmarkCompareInputSnapshot {
  cases: RagBenchmarkCaseRequest[]
  profiles: string[]
  topK: number
  maxContextChars: number
  semanticThreshold: number
}

export interface RagBenchmarkHistoryItem {
  id: string
  createdAtUtc: string
  createdByUserId?: string
  totalCases: number
  bestProfile?: string
  bestPassRate?: number
  profiles: string[]
  input?: RagBenchmarkCompareInputSnapshot
  payload: RagProfileComparisonResponse
}

export interface RagProfileStabilityMetric {
  profile: string
  sampleCount: number
  averagePassRate: number
  passRateStdDev: number
  stabilityScore: number
  recentAveragePassRate: number
  baselineAveragePassRate: number
  drift: number
  driftFlag: boolean
}

export interface RagBenchmarkHistoryAnalytics {
  totalRuns: number
  windowSizeRecent: number
  windowSizeBaseline: number
  latestRunAtUtc?: string
  profiles: RagProfileStabilityMetric[]
}
// ── Dashboard Analytics ───────────────────────────────────────────────────────
export interface DashboardStats {
  totalArticles: number
  publishedArticles: number
  draftArticles: number
  totalViews: number
  totalUsers: number
  activeUsers: number
  totalCategories: number
  totalComments: number
}

export interface ArticlesPerMonth {
  month: string
  count: number
}

export interface CategoryBreakdown {
  name: string
  count: number
}

export interface TopArticle {
  id: string
  title: string
  viewCount: number
  likeCount: number
  category: string
  publishedAt?: string
}

export interface RecentArticle {
  id: string
  title: string
  status: string
  author: string
  createdAt: string
}

export interface DashboardData {
  stats: DashboardStats
  articlesPerMonth: ArticlesPerMonth[]
  viewsPerMonth: ArticlesPerMonth[]
  categoryBreakdown: CategoryBreakdown[]
  statusBreakdown: CategoryBreakdown[]
  topArticles: TopArticle[]
  recentArticles: RecentArticle[]
}

// ── Two-Factor Authentication ─────────────────────────────────────────────────
export interface TwoFactorPendingResponse {
  requiresTwoFactor: true
  tempToken: string
}

export interface TwoFactorSetupDto {
  secret: string
  qrCodeUri: string
  isEnabled: boolean
}

// ── MinIO Storage ────────────────────────────────────────────────────────────
export interface StorageStatsDto {
  totalFiles: number
  totalSizeBytes: number
  totalSizeFormatted: string
  filesByType: Record<string, number>
  sizeByType: Record<string, number>
  bucketName: string
}

export interface StorageFileDto {
  key: string
  sizeBytes: number
  sizeFormatted: string
  contentType: string
  extension: string
  lastModified: string | null
  publicUrl: string
}

export interface StorageFileListDto {
  files: StorageFileDto[]
  totalCount: number
  page: number
  pageSize: number
  hasMore: boolean
}
