# KMS — Database Schema (เต็มรูปแบบ v2)

> **Stack:** PostgreSQL 16 · EF Core 10 · pgvector · ASP.NET Core Identity
> **Encoding:** UTF-8 · **Collation:** th-TH (Thai-aware)
> **Extensions:** `uuid-ossp`, `vector`, `unaccent`
> **เวอร์ชัน:** v4 — รวม MediaLibrary · AI Fallback Chain (no Ollama) · ContentEn · Publish-First Workflow · Immutable AuditLog

---

## สารบัญ

1. [Identity Group](#1-identity-group)
2. [Knowledge Group](#2-knowledge-group)
3. [Media Group](#3-media-group) *(ใหม่)*
4. [Interaction Group](#4-interaction-group)
5. [AI & Logging Group](#5-ai--logging-group)
6. [System Group](#6-system-group)
7. [Security Group](#7-security-group)
8. [Indexes & Performance](#8-indexes--performance)
9. [Relationships Summary](#9-relationships-summary)
10. [Seed Data](#10-seed-data)
11. [EF Core DbContext](#11-ef-core-dbcontext)
12. [Entity Configurations](#12-entity-configurations)

---

## 1. Identity Group

### AppUser (AspNetUsers)
> extends ASP.NET Core Identity IdentityUser\<Guid\>

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `UserName` | `varchar(256)` | NO | — | ชื่อผู้ใช้ (unique) |
| `NormalizedUserName` | `varchar(256)` | NO | — | uppercase index |
| `Email` | `varchar(256)` | YES | — | อีเมล |
| `NormalizedEmail` | `varchar(256)` | YES | — | uppercase index |
| `EmailConfirmed` | `boolean` | NO | `false` | ยืนยันอีเมลแล้ว |
| `PasswordHash` | `text` | YES | — | password hash |
| `SecurityStamp` | `text` | YES | — | security token |
| `ConcurrencyStamp` | `text` | YES | — | optimistic concurrency |
| `PhoneNumber` | `varchar(20)` | YES | — | เบอร์โทร |
| `PhoneNumberConfirmed` | `boolean` | NO | `false` | — |
| `TwoFactorEnabled` | `boolean` | NO | `false` | — |
| `LockoutEnd` | `timestamptz` | YES | — | ล็อกจนถึงเมื่อ |
| `LockoutEnabled` | `boolean` | NO | `true` | — |
| `AccessFailedCount` | `int` | NO | `0` | — |
| `FullNameTh` | `varchar(200)` | NO | — | ชื่อ-นามสกุล ภาษาไทย |
| `FullNameEn` | `varchar(200)` | YES | — | ชื่อ-นามสกุล ภาษาอังกฤษ |
| `Faculty` | `varchar(200)` | YES | — | คณะ |
| `Department` | `varchar(200)` | YES | — | ภาควิชา/สาขา |
| `Position` | `varchar(200)` | YES | — | ตำแหน่ง |
| `EmployeeCode` | `varchar(50)` | YES | — | รหัสบุคลากร |
| `Bio` | `text` | YES | — | ประวัติย่อ |
| `IsActive` | `boolean` | NO | `true` | เปิด/ปิดบัญชี |
| `CreatedAt` | `timestamptz` | NO | `now()` | วันที่สร้าง |
| `UpdatedAt` | `timestamptz` | YES | — | วันที่แก้ไขล่าสุด |
| `LastLoginAt` | `timestamptz` | YES | — | เข้าสู่ระบบล่าสุด |

> **v2:** ลบ `AvatarUrl` ออก — รูปโปรไฟล์จัดการผ่าน `MediaItems` (collection: `avatar`) แทน

```sql
CREATE TABLE "AspNetUsers" (
    "Id"                   uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserName"             varchar(256),
    "NormalizedUserName"   varchar(256),
    "Email"                varchar(256),
    "NormalizedEmail"      varchar(256),
    "EmailConfirmed"       boolean      NOT NULL DEFAULT false,
    "PasswordHash"         text,
    "SecurityStamp"        text,
    "ConcurrencyStamp"     text,
    "PhoneNumber"          varchar(20),
    "PhoneNumberConfirmed" boolean      NOT NULL DEFAULT false,
    "TwoFactorEnabled"     boolean      NOT NULL DEFAULT false,
    "LockoutEnd"           timestamptz,
    "LockoutEnabled"       boolean      NOT NULL DEFAULT true,
    "AccessFailedCount"    int          NOT NULL DEFAULT 0,
    -- Custom fields
    "FullNameTh"           varchar(200) NOT NULL,
    "FullNameEn"           varchar(200),
    "Faculty"              varchar(200),
    "Department"           varchar(200),
    "Position"             varchar(200),
    "EmployeeCode"         varchar(50),
    -- AvatarUrl ถูกลบออก → ใช้ MediaItems (collection: avatar) แทน
    "Bio"                  text,
    "IsActive"             boolean      NOT NULL DEFAULT true,
    "CreatedAt"            timestamptz  NOT NULL DEFAULT now(),
    "UpdatedAt"            timestamptz,
    "LastLoginAt"          timestamptz
);
```

---

### AppRole (AspNetRoles)

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `Name` | `varchar(256)` | YES | — | ชื่อ role |
| `NormalizedName` | `varchar(256)` | YES | — | uppercase |
| `ConcurrencyStamp` | `text` | YES | — | — |
| `Description` | `varchar(500)` | YES | — | คำอธิบาย role |
| `Permissions` | `text` | YES | — | JSON array of permissions |

```sql
CREATE TABLE "AspNetRoles" (
    "Id"               uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Name"             varchar(256),
    "NormalizedName"   varchar(256),
    "ConcurrencyStamp" text,
    "Description"      varchar(500),
    "Permissions"      text
);
```

**Roles และสิทธิ์ Publish:**

| Name | Description | Permissions | Publish โดยตรง |
|------|-------------|-------------|----------------|
| `Admin` | ผู้ดูแลระบบ | `["*"]` | ✅ ข้าม Review ได้ |
| `Faculty` | อาจารย์ | `["articles:write","articles:publish","articles:review"]` | ✅ ข้าม Review ได้ |
| `Researcher` | นักวิจัย | `["articles:write","articles:submit"]` | — ต้องรอ Review |
| `Student` | นักศึกษา | `["articles:read","comments:write"]` | — |
| `Guest` | บุคคลทั่วไป | `["articles:read:public"]` | — |

---

### AppUserRole (AspNetUserRoles)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `UserId` | `uuid` | NO | FK → AspNetUsers.Id |
| `RoleId` | `uuid` | NO | FK → AspNetRoles.Id |

---

### AppUserClaim / AppRoleClaim / AppUserLogin / AppUserToken
> Identity standard tables — ไม่แก้ไข schema

---

## 2. Knowledge Group

### Category

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `Name` | `varchar(200)` | NO | — | ชื่อหมวดหมู่ (ไทย) |
| `NameEn` | `varchar(200)` | YES | — | ชื่อหมวดหมู่ (EN) optional |
| `Slug` | `varchar(250)` | NO | — | URL-friendly (unique) |
| `Description` | `text` | YES | — | คำอธิบาย |
| `ParentId` | `uuid` | YES | — | FK self (หมวดหมู่แม่) |
| `IconName` | `varchar(100)` | YES | — | ชื่อ icon |
| `ColorHex` | `varchar(7)` | YES | — | สีประจำหมวด เช่น `#534AB7` |
| `SortOrder` | `int` | NO | `0` | ลำดับการแสดง |
| `IsActive` | `boolean` | NO | `true` | เปิด/ปิด |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |
| `UpdatedAt` | `timestamptz` | YES | — | — |

```sql
CREATE TABLE "Categories" (
    "Id"          uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Name"        varchar(200) NOT NULL,
    "NameEn"      varchar(200),
    "Slug"        varchar(250) NOT NULL UNIQUE,
    "Description" text,
    "ParentId"    uuid         REFERENCES "Categories"("Id") ON DELETE SET NULL,
    "IconName"    varchar(100),
    "ColorHex"    varchar(7),
    "SortOrder"   int          NOT NULL DEFAULT 0,
    "IsActive"    boolean      NOT NULL DEFAULT true,
    "CreatedAt"   timestamptz  NOT NULL DEFAULT now(),
    "UpdatedAt"   timestamptz
);
```

---

### KnowledgeArticle

> ตารางหลักของระบบ — เก็บบทความ งานวิจัย สื่อการสอน นโยบาย

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `Title` | `varchar(500)` | NO | — | ชื่อบทความ (ไทย) **required** |
| `TitleEn` | `varchar(500)` | YES | — | ชื่อบทความ (EN) optional |
| `Slug` | `varchar(600)` | NO | — | URL slug (unique) |
| `Content` | `text` | NO | — | เนื้อหา Markdown (ไทย) **required** |
| `ContentEn` | `text` | YES | — | เนื้อหา Markdown (EN) optional — AI แปลอัตโนมัติ |
| `Summary` | `varchar(2000)` | NO | — | สรุปย่อ (ไทย) |
| `SummaryEn` | `varchar(2000)` | YES | — | สรุปย่อ (EN) optional |
| `KeywordsEn` | `varchar(500)` | YES | — | keywords EN สำหรับ citation |
| `Status` | `varchar(20)` | NO | `'Draft'` | `Draft` / `UnderReview` / `Published` / `Archived` |
| `Visibility` | `varchar(20)` | NO | `'Internal'` | `Public` / `Internal` / `Restricted` |
| `CategoryId` | `uuid` | NO | — | FK → Categories.Id |
| `AuthorId` | `uuid` | NO | — | FK → AspNetUsers.Id |
| `ReviewerId` | `uuid` | YES | — | FK → AspNetUsers.Id (ผู้ Publish/Review) |
| `IsAutoTranslated` | `boolean` | NO | `false` | EN fields แปลโดย AI? |
| `TranslatedAt` | `timestamptz` | YES | — | วันที่แปล |
| `ViewCount` | `int` | NO | `0` | ยอดวิว |
| `LikeCount` | `int` | NO | `0` | ยอดถูกใจ |
| `Embedding` | `vector(1024)` | YES | — | pgvector (TH+EN combined) — dim 1024 (qwen3-embedding MRL) |
| `PublishedAt` | `timestamptz` | YES | — | วันที่เผยแพร่ |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |
| `UpdatedAt` | `timestamptz` | YES | — | — |
| `DeletedAt` | `timestamptz` | YES | — | soft delete |

> **v2:** ลบ `CoverImageUrl` ออก — รูปปกจัดการผ่าน `MediaItems` (collection: `cover`) แทน
> **v4:** เพิ่ม `ContentEn` — เนื้อหาภาษาอังกฤษ optional (ตาม Thai-First Strategy)

```sql
CREATE TABLE "KnowledgeArticles" (
    "Id"               uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Title"            varchar(500)  NOT NULL,
    "TitleEn"          varchar(500),
    "Slug"             varchar(600)  NOT NULL UNIQUE,
    "Content"          text          NOT NULL,
    "ContentEn"        text,                           -- EN optional — AI แปลอัตโนมัติ
    "Summary"          varchar(2000) NOT NULL,
    "SummaryEn"        varchar(2000),
    "KeywordsEn"       varchar(500),
    -- CoverImageUrl ถูกลบออก → ใช้ MediaItems (collection: cover) แทน
    "Status"           varchar(20)   NOT NULL DEFAULT 'Draft',
    "Visibility"       varchar(20)   NOT NULL DEFAULT 'Internal',
    "CategoryId"       uuid          NOT NULL REFERENCES "Categories"("Id") ON DELETE RESTRICT,
    "AuthorId"         uuid          NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ReviewerId"       uuid          REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "IsAutoTranslated" boolean       NOT NULL DEFAULT false,
    "TranslatedAt"     timestamptz,
    "ViewCount"        int           NOT NULL DEFAULT 0,
    "LikeCount"        int           NOT NULL DEFAULT 0,
    "Embedding"        vector(1024),
    "PublishedAt"      timestamptz,
    "CreatedAt"        timestamptz   NOT NULL DEFAULT now(),
    "UpdatedAt"        timestamptz,
    "DeletedAt"        timestamptz,

    CONSTRAINT chk_status     CHECK ("Status" IN ('Draft','UnderReview','Published','Archived')),
    CONSTRAINT chk_visibility CHECK ("Visibility" IN ('Public','Internal','Restricted'))
);
```

**Status Workflow (v2 — Role-based):**
```
Admin / Faculty:
  Draft ──────────────────────────→ Published → Archived
                                    (Direct Publish — ไม่รอ Review)
                                    ↓ แจ้ง Admin เพื่อ Post-audit อัตโนมัติ

Researcher:
  Draft → UnderReview → Published → Archived
            ↑__________|  (ส่งกลับแก้ไข)
```

---

### ArticleVersion

> เก็บประวัติทุก version ของบทความ

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `ArticleId` | `uuid` | NO | — | FK → KnowledgeArticles.Id |
| `VersionNumber` | `int` | NO | — | เลข version เรียงตาม article |
| `Title` | `varchar(500)` | NO | — | snapshot title |
| `Content` | `text` | NO | — | snapshot content |
| `Summary` | `varchar(2000)` | NO | — | snapshot summary |
| `EditedById` | `uuid` | NO | — | FK → AspNetUsers.Id |
| `ChangeNote` | `varchar(500)` | YES | — | บันทึกการเปลี่ยนแปลง |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |

```sql
CREATE TABLE "ArticleVersions" (
    "Id"            uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "ArticleId"     uuid          NOT NULL REFERENCES "KnowledgeArticles"("Id") ON DELETE CASCADE,
    "VersionNumber" int           NOT NULL,
    "Title"         varchar(500)  NOT NULL,
    "Content"       text          NOT NULL,
    "Summary"       varchar(2000) NOT NULL,
    "EditedById"    uuid          NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ChangeNote"    varchar(500),
    "CreatedAt"     timestamptz   NOT NULL DEFAULT now(),

    UNIQUE ("ArticleId", "VersionNumber")
);
```

---

### Tag

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `Name` | `varchar(100)` | NO | — | ชื่อ tag (unique) |
| `Slug` | `varchar(120)` | NO | — | URL-friendly (unique) |
| `UsageCount` | `int` | NO | `0` | จำนวนการใช้งาน |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |

```sql
CREATE TABLE "Tags" (
    "Id"         uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Name"       varchar(100) NOT NULL UNIQUE,
    "Slug"       varchar(120) NOT NULL UNIQUE,
    "UsageCount" int          NOT NULL DEFAULT 0,
    "CreatedAt"  timestamptz  NOT NULL DEFAULT now()
);
```

---

### ArticleTag (Junction)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `ArticleId` | `uuid` | NO | FK → KnowledgeArticles.Id |
| `TagId` | `uuid` | NO | FK → Tags.Id |

```sql
CREATE TABLE "ArticleTags" (
    "ArticleId" uuid NOT NULL REFERENCES "KnowledgeArticles"("Id") ON DELETE CASCADE,
    "TagId"     uuid NOT NULL REFERENCES "Tags"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("ArticleId", "TagId")
);
```

---

## 3. Media Group

> **ใหม่ใน v2** — แทนที่ตาราง `Attachments` เดิม ด้วยแนวคิด MediaLibrary (Spatie-inspired)
> ตารางกลางสำหรับจัดการไฟล์ทุกประเภทในระบบ รองรับ Polymorphic / Collections / Conversions

### MediaItem

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `ModelType` | `varchar(100)` | NO | — | ชื่อ Model เช่น `KnowledgeArticle`, `AppUser` |
| `ModelId` | `uuid` | NO | — | Id ของ Model |
| `CollectionName` | `varchar(100)` | NO | `'default'` | กลุ่มไฟล์ เช่น `cover`, `attachments`, `avatar` |
| `Name` | `varchar(255)` | NO | — | ชื่อที่ใช้แสดง (ไม่มี extension) |
| `FileName` | `varchar(255)` | NO | — | ชื่อไฟล์จริงบน disk (uuid-based) |
| `MimeType` | `varchar(100)` | NO | — | MIME type เช่น `image/jpeg` |
| `Extension` | `varchar(20)` | NO | — | นามสกุลไฟล์ เช่น `jpg`, `pdf` |
| `Disk` | `varchar(50)` | NO | `'local'` | storage backend: `local` / `minio` / `s3` |
| `ConversionsDisk` | `varchar(50)` | YES | — | disk สำหรับ conversions (null = ใช้ Disk เดียวกัน) |
| `Path` | `varchar(1000)` | NO | — | relative path บน disk |
| `Size` | `bigint` | NO | — | ขนาดไฟล์ (bytes) |
| `Manipulations` | `jsonb` | NO | `'{}'` | instructions สำหรับ crop/rotate |
| `CustomProperties` | `jsonb` | NO | `'{}'` | metadata เพิ่มเติม (arbitrary) |
| `GeneratedConversions` | `jsonb` | NO | `'{}'` | สถานะ conversions เช่น `{"thumb":true}` |
| `ResponsiveImages` | `jsonb` | NO | `'{}'` | srcset metadata |
| `OrderColumn` | `int` | YES | — | ลำดับในกรณีมีหลายไฟล์ใน collection |
| `UploadedById` | `uuid` | NO | — | FK → AspNetUsers.Id |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |
| `UpdatedAt` | `timestamptz` | YES | — | — |

```sql
CREATE TABLE "MediaItems" (
    "Id"                   uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,

    -- Polymorphic association
    "ModelType"            varchar(100)  NOT NULL,
    "ModelId"              uuid          NOT NULL,

    -- Collection & naming
    "CollectionName"       varchar(100)  NOT NULL DEFAULT 'default',
    "Name"                 varchar(255)  NOT NULL,
    "FileName"             varchar(255)  NOT NULL,
    "MimeType"             varchar(100)  NOT NULL,
    "Extension"            varchar(20)   NOT NULL,

    -- Storage
    "Disk"                 varchar(50)   NOT NULL DEFAULT 'local',
    "ConversionsDisk"      varchar(50),
    "Path"                 varchar(1000) NOT NULL,

    -- Metadata
    "Size"                 bigint        NOT NULL,
    "Manipulations"        jsonb         NOT NULL DEFAULT '{}',
    "CustomProperties"     jsonb         NOT NULL DEFAULT '{}',
    "GeneratedConversions" jsonb         NOT NULL DEFAULT '{}',
    "ResponsiveImages"     jsonb         NOT NULL DEFAULT '{}',

    -- Ordering
    "OrderColumn"          int,

    -- Audit
    "UploadedById"         uuid          NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "CreatedAt"            timestamptz   NOT NULL DEFAULT now(),
    "UpdatedAt"            timestamptz
);
```

**Collections ที่กำหนดในระบบ:**

| ModelType | CollectionName | SingleFile | ไฟล์ที่รองรับ | Conversions |
|---|---|---|---|---|
| `KnowledgeArticle` | `cover` | ✅ | image/jpeg, png, webp (max 5 MB) | `thumb` 400×300, `card` 800×450, `og` 1200×630 |
| `KnowledgeArticle` | `attachments` | — | PDF, image, video, docx, xlsx (max 100 MB) | ไม่มี |
| `AppUser` | `avatar` | ✅ | image/jpeg, png, webp (max 2 MB) | `thumb` 150×150 |

---

## 4. Interaction Group

### Comment

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `ArticleId` | `uuid` | NO | — | FK → KnowledgeArticles.Id |
| `AuthorId` | `uuid` | NO | — | FK → AspNetUsers.Id |
| `ParentId` | `uuid` | YES | — | FK self (reply) |
| `Content` | `text` | NO | — | ข้อความคอมเมนต์ |
| `IsApproved` | `boolean` | NO | `true` | ผ่านการอนุมัติ |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |
| `UpdatedAt` | `timestamptz` | YES | — | — |
| `DeletedAt` | `timestamptz` | YES | — | soft delete |

```sql
CREATE TABLE "Comments" (
    "Id"         uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "ArticleId"  uuid        NOT NULL REFERENCES "KnowledgeArticles"("Id") ON DELETE CASCADE,
    "AuthorId"   uuid        NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ParentId"   uuid        REFERENCES "Comments"("Id") ON DELETE CASCADE,
    "Content"    text        NOT NULL,
    "IsApproved" boolean     NOT NULL DEFAULT true,
    "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"  timestamptz,
    "DeletedAt"  timestamptz
);
```

---

### ArticleReaction

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `ArticleId` | `uuid` | NO | — | FK → KnowledgeArticles.Id |
| `UserId` | `uuid` | NO | — | FK → AspNetUsers.Id |
| `ReactionType` | `varchar(20)` | NO | — | `Like` / `Bookmark` / `Share` |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |

```sql
CREATE TABLE "ArticleReactions" (
    "Id"           uuid        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "ArticleId"    uuid        NOT NULL REFERENCES "KnowledgeArticles"("Id") ON DELETE CASCADE,
    "UserId"       uuid        NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ReactionType" varchar(20) NOT NULL,
    "CreatedAt"    timestamptz NOT NULL DEFAULT now(),

    UNIQUE ("ArticleId", "UserId", "ReactionType"),
    CONSTRAINT chk_reaction CHECK ("ReactionType" IN ('Like','Bookmark','Share'))
);
```

---

### Notification

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `UserId` | `uuid` | NO | — | FK → AspNetUsers.Id (ผู้รับ) |
| `Type` | `varchar(50)` | NO | — | `ArticlePublished` / `CommentAdded` / `ReviewRequested` / `AiComplete` |
| `Title` | `varchar(200)` | NO | — | หัวข้อการแจ้งเตือน |
| `Message` | `text` | NO | — | รายละเอียด |
| `ReferenceUrl` | `varchar(500)` | YES | — | ลิงก์ที่เกี่ยวข้อง |
| `IsRead` | `boolean` | NO | `false` | อ่านแล้ว? |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |
| `ReadAt` | `timestamptz` | YES | — | วันที่อ่าน |

```sql
CREATE TABLE "Notifications" (
    "Id"           uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"       uuid         NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "Type"         varchar(50)  NOT NULL,
    "Title"        varchar(200) NOT NULL,
    "Message"      text         NOT NULL,
    "ReferenceUrl" varchar(500),
    "IsRead"       boolean      NOT NULL DEFAULT false,
    "CreatedAt"    timestamptz  NOT NULL DEFAULT now(),
    "ReadAt"       timestamptz
);
```

---

## 5. AI & Logging Group

### AiWritingLog

> บันทึกการใช้ AI writing ทุกครั้ง — ใช้สำหรับ usage analytics และ billing

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `UserId` | `uuid` | NO | — | FK → AspNetUsers.Id |
| `ArticleId` | `uuid` | YES | — | FK → KnowledgeArticles.Id (ถ้ามี) |
| `FeatureType` | `varchar(50)` | NO | — | `GenerateDraft` / `Improve` / `Summarize` / `Translate` / `AutoTag` / `QA` |
| `ImprovementType` | `varchar(30)` | YES | — | `Grammar` / `Concise` / `Formal` / `Expand` / `Simplify` |
| `Prompt` | `text` | NO | — | prompt ที่ส่งไป |
| `Response` | `text` | YES | — | ผลลัพธ์จาก AI |
| `ModelUsed` | `varchar(100)` | NO | — | เช่น `qwen3.5:9b`, `qwen/qwen3.6-plus:free`, `mimo-v2-flash` |
| `Provider` | `varchar(50)` | NO | — | `Ollama` / `OpenRouter` / `XiaomiMiMo` / `AzureOpenAI` / `Anthropic` |
| `InputTokens` | `int` | YES | — | จำนวน token input |
| `OutputTokens` | `int` | YES | — | จำนวน token output |
| `DurationMs` | `int` | YES | — | เวลาที่ใช้ (ms) |
| `IsAccepted` | `boolean` | YES | — | ผู้ใช้ยอมรับผลลัพธ์? |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |

```sql
CREATE TABLE "AiWritingLogs" (
    "Id"               uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"           uuid         NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ArticleId"        uuid         REFERENCES "KnowledgeArticles"("Id") ON DELETE SET NULL,
    "FeatureType"      varchar(50)  NOT NULL,
    "ImprovementType"  varchar(30),
    "Prompt"           text         NOT NULL,
    "Response"         text,
    "ModelUsed"        varchar(100) NOT NULL,
    "Provider"         varchar(50)  NOT NULL,
    "InputTokens"      int,
    "OutputTokens"     int,
    "DurationMs"       int,
    "IsAccepted"       boolean,
    "CreatedAt"        timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT chk_feature CHECK ("FeatureType" IN (
        'GenerateDraft','Improve','Summarize','Translate','AutoTag','QA')),
    -- v4: คง Ollama ไว้สำหรับ historical logs — ระบบปัจจุบันใช้ OpenRouter + XiaomiMiMo
    CONSTRAINT chk_provider CHECK ("Provider" IN (
        'Ollama','OpenRouter','XiaomiMiMo','AzureOpenAI','Anthropic'))
);
```

---

### KnowledgeSearchLog

> บันทึกการค้นหา — ใช้วิเคราะห์ gap ความรู้ที่ขาดหายไป

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `UserId` | `uuid` | YES | — | FK → AspNetUsers.Id (null = anonymous) |
| `Query` | `varchar(1000)` | NO | — | คำค้นหา |
| `QueryEmbedding` | `vector(1024)` | YES | — | embedding ของคำค้นหา |
| `SearchType` | `varchar(20)` | NO | `'Keyword'` | `Keyword` / `Semantic` / `AI` |
| `ResultCount` | `int` | NO | `0` | จำนวนผลลัพธ์ที่พบ |
| `ClickedArticleId` | `uuid` | YES | — | FK → KnowledgeArticles.Id |
| `ClickedResultRank` | `int` | YES | — | ลำดับที่คลิก (1-based) |
| `HasResult` | `boolean` | NO | `true` | พบผลลัพธ์? |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |

```sql
CREATE TABLE "KnowledgeSearchLogs" (
    "Id"                uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"            uuid          REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "Query"             varchar(1000) NOT NULL,
    "QueryEmbedding"    vector(1024),
    "SearchType"        varchar(20)   NOT NULL DEFAULT 'Keyword',
    "ResultCount"       int           NOT NULL DEFAULT 0,
    "ClickedArticleId"  uuid          REFERENCES "KnowledgeArticles"("Id") ON DELETE SET NULL,
    "ClickedResultRank" int,
    "HasResult"         boolean       NOT NULL DEFAULT true,
    "CreatedAt"         timestamptz   NOT NULL DEFAULT now()
);
```

---

### AuditLog

> บันทึกทุก action สำคัญในระบบ — **Append-only อย่างเคร่งครัด**
> ป้องกันด้วย PostgreSQL Trigger + EF Core Interceptor (ดู §8)

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `UserId` | `uuid` | YES | — | FK → AspNetUsers.Id |
| `EntityName` | `varchar(100)` | NO | — | ชื่อ entity เช่น `KnowledgeArticle` |
| `EntityId` | `uuid` | YES | — | Id ของ entity |
| `Action` | `varchar(50)` | NO | — | `Create` / `Update` / `Delete` / `Publish` / `DirectPublish` / `SubmitForReview` / `Login` |
| `OldValues` | `jsonb` | YES | — | snapshot ค่าเดิม (JSONB) |
| `NewValues` | `jsonb` | YES | — | snapshot ค่าใหม่ (JSONB) |
| `IpAddress` | `varchar(45)` | YES | — | IPv4/IPv6 ของผู้กระทำ |
| `UserAgent` | `varchar(500)` | YES | — | browser/client info |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |

```sql
CREATE TABLE "AuditLogs" (
    "Id"         uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "UserId"     uuid         REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "EntityName" varchar(100) NOT NULL,
    "EntityId"   uuid,
    "Action"     varchar(50)  NOT NULL,
    "OldValues"  jsonb,
    "NewValues"  jsonb,
    "IpAddress"  varchar(45),
    "UserAgent"  varchar(500),
    "CreatedAt"  timestamptz  NOT NULL DEFAULT now()
);

-- ── Trigger: ป้องกัน UPDATE ──────────────────────────────────────
CREATE OR REPLACE FUNCTION prevent_audit_log_mutation()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION
        'AuditLogs เป็น append-only — ไม่อนุญาตให้ % (row id: %)',
        TG_OP, OLD."Id"
    USING ERRCODE = 'restrict_violation';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_audit_log_no_update
    BEFORE UPDATE ON "AuditLogs"
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_log_mutation();

CREATE TRIGGER trg_audit_log_no_delete
    BEFORE DELETE ON "AuditLogs"
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_log_mutation();
```

> **หมายเหตุ:** Trigger SQL ต้องรันแยกหลัง `dotnet ef database update` เนื่องจากไม่อยู่ใน EF Core Migration

---

## 6. System Group

### SystemSetting

> ตั้งค่าระบบแบบ key-value — แก้ไขได้โดย Admin ผ่าน UI

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `Key` | `varchar(200)` | NO | — | ชื่อ setting (unique) |
| `Value` | `text` | YES | — | ค่า setting |
| `Description` | `varchar(500)` | YES | — | คำอธิบาย |
| `Group` | `varchar(100)` | YES | — | กลุ่ม เช่น `AI`, `Email`, `Storage`, `Article` |
| `IsEncrypted` | `boolean` | NO | `false` | เข้ารหัสค่าไว้? |
| `UpdatedById` | `uuid` | YES | — | FK → AspNetUsers.Id |
| `UpdatedAt` | `timestamptz` | YES | — | — |

```sql
CREATE TABLE "SystemSettings" (
    "Id"            uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Key"           varchar(200) NOT NULL UNIQUE,
    "Value"         text,
    "Description"   varchar(500),
    "Group"         varchar(100),
    "IsEncrypted"   boolean      NOT NULL DEFAULT false,
    "UpdatedById"   uuid         REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "UpdatedAt"     timestamptz
);
```

---

## 7. Security Group

### ApiKey

> จัดการ API Key สำหรับ OpenClaw, Line OA Webhook และ Third-party integrations
> **ไม่เก็บ key จริง** — เก็บเฉพาะ hash เท่านั้น (SHA-256)

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | `uuid` | NO | `gen_random_uuid()` | PK |
| `Name` | `varchar(200)` | NO | — | ชื่อ key เช่น `"OpenClaw Production"` |
| `KeyHash` | `varchar(500)` | NO | — | SHA-256 hash ของ key จริง (unique) |
| `Prefix` | `varchar(10)` | NO | — | prefix แสดงให้ user เห็น เช่น `skc_live_` |
| `ClientType` | `varchar(50)` | NO | — | `OpenClaw` / `LineWebhook` / `ThirdParty` |
| `Permissions` | `text` | NO | — | JSON array เช่น `["linebot:chat","search:read"]` |
| `IsActive` | `boolean` | NO | `true` | เปิด/ปิด key |
| `ExpiresAt` | `timestamptz` | YES | — | `null` = ไม่หมดอายุ |
| `LastUsedAt` | `timestamptz` | YES | — | ใช้งานล่าสุดเมื่อ |
| `UsageCount` | `int` | NO | `0` | จำนวนครั้งที่ใช้ |
| `Description` | `varchar(500)` | YES | — | หมายเหตุ / วัตถุประสงค์ |
| `AllowedIps` | `text` | YES | — | JSON array of IP whitelist (null = ทุก IP) |
| `CreatedById` | `uuid` | YES | — | FK → AspNetUsers.Id |
| `CreatedAt` | `timestamptz` | NO | `now()` | — |
| `RevokedAt` | `timestamptz` | YES | — | วันที่ยกเลิก (null = ยังใช้งานได้) |
| `RevokedById` | `uuid` | YES | — | FK → AspNetUsers.Id |

```sql
CREATE TABLE "ApiKeys" (
    "Id"           uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Name"         varchar(200) NOT NULL,
    "KeyHash"      varchar(500) NOT NULL UNIQUE,
    "Prefix"       varchar(10)  NOT NULL,
    "ClientType"   varchar(50)  NOT NULL,
    "Permissions"  text         NOT NULL DEFAULT '[]',
    "IsActive"     boolean      NOT NULL DEFAULT true,
    "ExpiresAt"    timestamptz,
    "LastUsedAt"   timestamptz,
    "UsageCount"   int          NOT NULL DEFAULT 0,
    "Description"  varchar(500),
    "AllowedIps"   text,
    "CreatedById"  uuid         REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "CreatedAt"    timestamptz  NOT NULL DEFAULT now(),
    "RevokedAt"    timestamptz,
    "RevokedById"  uuid         REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,

    CONSTRAINT chk_client_type CHECK ("ClientType" IN ('OpenClaw','LineWebhook','ThirdParty'))
);
```

**Permissions ที่รองรับ:**

| Permission | ใช้สำหรับ |
|---|---|
| `linebot:chat` | เรียก `/api/linebot/chat` |
| `search:read` | ค้นหา Knowledge Base |
| `articles:read` | อ่านบทความ Public |
| `articles:write` | สร้าง/แก้ไขบทความ (Third-party) |
| `*` | ทุก permission (Admin only) |

---

## 8. Indexes & Performance

```sql
-- ── KnowledgeArticles ─────────────────────────────────────────────
CREATE INDEX idx_articles_embedding_hnsw
    ON "KnowledgeArticles"
    USING hnsw ("Embedding" vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

CREATE INDEX idx_articles_fts
    ON "KnowledgeArticles"
    USING gin(to_tsvector('simple',
        coalesce("Title",'')     || ' ' ||
        coalesce("Summary",'')   || ' ' ||
        coalesce("TitleEn",'')   || ' ' ||
        coalesce("ContentEn",'') || ' ' ||
        coalesce("KeywordsEn",'')));

CREATE INDEX idx_articles_status_visibility
    ON "KnowledgeArticles" ("Status", "Visibility")
    WHERE "DeletedAt" IS NULL;

CREATE INDEX idx_articles_author
    ON "KnowledgeArticles" ("AuthorId");

CREATE INDEX idx_articles_category
    ON "KnowledgeArticles" ("CategoryId");

CREATE INDEX idx_articles_published
    ON "KnowledgeArticles" ("PublishedAt" DESC)
    WHERE "Status" = 'Published' AND "DeletedAt" IS NULL;

-- ── MediaItems (ใหม่ใน v2) ───────────────────────────────────────
CREATE INDEX idx_media_model
    ON "MediaItems" ("ModelType", "ModelId");

CREATE INDEX idx_media_collection
    ON "MediaItems" ("ModelType", "ModelId", "CollectionName");

CREATE INDEX idx_media_order
    ON "MediaItems" ("ModelType", "ModelId", "CollectionName", "OrderColumn");

-- ── KnowledgeSearchLogs ──────────────────────────────────────────
CREATE INDEX idx_searchlogs_embedding_hnsw
    ON "KnowledgeSearchLogs"
    USING hnsw ("QueryEmbedding" vector_cosine_ops)
    WHERE "QueryEmbedding" IS NOT NULL;

CREATE INDEX idx_searchlogs_noresult
    ON "KnowledgeSearchLogs" ("CreatedAt" DESC)
    WHERE "HasResult" = false;

-- ── AiWritingLogs ────────────────────────────────────────────────
CREATE INDEX idx_ailog_user_date
    ON "AiWritingLogs" ("UserId", "CreatedAt" DESC);

CREATE INDEX idx_ailog_feature
    ON "AiWritingLogs" ("FeatureType", "CreatedAt" DESC);

-- ── AuditLogs ────────────────────────────────────────────────────
CREATE INDEX idx_audit_entity
    ON "AuditLogs" ("EntityName", "EntityId");

CREATE INDEX idx_audit_user_date
    ON "AuditLogs" ("UserId", "CreatedAt" DESC);

-- ── Notifications ────────────────────────────────────────────────
CREATE INDEX idx_notif_user_unread
    ON "Notifications" ("UserId", "CreatedAt" DESC)
    WHERE "IsRead" = false;

-- ── Comments ─────────────────────────────────────────────────────
CREATE INDEX idx_comments_article
    ON "Comments" ("ArticleId")
    WHERE "DeletedAt" IS NULL;

-- ── ApiKeys ──────────────────────────────────────────────────────
CREATE INDEX idx_apikeys_hash
    ON "ApiKeys" ("KeyHash")
    WHERE "IsActive" = true AND "RevokedAt" IS NULL;

CREATE INDEX idx_apikeys_clienttype
    ON "ApiKeys" ("ClientType")
    WHERE "IsActive" = true;

-- ── Tags ─────────────────────────────────────────────────────────
CREATE INDEX idx_tags_usage
    ON "Tags" ("UsageCount" DESC);
```

---

## 9. Relationships Summary

```
AspNetUsers ──< AppUserRole >── AspNetRoles
AspNetUsers ──< KnowledgeArticles (Author)
AspNetUsers ──< KnowledgeArticles (Reviewer / Publisher)
AspNetUsers ──< ArticleVersions (EditedBy)
AspNetUsers ──< Comments
AspNetUsers ──< ArticleReactions
AspNetUsers ──< AiWritingLogs
AspNetUsers ──< KnowledgeSearchLogs
AspNetUsers ──< AuditLogs
AspNetUsers ──< Notifications
AspNetUsers ──< ApiKeys (CreatedBy)
AspNetUsers ──< ApiKeys (RevokedBy)
AspNetUsers ──< MediaItems (UploadedBy)

Categories ──< KnowledgeArticles
Categories ──< Categories (self: Parent)

KnowledgeArticles ──< ArticleVersions
KnowledgeArticles ──< ArticleTags >── Tags
KnowledgeArticles ──< Comments
KnowledgeArticles ──< ArticleReactions
KnowledgeArticles ──< AiWritingLogs
KnowledgeArticles ──< KnowledgeSearchLogs (ClickedArticle)
KnowledgeArticles ──< MediaItems (Polymorphic: cover, attachments)

AspNetUsers ──< MediaItems (Polymorphic: avatar)

Comments ──< Comments (self: Reply)
```

**Cardinality:**

| Relationship | Type |
|---|---|
| User → Articles | One-to-Many |
| Category → Articles | One-to-Many |
| Article → Tags | Many-to-Many (via ArticleTags) |
| Article → Versions | One-to-Many |
| Article → Comments | One-to-Many |
| Comment → Comments | One-to-Many (self) |
| Category → Category | One-to-Many (self) |
| User → ApiKeys | One-to-Many (CreatedBy / RevokedBy) |
| Article → MediaItems | One-to-Many (Polymorphic) |
| User → MediaItems | One-to-Many (Polymorphic) |

---

## 10. Seed Data

```sql
-- Extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- ── Roles ─────────────────────────────────────────────────────────
INSERT INTO "AspNetRoles"
    ("Id","Name","NormalizedName","Description","Permissions","ConcurrencyStamp")
VALUES
    (gen_random_uuid(),'Admin','ADMIN',
     'ผู้ดูแลระบบ — Publish ได้โดยตรง',
     '["*"]',gen_random_uuid()),

    (gen_random_uuid(),'Faculty','FACULTY',
     'อาจารย์ — Publish ได้โดยตรง ไม่ต้องรอ Review',
     '["articles:write","articles:publish","articles:review"]',gen_random_uuid()),

    (gen_random_uuid(),'Researcher','RESEARCHER',
     'นักวิจัย — ส่ง Review ก่อน Publish',
     '["articles:write","articles:submit"]',gen_random_uuid()),

    (gen_random_uuid(),'Student','STUDENT',
     'นักศึกษา','["articles:read","comments:write"]',gen_random_uuid()),

    (gen_random_uuid(),'Guest','GUEST',
     'บุคคลทั่วไป','["articles:read:public"]',gen_random_uuid());

-- ── Default Categories ────────────────────────────────────────────
INSERT INTO "Categories"
    ("Id","Name","NameEn","Slug","Description","SortOrder","IsActive")
VALUES
    (gen_random_uuid(),'งานวิจัย','Research',
     'research','บทความและผลงานวิจัยทางวิชาการ',1,true),
    (gen_random_uuid(),'สื่อการสอน','Teaching Materials',
     'teaching','เอกสาร สื่อ และแหล่งเรียนรู้สำหรับการเรียนการสอน',2,true),
    (gen_random_uuid(),'นโยบายและระเบียบ','Policy',
     'policy','ระเบียบ ข้อบังคับ และนโยบายขององค์กร',3,true),
    (gen_random_uuid(),'ความรู้ทั่วไป','General Knowledge',
     'general','ความรู้ทั่วไปและข่าวสารที่เป็นประโยชน์',4,true),
    (gen_random_uuid(),'คู่มือและแนวปฏิบัติ','Manuals',
     'manuals','คู่มือการใช้งานและแนวปฏิบัติต่าง ๆ',5,true);

-- ── System Settings ───────────────────────────────────────────────
INSERT INTO "SystemSettings" ("Id","Key","Value","Description","Group")
VALUES
    -- AI: Chat Providers (v4 — no Ollama, Cloud-only Fallback Chain)
    (gen_random_uuid(),'AI:Chat:Provider:1:Name','OpenRouter',
     'Chat Provider ลำดับ 1 (cloud primary)','AI'),

    (gen_random_uuid(),'AI:Chat:Provider:1:Model','qwen/qwen3.6-plus:free',
     'OpenRouter Chat Model — ตรวจสอบ https://openrouter.ai/models','AI'),

    (gen_random_uuid(),'AI:Chat:Provider:1:Endpoint','https://openrouter.ai/api/v1',
     'OpenRouter API Endpoint','AI'),

    (gen_random_uuid(),'AI:Chat:Provider:2:Name','XiaomiMiMo',
     'Chat Provider ลำดับ 2 (last resort)','AI'),

    (gen_random_uuid(),'AI:Chat:Provider:2:Model','mimo-v2-flash',
     'XiaomiMiMo Chat Model','AI'),

    (gen_random_uuid(),'AI:Chat:Provider:2:Endpoint','https://api.xiaomimimo.com/v1',
     'XiaomiMiMo API Endpoint','AI'),

    -- AI: Embedding Providers (v4 — OpenRouter only + Queue on fail)
    (gen_random_uuid(),'AI:Embedding:Provider:1:Name','OpenRouter',
     'Embedding Provider ลำดับ 1','AI'),

    (gen_random_uuid(),'AI:Embedding:Provider:1:Model','qwen/qwen3-embedding',
     'OpenRouter Embedding Model (qwen3 family — 1024 dim MRL)','AI'),

    (gen_random_uuid(),'AI:Embedding:Dimensions','1024',
     'Vector dimensions — qwen3-embedding MRL output size','AI'),

    -- Storage (v2: MediaLibrary)
    (gen_random_uuid(),'Storage:Provider','Local',
     'Storage backend: Local | MinIO | S3','Storage'),

    (gen_random_uuid(),'Storage:MaxFileSizeMB','100',
     'ขนาดไฟล์สูงสุด (MB) — default collection','Storage'),

    (gen_random_uuid(),'Storage:AllowedTypes',
     'application/pdf,image/jpeg,image/png,image/webp,video/mp4,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
     'MIME types ที่อนุญาต','Storage'),

    -- Article Workflow (v4: Publish-First + ContentEn)
    (gen_random_uuid(),'Article:RequireReview','false',
     'ยกเลิกแล้ว — ใช้ Role-based workflow แทน (ดู DirectPublishRoles)','Article'),

    (gen_random_uuid(),'Article:DirectPublishRoles','["Admin","Faculty"]',
     'Roles ที่ Publish ได้โดยตรง ไม่ต้องรอ Review','Article'),

    (gen_random_uuid(),'Article:PostAuditNotifyRole','Admin',
     'Role ที่รับการแจ้งเตือน Post-audit หลัง Direct Publish','Article'),

    (gen_random_uuid(),'Article:AutoEmbedOnPublish','true',
     'สร้าง embedding อัตโนมัติเมื่อ Publish','Article'),

    (gen_random_uuid(),'Article:AutoTranslateFields','TitleEn,SummaryEn,ContentEn',
     'Fields ที่ AI แปลอัตโนมัติเมื่อกด Translate — ทั้งหมดเป็น optional (Thai-First)','Article');
```

---

## 11. EF Core DbContext

```csharp
// Infrastructure/Data/SkcKmsDbContext.cs
public class SkcKmsDbContext(DbContextOptions<SkcKmsDbContext> options)
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
            typeof(SkcKmsDbContext).Assembly);
    }
}
```

---

## 12. Entity Configurations

```csharp
// Infrastructure/Data/Configurations/ArticleConfiguration.cs
public class ArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> b)
    {
        b.ToTable("KnowledgeArticles");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(500).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(500);
        b.Property(x => x.Slug).HasMaxLength(600).IsRequired();
        b.Property(x => x.Content).HasColumnType("text").IsRequired();
        b.Property(x => x.ContentEn).HasColumnType("text");  // nullable — EN optional
        b.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
        b.Property(x => x.SummaryEn).HasMaxLength(2000);
        b.Property(x => x.KeywordsEn).HasMaxLength(500);
        // CoverImageUrl ถูกลบออกแล้ว (v2)
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsAutoTranslated).HasDefaultValue(false);
        b.Property(x => x.ViewCount).HasDefaultValue(0);
        b.Property(x => x.LikeCount).HasDefaultValue(0);

        // pgvector — dim 1024 (qwen3-embedding MRL)
        b.Property(x => x.Embedding).HasColumnType("vector(1024)");
        b.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // Full-text search
        b.HasIndex(x => new { x.Title, x.Summary })
            .HasDatabaseName("idx_articles_fts");

        // Unique slug
        b.HasIndex(x => x.Slug).IsUnique();

        // Soft delete filter
        b.HasQueryFilter(x => x.DeletedAt == null);

        // Relationships
        b.HasOne(x => x.Category)
            .WithMany(c => c.Articles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Tags)
            .WithMany(t => t.Articles)
            .UsingEntity<ArticleTag>();
    }
}

// Infrastructure/Data/Configurations/MediaItemConfiguration.cs (ใหม่ใน v2)
public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> b)
    {
        b.ToTable("MediaItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.ModelType).HasMaxLength(100).IsRequired();
        b.Property(x => x.CollectionName).HasMaxLength(100).IsRequired()
            .HasDefaultValue("default");
        b.Property(x => x.Name).HasMaxLength(255).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Extension).HasMaxLength(20).IsRequired();
        b.Property(x => x.Disk).HasMaxLength(50).IsRequired()
            .HasDefaultValue("local");
        b.Property(x => x.ConversionsDisk).HasMaxLength(50);
        b.Property(x => x.Path).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Manipulations).HasColumnType("jsonb")
            .HasDefaultValue("{}");
        b.Property(x => x.CustomProperties).HasColumnType("jsonb")
            .HasDefaultValue("{}");
        b.Property(x => x.GeneratedConversions).HasColumnType("jsonb")
            .HasDefaultValue("{}");
        b.Property(x => x.ResponsiveImages).HasColumnType("jsonb")
            .HasDefaultValue("{}");

        b.HasIndex(x => new { x.ModelType, x.ModelId });
        b.HasIndex(x => new { x.ModelType, x.ModelId, x.CollectionName });

        b.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Infrastructure/Data/Configurations/ApiKeyConfiguration.cs
public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("ApiKeys");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.KeyHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(10).IsRequired();
        b.Property(x => x.ClientType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Permissions).HasColumnType("text").IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.AllowedIps).HasColumnType("text");
        b.Property(x => x.UsageCount).HasDefaultValue(0);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => x.KeyHash).IsUnique();
        b.HasIndex(x => new { x.KeyHash, x.IsActive })
            .HasDatabaseName("idx_apikeys_hash");

        b.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.RevokedBy)
            .WithMany()
            .HasForeignKey(x => x.RevokedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## ตารางสรุปทั้งหมด (Table Summary)

| # | Table | Group | หน้าที่ | v2 |
|---|-------|-------|---------|-----|
| 1 | `AspNetUsers` | Identity | บัญชีผู้ใช้งาน | ลบ `AvatarUrl` |
| 2 | `AspNetRoles` | Identity | บทบาท/สิทธิ์ | — |
| 3 | `AspNetUserRoles` | Identity | เชื่อม User ↔ Role | — |
| 4 | `AspNetUserClaims` | Identity | Claims ของ User | — |
| 5 | `AspNetRoleClaims` | Identity | Claims ของ Role | — |
| 6 | `AspNetUserLogins` | Identity | External Login | — |
| 7 | `AspNetUserTokens` | Identity | Tokens | — |
| 8 | `Categories` | Knowledge | หมวดหมู่ (tree structure) | — |
| 9 | `KnowledgeArticles` | Knowledge | บทความ/งานวิจัย (ตารางหลัก) | ลบ `CoverImageUrl`, เพิ่ม `ContentEn` |
| 10 | `ArticleVersions` | Knowledge | ประวัติ version ของบทความ | — |
| 11 | `Tags` | Knowledge | แท็กบทความ | — |
| 12 | `ArticleTags` | Knowledge | เชื่อม Article ↔ Tag | — |
| 13 | `MediaItems` | **Media** *(ใหม่)* | จัดการไฟล์ทุกประเภท (แทน Attachments) | ✅ ใหม่ |
| 14 | `Comments` | Interaction | ความคิดเห็น (threaded) | — |
| 15 | `ArticleReactions` | Interaction | Like / Bookmark / Share | — |
| 16 | `Notifications` | Interaction | การแจ้งเตือน | — |
| 17 | `AiWritingLogs` | AI & Logging | Log การใช้ AI | เพิ่ม OpenRouter provider |
| 18 | `KnowledgeSearchLogs` | AI & Logging | Log การค้นหา | — |
| 19 | `AuditLogs` | AI & Logging | บันทึกทุก action (append-only) | DB Trigger ป้องกัน mutate |
| 20 | `SystemSettings` | System | ตั้งค่าระบบ key-value | อัปเดต AI/Workflow settings |
| 21 | `ApiKeys` | Security | จัดการ API Key สำหรับ OpenClaw / Third-party | — |

> **ตาราง `Attachments` ถูกลบออกใน v2** — แทนด้วย `MediaItems`

---

*KMS — Knowledge Management System*
*Stack: .NET 10 · EF Core 10 · PostgreSQL 16 · pgvector · ASP.NET Core Identity*
*Version: Schema v4 — MediaLibrary · AI Fallback Chain (no Ollama) · ContentEn · Publish-First · Immutable AuditLog*
