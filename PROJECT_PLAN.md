# KMS Project Plan (v4)

## 📋 Project Overview
**KMS (Knowledge Management System)** - ระบบจัดการองค์ความรู้สำหรับมหาวิทยาลัยเทคโนโลยีราชมงคลอีสาน วิทยาเขตสกลนคร

**Stack:** .NET 10 · ASP.NET Core Web API (Controllers) · React 19 + TypeScript + Vite · PostgreSQL 16 · EF Core 10 · pgvector
**AI Stack (v4):** Fallback Chain — OpenRouter `qwen/qwen3.6-plus:free` → XiaomiMiMo `mimo-v2-flash`
**Embedding:** OpenRouter `qwen/qwen3-embedding` (Queue on fail)
**Frontend:** React 19 · TanStack Query · TanStack Router · TipTap · Tailwind CSS · shadcn/ui
**เวอร์ชัน:** v4 — MediaLibrary · AI Fallback Chain (no Ollama) · Swagger+JWT · Publish-First · Immutable AuditLog

## ✅ Current Status (April 9, 2026 - อัพเดทล่าสุด)

### What's Done:
1. ✅ **Project Structure** - Clean Architecture (Domain / Application / Infrastructure / Api)
2. ✅ **Build & Tests** - Build สำเร็จ, tests pass ทั้ง 2 projects
3. ✅ **Domain Entities** - ครบตาม v4 spec (Identity, Knowledge, Media, Interaction, Logging, System)
4. ✅ **DbContext + Migrations** - PostgreSQL 16 + pgvector(1024) + uuid-ossp + unaccent
5. ✅ **Repository Pattern** - Generic + IArticleRepository
6. ✅ **Application Services** - ArticleService, CategoryService, TagService, MediaService, SearchService, NotificationService, AuditLogService + FluentValidation + Mapster
7. ✅ **API Controllers** - Articles, Auth, Categories, Tags, Comments, Media, Search, Notifications, AuditLogs
8. ✅ **JWT Authentication** - ASP.NET Core Identity + JWT Bearer + Role-based (Admin, Faculty, Researcher, Student, Guest)
9. ✅ **Swagger + JWT** - Swashbuckle 6.5.0 + SecurityRequirementsOperationFilter + swagger-auto-auth.js
   - Auto-authorize หลัง login (ไม่ต้องวาง token เอง)
   - Login panel ใน Authorize modal (Username/Password fields)
   - 🔒 icon ก่อน login → 🔓 หลัง login (ทั้งปุ่ม header และ endpoint)
   - Token persist ด้วย localStorage (F5 ยังรักษา token)
   - รายละเอียดทั้งหมดใน `docs/KMS_Swagger_JWT_Setup.md`
10. ✅ **AI Services** - OpenRouterChatService (P1) → XiaomiMimoChatService (P2) fallback chain
11. ✅ **Embedding Service** - OpenRouterEmbeddingService + queue-on-fail
12. ✅ **MediaLibrary** - MediaItem polymorphic + ImageSharp processor (thumbnail/WebP)
13. ✅ **Publish-First Workflow** - Admin/Faculty direct publish, Researcher ต้อง review
14. ✅ **Seed Data** - Users, Roles, Categories, Tags, Articles (admin@rmuti.ac.th / Admin@1234)
15. ✅ **React Frontend** - React 19 + TypeScript + Vite + TanStack Query/Router + TipTap
   - Routes: `/` `/login` `/register` `/articles` `/articles/create` `/articles/:id` `/articles/:id/edit` `/profile` `/media` `/admin` `/admin/users` `/admin/categories` `/admin/audit` `/admin/settings` `/admin/roles`
   - Hooks: useAuth, useArticles, useComments
    - API Service: JWT-aware client
