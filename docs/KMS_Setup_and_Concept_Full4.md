# KMS — ขั้นตอนการสร้างโปรเจกต์ด้วย CLI และ Concept ระบบ (v4)

> **Stack:** .NET 10 · ASP.NET Core Web API (Controllers) · React 19 + TypeScript + Vite
> **Database:** PostgreSQL 16 · EF Core 10 · pgvector
> **Identity:** ASP.NET Core Identity · JWT Bearer · Swashbuckle (Swagger UI)
> **AI (Chat):** Fallback Chain — OpenRouter `qwen/qwen3.6-plus:free` → XiaomiMiMo `mimo-v2-flash`
> **AI (Embedding):** Fallback Chain — OpenRouter `qwen/qwen3-embedding` (Queue on fail)
> **Frontend:** React 19 · TanStack Query · TanStack Router · TipTap · Tailwind CSS · shadcn/ui
> **เวอร์ชัน:** v4 — MediaLibrary · AI Fallback Chain (no Ollama) · Swagger+JWT · Publish-First · Immutable AuditLog

---

## สารบัญ

1. [System Concept](#1-system-concept)
2. [Architecture Overview](#2-architecture-overview)
3. [Prerequisites](#3-prerequisites)
4. [สร้าง Solution และ Projects (.NET)](#4-สร้าง-solution-และ-projects-net)
5. [Project References](#5-project-references)
6. [NuGet Packages](#6-nuget-packages)
7. [Configuration Files (API)](#7-configuration-files-api)
8. [DbContext และ Identity](#8-dbcontext-และ-identity)
9. [EF Core Migration](#9-ef-core-migration)
10. [Program.cs — DI Registration](#10-programcs--di-registration)
11. [Swagger + JWT Setup](#11-swagger--jwt-setup)
12. [สร้าง React Project](#12-สร้าง-react-project)
13. [React — Packages และ Configuration](#13-react--packages-และ-configuration)
14. [React — โครงสร้างโปรเจกต์](#14-react--โครงสร้างโปรเจกต์)
15. [React — API Client (openapi-ts)](#15-react--api-client-openapi-ts)
16. [API Key Management](#16-api-key-management)
17. [Run และ Test](#17-run-และ-test)
18. [CLAUDE.md สำหรับ Claude Code](#18-claudemd-สำหรับ-claude-code)

---

## 1. System Concept

### KMS คืออะไร

**KMS (Knowledge Management System)** คือระบบจัดการองค์ความรู้สำหรับมหาวิทยาลัยเทคโนโลยีราชมงคลอีสาน วิทยาเขตสกลนคร ออกแบบมาเพื่อรวบรวม จัดเก็บ และเผยแพร่ความรู้ภายในองค์กรอย่างเป็นระบบ โดยมี AI เป็นตัวช่วยสำคัญในทุกขั้นตอน รองรับการเชื่อมต่อกับ Line OA เพื่อให้บริการคำปรึกษาอัตโนมัติ

---

### กลุ่มผู้ใช้งาน

| Role | ภาษาไทย | สิทธิ์หลัก | Publish โดยตรง |
|------|---------|-----------|----------------|
| `Admin` | ผู้ดูแลระบบ | จัดการทุกอย่างในระบบ | ✅ ข้าม Review ได้ |
| `Faculty` | อาจารย์ | เขียน อนุมัติ และเผยแพร่บทความ | ✅ ข้าม Review ได้ |
| `Researcher` | นักวิจัย | เขียนและส่งบทความเพื่อ review | — ต้องผ่าน Review |
| `Student` | นักศึกษา | อ่านและแสดงความคิดเห็น | — |
| `Guest` | บุคคลทั่วไป | อ่านเนื้อหาสาธารณะ | — |

---

### Features หลัก

#### Knowledge Repository
- บทความ งานวิจัย สื่อการสอน นโยบาย คู่มือ
- Version control ทุก edit
- หมวดหมู่แบบ tree (parent-child categories)
- Tag system พร้อม auto-suggest จาก AI
- Soft delete — ไม่ลบข้อมูลจริง

#### MediaLibrary (v2)
- จัดการไฟล์ทุกประเภทผ่านตาราง `MediaItems` กลางตารางเดียว
- Polymorphic — ผูกกับ Model ใดก็ได้ (`KnowledgeArticle`, `AppUser`)
- Collections: `cover` (รูปปก), `attachments` (ไฟล์แนบ), `avatar` (รูปโปรไฟล์)
- Auto Conversion: thumbnail / WebP ด้วย SixLabors.ImageSharp
- รองรับ Storage backend: Local / MinIO / S3

#### Publish-First Workflow (v2)
```
Admin / Faculty:
  Draft ──────────────────────────→ Published → Archived
                                    (Direct Publish)
                                    ↓ แจ้ง Admin Post-audit อัตโนมัติ

Researcher:
  Draft → UnderReview → Published → Archived
            ↑__________|  (ส่งกลับแก้ไข)
```
- Notification แจ้งเตือนทุก step อัตโนมัติ
- Audit Log บันทึกทุก action — Append-only (ลบ/แก้ไขไม่ได้)

#### AI Writing Assistant

| Feature | ทำอะไร |
|---------|--------|
| Generate Draft | สร้างบทความจากหัวข้อ — ดึง context จาก knowledge base (RAG) |
| Improve Text | ปรับปรุงข้อความ: ทางการ / กระชับ / ขยายความ / แก้ไวยากรณ์ |
| Summarize | สรุปเนื้อหายาวเป็น bullet points |
| Auto Translate | แปล Title และ Summary เป็นภาษาอังกฤษ (optional) |
| Auto Tag | แนะนำ tags และหมวดหมู่อัตโนมัติ |
| Q&A | ถามตอบจากเนื้อหาใน knowledge base (RAG pipeline) |

#### Line OA Integration
- รับคำถามผ่าน Line Messaging API
- RAG pipeline ค้นหาคำตอบจาก Knowledge Base
- AI ปรับภาษาให้เหมาะสมและโต้ตอบอัตโนมัติ
- เฉพาะขอบเขตงานบริการวิชาการและงานวิจัย

#### Phase 8 — Production Features (✅ เสร็จสมบูรณ์ทั้งหมด)
- ✅ **Redis Cache** — StackExchange.Redis + `IDistributedCache` cache articles/search/sessions
- ✅ **Health Check** — `/health` + `/health/ready` endpoint สำหรับ monitoring
- ✅ **Refresh Token** — JWT rotation โดยไม่ต้อง login ใหม่ (`RefreshTokens` table, 7-day window)
- ✅ **Rate Limiting** — ป้องกัน abuse บน AI/Line endpoints (sliding window middleware)
- ✅ **Email Notifications** — `SmtpEmailService` สำหรับ notification + forgot/reset password
- ✅ **MinIO Storage** — `MinioFileStorageService` (16 methods) + systemd autostart + DI switch
- ✅ **Article Draft Autosave** — auto-save ทุก 30 วินาที + UI indicator
- ✅ **Dashboard Analytics** — Bar/Area/Pie charts (recharts) บน admin home + dark mode
- ✅ **Dark Mode** — ThemeContext + localStorage persist + Tailwind `dark:` classes ทั่ว admin
- ✅ **PDF Export** — `GET /api/export/article/{id}/pdf` ด้วย QuestPDF + ปุ่มดาวน์โหลดใน article detail
- ✅ **2FA/TOTP** — OtpNet library + 4 endpoints + Login OTP step + Profile 2FA setup UI

#### Phase 9+ — Planned
- 🔲 **Word Export** — Export บทความเป็น `.docx`
- 🔲 **Email Verification UI** — frontend flow สำหรับ verify email
- 🔲 **Forgot Password UI** — หน้า frontend สำหรับ forgot/reset password
- 🔲 **MinIO Frontend Panel** — admin UI สำหรับดู bucket usage
- Keyword search (PostgreSQL Full-Text Search)
- Semantic search ด้วย pgvector (ค้นด้วยความหมาย)
- ค้นด้วยภาษาอังกฤษ พบบทความภาษาไทยได้

#### Bilingual Strategy (Thai-First)
- เนื้อหาภาษาไทย **required** — `Title`, `Content`, `Summary`
- ภาษาอังกฤษ **optional** (suffix `En`) — `TitleEn`, `ContentEn`, `SummaryEn`, `KeywordsEn`
- AI แปลอัตโนมัติเมื่อต้องการด้วยปุ่มเดียว — ไม่บังคับ
- Vector embedding รวม 2 ภาษาใน field เดียว (vector(1024))

---

### AI Pipeline (RAG) — v4 No-Ollama Fallback Chain

```
ผู้ใช้ถาม / ขอเขียน
        │
        ▼
  Embed คำถาม/หัวข้อ
  ┌──────────────────────────────────────────┐
  │ 1. OpenRouter: qwen/qwen3-embedding (P1) │  ← ลอง Primary ก่อน
  │ 2. Queue + retry ถ้าล้มเหลว              │
  └──────────────────────────────────────────┘
        │
        ▼
  pgvector cosine search
  ดึง top-5 บทความที่เกี่ยวข้อง
        │
        ▼
  สร้าง prompt + context
        │
        ▼
  Chat LLM สร้างคำตอบ (streaming via SSE)
  ┌──────────────────────────────────────────────┐
  │ 1. OpenRouter: qwen/qwen3.6-plus:free (P1)  │  ← ลอง Primary ก่อน
  │ 2. XiaomiMiMo: mimo-v2-flash (P2)           │  ← Last Resort
  └──────────────────────────────────────────────┘
        │
        ▼
  แสดงผลใน React Editor (ทีละ chunk)
```

---

## 2. Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────┐
│                  Presentation                   │
│     React 19 (Vite)  │  ASP.NET Core API        │
│                       │  Swagger UI (/swagger)   │
├─────────────────────────────────────────────────┤
│                  Application                    │
│  CQRS (MediatR) · DTOs · AI Services           │
│  IMediaService · IPublishWorkflowService        │
│  IAuditService · IEmbeddingService              │
│  IFallbackChatService (Priority chain)          │
├─────────────────────────────────────────────────┤
│                    Domain                       │
│  Entities · Enums · Events · Interfaces         │
│  IHasMedia · IAuditable · PublishMode           │
│  AiProviderConfig · AiProviderType              │
├─────────────────────────────────────────────────┤
│                Infrastructure                   │
│  EF Core · Repos · MediaService · AuditService  │
│  OpenRouterChatService (P1) → cloud primary     │
│  XiaomiMimoChatService (P2) → last resort       │
│  FallbackEmbeddingService → OpenRouter embed    │
│  IStorageProvider (Local/MinIO/S3)              │
├─────────────────────────────────────────────────┤
│                    Data                         │
│  PostgreSQL 18 · pgvector(1024) · Redis 8       │
└─────────────────────────────────────────────────┘
```

### Solution Structure (.NET) — v2

```
KMS/
├── src/
│   ├── KMS.Domain/
│   │   ├── Entities/
│   │   │   ├── Identity/
│   │   │   │   ├── AppUser.cs
│   │   │   │   ├── AppRole.cs
│   │   │   │   └── AppUserRole.cs
│   │   │   ├── Knowledge/
│   │   │   │   ├── KnowledgeArticle.cs
│   │   │   │   ├── ArticleVersion.cs
│   │   │   │   ├── Category.cs
│   │   │   │   ├── Tag.cs
│   │   │   │   └── ArticleTag.cs
│   │   │   ├── Media/
│   │   │   │   └── MediaItem.cs              # ใหม่ v2
│   │   │   ├── Interaction/
│   │   │   │   ├── Comment.cs
│   │   │   │   ├── ArticleReaction.cs
│   │   │   │   └── Notification.cs
│   │   │   ├── Logging/
│   │   │   │   ├── AiWritingLog.cs
│   │   │   │   ├── KnowledgeSearchLog.cs
│   │   │   │   └── AuditLog.cs
│   │   │   └── ApiKey.cs
│   │   ├── Enums/
│   │   │   ├── ArticleStatus.cs   # Draft|UnderReview|Published|Archived
│   │   │   ├── Visibility.cs      # Public|Internal|Restricted
│   │   │   ├── ImprovementType.cs # Grammar|Concise|Formal|Expand|Simplify
│   │   │   ├── PublishMode.cs     # DirectPublish|RequireReview (ใหม่ v2)
│   │   │   └── AiProviderType.cs  # OpenRouter|XiaomiMiMo
│   │   ├── Events/
│   │   │   ├── ArticlePublishedEvent.cs
│   │   │   └── ArticleReviewRequestedEvent.cs
│   │   ├── Interfaces/
│   │   │   ├── IArticleRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   ├── IHasMedia.cs          # ใหม่ v2
│   │   │   └── IAuditable.cs         # ใหม่ v2
│   │   └── Models/
│   │       ├── MediaCollection.cs    # ใหม่ v2
│   │       └── MediaConversion.cs    # ใหม่ v2
│   │
│   ├── KMS.Application/
│   │   ├── Articles/
│   │   │   ├── Commands/              # CreateArticle, UpdateArticle, PublishArticle
│   │   │   └── Queries/               # GetArticles, GetArticleById, SearchArticles
│   │   ├── AI/
│   │   │   ├── AiWritingService.cs
│   │   │   ├── TranslationService.cs
│   │   │   ├── EmbeddingService.cs
│   │   │   └── RagService.cs
│   │   ├── Auth/
│   │   │   ├── Commands/              # Login, Register, RefreshToken
│   │   │   └── JwtService.cs
│   │   ├── Media/
│   │   │   ├── ArticleMediaCollections.cs   # ใหม่ v2
│   │   │   └── UserMediaCollections.cs      # ใหม่ v2
│   │   ├── Services/
│   │   │   ├── PublishWorkflowService.cs    # ใหม่ v2
│   │   │   └── AuditService.cs              # ใหม่ v2
│   │   ├── Interfaces/
│   │   │   ├── IMediaService.cs             # ใหม่ v2
│   │   │   ├── IStorageProvider.cs          # ใหม่ v2
│   │   │   ├── IChatService.cs              # ใหม่ v2
│   │   │   ├── IFallbackChatService.cs      # ใหม่ v3
│   │   │   ├── IEmbeddingService.cs         # ใหม่ v3
│   │   │   ├── IPublishWorkflowService.cs   # ใหม่ v2
│   │   │   ├── IAuditService.cs             # ใหม่ v2
│   │   │   └── INotificationService.cs
│   │   ├── Models/
│   │   │   ├── AuditEntry.cs                # ใหม่ v2
│   │   │   ├── PublishResult.cs             # ใหม่ v2
│   │   │   └── AiProviderConfig.cs          # ใหม่ v3 — config class ต่อ provider
│   │   └── DTOs/
│   │       ├── ArticleDto.cs
│   │       ├── MediaItemDto.cs              # ใหม่ v2
│   │       └── UserDto.cs
│   │
│   ├── KMS.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── ArticleConfiguration.cs
│   │   │   │   ├── MediaItemConfiguration.cs   # ใหม่ v2
│   │   │   │   └── ApiKeyConfiguration.cs
│   │   │   ├── Interceptors/
│   │   │   │   ├── AuditLogImmutabilityInterceptor.cs  # ใหม่ v2
│   │   │   │   └── AutoAuditInterceptor.cs             # ใหม่ v2
│   │   │   └── Migrations/
│   │   ├── AI/
│   │   │   ├── OpenRouterChatService.cs     # P1 Chat — qwen/qwen3.6-plus:free
│   │   │   ├── XiaomiMimoChatService.cs     # P2 Chat — mimo-v2-flash (last resort)
│   │   │   ├── FallbackChatService.cs       # orchestrates P1→P2
│   │   │   ├── OpenRouterHeaderHandler.cs   # HTTP-Referer, X-Title headers
│   │   │   └── FallbackEmbeddingService.cs  # OpenRouter embed + queue on fail
│   │   ├── Storage/
│   │   │   ├── LocalStorageProvider.cs      # ใหม่ v2
│   │   │   └── MinIOStorageProvider.cs      # ใหม่ v2 (optional)
│   │   ├── Media/
│   │   │   ├── MediaService.cs              # ใหม่ v2
│   │   │   └── ImageSharpProcessor.cs       # ใหม่ v2
│   │   └── Services/
│   │       ├── AuditService.cs              # ใหม่ v2
│   │       ├── ApiKeyService.cs
│   │       └── EmailService.cs
│   │
│   └── KMS.Api/
│       ├── Controllers/
│       │   ├── Admin/                                   # namespace KMS.Api.Controllers.Admin
│       │   │   ├── AdminDtos.cs                         # DTOs ทั้งหมดสำหรับ Admin
│       │   │   ├── AdminUsersController.cs              # GET/PUT users, lock/unlock, reset-password
│       │   │   ├── AdminRolesController.cs              # GET/POST/PUT/DELETE roles
│       │   │   └── AdminSettingsController.cs           # GET/PUT settings + policies
│       │   └── Api/                                     # namespace KMS.Api.Controllers.Api
│       │       ├── AuthController.cs
│       │       ├── ArticlesController.cs
│       │       ├── CategoriesController.cs
│       │       ├── TagsController.cs
│       │       ├── SearchController.cs
│       │       ├── CommentsController.cs
│       │       ├── MediaController.cs
│       │       ├── NotificationsController.cs
│       │       ├── AuditLogsController.cs
│       │       ├── AiController.cs
│       │       └── LineController.cs
│       ├── Filters/
│       │   └── SecurityRequirementsOperationFilter.cs  # Swagger lock icon
│       ├── Middleware/
│       │   ├── ErrorHandlingMiddleware.cs
│       │   └── ApiKeyMiddleware.cs
│       ├── appsettings.Production.json                 # ใหม่ — #{PLACEHOLDER}# tokens สำหรับ CI/CD
│       └── Program.cs
│
├── kms-web/                               # React Project
│   ├── src/
│   │   ├── api/                              # openapi-ts generated (อย่าแก้มือ)
│   │   ├── components/
│   │   │   ├── ui/                           # shadcn/ui
│   │   │   ├── layout/
│   │   │   ├── article/
│   │   │   ├── editor/
│   │   │   ├── media/                        # ใหม่ v2 — MediaUploader, MediaGallery
│   │   │   └── common/
│   │   ├── pages/
│   │   │   ├── auth/
│   │   │   ├── articles/
│   │   │   ├── search/
│   │   │   └── admin/
│   │   ├── hooks/
│   │   ├── stores/
│   │   └── lib/
│   ├── vite.config.ts
│   ├── tsconfig.json
│   └── package.json
│
├── tests/
│   ├── KMS.Domain.Tests/
│   └── KMS.Api.Tests/
│
├── scripts/
│   └── audit_log_trigger.sql                # ใหม่ v2
│
├── uploads/                                 # Local storage (ใหม่ v2)
│
├── KMS.slnx
├── .env.example
├── .gitignore
└── CLAUDE.md
```

---

## 3. Prerequisites

```bash
# ── .NET 10 ───────────────────────────────────────────────────────
dotnet --version   # ต้องได้ 10.x.x
dotnet tool install --global dotnet-ef
dotnet ef --version

# ── Node.js ───────────────────────────────────────────────────────
node --version   # ต้องได้ 20.x.x หรือสูงกว่า
npm --version

# ── PostgreSQL (ติดตั้งโดยตรง — ไม่ใช้ Docker) ────────────────────
# ✅ ติดตั้งแล้ว — PostgreSQL 18.3 (Debian)
# ตรวจสอบ service
sudo systemctl status postgresql

# สร้าง database สำหรับ development
sudo -u postgres psql -c "CREATE DATABASE \"KMS_Dev\";"
sudo -u postgres psql -c "CREATE EXTENSION IF NOT EXISTS vector;" -d KMS_Dev
sudo -u postgres psql -c "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";" -d KMS_Dev
sudo -u postgres psql -c "CREATE EXTENSION IF NOT EXISTS unaccent;" -d KMS_Dev

# ตรวจสอบ connection string ใน appsettings.Development.json
# "Host=localhost;Port=5432;Database=KMS_Dev;Username=postgres;Password=..."

# ── Redis (ติดตั้งโดยตรง) ─────────────────────────────────────────
# ✅ ติดตั้งแล้ว — Redis 8.0.2
sudo systemctl status redis-server
redis-cli ping   # → PONG

# ติดตั้งใหม่ (ถ้ายังไม่มี):
# sudo apt install redis-server -y
# sudo systemctl enable --now redis-server

# ── OpenRouter API Key ────────────────────────────────────────────
# ลงทะเบียนที่ https://openrouter.ai → สร้าง API Key
# ใช้สำหรับทั้ง Chat (qwen/qwen3.6-plus:free) และ Embedding (qwen/qwen3-embedding)

# ── XiaomiMiMo API Key (Last Resort Chat) ────────────────────────
# ลงทะเบียนที่ https://platform.xiaomimimo.com → สร้าง API Key
# ใช้สำหรับ Chat fallback (mimo-v2-flash)
```

> **v4 เปลี่ยน:** ตัด Ollama ออกทั้งหมด — ใช้ Cloud providers เท่านั้น
> ไม่ต้องติดตั้ง runtime เพิ่มเติมบน server ใดๆ

---

## 4. สร้าง Solution และ Projects (.NET)

```bash
# ──────────────────────────────────────────
# STEP 1: สร้างโฟลเดอร์และ Solution
# ──────────────────────────────────────────
mkdir KMS && cd KMS
dotnet new sln -n KMS

# ──────────────────────────────────────────
# STEP 2: สร้าง src projects
# ──────────────────────────────────────────

# Domain Layer — ไม่ขึ้นกับ framework ใด
dotnet new classlib -n KMS.Domain \
  -o src/KMS.Domain

# Application Layer — Business Logic
dotnet new classlib -n KMS.Application \
  -o src/KMS.Application

# Infrastructure Layer — EF Core, AI, External
dotnet new classlib -n KMS.Infrastructure \
  -o src/KMS.Infrastructure

# API Layer — Controllers API
dotnet new webapi -n KMS.Api \
  -o src/KMS.Api \
  --use-minimal-apis

# ──────────────────────────────────────────
# STEP 3: สร้าง test projects
# ──────────────────────────────────────────
dotnet new xunit -n KMS.Domain.Tests \
  -o tests/KMS.Domain.Tests

dotnet new xunit -n KMS.Api.Tests \
  -o tests/KMS.Api.Tests

# ──────────────────────────────────────────
# STEP 4: เพิ่มทั้งหมดเข้า Solution
# ──────────────────────────────────────────
dotnet sln add src/KMS.Domain/KMS.Domain.csproj
dotnet sln add src/KMS.Application/KMS.Application.csproj
dotnet sln add src/KMS.Infrastructure/KMS.Infrastructure.csproj
dotnet sln add src/KMS.Api/KMS.Api.csproj
dotnet sln add tests/KMS.Domain.Tests/KMS.Domain.Tests.csproj
dotnet sln add tests/KMS.Api.Tests/KMS.Api.Tests.csproj

# ตรวจสอบ — ควรเห็น 6 projects
dotnet sln list

# ──────────────────────────────────────────
# STEP 5: สร้าง uploads directory (Local Storage)
# ──────────────────────────────────────────
mkdir -p uploads/KnowledgeArticle uploads/AppUser
mkdir -p scripts
```

---

## 5. Project References

```bash
# Application → Domain
dotnet add src/KMS.Application \
  reference src/KMS.Domain/KMS.Domain.csproj

# Infrastructure → Application + Domain
dotnet add src/KMS.Infrastructure \
  reference src/KMS.Application/KMS.Application.csproj

dotnet add src/KMS.Infrastructure \
  reference src/KMS.Domain/KMS.Domain.csproj

# Api → Infrastructure + Application
dotnet add src/KMS.Api \
  reference src/KMS.Infrastructure/KMS.Infrastructure.csproj

dotnet add src/KMS.Api \
  reference src/KMS.Application/KMS.Application.csproj

# Tests
dotnet add tests/KMS.Domain.Tests \
  reference src/KMS.Domain/KMS.Domain.csproj

dotnet add tests/KMS.Api.Tests \
  reference src/KMS.Api/KMS.Api.csproj
```

**Dependency Rule (Clean Architecture):**
```
Domain ←── Application ←── Infrastructure
                ↑
               Api
```
> Domain ไม่ขึ้นกับ project ไหนเลย — เป็นหัวใจของระบบ

---

## 6. NuGet Packages

```bash
# ──────────────────────────────────────────
# Infrastructure
# ──────────────────────────────────────────
cd src/KMS.Infrastructure

# EF Core + PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# pgvector — Vector similarity search
dotnet add package Pgvector.EntityFrameworkCore

# Identity
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# AI abstraction layer
dotnet add package Microsoft.Extensions.AI

# OpenAI SDK — ใช้เชื่อมต่อ OpenRouter และ XiaomiMiMo (OpenAI-compatible)
dotnet add package OpenAI

# Image processing — สำหรับ MediaLibrary conversions
dotnet add package SixLabors.ImageSharp

# MinIO client — S3-compatible object storage
dotnet add package Minio

# 2FA / TOTP
dotnet add package Otp.NET

# PDF Export
dotnet add package QuestPDF
dotnet add package HtmlAgilityPack

# Caching
dotnet add package StackExchange.Redis
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis

# Email
dotnet add package MailKit

# ──────────────────────────────────────────
# Application
# ──────────────────────────────────────────
cd ../KMS.Application

dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Mapster
dotnet add package Mapster.DependencyInjection

# ──────────────────────────────────────────
# Api
# ──────────────────────────────────────────
cd ../KMS.Api

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Swagger UI (แทน Scalar)  ← ใหม่ v4
dotnet add package Swashbuckle.AspNetCore

# ──────────────────────────────────────────
# กลับ root และ build ทดสอบ
# ──────────────────────────────────────────
cd ../../
dotnet build
```

### Package Summary

| Package | Layer | ใช้ทำอะไร |
|---------|-------|-----------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infra | EF Core provider สำหรับ PostgreSQL |
| `Pgvector.EntityFrameworkCore` | Infra | Vector similarity search |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Infra | User/Role management |
| `Microsoft.Extensions.AI` | Infra | AI abstraction interface |
| `OpenAI` | Infra | OpenRouter + XiaomiMiMo (OpenAI-compatible API) |
| `SixLabors.ImageSharp` | Infra | Image resize/convert สำหรับ MediaLibrary |
| `StackExchange.Redis` | Infra | Cache session และ query results |
| `MailKit` | Infra | ส่งอีเมล SMTP |
| `Minio` | Infra | MinIO/S3-compatible object storage |
| `Otp.NET` | Infra | TOTP 2FA (RFC 6238, Google Authenticator compatible) |
| `QuestPDF` | Infra | Generate PDF documents (community license) |
| `HtmlAgilityPack` | Infra | Parse HTML → plain text สำหรับ PDF export |
| `MediatR` | App | CQRS — Commands และ Queries |
| `FluentValidation` | App | Input validation |
| `Mapster` | App | DTO mapping |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Api | JWT Auth |
| `Swashbuckle.AspNetCore` | Api | Swagger UI (/swagger) + JWT lock icon |

---

## 7. Configuration Files (API)

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=KMS_Dev;Username=postgres;Password=Dev@1234",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key":                      "your-super-secret-key-minimum-32-characters!!",
    "Issuer":                   "KmsApi",
    "Audience":                 "KmsClient",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays":   7
  },
  "Swagger": {
    "Enabled":     true,
    "Title":       "KMS API",
    "Version":     "v1",
    "Description": "Knowledge Management System — RMUTI Sakon Nakhon Campus"
  },
  "AI": {
    "Chat": {
      "Providers": [
        {
          "Name":           "OpenRouter",
          "Endpoint":       "https://openrouter.ai/api/v1",
          "Model":          "qwen/qwen3.6-plus:free",
          "Priority":       1,
          "Enabled":        true,
          "TimeoutSeconds": 30,
          "IsLocal":        false,
          "Headers": {
            "HTTP-Referer": "https://kms.rmuti-skc.ac.th",
            "X-Title":      "KMS"
          }
        },
        {
          "Name":           "XiaomiMiMo",
          "Endpoint":       "https://api.xiaomimimo.com/v1",
          "Model":          "mimo-v2-flash",
          "Priority":       2,
          "Enabled":        true,
          "TimeoutSeconds": 30,
          "IsLocal":        false
        }
      ]
    },
    "Embedding": {
      "Dimensions":       1024,
      "QueueOnAllFailed": true,
      "Providers": [
        {
          "Name":           "OpenRouter",
          "Endpoint":       "https://openrouter.ai/api/v1",
          "Model":          "qwen/qwen3-embedding",
          "Priority":       1,
          "Enabled":        true,
          "TimeoutSeconds": 20,
          "IsLocal":        false
        }
      ]
    }
  },
  "Storage": {
    "Provider": "Local",
    "BasePath": "uploads",
    "MaxFileSizeMB": 100,
    "AllowedTypes": [
      "application/pdf",
      "image/jpeg", "image/png", "image/webp",
      "video/mp4",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ],
    "MinIO": {
      "Endpoint":  "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "Bucket":    "kms",
      "UseSSL":    false
    }
  },
  "Email": {
    "SmtpHost":    "smtp.example.com",
    "SmtpPort":    587,
    "FromAddress": "noreply@rmuti-skc.ac.th",
    "FromName":    "KMS System"
  },
  "LineBot": {
    "ChannelSecret":      "your-line-channel-secret",
    "ChannelAccessToken": "your-line-channel-access-token"
  },
  "AllowedOrigins": [
    "http://localhost:5173",
    "https://kms.rmuti-skc.ac.th"
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=KMS_Dev;Username=postgres;Password=Dev@1234"
  },
  "AllowedOrigins": [
    "http://localhost:5173"
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### User Secrets (สำหรับ sensitive data ใน dev)

```bash
cd src/KMS.Api

dotnet user-secrets init

# JWT
dotnet user-secrets set "Jwt:Key" "your-real-secret-key-here-minimum-32-chars"

# Database
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Database=KMS_Dev;Username=postgres;Password=Dev@1234"

# OpenRouter API Key (Chat P1 + Embedding P1)  ← v4: P1 = index 0 (no Ollama)
dotnet user-secrets set "AI:Chat:Providers:0:ApiKey"      "sk-or-v1-xxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "AI:Embedding:Providers:0:ApiKey" "sk-or-v1-xxxxxxxxxxxxxxxxxxxx"

# XiaomiMiMo API Key (Chat P2 — Last Resort)  ← v4
dotnet user-secrets set "AI:Chat:Providers:1:ApiKey" "sk-mimo-xxxxxxxxxxxxxxxxxxxx"

# Email
dotnet user-secrets set "Email:SmtpPassword" "your-smtp-password"

# Line OA
dotnet user-secrets set "LineBot:ChannelSecret"      "your-line-channel-secret"
dotnet user-secrets set "LineBot:ChannelAccessToken" "your-line-channel-access-token"
```

---

## 8. DbContext และ Identity

### Domain Entities (Identity)

```csharp
// Domain/Entities/Identity/AppUser.cs
public class AppUser : IdentityUser<Guid>
{
    public string  FullNameTh    { get; set; } = string.Empty;
    public string? FullNameEn    { get; set; }
    public string? Faculty       { get; set; }
    public string? Department    { get; set; }
    public string? Position      { get; set; }
    public string? EmployeeCode  { get; set; }
    // AvatarUrl ถูกลบออก v2 → ใช้ MediaItems (collection: avatar) แทน
    public string? Bio           { get; set; }
    public bool    IsActive      { get; set; } = true;
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt   { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// Domain/Entities/Identity/AppRole.cs
public class AppRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public string? Permissions { get; set; } // JSON array
}

// Domain/Entities/Identity/AppUserRole.cs
public class AppUserRole : IdentityUserRole<Guid> { }
```

### Domain Entity — KnowledgeArticle (v4)

```csharp
// Domain/Entities/Knowledge/KnowledgeArticle.cs
public class KnowledgeArticle
{
    public Guid    Id               { get; set; } = Guid.NewGuid();

    // Thai-First — required
    public string  Title            { get; set; } = string.Empty;
    public string  Content          { get; set; } = string.Empty;
    public string  Summary          { get; set; } = string.Empty;
    public string  Slug             { get; set; } = string.Empty;

    // EN — optional (AI แปลอัตโนมัติเมื่อกด Translate)
    public string? TitleEn          { get; set; }
    public string? ContentEn        { get; set; }   // ← v4: เพิ่มใหม่
    public string? SummaryEn        { get; set; }
    public string? KeywordsEn       { get; set; }

    // Status & Visibility
    public ArticleStatus Status     { get; set; } = ArticleStatus.Draft;
    public Visibility    Visibility { get; set; } = Visibility.Internal;

    // Relations
    public Guid    CategoryId       { get; set; }
    public Guid    AuthorId         { get; set; }
    public Guid?   ReviewerId       { get; set; }

    // AI translation tracking
    public bool      IsAutoTranslated { get; set; } = false;
    public DateTime? TranslatedAt     { get; set; }

    // Stats
    public int     ViewCount        { get; set; } = 0;
    public int     LikeCount        { get; set; } = 0;

    // pgvector — TH+EN combined (1024 dim)
    public Vector? Embedding        { get; set; }

    // Timestamps
    public DateTime  PublishedAt    { get; set; }
    public DateTime  CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt      { get; set; }
    public DateTime? DeletedAt      { get; set; }   // soft delete

    // Navigation
    public Category          Category  { get; set; } = null!;
    public AppUser           Author    { get; set; } = null!;
    public ICollection<Tag>  Tags      { get; set; } = [];
}
```

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid,
        IdentityUserClaim<Guid>, AppUserRole,
        IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>(options)
{
    // Knowledge Group
    public DbSet<Category>           Categories       => Set<Category>();
    public DbSet<KnowledgeArticle>   Articles         => Set<KnowledgeArticle>();
    public DbSet<ArticleVersion>     ArticleVersions  => Set<ArticleVersion>();
    public DbSet<Tag>                Tags             => Set<Tag>();
    public DbSet<ArticleTag>         ArticleTags      => Set<ArticleTag>();

    // Media Group (v2: แทนที่ Attachments)
    public DbSet<MediaItem>          MediaItems       => Set<MediaItem>();

    // Interaction Group
    public DbSet<Comment>            Comments         => Set<Comment>();
    public DbSet<ArticleReaction>    ArticleReactions => Set<ArticleReaction>();
    public DbSet<Notification>       Notifications    => Set<Notification>();

    // AI & Logging Group
    public DbSet<AiWritingLog>       AiWritingLogs    => Set<AiWritingLog>();
    public DbSet<KnowledgeSearchLog> SearchLogs       => Set<KnowledgeSearchLog>();
    public DbSet<AuditLog>           AuditLogs        => Set<AuditLog>();

    // System Group
    public DbSet<SystemSetting>      SystemSettings   => Set<SystemSetting>();

    // Security Group
    public DbSet<ApiKey>             ApiKeys          => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("uuid-ossp");
        builder.HasPostgresExtension("vector");
        builder.HasPostgresExtension("unaccent");

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

---

## 9. EF Core Migration

```bash
# ──────────────────────────────────────────
# STEP 1: Enable PostgreSQL extensions
# ──────────────────────────────────────────
psql -U postgres -d KMS_Dev \
  -c "CREATE EXTENSION IF NOT EXISTS vector;"

psql -U postgres -d KMS_Dev \
  -c "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";"

psql -U postgres -d KMS_Dev \
  -c "CREATE EXTENSION IF NOT EXISTS unaccent;"

# ──────────────────────────────────────────
# STEP 2: สร้าง Initial Migration
# ──────────────────────────────────────────
dotnet ef migrations add InitialCreate \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api \
  --output-dir Data/Migrations

# ──────────────────────────────────────────
# STEP 3: Apply Migration
# ──────────────────────────────────────────
dotnet ef database update \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api

# ──────────────────────────────────────────
# STEP 4: รัน Audit Log Trigger (แยกจาก EF)
# ──────────────────────────────────────────
psql -U postgres -d KMS_Dev -f scripts/audit_log_trigger.sql

# ──────────────────────────────────────────
# ตรวจสอบตารางที่สร้าง
# ──────────────────────────────────────────
psql -U postgres -d KMS_Dev -c "\dt"

# ── Migration สำหรับ v4: เพิ่ม ContentEn ──────────────────────
# (ถ้า DB มีอยู่แล้วและต้องการ add column)
dotnet ef migrations add AddContentEnToArticles \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api \
  --output-dir Data/Migrations

dotnet ef database update \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api

# ──────────────────────────────────────────
# คำสั่ง Migration ที่ใช้บ่อย
# ──────────────────────────────────────────

# เพิ่ม migration ใหม่
dotnet ef migrations add <MigrationName> \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api \
  --output-dir Data/Migrations

# ย้อน migration ล่าสุด
dotnet ef migrations remove \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api

# ดู migration list
dotnet ef migrations list \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api

# Generate SQL script (สำหรับ production)
dotnet ef migrations script \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api \
  --output migrations.sql

# Reset database (dev only)
dotnet ef database drop \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api \
  --force && \
dotnet ef database update \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api && \
psql -U postgres -d KMS_Dev -f scripts/audit_log_trigger.sql
```

### scripts/audit_log_trigger.sql ← ใหม่ v2

```sql
-- ── Trigger Function ──────────────────────────────────────────────
CREATE OR REPLACE FUNCTION prevent_audit_log_mutation()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION
        'AuditLogs เป็น append-only — ไม่อนุญาตให้ % (row id: %)',
        TG_OP, OLD."Id"
    USING ERRCODE = 'restrict_violation';
END;
$$ LANGUAGE plpgsql;

-- ── Trigger: บล็อก UPDATE ────────────────────────────────────────
CREATE TRIGGER trg_audit_log_no_update
    BEFORE UPDATE ON "AuditLogs"
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_log_mutation();

-- ── Trigger: บล็อก DELETE ────────────────────────────────────────
CREATE TRIGGER trg_audit_log_no_delete
    BEFORE DELETE ON "AuditLogs"
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_log_mutation();
```

> **หมายเหตุ:** Trigger SQL ต้องรันแยกหลัง `dotnet ef database update` เสมอ
> เพราะ EF Core ไม่รองรับการสร้าง Trigger ผ่าน Migration โดยตรง

---

## 10. Program.cs — DI Registration

```csharp
// src/KMS.Api/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using OpenAI;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// ── Database ───────────────────────────────────────────────────
builder.Services.AddSingleton<AuditLogImmutabilityInterceptor>();  // ← v2

builder.Services.AddDbContext<ApplicationDbContext>((sp, opt) =>
    opt.UseNpgsql(
        config.GetConnectionString("Default"),
        npgsql => npgsql.UseVector())
       .AddInterceptors(
           sp.GetRequiredService<AuditLogImmutabilityInterceptor>()));  // ← v2

// ── Identity ───────────────────────────────────────────────────
builder.Services
    .AddIdentity<AppUser, AppRole>(opt =>
    {
        opt.Password.RequireDigit           = true;
        opt.Password.RequiredLength         = 8;
        opt.Password.RequireUppercase       = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail         = true;
        opt.SignIn.RequireConfirmedEmail     = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ── JWT Authentication ─────────────────────────────────────────
builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"],
            ValidAudience            = config["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });

// ── Authorization Policies ─────────────────────────────────────
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("CanPublish",
        p => p.RequireRole("Admin", "Faculty"));
    opt.AddPolicy("CanReview",
        p => p.RequireRole("Admin", "Faculty", "Researcher"));
    opt.AddPolicy("CanWrite",
        p => p.RequireRole("Admin", "Faculty", "Researcher"));
    opt.AddPolicy("AdminOnly",
        p => p.RequireRole("Admin"));
});

// ── AI Chat: Fallback Chain P1→P2→P3 (v3) ────────────────────
// P2 OpenRouter — ต้องการ custom headers
builder.Services.AddTransient<OpenRouterHeaderHandler>();
builder.Services
    .AddHttpClient("openrouter", client =>
    {
        client.BaseAddress = new Uri(
            config["AI:Chat:Providers:0:Endpoint"]       // P1 = index 0 (no Ollama)
            ?? "https://openrouter.ai/api/v1");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", config["AI:Chat:Providers:0:ApiKey"]!);
    })
    .AddHttpMessageHandler<OpenRouterHeaderHandler>();
builder.Services.AddScoped<OpenRouterChatService>();

// P2 XiaomiMiMo (last resort)
builder.Services.AddScoped<XiaomiMimoChatService>();

// Fallback orchestrator — P1 OpenRouter → P2 XiaomiMiMo
builder.Services.AddScoped<IFallbackChatService, FallbackChatService>();
builder.Services.AddScoped<IChatService>(sp =>
    sp.GetRequiredService<IFallbackChatService>());

// ── AI Embedding: OpenRouter (v4 — no Ollama) ────────────────
// FallbackEmbeddingService จัดการ P1 (OpenRouter) และ Queue on fail เองภายใน
builder.Services.AddScoped<IEmbeddingService, FallbackEmbeddingService>();

// Background queue สำหรับ retry เมื่อ embedding ล้มเหลว
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<EmbeddingQueueWorker>();

// ── Storage Provider (v2 — MediaLibrary) ──────────────────────
var storageProvider = config["Storage:Provider"];
if (storageProvider == "MinIO")
    builder.Services.AddScoped<IStorageProvider, MinIOStorageProvider>();
else
    builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();

// ── Media Services (v2) ────────────────────────────────────────
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IImageProcessor, ImageSharpProcessor>();

// ── Application Services ───────────────────────────────────────
builder.Services.AddScoped<AiWritingService>();
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<IPublishWorkflowService, PublishWorkflowService>();  // ← v2
builder.Services.AddScoped<IAuditService, AuditService>();                       // ← v2
builder.Services.AddHttpContextAccessor();                                        // ← v2

// ── MediatR (CQRS) ─────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(KMS.Application.AssemblyReference).Assembly));

// ── FluentValidation ───────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(KMS.Application.AssemblyReference).Assembly);

// ── Redis Cache ────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = config.GetConnectionString("Redis"));

// ── OpenAPI ────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── CORS ───────────────────────────────────────────────────────
var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KMS API v1");
        c.RoutePrefix   = "swagger";
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();           // ← v2: serve uploads/ directory
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();

// ── Controllers Endpoints ───────────────────────────────────────
app.MapControllers();

// ── Auto Seed on startup ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db, roleManager, userManager);
}

app.Run();
```

---

## 11. Swagger + JWT Setup

> **รายละเอียดทั้งหมดย้ายไปที่ [`docs/KMS_Swagger_JWT_Setup.md`](./KMS_Swagger_JWT_Setup.md)**
>
> เอกสารนั้นครอบคลุมครบถ้วน:
> - `SecurityRequirementsOperationFilter` — lock icon ต่อ endpoint
> - `Program.cs` — `AddSwaggerGen`, `UseSwaggerUI`, `UseStaticFiles`, `InjectJavascript`
> - `swagger-auto-auth.js` — Login panel, auto-authorize, token persistence, lock/unlock icons
> - Authorization Policies (CanWrite, CanPublish, CanReview, AdminOnly)
> - Swagger UI Flow (วิธี Login → auto-authorize → ไม่ต้องวาง token เอง)
> - Quick Troubleshooting

### สิ่งที่ต้องเพิ่มใน Program.cs (สรุปย่อ)

```csharp
// ── 1. Register Swagger + Filter ───────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // ... SwaggerDoc, AddSecurityDefinition("Bearer", Http type) ...
    c.OperationFilter<SecurityRequirementsOperationFilter>();  // ← lock icon
});

// ── 2. Middleware Pipeline ──────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KMS API v1");
        c.RoutePrefix           = "swagger";
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
        c.DefaultModelsExpandDepth(-1);
        c.InjectJavascript("/swagger-auto-auth.js?v=20260408-5");  // ← auto-authorize
    });
}

app.UseStaticFiles();      // ← ก่อน UseAuthentication เพื่อเสิร์ฟ swagger-auto-auth.js
app.UseAuthentication();
app.UseAuthorization();
```

### Lock Icon Reference (สรุปย่อ)

| Endpoint | Auth | Lock |
|----------|------|------|
| `POST /api/auth/login` | — | 🔓 |
| `POST /api/auth/register` | — | 🔓 |
| `GET  /api/articles` | — | 🔓 |
| `POST /api/articles` | CanWrite | 🔒 |
| `POST /api/articles/{id}/publish` | CanPublish | 🔒 |
| `GET  /api/admin/*` | AdminOnly | 🔒 |

---

## 12. สร้าง React Project

```bash
# ── อยู่ที่ root /KMS ──────────────────────────────────────────
npm create vite@latest kms-web -- --template react-ts
cd kms-web
npm install
```

---

## 13. React — Packages และ Configuration

```bash
cd kms-web

# ── Data Fetching & State ─────────────────────────────────────────
npm install @tanstack/react-query
npm install @tanstack/react-router

# ── UI Components ─────────────────────────────────────────────────
npm install tailwindcss @tailwindcss/vite
npm install class-variance-authority clsx tailwind-merge
npm install lucide-react

# shadcn/ui
npx shadcn@latest init

# ── Rich Text Editor ──────────────────────────────────────────────
npm install @tiptap/react @tiptap/starter-kit
npm install @tiptap/extension-placeholder
npm install @tiptap/extension-character-count

# ── State Management ──────────────────────────────────────────────
npm install zustand

# ── API Client Generator ──────────────────────────────────────────
npm install -D @hey-api/openapi-ts

# ── Form Handling ─────────────────────────────────────────────────
npm install react-hook-form zod @hookform/resolvers

# ── Utilities ─────────────────────────────────────────────────────
npm install axios
npm install date-fns
npm install react-hot-toast
```

### vite.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
      },
      // v2: proxy สำหรับ static media files
      '/uploads': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
```

### tsconfig.json

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

### package.json scripts

```json
{
  "scripts": {
    "dev":          "vite",
    "build":        "tsc && vite build",
    "preview":      "vite preview",
    "lint":         "eslint . --ext ts,tsx",
    "generate:api": "openapi-ts --input http://localhost:5001/openapi/v1.json --output ./src/api --client fetch"
  }
}
```

---

## 14. React — โครงสร้างโปรเจกต์

```
kms-web/
├── public/
├── src/
│   ├── api/                          # openapi-ts generated (อย่าแก้มือ)
│   │   ├── client.gen.ts
│   │   ├── schemas.gen.ts
│   │   ├── services.gen.ts
│   │   └── types.gen.ts
│   │
│   ├── components/
│   │   ├── ui/                       # shadcn/ui components
│   │   ├── layout/
│   │   │   ├── AppLayout.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   └── Header.tsx
│   │   ├── article/
│   │   │   ├── ArticleCard.tsx
│   │   │   ├── ArticleList.tsx
│   │   │   └── ArticleStatusBadge.tsx
│   │   ├── editor/
│   │   │   ├── RichEditor.tsx        # TipTap editor
│   │   │   ├── AiToolbar.tsx         # AI writing tools
│   │   │   └── AiStreamPanel.tsx     # SSE streaming display
│   │   ├── media/                    # ← ใหม่ v2
│   │   │   ├── MediaUploader.tsx     # drag-and-drop upload
│   │   │   ├── MediaGallery.tsx      # แสดง attachments
│   │   │   └── AvatarUploader.tsx    # อัปโหลดรูปโปรไฟล์
│   │   └── common/
│   │       ├── SearchBar.tsx
│   │       └── Pagination.tsx
│   │
│   ├── pages/
│   │   ├── auth/
│   │   │   ├── LoginPage.tsx
│   │   │   └── RegisterPage.tsx
│   │   ├── articles/
│   │   │   ├── ArticlesPage.tsx
│   │   │   ├── ArticleDetailPage.tsx
│   │   │   └── ArticleEditPage.tsx
│   │   ├── search/
│   │   │   └── SearchPage.tsx
│   │   └── admin/
│   │       ├── AdminDashboard.tsx
│   │       ├── UsersPage.tsx
│   │       ├── ReviewQueuePage.tsx   # ← ใหม่ v2 (Post-audit queue)
│   │       └── SettingsPage.tsx
│   │
│   ├── hooks/
│   │   ├── useArticles.ts            # TanStack Query hooks
│   │   ├── useAuth.ts
│   │   ├── useAiStream.ts            # SSE streaming hook
│   │   ├── useMedia.ts               # ← ใหม่ v2
│   │   └── useSearch.ts
│   │
│   ├── stores/
│   │   ├── authStore.ts              # Zustand — JWT token, user info
│   │   └── uiStore.ts                # Zustand — sidebar, theme
│   │
│   ├── lib/
│   │   ├── axios.ts                  # Axios instance + interceptors
│   │   ├── queryClient.ts            # TanStack Query client config
│   │   └── utils.ts                  # cn(), formatDate(), etc.
│   │
│   ├── router.tsx
│   ├── App.tsx
│   └── main.tsx
│
├── vite.config.ts
├── tsconfig.json
├── tailwind.config.ts
└── package.json
```

---

## 15. React — API Client (openapi-ts)

### Generate TypeScript Client จาก OpenAPI

```bash
# ต้องรัน KMS.Api ก่อน
cd kms-web
npm run generate:api

# หรือรันตรงๆ
npx openapi-ts \
  --input http://localhost:5001/openapi/v1.json \
  --output ./src/api \
  --client fetch
```

> ทุกครั้งที่เพิ่ม endpoint ใน API ให้รันคำสั่งนี้ใหม่

### Axios Instance พร้อม JWT

```typescript
// src/lib/axios.ts
import axios from 'axios'
import { useAuthStore } from '@/stores/authStore'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5001',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      useAuthStore.getState().logout()
    }
    return Promise.reject(err)
  }
)

export default api
```

### SSE Streaming Hook (AI)

```typescript
// src/hooks/useAiStream.ts
import { useState, useCallback } from 'react'
import { useAuthStore } from '@/stores/authStore'

export function useAiStream() {
  const [output, setOutput]   = useState('')
  const [loading, setLoading] = useState(false)
  const token = useAuthStore((s) => s.token)

  const stream = useCallback(async (prompt: string) => {
    setOutput('')
    setLoading(true)

    const res = await fetch(
      `${import.meta.env.VITE_API_URL}/api/ai/stream`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ prompt }),
      }
    )

    const reader  = res.body!.getReader()
    const decoder = new TextDecoder()

    while (true) {
      const { value, done } = await reader.read()
      if (done) break

      const chunk = decoder.decode(value)
      // แยก SSE format: "data: <content>\n\n"
      for (const line of chunk.split('\n')) {
        if (line.startsWith('data: ')) {
          setOutput((prev) => prev + line.slice(6))
        }
      }
    }

    setLoading(false)
  }, [token])

  return { output, loading, stream }
}
```

### Media Upload Hook (v2)

```typescript
// src/hooks/useMedia.ts
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/axios'

// ดึง media ของ article
export function useArticleMedia(articleId: string, collection: string) {
  return useQuery({
    queryKey: ['media', articleId, collection],
    queryFn: () =>
      api.get(`/api/media/article/${articleId}/${collection}`)
         .then(r => r.data),
  })
}

// อัปโหลดไฟล์
export function useUploadMedia(articleId: string, collection: string) {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return api.post(
        `/api/media/article/${articleId}/${collection}`,
        form,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      ).then(r => r.data)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['media', articleId, collection] })
    },
  })
}

// ลบไฟล์
export function useDeleteMedia() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (mediaId: string) =>
      api.delete(`/api/media/${mediaId}`).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['media'] })
    },
  })
}
```

---

## 16. API Key Management

### Domain Entity

```csharp
// Domain/Entities/ApiKey.cs
public class ApiKey
{
    public Guid      Id          { get; set; } = Guid.NewGuid();
    public string    Name        { get; set; } = string.Empty;
    public string    KeyHash     { get; set; } = string.Empty;  // SHA-256
    public string    Prefix      { get; set; } = string.Empty;  // "skc_live_"
    public string    ClientType  { get; set; } = string.Empty;
    public string    Permissions { get; set; } = "[]";          // JSON array
    public bool      IsActive    { get; set; } = true;
    public DateTime? ExpiresAt   { get; set; }
    public DateTime? LastUsedAt  { get; set; }
    public int       UsageCount  { get; set; } = 0;
    public string?   Description { get; set; }
    public string?   AllowedIps  { get; set; }                  // JSON array
    public Guid?     CreatedById { get; set; }
    public AppUser?  CreatedBy   { get; set; }
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt   { get; set; }
    public Guid?     RevokedById { get; set; }
    public AppUser?  RevokedBy   { get; set; }

    public bool IsValid() =>
        IsActive &&
        RevokedAt == null &&
        (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    public bool HasPermission(string permission)
    {
        var perms = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(Permissions) ?? [];
        return perms.Contains("*") || perms.Contains(permission);
    }
}
```

### ApiKeyService

```csharp
// Application/Services/ApiKeyService.cs
public class ApiKeyService(ApplicationDbContext db)
{
    public async Task<(ApiKey entity, string rawKey)> CreateAsync(
        string name, string clientType, List<string> permissions,
        Guid createdById, DateTime? expiresAt = null,
        string? description = null, List<string>? allowedIps = null)
    {
        var prefix = "skc_live_";
        var rawKey = prefix + Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))
            .Replace("+", "").Replace("/", "").Replace("=", "")[..32];

        var entity = new ApiKey
        {
            Name        = name,
            KeyHash     = HashKey(rawKey),
            Prefix      = prefix,
            ClientType  = clientType,
            Permissions = System.Text.Json.JsonSerializer.Serialize(permissions),
            ExpiresAt   = expiresAt,
            Description = description,
            AllowedIps  = allowedIps != null
                ? System.Text.Json.JsonSerializer.Serialize(allowedIps)
                : null,
            CreatedById = createdById,
        };

        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync();

        return (entity, rawKey); // rawKey แสดงให้ user เห็นครั้งเดียว
    }

    public async Task<ApiKey?> ValidateAsync(string rawKey)
    {
        var apiKey = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == HashKey(rawKey));

        if (apiKey == null || !apiKey.IsValid()) return null;

        apiKey.LastUsedAt = DateTime.UtcNow;
        apiKey.UsageCount++;
        await db.SaveChangesAsync();

        return apiKey;
    }

    public async Task<bool> RevokeAsync(Guid keyId, Guid revokedById)
    {
        var apiKey = await db.ApiKeys.FindAsync(keyId);
        if (apiKey == null) return false;

        apiKey.IsActive    = false;
        apiKey.RevokedAt   = DateTime.UtcNow;
        apiKey.RevokedById = revokedById;
        await db.SaveChangesAsync();

        return true;
    }

    private static string HashKey(string rawKey)
    {
        var bytes = System.Security.Cryptography.SHA256
            .HashData(System.Text.Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLower();
    }
}
```

### ApiKeyMiddleware

```csharp
// Api/Middleware/ApiKeyMiddleware.cs
public class ApiKeyMiddleware(RequestDelegate next)
{
    private static readonly Dictionary<string, string> ProtectedRoutes = new()
    {
        { "/api/linebot/chat", "linebot:chat" },
        { "/api/search",       "search:read"  },
    };

    public async Task InvokeAsync(HttpContext ctx, ApiKeyService apiKeyService)
    {
        var path = ctx.Request.Path.Value?.ToLower() ?? "";

        if (!ProtectedRoutes.TryGetValue(path, out var requiredPermission))
        {
            await next(ctx);
            return;
        }

        var rawKey = ctx.Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(rawKey))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = "API Key required" });
            return;
        }

        var apiKey = await apiKeyService.ValidateAsync(rawKey);
        if (apiKey == null)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = "Invalid or expired API Key" });
            return;
        }

        if (!apiKey.HasPermission(requiredPermission))
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(new { error = "Insufficient permissions" });
            return;
        }

        if (apiKey.AllowedIps != null)
        {
            var allowedIps = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(apiKey.AllowedIps) ?? [];
            var clientIp = ctx.Connection.RemoteIpAddress?.ToString();

            if (!allowedIps.Contains(clientIp ?? ""))
            {
                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(new { error = "IP not allowed" });
                return;
            }
        }

        ctx.Items["ApiKey"] = apiKey;
        await next(ctx);
    }
}
```

### ตัวอย่างการใช้ใน OpenClaw

```javascript
// OpenClaw — เรียก KMS API
const response = await fetch(
  'https://api.kms.rmuti-skc.ac.th/api/linebot/chat',
  {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': process.env.SKCKMS_API_KEY, // skc_live_xxxx...
    },
    body: JSON.stringify({ message: userMessage, userId: lineUserId }),
  }
);
```

### Seed — สร้าง ApiKey เริ่มต้น

```csharp
// Infrastructure/Data/SeedData.cs
public static async Task SeedApiKeysAsync(
    ApplicationDbContext db,
    ApiKeyService   apiKeyService,
    Guid            adminUserId)
{
    if (await db.ApiKeys.AnyAsync()) return;

    var (_, rawKey) = await apiKeyService.CreateAsync(
        name:        "OpenClaw Development",
        clientType:  "OpenClaw",
        permissions: ["linebot:chat", "search:read"],
        createdById: adminUserId,
        description: "Key สำหรับ OpenClaw ในสภาพแวดล้อม Development"
    );

    Console.WriteLine("===========================================");
    Console.WriteLine($"OpenClaw Dev API Key: {rawKey}");
    Console.WriteLine("บันทึก key นี้ไว้ จะไม่สามารถดูได้อีก");
    Console.WriteLine("===========================================");
}
```

---

## 17. Run และ Test

```bash
# ──────────────────────────────────────────
# Terminal 1: รัน API
# ──────────────────────────────────────────
dotnet run --project src/KMS.Api
# API:     https://localhost:5001
# Swagger: https://localhost:5001/swagger  ← เข้าที่นี่

# ──────────────────────────────────────────
# Terminal 2: รัน React (Vite)
# ──────────────────────────────────────────
cd kms-web
npm run dev
# Web: http://localhost:5173

# ──────────────────────────────────────────────────────────
# v4: ไม่มี Ollama — ใช้ Cloud providers (OpenRouter + XiaomiMiMo)
# ไม่ต้องรัน Terminal 3 สำหรับ Ollama อีกต่อไป
# ──────────────────────────────────────────────────────────

# ──────────────────────────────────────────
# Terminal 4: Redis (ต้องติดตั้งก่อน: sudo apt install redis-server)
# ──────────────────────────────────────────
# sudo systemctl start redis-server
# redis-cli ping  → PONG

# ──────────────────────────────────────────
# Hot Reload
# ──────────────────────────────────────────
dotnet watch --project src/KMS.Api  # API watch mode
# React: Vite HMR เปิดอัตโนมัติ

# ──────────────────────────────────────────
# Testing
# ──────────────────────────────────────────
dotnet test
dotnet test --verbosity normal
dotnet test --filter "Category=Unit"

# ──────────────────────────────────────────
# Build
# ──────────────────────────────────────────
dotnet build --configuration Release

cd kms-web
npm run build

# ──────────────────────────────────────────
# Database shortcuts
# ──────────────────────────────────────────
psql -U postgres -d KMS_Dev    # เปิด psql
\dt                              # ดูตาราง
\d "KnowledgeArticles"           # ดู schema
\di                              # ดู indexes

# ตรวจสอบ Trigger (v2)
psql -U postgres -d KMS_Dev \
  -c "SELECT tgname FROM pg_trigger WHERE tgrelid = '\"AuditLogs\"'::regclass;"
# ต้องเห็น: trg_audit_log_no_update, trg_audit_log_no_delete
```

---

## 18. CLAUDE.md สำหรับ Claude Code

> วางไฟล์นี้ที่ root `/KMS/CLAUDE.md`
> Claude Code จะอ่านทุกครั้งที่เริ่ม session อัตโนมัติ

```markdown
# CLAUDE.md — KMS Project Context (v3)

## Project
KMS (Knowledge Management System) — ระบบจัดการองค์ความรู้
มหาวิทยาลัยเทคโนโลยีราชมงคลอีสาน วิทยาเขตสกลนคร
รองรับการเชื่อมต่อ Line OA เพื่อให้บริการคำปรึกษาอัตโนมัติ

## Tech Stack

### Backend (src/)
- .NET 10, ASP.NET Core Web API (Controllers)
- EF Core 10 + PostgreSQL 16 + pgvector extension
- ASP.NET Core Identity + JWT Bearer
- AI Chat Fallback: OpenRouter `qwen/qwen3.6-plus:free` (P1) → XiaomiMiMo `mimo-v2-flash` (P2)
- AI Embed: OpenRouter `qwen/qwen3-embedding` (Queue on fail)
- Microsoft.Extensions.AI + OpenAI SDK
- MediatR (CQRS), FluentValidation, Mapster
- SixLabors.ImageSharp (Media conversions)
- Swashbuckle.AspNetCore (Swagger UI at /swagger)

### Frontend (kms-web/)
- React 19 + TypeScript + Vite
- TanStack Query (data fetching + caching)
- TanStack Router (routing)
- TipTap (Rich Text Editor)
- Tailwind CSS + shadcn/ui
- Zustand (global state: auth, ui)
- openapi-ts (TypeScript client — generate จาก OpenAPI spec)
- react-hook-form + zod (form validation)

## Architecture
Clean Architecture — 4 .NET projects + 1 React project:
- KMS.Domain        → Entities, Enums, Events, Interfaces, AiProviderConfig
- KMS.Application   → CQRS, DTOs, IMediaService, IPublishWorkflowService, IFallbackChatService, IEmbeddingService
- KMS.Infrastructure → EF Core, OpenRouterChatService, XiaomiMimoChatService, FallbackChatService, FallbackEmbeddingService, MediaService, AuditService
- KMS.Api           → Controllers, Middleware, Program.cs
- kms-web/          → React 19 Frontend (Vite)

## Key Design Decisions
- Thai-first: Title/Content/Summary required (ภาษาไทย); TitleEn/ContentEn/SummaryEn/KeywordsEn optional
- AI Fallback Chain: OpenRouter (P1) → XiaomiMiMo (P2) สำหรับ Chat; OpenRouter + Queue on fail สำหรับ Embedding
- Embedding: qwen/qwen3-embedding (OpenRouter) — vector space สม่ำเสมอทั้งระบบ ไม่มี dimension mismatch
- Streaming AI: IAsyncEnumerable + SSE — React อ่านด้วย fetch + ReadableStream
- MediaLibrary: ตาราง MediaItems กลางตารางเดียว — Polymorphic + Collections + Conversions
- Publish-First: Faculty/Admin → Direct Publish; Researcher → UnderReview
- AuditLog: Append-only — DB Trigger + EF Interceptor ป้องกัน mutate
- pgvector HNSW index (cosine similarity), Embedding vector(1024) — qwen3-embedding MRL
- Soft delete: DeletedAt + HasQueryFilter

## Database
- PostgreSQL 16 + extensions: vector, uuid-ossp, unaccent
- DbContext: ApplicationDbContext (IdentityDbContext<AppUser, AppRole, Guid>)
- Migration: --project src/KMS.Infrastructure --startup-project src/KMS.Api
- หลัง migrate ต้องรัน: psql ... -f scripts/audit_log_trigger.sql
- Embedding dimension: 1024 (vector(1024)) — qwen3-embedding MRL output

## AI Fallback Chain
| Role      | Priority 1 (P1)                         | Priority 2 (P2)            |
|-----------|-----------------------------------------|----------------------------|
| Chat      | OpenRouter `qwen/qwen3.6-plus:free`     | XiaomiMiMo `mimo-v2-flash` |
| Embedding | OpenRouter `qwen/qwen3-embedding` (1024d) | Queue + Retry              |

## Roles & Publish Workflow
| Role       | สิทธิ์                          | Publish              |
|------------|----------------------------------|----------------------|
| Admin      | ทุกอย่าง                         | Direct (ข้าม Review) |
| Faculty    | write, publish, review           | Direct (ข้าม Review) |
| Researcher | write, submit                    | ต้องผ่าน UnderReview |
| Student    | read, comment                    | —                    |
| Guest      | read public only                 | —                    |

## MediaLibrary Collections
| ModelType        | Collection  | SingleFile | Max Size | Conversions         |
|------------------|-------------|-----------|----------|---------------------|
| KnowledgeArticle | cover       | ✅        | 5 MB     | thumb, card, og     |
| KnowledgeArticle | attachments | —         | 100 MB   | ไม่มี               |
| AppUser          | avatar      | ✅        | 2 MB     | thumb 150×150       |

## API Endpoints
| Group   | Endpoint                           | Method         | Auth      |
|---------|------------------------------------|----------------|-----------|
| Auth    | /api/auth/login                    | POST           | —         |
| Auth    | /api/auth/register                 | POST           | —         |
| Auth    | /api/auth/refresh                  | POST           | —         |
| Articles| /api/articles                      | GET/POST       | JWT       |
| Articles| /api/articles/{id}                 | GET/PUT/DELETE | JWT       |
| Articles| /api/articles/{id}/publish         | POST           | CanWrite  |
| AI      | /api/ai/generate                   | POST           | CanWrite  |
| AI      | /api/ai/improve                    | POST           | CanWrite  |
| AI      | /api/ai/stream                     | GET            | CanWrite  |
| Media   | /api/media/{id}                    | GET            | —         |
| Media   | /api/media/{id}/conversions/{name} | GET            | —         |
| Media   | /api/media/article/{id}/{coll}     | GET/POST       | JWT       |
| Media   | /api/media/{id}                    | DELETE         | JWT       |
| Search  | /api/search                        | GET            | —         |
| LineBot | /api/linebot/chat                  | POST           | X-Api-Key |
| Admin   | /api/admin/*                       | *              | AdminOnly |
| Admin   | /api/admin/ai-health               | GET            | AdminOnly |
| ApiKeys | /api/admin/apikeys                 | GET/POST       | AdminOnly |
| ApiKeys | /api/admin/apikeys/{id}            | DELETE         | AdminOnly |

## API Key Management
- ตาราง: ApiKeys — เก็บ SHA-256 hash ไม่เก็บ raw key
- Header: X-Api-Key: skc_live_xxxx...
- Middleware: ApiKeyMiddleware
- Raw key แสดงครั้งเดียวตอนสร้าง

## AI Features
- GenerateDraft: RAG → top-5 articles → Chat Fallback Chain (streaming)
- ImproveText: Grammar | Concise | Formal | Expand | Simplify
- Summarize: bullet points
- Translate: TitleEn, SummaryEn, **ContentEn** (on-demand — ทั้งหมด optional, Thai-First)
- AutoTag: JSON array จาก LLM
- Q&A: RAG full pipeline
- LineBot: System Prompt + RAG → ตอบเฉพาะขอบเขตวิชาการและวิจัย

## Coding Conventions — Backend
- Primary constructors ทุกที่
- Controllers API (โปรเจคปัจจุบันใช้ Controllers)
- IAsyncEnumerable<string> สำหรับ streaming
- AI Chat ผ่าน IFallbackChatService (ไม่ inject IChatService ตรงๆ)
- AI Embedding ผ่าน IEmbeddingService (ไม่ inject IEmbeddingGenerator ตรงๆ)
- Swagger UI ที่ /swagger (dev) — ใช้ lock icon ผ่าน SecurityRequirementsOperationFilter
- IEntityTypeConfiguration แยกไฟล์ต่อ entity
- ApplyConfigurationsFromAssembly — ไม่ config ใน OnModelCreating โดยตรง
- เขียน comments ภาษาไทย

## Coding Conventions — Frontend
- TypeScript strict mode เสมอ
- TanStack Query สำหรับ data fetching ทุกกรณี
- ไม่ใช้ useEffect สำหรับ data fetching
- Zustand เฉพาะ global state — local state ใช้ useState
- shadcn/ui เป็น base component
- ไม่ generate ไฟล์ใน src/api/ มือ — ใช้ npm run generate:api เสมอ
- เขียน comments ภาษาไทย

## Important File Paths
- Schema:      KMS_Database_Schema_Full3.md
- This file:   CLAUDE.md
- API runs:    https://localhost:5001
- Swagger:     https://localhost:5001/swagger (dev only)
- React runs:  http://localhost:5173
- API client:  kms-web/src/services/api.ts + kms-web/src/types/api.ts
- Trigger SQL: scripts/audit_log_trigger.sql
- Uploads:     uploads/ (Local storage)
- AI Health:   GET /api/admin/ai-health (ดูสถานะ providers)
- Trigger SQL: scripts/audit_log_trigger.sql
```

---

## Quick Reference — คำสั่งที่ใช้บ่อย

```bash
# ── .NET ──────────────────────────────────────────────────────────

# รัน API (dev)
dotnet watch --project src/KMS.Api
# Swagger: https://localhost:5001/swagger

# สร้าง migration ใหม่
dotnet ef migrations add <MigrationName> \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api \
  --output-dir Data/Migrations

# Apply migration + trigger
dotnet ef database update \
  --project src/KMS.Infrastructure \
  --startup-project src/KMS.Api && \
psql -U postgres -d KMS_Dev -f scripts/audit_log_trigger.sql

# รัน tests
dotnet test

# Build release
dotnet build --configuration Release

# ── React ─────────────────────────────────────────────────────────

# รัน dev
cd kms-web && npm run dev

# Generate API client (หลัง API เปลี่ยน)
npm run generate:api

# Build production
npm run build

# ── PostgreSQL (ติดตั้งโดยตรง) ───────────────────────────────────

# ตรวจสอบ service
sudo systemctl status postgresql

# ── Redis (ติดตั้งโดยตรง — ยังไม่ได้ติดตั้งบนเครื่องนี้) ──────────

# ── Database ──────────────────────────────────────────────────────

psql -U postgres -d KMS_Dev       # เปิด psql
\dt                              # ดูตาราง
\d "KnowledgeArticles"           # ดู schema
\d "MediaItems"

# ตรวจสอบ Audit Log Trigger
SELECT tgname FROM pg_trigger
WHERE tgrelid = '"AuditLogs"'::regclass;
# ต้องเห็น: trg_audit_log_no_update, trg_audit_log_no_delete

# ── AI Health Check ───────────────────────────────────────────────

# ดูสถานะ AI providers (Admin only)
curl -H "Authorization: Bearer <admin-token>" \
     https://localhost:5001/api/admin/ai-health
```

---

*KMS — Knowledge Management System*
*Stack: .NET 10 · EF Core 10 · PostgreSQL 16 · pgvector(1024) · React 19 · Vite · OpenRouter · XiaomiMiMo · Swagger*
*Version: Setup v4 — MediaLibrary · AI Fallback Chain (no Ollama) · Swagger+JWT · Publish-First · Immutable AuditLog*