16. ✅ **Article Detail Page** - แสดงบทความเดี่ยว + tags + attachments + comments section
17. ✅ **Comments API + UI** - CommentsController + create/list/reply/like endpoints + frontend integration
18. ✅ **Media Upload UI** - drag-and-drop upload component + attach file ในหน้า Article Detail
19. ✅ **AI Writing API** - `/api/ai/draft` `/api/ai/improve` `/api/ai/translate` `/api/ai/tags`
20. ✅ **Frontend AI Writing Panel** - Generate/Improve/Translate/Suggest Tags in article editor
21. ✅ **SSE Streaming Responses** - `/api/ai/stream?access_token=...` + EventSource live preview
22. ✅ **Article Reactions** - `/api/articles/{id}/react` + Like / Bookmark toggle ในหน้า Article Detail
23. ✅ **Create Page Media Upload** - อัปโหลด cover/attachments ได้ทันทีหลังสร้างบทความสำเร็จ
24. ✅ **Admin Frontend Scaffold** - เพิ่ม `/admin` `/admin/users` `/admin/categories` พร้อมเชื่อม API พื้นฐาน
25. ✅ **Admin User Management (Basic)** - `/api/admin/users` `/api/admin/roles` + แก้ไข role/active state จากหน้า admin ได้
26. ✅ **Admin Category Management (Basic)** - สร้าง/ลบ category จากหน้า `/admin/categories`
27. ✅ **Frontend API Response Normalization** - client รองรับทั้ง raw JSON และ `ApiResponse<T>`
28. ✅ **Article Edit Page** - `/articles/:id/edit` พร้อม editor, AI panel, save/review flow
29. ✅ **Edit Page Media Gallery** - upload/delete cover และ attachments ได้จากหน้า edit
30. ✅ **Admin UX Upgrade** - user search/filter + category update form
31. ✅ **Profile Page + Avatar Workflow** - `/profile` + update profile + upload/delete avatar
32. ✅ **Media Metadata Editing** - แก้ไข title/alt text/description ได้ใน gallery ของหน้า edit/profile
33. ✅ **Media Library + Search/History UX** - `/media` + search/filter/grouping + profile media history section
34. ✅ **Admin Security + Category Tree Controls (Baseline)** - lock/unlock/reset-password tools + parent/order/isActive editing in categories
35. ✅ **Admin Audit Dashboard (Baseline+)** - `/admin/audit` พร้อม filters, quick date presets, summary cards, previous-period compare, top users/daily insights + spike indicator, activity trend chart, activity table, pagination และ export CSV
36. ✅ **Admin Settings (Baseline)** - `/admin/settings` + list/search/group + create/update key-value settings
37. ✅ **Category Tree Reorder (Bulk Controls)** - ปรับลำดับด้วยปุ่ม Up/Down ตาม sibling group + save draft ทั้งชุด
38. ✅ **Settings Governance (Phase 1)** - key format validation, protected prefixes, locked keys, sensitive-key encryption guardrails
39. ✅ **Category Tree Drag-Drop (Phase 1)** - ลากวาง reorder ใน sibling group + ใช้ draft/bulk-save pipeline เดิม
40. ✅ **Settings Governance (Phase 2)** - policy endpoint + typed validation (`bool/int/json/url`) + policy-aware admin UI
41. ✅ **Category Tree UX (Phase 2)** - cross-parent drag/drop + loop/depth safety constraints + root drop zone
42. ✅ **Audit Analytics (Advanced)** - trend chart + refined anomaly scoring + actionable insight shortcuts
43. ✅ **Media Library Advanced UX** - pagination + filter presets + multi-select bulk delete
44. ✅ **RAG QA Endpoint Upgrade** - `/api/ai/answer` และ `/api/ai/stream-rag` พร้อม hybrid retrieval, context assembly, source tracing
45. ✅ **Line OA Integration (Phase 1)** - webhook endpoint + signature verification + text message handling + AI auto-reply
46. ✅ **Line OA Integration (Phase 2A)** - command routing (`/search`, `/help`), webhook event dedup, fallback/help strategy
47. ✅ **Line OA Integration (Phase 2B - Partial)** - reply retry/backoff + event-level telemetry scope + persistent dedup store + inbound rate-limiting config
48. ✅ **RAG Evaluation Endpoint** - `/api/ai/evaluate` พร้อม keyword coverage metrics และ source diagnostics
49. ✅ **Line OA Observability (Increment)** - rolling webhook telemetry counters + alert-threshold logging + admin telemetry endpoint (`/api/line/webhook/telemetry`)
50. ✅ **Line OA External Alerts (Increment)** - webhook alert channel integration (Slack/Pager-compatible) + per-metric cooldown anti-spam
51. ✅ **Full Validation Sweep (Backend + Frontend)** - `dotnet build` และ `npx tsc --noEmit` ผ่านหลังรวม LINE hardening + alerts
52. ✅ **Line OA Admin Dashboard (Increment)** - แสดง telemetry/SLO health panel บนหน้า admin settings พร้อม refresh + live query
53. ✅ **Line OA Incident Runbook** - operational SLOs, severity matrix, triage steps, escalation flow, post-incident checklist
54. ✅ **RAG Benchmark Workflow (Batch)** - เพิ่ม `/api/ai/evaluate-batch` + summary metrics + benchmark docs/sample cases
55. ✅ **RAG Prompt Strategy Controls** - prompt profiles (`default/balanced/strict`) + `/api/ai/evaluate-compare` สำหรับ A/B benchmark
56. ✅ **Admin RAG Benchmark Runner** - `/admin/rag` หน้า UI สำหรับใส่ benchmark cases, compare profiles, และ leaderboard รายโปรไฟล์
57. ✅ **Admin RAG Export Support** - export compare results เป็น JSON/CSV จากหน้า `/admin/rag` สำหรับ release review และ traceability
58. ✅ **Admin RAG History Persistence** - เก็บผล benchmark compare ล่าสุด 20 ครั้งใน localStorage พร้อม reuse parameters
59. ✅ **Admin RAG Shared History (Backend)** - persist compare history ผ่าน `/api/ai/benchmark-history` และแสดงผลข้าม session ใน `/admin/rag`
60. ✅ **Admin RAG Shared History UX Refinement** - เพิ่ม filter/search/date/min-cases + load run จาก shared history เพื่อ reuse benchmark context ได้เร็วขึ้น
61. ✅ **Admin RAG Replay Fidelity (Shared History)** - persist compare input snapshot (profiles/topK/context/threshold/cases+expectedKeywords) และ restore ได้ครบจาก shared history
62. ✅ **Admin RAG History Analytics** - เพิ่ม `/api/ai/benchmark-history/analytics` + trend baseline, drift flag, profile stability scoring ใน `/admin/rag`
63. ✅ **DB Seeded** — 4 users, 5 categories, 10 tags, 2 articles ใน `KMS_Dev` / admin: `denpha` / `Denpha_@$&2022`
64. ✅ **Npgsql Vulnerability Fixed** — Pgvector 0.2.0 → 0.3.2 (ขจัด NU1903 high severity)
65. ✅ **Build Warnings Eliminated** — แก้ CA2024 ×5 (EndOfStream → null-check) + CS8603 ×2 (nullable cast) → 0 errors, 0 warnings
66. ✅ **Project Renamed SkcKMS → KMS** — namespaces, folders, .csproj, .slnx ทั้งหมด
67. ✅ **Deployment Hardening (Phase 1)** — `appsettings.Production.json` (#{PLACEHOLDER}# tokens), `.env.example`, `.gitignore`
68. ✅ **Role Management CRUD** — `POST/PUT/DELETE /api/admin/roles` + frontend `/admin/roles` page + TanStack Query hooks
69. ✅ **AdminController Split** — แยก AdminController.cs (756 lines) → `AdminUsersController` + `AdminRolesController` + `AdminSettingsController` + `AdminDtos.cs`
70. ✅ **Controllers Reorganized** — `Controllers/Admin/` (namespace `KMS.Api.Controllers.Admin`) + `Controllers/Api/` (namespace `KMS.Api.Controllers.Api`)

### ✅ Phase 8 — Production Features (เสร็จสมบูรณ์ทั้งหมด):
71. ✅ **Redis Cache Integration** — StackExchange.Redis + `IDistributedCache` + article/search/session cache + TTL policy
72. ✅ **Health Check Endpoints** — `/health` + `/health/ready` (DB + Redis + MinIO) + custom liveness probe
73. ✅ **Refresh Token Flow** — `RefreshTokens` table + `/api/auth/refresh` + `/api/auth/logout` + 7-day rolling window
74. ✅ **Rate Limiting** — `RateLimiter` middleware บน `/api/ai/*` + `/api/line/webhook` (sliding window)
75. ✅ **Email Service (SMTP)** — `SmtpEmailService` + `/api/auth/forgot-password` + `/api/auth/reset-password` + email confirmation
76. ✅ **MinIO Storage** — `MinioFileStorageService` ครบ 16 methods + systemd service + `FileStorage:Provider` DI switch + auto-create bucket
77. ✅ **Article Draft Autosave** — debounced 30s autosave ใน article editor + "กำลังบันทึก…/บันทึกแล้ว" indicator
78. ✅ **Dashboard Analytics** — `AdminDashboardController` + Bar/Area/Pie charts (recharts) + TopArticles/RecentArticles tables + dark mode support
79. ✅ **Dark Mode** — ThemeContext + localStorage persist + `dark:` Tailwind classes ทั่ว AdminLayout + dashboard
80. ✅ **2FA / TOTP** — `OtpNet` library + `IsTwoFactorAuthEnabled`/`TwoFactorSecretKey` บน AppUser + 4 endpoints + Login OTP step + Profile setup/enable/disable UI
81. ✅ **PDF Export** — `QuestPDF` + `HtmlAgilityPack` + `GET /api/export/article/{id}/pdf` + ปุ่ม "📄 PDF" ใน article detail

### Remaining Work (Current Focus):
1. ✅ **Security Remediation** — Npgsql advisory fixed (Pgvector 0.2.0 → 0.3.2), build 0 warnings
2. ✅ **Deployment Hardening (Phase 1)** — production config template + secrets management complete
3. ✅ **Phase 8 Features** — Redis, Health, Auth, Email, MinIO, Autosave, Dashboard, Dark Mode, 2FA, PDF Export — ทั้งหมดเสร็จสมบูรณ์
4. 🔄 **Release Readiness** — smoke tests for API + frontend + admin RAG benchmark workflows
5. 🔄 **Operational Runbook Finalization** — rollout/rollback steps and post-deploy validation checklist

### Planned Features (Phase 9+):
#### 🥇 Production Readiness
- 🔲 **Word Export** — Export บทความเป็น `.docx` (DocumentFormat.OpenXml)
- 🔲 **Email Verification UI** — frontend flow สำหรับ verify email + resend confirmation
- 🔲 **Forgot Password UI** — หน้า forgot/reset password สำหรับ frontend
- 🔲 **MinIO Frontend Panel** — admin UI สำหรับดู bucket usage + file browser

## 🗺️ Implementation Roadmap (v4 Alignment)

### ✅ Phase 1: Foundation (Core Domain & Database) - เสร็จสมบูรณ์
1. ✅ **Entity Classes** - Complete v4 entity structure
   - Identity: AppUser, Role, UserRole (extends IdentityUser<Guid>)
   - Knowledge: KnowledgeArticle, ArticleVersion, Category, Tag, ArticleTag
   - Media: MediaItem (v4 MediaLibrary with polymorphic associations)
   - Interaction: Comment, ArticleReaction, Notification
   - Logging: AuditLog, AiWritingLog, KnowledgeSearchLog
   - System: SystemSetting, ApiKey

2. ✅ **DbContext & Configuration** - ApplicationDbContext with all v4 configurations
3. ✅ **EF Core Migrations** - Initial migration + pgvector(1024) extension
4. ✅ **Repository Pattern** - Generic + specific repositories (IArticleRepository)
5. ✅ **Database Setup** - PostgreSQL 16 พร้อมใช้งาน (KMS_Dev)
6. ✅ **Basic API Endpoints** - CRUD สำหรับ Articles, Categories, Tags, Auth

### ✅ Phase 2: Application Layer & Services - เสร็จสมบูรณ์
1. ✅ **DTOs** - Complete DTO structure for all entities
   - Base DTOs: PaginatedResult, SearchParams, etc.
   - Identity DTOs: UserDto, RoleDto, LoginDto, RegisterDto
   - Knowledge DTOs: ArticleDto, CategoryDto, TagDto, ArticleVersionDto
   - Media DTOs: MediaItemDto (v4 MediaLibrary)
   - Interaction DTOs: CommentDto, ReactionDto, NotificationDto
   - Logging DTOs: AuditLogDto, AiWritingLogDto
   - System DTOs: SystemSettingDto, ApiKeyDto

2. ✅ **Services** - Business logic services implemented
   - ArticleService (CRUD + versioning + publishing)
   - CategoryService (hierarchical + tree structure)
   - TagService, MediaService, SearchService
   - NotificationService, AuditLogService

3. ✅ **Validation** - FluentValidation rules for all Create/Update DTOs
4. ✅ **Mapping** - Mapster configuration for entity-DTO mapping
5. ✅ **API Integration** - All controllers use Application services

### ✅ Phase 3: API Layer & Authentication - เสร็จสมบูรณ์
1. ✅ **Endpoints** - CRUD operations for core entities
   - Articles, Categories, Tags, Auth, Comments endpoints
   - Media, Search, Notifications, AuditLogs endpoints (Phase 4)

2. ✅ **Authentication/Authorization** - JWT with ASP.NET Core Identity
   - ASP.NET Core Identity setup complete
   - JWT token generation and validation
   - Role-based authorization (5 roles: Admin, Faculty, Researcher, Student, Guest)

3. ✅ **Swagger Documentation** - OpenAPI with JWT support
4. ✅ **Exception Handling** - Global exception handling middleware
5. ✅ **Response Wrappers** - Consistent ApiResponse<T> format

### ✅ Phase 4: Advanced Features & MediaLibrary - COMPLETE
1. ✅ **MediaLibrary (v4)** - MediaItem with polymorphic associations
   - Collections: cover, attachments, avatar
   - Storage providers: Local, MinIO, S3 ready
   - File upload/download functionality
   - ✅ **ImageSharp Media Processing** - Thumbnail generation and WebP conversion

2. ✅ **Search Service** - Full-text + pgvector semantic search
3. ✅ **Notification Service** - Real-time user notifications
4. ✅ **Audit Log Service** - Automatic audit trail (append-only)
5. ✅ **Publish-First Workflow** - Role-based publishing implemented
   - Admin/Faculty: Direct publish (skip review)
   - Researcher: Require review before publishing

**Current Status:** Phase 4 implementation is complete with all features implemented and tested.

### ✅ Phase 5: AI Integration (v4 Cloud-Only) - COMPLETE
1. ✅ **AI Services Architecture** - v4 fallback chain implemented
   - ✅ OpenRouterChatService (P1: qwen/qwen3.6-plus:free)
   - ✅ XiaomiMimoChatService (P2: mimo-v2-flash - last resort)
   - ✅ FallbackChatService (orchestrates P1→P2)

2. ✅ **Embedding Service** - OpenRouter qwen/qwen3-embedding
   - ✅ Queue-on-fail mechanism implemented
   - ✅ 1024-dimensional vectors

3. ✅ **AI Writing Assistant** - Implemented end-to-end
   - ✅ Generate Draft endpoint + frontend panel integration
   - ✅ Improve Text endpoint + frontend panel integration
   - ✅ Auto Translate endpoint + frontend panel integration
   - ✅ Auto Tag endpoint + frontend panel integration

4. ✅ **RAG Pipeline** - Question answering + benchmark workflow completed
   - ✅ Embedding service available
   - ✅ pgvector similarity search available
   - ✅ Context assembly + source tracing endpoints (`/api/ai/answer`, `/api/ai/stream-rag`)
   - ✅ Prompt strategy tuning + answer quality evaluation (`/api/ai/evaluate`, `/api/ai/evaluate-batch`, `/api/ai/evaluate-compare`)
   - ✅ Streaming responses (SSE) - Implemented (`/api/ai/stream`)

### Phase 6: Line OA Integration
1. ✅ **Line Webhook Handler (Phase 1)** - `POST /api/line/webhook` + signature verification
2. ✅ **Message Processing (Phase 1)** - รองรับ text message และ trigger AI answer flow
3. ✅ **Line API Client (Phase 1)** - reply API integration ผ่าน `LineOA` HttpClient
4. ✅ **Routing Strategy (Phase 2A)** - command routing (`/search`, `/help`) + default QA fallback
5. ✅ **Idempotency Guard (Phase 2A)** - dedup webhook events via `webhookEventId` window
6. ✅ **RAG Response Tuning (Phase 2B)** - prompt profiles + benchmark compare workflow completed
7. ✅ **Production Hardening (Phase 2B)** - persistent dedup store, structured observability, rate-limit guard completed

### ✅ Phase 7: React Frontend (v4 Specification) - COMPLETE
1. ✅ **Project Setup** - React 19 + TypeScript + Vite
   - ✅ Created `kms-web/` directory with Vite template
   - ✅ Installed React 19.2.4 with TypeScript
   - ✅ Configured development server (http://localhost:5173)

2. ✅ **UI Components** - shadcn/ui with Tailwind CSS
   - ✅ Installed Tailwind CSS with PostCSS
   - ✅ Set up shadcn/ui dependencies (class-variance-authority, clsx, tailwind-merge, lucide-react)
   - ✅ Created responsive, modern UI components

3. ✅ **State Management** - TanStack Query + Zustand
   - ✅ Installed and configured TanStack Query (@tanstack/react-query)
   - ✅ Set up QueryClientProvider with devtools
   - ✅ Created custom hooks for API integration (useArticles, useAuth)

4. ✅ **Routing** - TanStack Router
   - ✅ Installed and configured TanStack Router (@tanstack/react-router)
   - ✅ Set up file-based routing system
   - ✅ Created routes for: `/` (home), `/login`, `/register`, `/profile`, `/media`, `/articles`, `/articles/create`, `/articles/:id`, `/articles/:id/edit`, `/admin`, `/admin/users`, `/admin/categories`, `/admin/audit`, `/admin/settings`, `/admin/roles`

5. ✅ **Editor** - TipTap rich text editor
   - ✅ Installed TipTap with extensions (@tiptap/react, @tiptap/starter-kit, @tiptap/extension-placeholder)
   - ✅ Created reusable RichTextEditor component
   - ✅ Added toolbar with formatting options (bold, italic, headings, lists)

6. ✅ **Pages** - Core pages + advanced admin/RAG management completed
   - ✅ **Auth Pages**: Login and Register with form validation
   - ✅ **Articles Pages**: List view + Create form + Article Detail page + Edit page + reactions
   - ✅ **Profile Page**: update profile + avatar upload/delete + media history
   - ✅ **Media Library Page**: search/filter/group by collection
   - ✅ **AI Writing Panel**: Generate/Improve/Translate/Suggest Tags + stream preview
   - ✅ **Admin Pages**: Overview + Users (role/active update + search/filter + lock/unlock + reset-password) + Categories (create/delete/update + parent/order/isActive + bulk reorder + drag-drop reorder + cross-parent move safeguards) + Audit (summary + filtered activity table + export + quick presets + compare + spike indicator) + Settings (list/search/create/update + governance phase 1-2 with policy-aware typed validation)
   - ✅ **Home Page**: Dashboard with feature overview
   - ✅ Advanced admin core milestones complete (settings governance 1-2, category tree phase 1-2, audit analytics advanced)

7. ✅ **API Integration** - Full backend connectivity
   - ✅ Created TypeScript API types matching backend DTOs
   - ✅ Built API client service with JWT authentication support
   - ✅ Implemented React Query hooks for all major endpoints
   - ✅ Integrated API calls into all pages

8. ✅ **Backend API Connection** - Full-stack integration complete
   - ✅ Backend API Swashbuckle version issue resolved
   - ✅ API running successfully on http://localhost:5000
   - ✅ Database seeding completed successfully
   - ✅ Swagger documentation available at http://localhost:5000/swagger
   - ✅ Full-stack application now operational

## 🗂️ Domain Entities Structure (v4 Alignment)

### ✅ Identity Entities - เสร็จสมบูรณ์ (v4 compliant):
- `AppUser` - ผู้ใช้งานระบบ (extends IdentityUser<Guid>)
  - ลบ `AvatarUrl` ออก (v4: ใช้ MediaItems collection `avatar` แทน)
  - มี custom fields: FullNameTh, FullNameEn, Faculty, Department, Position, EmployeeCode, Bio
- `AppRole` - บทบาท (extends IdentityRole<Guid>)
  - Roles: Admin, Faculty, Researcher, Student, Guest
  - Permissions: JSON array of permissions
- `AppUserRole` - Many-to-many relationship
- **Publish-First Workflow (v4):**
  - Admin/Faculty: Publish โดยตรง (ข้าม Review ได้)
  - Researcher: ต้องผ่าน Review ก่อนเผยแพร่

### ✅ Knowledge Entities - เสร็จสมบูรณ์ (v4 compliant):
- `KnowledgeArticle` - บทความ/ความรู้ (มี pgvector embedding 1024-dim)
  - ✅ Title, TitleEn, Slug, Content, **ContentEn**, Summary, SummaryEn, KeywordsEn
  - ✅ **ContentEn field implemented** (v4 Thai-First strategy)
  - ลบ `CoverImageUrl` ออก (v4: ใช้ MediaItems collection `cover` แทน)
  - Status: Draft, UnderReview, Published, Archived
  - Visibility: Public, Internal, Restricted
- `ArticleVersion` - เวอร์ชันของบทความ (version control)
  - ✅ ContentEn field included in version history
- `Category` - หมวดหมู่แบบ tree structure (ParentId for hierarchy)
- `Tag` - Tags สำหรับบทความ
- `ArticleTag` - Many-to-many relationship

### ✅ Media Entities (v4 MediaLibrary) - เสร็จสมบูรณ์:
- `MediaItem` - ตารางกลางสำหรับจัดการไฟล์ทุกประเภท
  - Polymorphic association: ModelType + ModelId
  - Collections: `cover` (บทความ), `attachments` (บทความ), `avatar` (ผู้ใช้)
  - Storage: Local, MinIO, S3
  - Conversions: thumb, card, og (สำหรับรูปภาพ)
  - Metadata: Size, MimeType, Manipulations (JSON), CustomProperties (JSON)

### ✅ Interaction Entities - เสร็จสมบูรณ์:
- `Comment` - ความคิดเห็นในบทความ (support nested comments via ParentId)
- `ArticleReaction` - รวม Like, Bookmark, Share
- `Notification` - การแจ้งเตือนผู้ใช้ (ArticlePublished, CommentAdded, ReviewRequested, AiComplete)

### ✅ Logging Entities - เสร็จสมบูรณ์:
- `AuditLog` - บันทึกการกระทำทั้งหมด (append-only, immutable)
- `AiWritingLog` - บันทึกการใช้ AI writing assistant
- `KnowledgeSearchLog` - บันทึกการค้นหา

### ✅ System Entities - เสร็จสมบูรณ์:
- `SystemSetting` - ตั้งค่าระบบ
- `ApiKey` - API keys สำหรับ external access

## ⚙️ Technical Decisions (v4 Updates)

### Database:
- **PostgreSQL 16** - Primary database (v4 specifies 16)
- **pgvector(1024)** - สำหรับ semantic search (1024 dimensions for qwen3-embedding)
- **EF Core 10** - ORM with code-first migrations
- **Extensions:** uuid-ossp, vector, unaccent

### Authentication:
- **ASP.NET Core Identity** - User management
- **JWT Bearer** - API authentication
- **Role-based Authorization** - 5 roles with different permissions
- **Publish-First Workflow:** Admin/Faculty publish directly, Researchers require review

### AI Services (v4 Cloud-Only Fallback Chain):
> **v4 เปลี่ยน:** ตัด Ollama ออกทั้งหมด — ใช้ Cloud providers เท่านั้น

**Chat Services (Priority Chain):**
1. **Primary (P1):** OpenRouter `qwen/qwen3.6-plus:free`
   - Cloud-based, no local runtime required
   - Headers: HTTP-Referer, X-Title for API compliance

2. **Last Resort (P2):** XiaomiMiMo `mimo-v2-flash`
   - Fallback when OpenRouter fails
   - OpenAI-compatible API

**Embedding Service:**
- **OpenRouter:** `qwen/qwen3-embedding` (1024 dimensions)
- **Queue-on-Fail:** Retry mechanism when embedding fails
- **MRL Support:** Multi-lingual representation learning (TH+EN combined)

**AI Pipeline (RAG):**
```
Question → Embedding → pgvector similarity search → Context + Prompt → AI Response
```

### File Storage (v4 MediaLibrary):
1. **Local Storage** - สำหรับ development (`uploads/` directory)
2. **MinIO** - สำหรับ staging/production (S3-compatible)
3. **AWS S3** - สำหรับ production scale
4. **Image Processing:** SixLabors.ImageSharp สำหรับ thumbnail/WebP conversions

### API Documentation:
- **Swagger/OpenAPI** - แทนที่ Scalar (v4 specifies Swashbuckle)
  - ✅ **Swashbuckle Version Compatibility Fixed** - Resolved TypeLoadException by removing conflicting Microsoft.AspNetCore.OpenApi package
  - ✅ **Swashbuckle 6.5.0** - Stable version compatible with .NET 10
  - ✅ **API Running Successfully** - Backend API now operational on http://localhost:5000
- **JWT Integration** - Lock icon สำหรับ secured endpoints
- **Security Requirements Filter** - แสดง authentication requirements

## 📅 Next Steps (Priority Order)

### 🥇 ทำต่อเลย — Production Readiness
1. ✅ **Dependency/Vulnerability Remediation** — Pgvector upgraded, 0 build warnings ✅
2. ✅ **Deployment Hardening** — production appsettings + secrets template complete ✅

### 🥈 สำคัญ — Verification & Rollout
3. **Release Smoke Tests**
   - ทดสอบ login/auth, articles CRUD, media flow, admin pages
   - ทดสอบ `/admin/rag` ทั้ง compare/history/replay/analytics

4. **Go-Live Runbook Finalization**
   - pre-deploy / deploy / post-deploy steps
   - rollback criteria + incident escalation path

### 🥉 ระยะถัดไป — Phase 8
5. **Redis Cache Integration** — cache article list, search results, user sessions
6. **Health Check + Rate Limiting** — `/health` endpoint + ASP.NET Core rate limiter บน AI/Line routes
7. **Refresh Token** — RefreshToken entity + `/api/auth/refresh` endpoint + frontend token rotation
8. **Email Service** — เชื่อม SMTP จริง (notification, reset password)
9. **MinIO Integration** — production file storage
10. **Article Draft Autosave** — periodic save ทุก 30 วินาที

---

## 🔧 Development Setup (อัพเดท)

### Prerequisites (v4):
- ✅ **.NET 10 SDK** - Required for all .NET projects
- ✅ **PostgreSQL 18.x** — ติดตั้งโดยตรง (ไม่ใช้ Docker), pgvector extension
- ✅ **Redis 8.x** — ติดตั้งโดยตรง (ไม่ใช้ Docker), localhost:6379
- ✅ **Node.js 20+** — for React frontend (kms-web)
- ❌ **Ollama** — **Removed in v4** (ใช้ Cloud AI providers เท่านั้น)
- ✅ **OpenRouter Account** - สำหรับ AI Chat (P1) + Embedding
- ✅ **XiaomiMiMo Account** (optional) - สำหรับ AI Chat fallback (P2)
- ✅ **Package Version Compatibility** - Swashbuckle 6.5.0 (not 7.x or 8.x) for .NET 10 compatibility

### Environment Variables (v4 Configuration):

**appsettings.json (v4 structure):**
```json
{
  "ConnectionStrings": {
   "Default": "Host=localhost;Database=KMS_Dev;Username=postgres;Password=<DB_PASSWORD>",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "your-super-secret-key-minimum-32-characters!!",
    "Issuer": "KmsApi",
    "Audience": "KmsClient",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "AI": {
    "Chat": {
      "Providers": [
        {
          "Name": "OpenRouter",
          "Endpoint": "https://openrouter.ai/api/v1",
          "Model": "qwen/qwen3.6-plus:free",
          "Priority": 1,
          "Enabled": true,
          "ApiKey": "sk-or-v1-xxxxxxxxxxxxxxxxxxxx"
        },
        {
          "Name": "XiaomiMiMo",
          "Endpoint": "https://api.xiaomimimo.com/v1",
          "Model": "mimo-v2-flash",
          "Priority": 2,
          "Enabled": true,
          "ApiKey": "sk-mimo-xxxxxxxxxxxxxxxxxxxx"
        }
      ]
    },
    "Embedding": {
      "Dimensions": 1024,
      "QueueOnAllFailed": true,
      "Providers": [
        {
          "Name": "OpenRouter",
          "Endpoint": "https://openrouter.ai/api/v1",
          "Model": "qwen/qwen3.6-plus:free",
          "Priority": 1,
          "Enabled": true,
          "ApiKey": "sk-or-v1-xxxxxxxxxxxxxxxxxxxx"
        }
      ]
    }
  },
  "Storage": {
    "Provider": "Local",
    "BasePath": "uploads",
    "MaxFileSizeMB": 100
  },
  "AllowedOrigins": [
    "http://localhost:5173"
  ]
}
```

**Development Secrets (User Secrets):**
```bash
# JWT Secret
dotnet user-secrets set "Jwt:Key" "your-real-secret-key-here-minimum-32-chars"

# Database
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=SkcKmsDb;Username=postgres;Password=<DB_PASSWORD>"

# OpenRouter API Keys (Chat P1 + Embedding P1)
dotnet user-secrets set "AI:Chat:Providers:0:ApiKey" "sk-or-v1-xxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "AI:Embedding:Providers:0:ApiKey" "sk-or-v1-xxxxxxxxxxxxxxxxxxxx"

# XiaomiMiMo API Key (Chat P2 — Last Resort)
dotnet user-secrets set "AI:Chat:Providers:1:ApiKey" "sk-mimo-xxxxxxxxxxxxxxxxxxxx"
```

### 🚀 How to Run the Project (v4):

#### 1. Docker Setup (Optional):
```bash
# PostgreSQL 16 with pgvector
docker run --name skcKMS-db \
   -e POSTGRES_PASSWORD=<DB_PASSWORD> \
  -e POSTGRES_DB=SkcKmsDb \
  -p 5432:5432 \
  -d postgres:16

# Redis (สำหรับ caching)
docker run --name skcKMS-redis \
  -p 6379:6379 \
  -d redis:7-alpine

# MinIO (optional - สำหรับ object storage)
docker run --name skcKMS-minio \
  -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  -d minio/minio server /data --console-address ":9001"
```

#### 2. Database Setup:
```bash
# ติดตั้ง pgvector extension
PGPASSWORD=<DB_PASSWORD> psql -h localhost -U postgres -d SkcKmsDb -c "CREATE EXTENSION IF NOT EXISTS vector;"

# อัพเดท database migrations
cd /home/denpha/Dotnet10/KMS02
dotnet ef database update --project src/KMS.Infrastructure --startup-project src/KMS.Api

# สร้าง uploads directory (สำหรับ local storage)
mkdir -p uploads/KnowledgeArticle uploads/AppUser
```

#### 3. Configure User Secrets:
```bash
cd src/KMS.Api
dotnet user-secrets init

# ตั้งค่า secrets (ตามตัวอย่างด้านบน)
dotnet user-secrets set "Jwt:Key" "your-real-secret-key-here-minimum-32-chars"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=SkcKmsDb;Username=postgres;Password=<DB_PASSWORD>"
```

#### 4. Run API:
```bash
cd /home/denpha/Dotnet10/KMS02
dotnet run --project src/KMS.Api --urls "http://localhost:5000"

# ✅ API now runs successfully with:
# - Swashbuckle 6.5.0 (Swagger documentation available at http://localhost:5000/swagger)
# - Database seeding completed automatically
# - JWT authentication configured

# หรือใช้ watch mode สำหรับ development
dotnet watch run --project src/KMS.Api --urls "http://localhost:5000"
```

#### 5. Test API:
```bash
# ดึงบทความทั้งหมด
curl http://localhost:5000/api/articles

# Login และ get JWT token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin@123"}'

# ดึงบทความพร้อม authorization
curl http://localhost:5000/api/articles \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

#### 6. Build and Test:
```bash
# Build โปรเจค
dotnet build

# Run tests
dotnet test

# Run specific test project
dotnet test tests/KMS.Api.Tests
```

## 📝 Notes

### Thai-First Bilingual Strategy (ContentEn Implemented):
- เนื้อหาภาษาไทย **required** ในทุก entity
- ภาษาอังกฤษ **optional** (suffix `En` เช่น `TitleEn`, `ContentEn`, `SummaryEn`)
- ✅ **ContentEn field implemented** - Stores English translations of article content
- AI แปลอัตโนมัติเมื่อต้องการ
- Vector embedding รวม 2 ภาษาใน field เดียว

### Publish-First Workflow:
- Admin/Faculty: สามารถเผยแพร่ได้โดยตรง
- Researcher: ต้องผ่าน review ก่อนเผยแพร่
- Audit Log บันทึกทุก action (ลบ/แก้ไขไม่ได้)

## 📊 Project Progress Summary (v4 Alignment)

### ✅ Foundation Complete (Phases 1-4):
1. **Database Layer** - Complete v4 entity structure with MediaLibrary + **ContentEn field**
2. **Repository Pattern** - Generic + specific repositories with Query property
3. **Application Layer** - Complete DTOs, Services, Validators, Mapping (includes **ContentEn**)
4. **API Layer** - Core endpoints + Authentication/Authorization + Swagger (supports **ContentEn**)
5. **MediaLibrary (v4)** - MediaItem with polymorphic associations + storage providers
6. **Advanced Services** - SearchService, NotificationService, AuditLogService implemented
7. **ContentEn Implementation** - Complete bilingual support across all layers:
   - ✅ Entity classes (KnowledgeArticle, ArticleVersion)
   - ✅ DTOs (ArticleDto, CreateArticleDto, UpdateArticleDto, ArticleVersionDto)
   - ✅ Services (ArticleService handles ContentEn in CRUD operations)
   - ✅ API models (CreateArticleRequest, UpdateArticleRequest, ArticleResponse)
   - ✅ Controllers (ArticlesController maps ContentEn in requests)
   - ✅ Database migration applied

### 🟢 Current Status vs v4 Requirements:
| Feature | Current Status | v4 Requirement | Action Needed |
|---------|---------------|----------------|---------------|
| **KnowledgeArticle** | ✅ ContentEn field implemented | ContentEn required for Thai-First strategy | ✅ Complete |
| **AI Services** | ✅ Full fallback chain implemented | OpenRouter + XiaomiMiMo fallback chain | ✅ Complete |
| **Media Conversions** | ✅ ImageSharp processor implemented | ImageSharp for thumbnails/WebP | ✅ Complete |
| **Publish-First** | ✅ Role-based workflow implemented | Role-based workflow complete | ✅ Complete |
| **Integration Testing** | ✅ All tests pass, build successful | Test all Phase 4+5 features | ✅ Complete |
| **React Frontend** | ✅ Complete frontend created | React 19 + Vite + TanStack | ✅ Complete |
| **Frontend-Backend Integration** | ✅ Full-stack operational | Full integration working | ✅ Complete |
| **AI Writing Assistant** | ✅ End-to-end implemented | Complete feature set | ✅ Complete |
| **RAG Pipeline** | ✅ Advanced pipeline complete | Complete Q&A system | ✅ Complete |

### 🎯 COMPLETED v4 Implementation Priority:
1. ✅ **ContentEn Field** - KnowledgeArticle entity and database updated
2. ✅ **AI Services** - OpenRouter + XiaomiMiMo fallback chain implemented
3. ✅ **Media Processing** - ImageSharp processor for image conversions implemented
4. ✅ **Integration Testing** - All Phase 4+5 features tested, all tests pass
5. ✅ **Publish-First Workflow** - Role-based publishing implemented
6. ✅ **React Frontend** - Complete v4 frontend application created and functional

### 🎯 NEXT v4 Implementation Priority:
1. ✅ **Fix Backend Swashbuckle Issue** - Resolved version compatibility, backend API now running successfully
2. ✅ **Complete Frontend-Backend Integration** - Full-stack flow completed
3. ✅ **AI Writing Assistant** - Full feature set completed
4. ✅ **RAG Pipeline** - Complete Q&A + benchmark pipeline completed
5. ✅ **Line OA Integration** - Completed with observability and runbook

### 📈 Next Milestones:
1. ✅ **Week 1-7+:** v4 core implementation milestones completed
2. **Next:** deployment hardening + vulnerability remediation (Npgsql advisory) + production rollout checklist

---

*Last Updated: April 8, 2026 (sync status, replay fidelity + analytics milestone, and completed roadmap alignment)*
*Project Status: Foundation Complete with all v4 core features implemented*
*Backend Status: ✅ API running successfully with Swashbuckle 6.5.0*
*Build Status: ✅ SUCCESS (0 errors)*
*Test Status: ✅ ALL TESTS PASS (KMS.Domain.Tests + KMS.Api.Tests)*
*API Status: ✅ RUNNING SUCCESSFULLY (http://localhost:5000)*
*Database: ✅ PostgreSQL 16 with pgvector + ContentEn column*
*ContentEn Implementation: ✅ COMPLETE (All layers updated)*
*AI Services: ✅ COMPLETE (v4 cloud-only fallback chain: OpenRouter → XiaomiMiMo)*
*Media Processing: ✅ COMPLETE (ImageSharp for thumbnails + WebP conversions)*
*Publish-First Workflow: ✅ COMPLETE (Role-based: Admin/Faculty direct, Researcher requires review)*
*Integration Testing: ✅ COMPLETE (All Phase 4+5 features tested)*
*Frontend: ✅ COMPLETE (React 19 + TypeScript + Vite running on http://localhost:5173)*
*Frontend-Backend Integration: ✅ API READY (Backend fixed, full-stack operational)*

**KMS v4 - Knowledge Management System**
*All priority tasks from PROJECT_PLAN.md successfully completed*
*Aligning with KMS_Setup_and_Concept_Full4.md + KMS_Database_Schema_Full4.md*

### ✅ Summary of Completed Work:
1. ✅ **AI Services Implementation (v4)** - Cloud-only fallback chain complete
2. ✅ **Media Conversions** - ImageSharp processor for thumbnails/WebP complete  
3. ✅ **Publish-First Workflow** - Role-based publishing complete
4. ✅ **Phase 4 Integration Testing** - All advanced features tested and working
5. ✅ **React Frontend Development** - Complete v4 specification frontend created

### 🎯 React Frontend Highlights:
- **Modern Stack**: React 19 + TypeScript + Vite + Tailwind CSS
- **State Management**: TanStack Query + TanStack Router
- **Rich Text Editor**: TipTap with formatting toolbar
- **Pages**: Home, Login, Register, Articles List, Article Create
- **API Integration**: Full TypeScript API client with JWT support
- **Responsive Design**: Mobile-friendly UI components

### 🔑 Key Technical Achievements:
- Fixed critical compilation errors in ArticleService (notification service calls)
- Resolved ImageSharp API compatibility issues with proper pixel type handling
- Implemented v4 AI service architecture with cloud-only fallback chain (OpenRouter → XiaomiMiMo)
- Ensured proper dependency injection for all new services
- Maintained Clean Architecture by moving implementations to Infrastructure layer
- Created complete React frontend with modern tooling and best practices
- Implemented type-safe API integration with React Query hooks

### 🚀 Next Steps:
1. ✅ **Fix Backend**: Swashbuckle version compatibility issue resolved
   - Removed conflicting Microsoft.AspNetCore.OpenApi package
   - Downgraded Swashbuckle to stable version 6.5.0
   - Fixed TypeLoadException with proper dependency resolution
2. ✅ **Start Backend API**: Backend now running successfully on `http://localhost:5000`
   - API starts successfully with Swagger enabled
   - Database seeding completes without errors
   - JWT authentication configured and working
3. **Full Integration**: Connect frontend to live backend API
4. **Deploy**: Prepare for development/production deployment

**KMS is now a complete full-stack application with backend API operational!**