# KMS — .NET 10 + Swagger + JWT Bearer Setup

> **Stack:** .NET 10 · ASP.NET Core Identity · JWT Bearer · Swashbuckle · Minimal API  
> **ไม่ใช้:** Duende IdentityServer (ระบบออก token เองผ่าน `/api/auth/login`)

---

## สารบัญ

1. [NuGet Packages](#1-nuget-packages)
2. [appsettings.json](#2-appsettingsjson)
3. [Program.cs — JWT Authentication](#3-programcs--jwt-authentication)
4. [Program.cs — Swagger + Security](#4-programcs--swagger--security)
5. [SecurityRequirementsOperationFilter](#5-securityrequirementsoperationfilter)
6. [Authorization Policies](#6-authorization-policies)
7. [Minimal API Endpoints — Auth Pattern](#7-minimal-api-endpoints--auth-pattern)
8. [AuthEndpoints.cs — Login / Register / Refresh](#8-authendpointscs--login--register--refresh)
9. [swagger-auto-auth.js — Auto-Authorize](#9-swagger-auto-authjs--auto-authorize)
10. [Swagger UI Flow (วิธีใช้งาน)](#10-swagger-ui-flow-วิธีใช้งาน)
11. [Lock Icon Reference](#11-lock-icon-reference)
12. [ตรวจสอบ JWT Token](#12-ตรวจสอบ-jwt-token)

---

## 1. NuGet Packages

```bash
# ── KMS.Api ─────────────────────────────────────────
dotnet add src/KMS.Api package Swashbuckle.AspNetCore
dotnet add src/KMS.Api package Microsoft.AspNetCore.Authentication.JwtBearer

# ── KMS.Infrastructure ──────────────────────────────
dotnet add src/KMS.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

**ตรวจสอบ version ใน `.csproj`:**

```xml
<!-- src/KMS.Api/KMS.Api.csproj -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*" />
```

---

## 2. appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SkcKmsDb;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-characters-long!!",
    "Issuer": "SkcKmsApi",
    "Audience": "SkcKmsClient",
        "ExpiryInMinutes": 60
  },
  "Swagger": {
    "Enabled": true,
    "Title": "KMS API",
    "Version": "v1",
    "Description": "Knowledge Management System — RMUTI Sakon Nakhon Campus"
  }
}
```

> **⚠️ Production:** ย้าย `Jwt:Key` ไปที่ Environment Variable หรือ Azure Key Vault  
> ห้าม commit key จริงใน git เด็ดขาด

---

## 3. Program.cs — JWT Authentication

```csharp
// ── JWT Authentication ──────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer      = jwtSettings["Issuer"],
            ValidAudience    = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),

            // ✅ ลด clock skew เป็น 0 — token หมดอายุตรงเวลา
            ClockSkew = TimeSpan.Zero
        };

        // ✅ SSE / streaming endpoints — อ่าน token จาก query string ได้
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // รองรับ /api/ai/stream?access_token=...
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/ai"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
```

---

## 4. Program.cs — Swagger + Security

```csharp
// ── Swagger + JWT Security Definition ──────────────────────────────
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    var swaggerSettings = builder.Configuration.GetSection("Swagger");

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = swaggerSettings["Title"] ?? "KMS API",
        Version     = swaggerSettings["Version"] ?? "v1",
        Description = swaggerSettings["Description"],
        Contact     = new OpenApiContact
        {
            Name  = "RMUTI Sakon Nakhon",
            Email = "admin@skc.rmuti.ac.th"
        }
    });

    // 🔒 กำหนด Bearer Token Security Scheme
    // ใช้ Http type — ไม่ใช่ OAuth2 เพราะเราออก token เอง
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = """
            JWT Bearer token — ได้มาจาก POST /api/auth/login
            
                ขั้นตอน:
                1. เรียก POST /api/Auth/login (ไม่ต้อง auth)
                2. เมื่อ login สำเร็จ ระบบจะ authorize ให้อัตโนมัติ
                3. ทุก request จะแนบ Authorization: Bearer <token> อัตโนมัติ
            """
    });

    // 🔒 แสดง lock icon เฉพาะ endpoint ที่ต้องการ auth จริงๆ
    c.OperationFilter<SecurityRequirementsOperationFilter>();

    // ✅ เรียงลำดับ endpoints ตาม tag
    c.TagActionsBy(api => [api.GroupName ?? api.ActionDescriptor.DisplayName ?? "Default"]);
    c.DocInclusionPredicate((_, _) => true);
});

// ── Middleware Pipeline ─────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KMS API v1");
        c.RoutePrefix      = "swagger";           // เข้าที่ /swagger
        c.DocumentTitle    = "KMS API";
        c.DisplayRequestDuration();               // แสดงเวลา response
        c.EnableFilter();                          // ค้นหา endpoint ได้
        c.EnableDeepLinking();                     // share link ไปยัง endpoint เฉพาะได้
        c.DefaultModelsExpandDepth(-1);            // ซ่อน Schemas section (ไม่รก)
        c.InjectJavascript("/swagger-auto-auth.js?v=20260408-5"); // auto-authorize + login panel
    });
}

app.UseStaticFiles(); // ← ต้องมาก่อน UseAuthentication เพื่อเสิร์ฟ swagger-auto-auth.js
app.UseAuthentication();   // ← ต้องมาก่อน Authorization เสมอ
app.UseAuthorization();
```

---

## 5. SecurityRequirementsOperationFilter

สร้างไฟล์ใหม่ใน `src/KMS.Api/Filters/`

```csharp
// src/KMS.Api/Filters/SecurityRequirementsOperationFilter.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KMS.Api.Filters;

/// <summary>
/// ควบคุม lock icon ใน Swagger UI
/// - endpoint ที่ RequireAuthorization() → 🔒 lock
/// - endpoint ที่ AllowAnonymous() → 🔓 ไม่มี lock
/// - endpoint ที่ไม่ระบุ → 🔓 ไม่มี lock (safe default)
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // ดึง metadata จาก endpoint
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // ตรวจ AllowAnonymous ก่อน — ถ้ามีให้ข้ามเลย
        var isAnonymous = endpointMetadata.OfType<AllowAnonymousAttribute>().Any()
                       || endpointMetadata.OfType<IAllowAnonymous>().Any();
        if (isAnonymous)
        {
            operation.Security?.Clear();
            return;
        }

        // ตรวจว่าต้องการ auth หรือเปล่า
        var requiresAuth = endpointMetadata.OfType<AuthorizeAttribute>().Any()
                        || endpointMetadata.OfType<IAuthorizeData>().Any();
        if (!requiresAuth)
        {
            operation.Security?.Clear();
            return;
        }

        // 🔒 endpoint นี้ต้อง auth — แสดง lock icon
        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        ];

        // เพิ่ม 401 / 403 response อัตโนมัติ
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized — token หมดอายุหรือไม่ถูกต้อง" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden — ไม่มีสิทธิ์เพียงพอ" });
    }
}
```

---

## 6. Authorization Policies

```csharp
// Program.cs — กำหนด policy ตาม roles ของ KMS
builder.Services.AddAuthorization(options =>
{
    // เขียนและส่งบทความ — Faculty, Researcher, Admin
    options.AddPolicy("CanWrite", policy =>
        policy.RequireRole("Faculty", "Researcher", "Admin"));

    // เผยแพร่โดยตรง — Faculty, Admin (ข้าม Review)
    options.AddPolicy("CanPublish", policy =>
        policy.RequireRole("Faculty", "Admin"));

    // อนุมัติ / Review บทความ — Faculty, Admin
    options.AddPolicy("CanReview", policy =>
        policy.RequireRole("Faculty", "Admin"));

    // Admin เท่านั้น
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // อ่านได้ทุกคนที่ login — Student, Faculty, Researcher, Admin
    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});
```

---

## 7. Minimal API Endpoints — Auth Pattern

```csharp
// ตัวอย่าง ArticleEndpoints.cs
// สังเกต: WithGroupName กำหนด tag ใน Swagger

public static class ArticleEndpoints
{
    public static RouteGroupBuilder MapArticleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/articles")
                       .WithGroupName("Articles")   // tag ใน Swagger
                       .WithTags("Articles");

        // 🔓 สาธารณะ — ไม่ต้อง token
        group.MapGet("/", GetAllPublished)
             .AllowAnonymous()
             .WithSummary("ดูบทความที่เผยแพร่แล้ว (สาธารณะ)")
             .Produces<List<ArticleDto>>();

        // 🔒 ต้อง login
        group.MapPost("/", CreateArticle)
             .RequireAuthorization("CanWrite")
             .WithSummary("สร้างบทความใหม่ (Faculty, Researcher, Admin)")
             .Produces<ArticleDto>(201)
             .ProducesProblem(400)
             .ProducesProblem(401);

        // 🔒 ดูรายละเอียด — login แล้วเท่านั้น (draft ไม่สาธารณะ)
        group.MapGet("/{id:guid}", GetById)
             .RequireAuthorization()
             .WithSummary("ดูบทความตาม ID")
             .Produces<ArticleDto>()
             .ProducesProblem(404);

        // 🔒 เผยแพร่ — Faculty, Admin เท่านั้น
        group.MapPost("/{id:guid}/publish", PublishArticle)
             .RequireAuthorization("CanPublish")
             .WithSummary("เผยแพร่บทความ (Faculty, Admin)")
             .Produces(200)
             .ProducesProblem(403);

        // 🔒 ลบ — Admin เท่านั้น
        group.MapDelete("/{id:guid}", DeleteArticle)
             .RequireAuthorization("AdminOnly")
             .WithSummary("ลบบทความ (Admin)")
             .Produces(204)
             .ProducesProblem(403);

        return group;
    }
}
```

---

## 8. AuthEndpoints.cs — Login / Register / Refresh

```csharp
// src/KMS.Api/Endpoints/AuthEndpoints.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace KMS.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
                       .WithGroupName("Auth")
                       .WithTags("Auth")
                       .AllowAnonymous();   // ← group ทั้งหมดเป็น anonymous

        // ── POST /api/auth/login ──────────────────────────────────
        // 🔓 ไม่ต้อง token — เป็นจุดเริ่มต้นของ auth flow
        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            // ตรวจสอบ user
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Problem("อีเมลหรือรหัสผ่านไม่ถูกต้อง", statusCode: 401);

            if (!user.IsActive)
                return Results.Problem("บัญชีนี้ถูกระงับการใช้งาน", statusCode: 403);

            // ดึง roles
            var roles = await userManager.GetRolesAsync(user);

            // สร้าง JWT
            var (accessToken, refreshToken) = GenerateTokens(user, roles, config);

            // บันทึก refresh token (ควรเก็บใน DB จริง)
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return Results.Ok(new LoginResponse(
                AccessToken:  accessToken,
                RefreshToken: refreshToken,
                ExpiresIn:    int.Parse(config["Jwt:AccessTokenExpiryMinutes"]!) * 60,
                User: new UserInfo(
                    Id:         user.Id,
                    Email:      user.Email!,
                    FullNameTh: user.FullNameTh,
                    Roles:      roles.ToList()
                )
            ));
        })
        .WithSummary("เข้าสู่ระบบ — รับ JWT access token")
        .Produces<LoginResponse>()
        .ProducesProblem(401);

        // ── POST /api/auth/register ───────────────────────────────
        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager) =>
        {
            var user = new AppUser
            {
                UserName    = req.Email,
                Email       = req.Email,
                FullNameTh  = req.FullNameTh,
                Faculty     = req.Faculty,
                Department  = req.Department,
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.ValidationProblem(
                    result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

            // กำหนด role เริ่มต้น = Student
            await userManager.AddToRoleAsync(user, "Student");

            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email });
        })
        .WithSummary("ลงทะเบียนผู้ใช้ใหม่ (role: Student)")
        .Produces(201)
        .ProducesProblem(400);

        // ── POST /api/auth/refresh ────────────────────────────────
        group.MapPost("/refresh", async (
            RefreshRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            // TODO: ตรวจสอบ refresh token จาก DB
            // ตัวอย่างนี้ simplified — production ต้องเก็บ refresh token ใน DB
            var principal = GetPrincipalFromExpiredToken(req.AccessToken, config);
            if (principal is null)
                return Results.Problem("Token ไม่ถูกต้อง", statusCode: 401);

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user   = await userManager.FindByIdAsync(userId!);
            if (user is null)
                return Results.Problem("ไม่พบผู้ใช้", statusCode: 401);

            var roles = await userManager.GetRolesAsync(user);
            var (newAccessToken, newRefreshToken) = GenerateTokens(user, roles, config);

            return Results.Ok(new LoginResponse(
                AccessToken:  newAccessToken,
                RefreshToken: newRefreshToken,
                ExpiresIn:    int.Parse(config["Jwt:AccessTokenExpiryMinutes"]!) * 60,
                User: new UserInfo(user.Id, user.Email!, user.FullNameTh, roles.ToList())
            ));
        })
        .WithSummary("ต่ออายุ token ด้วย refresh token")
        .Produces<LoginResponse>()
        .ProducesProblem(401);

        return group;
    }

    // ── Helper: สร้าง JWT + Refresh Token ────────────────────────────
    private static (string AccessToken, string RefreshToken) GenerateTokens(
        AppUser user, IList<string> roles, IConfiguration config)
    {
        var jwtSettings = config.GetSection("Jwt");
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds       = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry      = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpiryMinutes"]!));

        // Claims — ข้อมูลที่ฝังใน token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("fullNameTh",                  user.FullNameTh),
        };

        // เพิ่ม roles ทั้งหมดเป็น claims
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer:             jwtSettings["Issuer"],
            audience:           jwtSettings["Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds
        );

        var accessToken  = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return (accessToken, refreshToken);
    }

    // ── Helper: ตรวจสอบ expired token ────────────────────────────────
    private static ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, IConfiguration config)
    {
        var jwtSettings = config.GetSection("Jwt");
        var validation  = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                         Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ValidateIssuer   = true,
            ValidIssuer      = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience    = jwtSettings["Audience"],
            ValidateLifetime = false   // ← ยอมรับ expired token ตอน refresh
        };

        try
        {
            return new JwtSecurityTokenHandler()
                .ValidateToken(token, validation, out _);
        }
        catch
        {
            return null;
        }
    }
}

// ── Request / Response Records ────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FullNameTh,
                               string? Faculty, string? Department);
public record RefreshRequest(string AccessToken, string RefreshToken);
public record LoginResponse(string AccessToken, string RefreshToken,
                             int ExpiresIn, UserInfo User);
public record UserInfo(Guid Id, string Email, string FullNameTh, List<string> Roles);
```

---

## 9. swagger-auto-auth.js — Auto-Authorize

ไฟล์ JavaScript ที่ inject เข้า Swagger UI เพื่อเพิ่ม:
- **Login panel** ในหน้าต่าง Authorize modal (ช่อง Username/Password)
- **Auto-authorize** Bearer token หลัง login สำเร็จ (ไม่ต้องวาง token เอง)
- **Lock icons** ที่ถูกต้อง — 🔒 ก่อน login, 🔓 หลัง login
- **Token persistence** — token ยังอยู่ใน localStorage เมื่อ refresh หน้า
- **Logout** — ล้าง token ออกจาก localStorage และ Swagger UI state

### ที่ตั้งไฟล์

```
src/KMS.Api/
└── wwwroot/
    └── swagger-auto-auth.js    ← สร้างไฟล์นี้
```

### Program.cs — สิ่งที่ต้องเพิ่ม

```csharp
// 1. เสิร์ฟ static files (ก่อน UseAuthentication)
app.UseStaticFiles();

// 2. Inject ใน UseSwaggerUI
app.UseSwaggerUI(c =>
{
    // ...
    // เพิ่ม cache-buster ?v=... ทุกครั้งที่แก้ไขไฟล์ เพื่อบังคับ browser reload
    c.InjectJavascript("/swagger-auto-auth.js?v=20260408-5");
});
```

> **หมายเหตุ:** เมื่อแก้ไข `swagger-auto-auth.js` ให้ bump `?v=` ด้วยทุกครั้ง เช่น `?v=20260408-6` เพื่อบังคับ browser โหลดเวอร์ชันใหม่

### ไฟล์เต็ม: swagger-auto-auth.js

```javascript
(function () {
  const STORAGE_KEY = "kms.swagger.jwt";
  const MODAL_CONTAINER_SELECTOR = ".auth-container";
  const LOGIN_PANEL_ID = "kms-swagger-login-panel";
  const ACTIONS_ROW_ID = "kms-swagger-actions-row";
  const LOGIN_BUTTON_ID = "kms-login-button";
  const LOGOUT_BUTTON_ID = "kms-logout-button";
  const CLOSE_BUTTON_ID = "kms-close-button";

  function getSwaggerUi() {
    return window.ui || null;
  }

  function applyToken(token) {
    if (!token) return;
    const ui = getSwaggerUi();
    if (!ui) return;
    try {
      if (ui.authActions && typeof ui.authActions.authorize === "function") {
        ui.authActions.authorize({ Bearer: { name: "Bearer", value: token } });
      }
      if (typeof ui.preauthorizeApiKey === "function") {
        ui.preauthorizeApiKey("Bearer", token);
      }
      localStorage.setItem(STORAGE_KEY, token);
    } catch (error) {
      console.warn("Swagger auto-authorize failed:", error);
    }
  }

  function extractToken(payload) {
    if (!payload || typeof payload !== "object") return null;
    if (typeof payload.token === "string" && payload.token.length > 0) return payload.token;
    if (typeof payload.accessToken === "string" && payload.accessToken.length > 0) return payload.accessToken;
    return null;
  }

  function isLoginRequest(url, method) {
    if (!url || !method) return false;
    return String(method).toUpperCase() === "POST" &&
           String(url).toLowerCase().includes("/api/auth/login");
  }

  function interceptFetch() {
    const originalFetch = window.fetch;
    if (typeof originalFetch !== "function") return;
    window.fetch = async function (input, init) {
      const response = await originalFetch(input, init);
      try {
        const reqMethod = (init && init.method) || "GET";
        const reqUrl = typeof input === "string" ? input : (input && input.url) || "";
        if (response.ok && isLoginRequest(reqUrl, reqMethod)) {
          const data = await response.clone().json();
          const token = extractToken(data);
          applyToken(token);
        }
      } catch (error) {
        console.warn("Swagger login interceptor error:", error);
      }
      return response;
    };
  }

  function applyStoredToken() {
    const token = localStorage.getItem(STORAGE_KEY);
    if (token) applyToken(token);
  }

  function clearAuthorizationState() {
    const ui = getSwaggerUi();
    if (ui && ui.authActions && typeof ui.authActions.logout === "function") {
      try { ui.authActions.logout(["Bearer"]); } catch (_) {}
    }
    try {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem("authorized");
      localStorage.removeItem("swagger_authorization");
      localStorage.removeItem("swagger-ui.auth");
    } catch (_) {}
  }

  async function loginWithCredentials(username, password, statusEl) {
    try {
      const response = await fetch("/api/Auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password })
      });
      let payload = null;
      try { payload = await response.json(); } catch (_) {}
      if (!response.ok) {
        statusEl.textContent = "Login failed: username or password is incorrect.";
        statusEl.style.color = "#b91c1c";
        return;
      }
      const token = extractToken(payload);
      if (!token) {
        statusEl.textContent = "Login succeeded but token was not found in response.";
        statusEl.style.color = "#b91c1c";
        return;
      }
      applyToken(token);
      statusEl.textContent = "Login success: token applied to Bearer authorization.";
      statusEl.style.color = "#166534";
    } catch (error) {
      console.warn("Swagger credential login failed:", error);
      statusEl.textContent = "Login request failed. Check API availability.";
      statusEl.style.color = "#b91c1c";
    }
  }

  function isAuthorized() {
    const ui = getSwaggerUi();
    if (!ui || !ui.authSelectors || typeof ui.authSelectors.authorized !== "function") return false;
    try {
      const state = ui.authSelectors.authorized();
      const authObject = state && typeof state.toJS === "function" ? state.toJS() : state;
      return !!(authObject && authObject.Bearer);
    } catch (_) { return false; }
  }

  function syncOperationLockIcons() {
    const authorized = isAuthorized();
    // ครอบทั้งปุ่ม Authorize header (.btn.authorize) และปุ่มระดับ endpoint (button.authorization__btn)
    const authButtons = document.querySelectorAll("button.authorization__btn, .btn.authorize");
    authButtons.forEach(function (btn) {
      const useEl = btn.querySelector("use");
      if (authorized) {
        btn.classList.remove("locked");
        btn.classList.add("unlocked");
        btn.setAttribute("aria-label", "authorization button unlocked");
        if (useEl) {
          useEl.setAttribute("href", "#unlocked");
          useEl.setAttribute("xlink:href", "#unlocked");
        }
      } else {
        btn.classList.remove("unlocked");
        btn.classList.add("locked");
        btn.setAttribute("aria-label", "authorization button locked");
        if (useEl) {
          useEl.setAttribute("href", "#locked");
          useEl.setAttribute("xlink:href", "#locked");
        }
      }
    });
  }

  function updateModalButtonsVisibility() {
    const authContainer = document.querySelector(MODAL_CONTAINER_SELECTOR);
    if (!authContainer) return;
    const actions = authContainer.querySelector(".auth-btn-wrapper");
    if (!actions) return;
    actions.style.display = "none"; // ซ่อนปุ่ม default ของ Swagger เสมอ
    const loginButton = document.getElementById(LOGIN_BUTTON_ID);
    const actionsRow = document.getElementById(ACTIONS_ROW_ID);
    const authorized = isAuthorized();
    if (loginButton) loginButton.style.display = authorized ? "none" : "inline-block";
    if (actionsRow) actionsRow.style.display = authorized ? "flex" : "none";
  }

  function createLoginPanel() {
    const panel = document.createElement("div");
    panel.id = LOGIN_PANEL_ID;
    panel.style.cssText = "border-top:1px solid #e5e7eb;margin-top:12px;padding-top:12px;";

    const title = document.createElement("h4");
    title.textContent = "Login with Username/Password";
    title.style.cssText = "margin:0 0 8px;font-size:14px;";

    const usernameInput = document.createElement("input");
    usernameInput.type = "text";
    usernameInput.placeholder = "Username";
    usernameInput.style.cssText = "flex:1 1 220px;padding:6px 8px;box-sizing:border-box;";

    const passwordInput = document.createElement("input");
    passwordInput.type = "password";
    passwordInput.placeholder = "Password";
    passwordInput.style.cssText = "flex:1 1 220px;padding:6px 8px;box-sizing:border-box;";

    const loginBtn = document.createElement("button");
    loginBtn.id = LOGIN_BUTTON_ID;
    loginBtn.type = "button";
    loginBtn.textContent = "Login and Authorize";
    loginBtn.style.cssText = "padding:7px 12px;cursor:pointer;white-space:nowrap;";

    const status = document.createElement("div");
    status.style.cssText = "margin-top:8px;font-size:12px;";

    const actionsRow = document.createElement("div");
    actionsRow.id = ACTIONS_ROW_ID;
    actionsRow.style.cssText = "display:none;gap:8px;align-items:center;margin-top:8px;";

    const logoutBtn = document.createElement("button");
    logoutBtn.id = LOGOUT_BUTTON_ID;
    logoutBtn.type = "button";
    logoutBtn.textContent = "Logout";
    logoutBtn.style.cssText = "padding:7px 12px;cursor:pointer;";

    const closeBtn = document.createElement("button");
    closeBtn.id = CLOSE_BUTTON_ID;
    closeBtn.type = "button";
    closeBtn.textContent = "Close";
    closeBtn.style.cssText = "padding:7px 12px;cursor:pointer;";

    loginBtn.addEventListener("click", async function () {
      const username = usernameInput.value.trim();
      const password = passwordInput.value;
      if (!username || !password) {
        status.textContent = "Please enter both username and password.";
        status.style.color = "#b91c1c";
        return;
      }
      loginBtn.disabled = true;
      status.textContent = "Logging in...";
      status.style.color = "#374151";
      await loginWithCredentials(username, password, status);
      updateModalButtonsVisibility();
      loginBtn.disabled = false;
    });

    logoutBtn.addEventListener("click", function () {
      clearAuthorizationState();
      status.textContent = "Logged out. Bearer authorization cleared.";
      status.style.color = "#374151";
      updateModalButtonsVisibility();
      syncOperationLockIcons();
    });

    closeBtn.addEventListener("click", function () {
      const nativeClose = document.querySelector(".auth-container .modal-ux-header button");
      if (nativeClose) nativeClose.click();
    });

    const row = document.createElement("div");
    row.style.cssText = "display:flex;flex-wrap:wrap;gap:8px;align-items:center;";
    row.appendChild(usernameInput);
    row.appendChild(passwordInput);
    row.appendChild(loginBtn);

    actionsRow.appendChild(logoutBtn);
    actionsRow.appendChild(closeBtn);

    panel.appendChild(title);
    panel.appendChild(row);
    panel.appendChild(actionsRow);
    panel.appendChild(status);
    return panel;
  }

  function ensureLoginPanel() {
    const authContainer = document.querySelector(MODAL_CONTAINER_SELECTOR);
    if (!authContainer) return;
    if (document.getElementById(LOGIN_PANEL_ID)) {
      updateModalButtonsVisibility();
      return;
    }
    authContainer.appendChild(createLoginPanel());
    updateModalButtonsVisibility();
  }

  function boot() {
    interceptFetch();
    const timer = setInterval(function () {
      if (getSwaggerUi()) {
        clearInterval(timer);
        applyStoredToken();          // restore token จาก localStorage (ถ้ามี)
        updateModalButtonsVisibility();
        syncOperationLockIcons();
      }
    }, 200);

    function initLoginPanelWatcher() {
      if (!document.body) return;
      const observer = new MutationObserver(function () {
        ensureLoginPanel();
      });
      observer.observe(document.body, { childList: true, subtree: true });
      setInterval(function () {
        ensureLoginPanel();
        updateModalButtonsVisibility();
        syncOperationLockIcons();
      }, 300);
      syncOperationLockIcons();
    }

    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", initLoginPanelWatcher, { once: true });
    } else {
      initLoginPanelWatcher();
    }
  }

  boot();
})();
```

### พฤติกรรมหลังติดตั้ง

| สถานการณ์ | ผล |
|-----------|-----|
| เปิด Swagger ครั้งแรก | 🔒 locked ทุก endpoint, ปุ่ม "Login and Authorize" แสดง |
| Refresh หน้า (F5) | token คืนจาก localStorage → 🔓 unlocked อัตโนมัติ |
| กด Logout | ลบ token จาก localStorage + Swagger UI state → 🔒 locked |
| Login สำเร็จ | Bearer token ถูก set → 🔓 unlocked ทันที |

---

## 10. Swagger UI Flow (วิธีใช้งาน)

```
เปิด http://127.0.0.1:5000/swagger
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  ก่อน login — icon ทุกตัวแสดง 🔒 (closed padlock)      │
│                                                         │
│  [🔒 Authorize]  ← ปุ่ม header                         │
│                                                         │
│  Auth                                                   │
│  🔓 POST /api/auth/login      ← AllowAnonymous          │
│  🔓 POST /api/auth/register                             │
│                                                         │
│  Articles                                               │
│  🔓 GET  /api/articles        ← AllowAnonymous          │
│  🔒 POST /api/articles        ← RequireAuthorization    │
│  🔒 GET  /api/articles/{id}                             │
│  🔒 DELETE /api/articles/{id}                           │
└─────────────────────────────────────────────────────────┘
         │
         ▼ วิธี Login

Step 1: คลิก [🔒 Authorize] ที่ header
        → หน้าต่าง Authorize เปิดขึ้น
        → เห็น section "Login with Username/Password"

Step 2: กรอก Username และ Password
        Username: admin@rmuti.ac.th
        Password: Admin@1234
        → คลิก [Login and Authorize]

Step 3: Login สำเร็จ
        → token ถูก set ใน Swagger UI อัตโนมัติ
        → ปุ่มเปลี่ยนเป็น [Logout] [Close]
        → icon ทุกตัวเปลี่ยนเป็น 🔓 (open padlock)

Step 4: ปิด modal แล้วทดสอบ endpoint ที่มี 🔒
        → Swagger แนบ header อัตโนมัติ:
           Authorization: Bearer eyJhbGci...

Step 5: Refresh หน้า (F5)
        → token ยังอยู่ — ยัง 🔓 อยู่ (restore จาก localStorage)

Step 6: Logout
        → คลิก [🔓 Authorize] → คลิก [Logout]
        → token ถูกลบ → icon กลับเป็น 🔒
```

---

## 11. Lock Icon Reference

| Endpoint | Auth | Lock | หมายเหตุ |
|----------|------|------|---------|
| `POST /api/auth/login` | — | 🔓 | จุดเริ่มต้น — รับ token |
| `POST /api/auth/register` | — | 🔓 | สมัครสมาชิก |
| `GET  /api/articles` | — | 🔓 | บทความสาธารณะ |
| `GET  /api/media/{id}` | — | 🔓 | ดูไฟล์สาธารณะ |
| `GET  /api/search` | — | 🔓 | ค้นหาสาธารณะ |
| `POST /api/articles` | CanWrite | 🔒 | Faculty, Researcher, Admin |
| `PUT  /api/articles/{id}` | CanWrite | 🔒 | เจ้าของหรือ Admin |
| `POST /api/articles/{id}/publish` | CanPublish | 🔒 | Faculty, Admin |
| `POST /api/media/article/{id}/*` | JWT | 🔒 | ผู้ที่ login แล้ว |
| `GET  /api/ai/generate` | CanWrite | 🔒 | AI features |
| `POST /api/linebot/chat` | X-Api-Key | — | ใช้ API Key แทน JWT |
| `GET  /api/admin/*` | AdminOnly | 🔒 | Admin เท่านั้น |

---

## 12. ตรวจสอบ JWT Token

### Decode token (ไม่ต้อง verify)

```bash
# ใช้ jwt.io หรือ decode ด้วย base64
echo "eyJhbGci..." | cut -d. -f2 | base64 -d | jq
```

### ตัวอย่าง payload ที่ควรได้

```json
{
  "sub":        "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email":      "faculty@skc.rmuti.ac.th",
  "jti":        "abc123...",
  "fullNameTh": "อาจารย์ ทดสอบ ระบบ",
  "role":       ["Faculty"],
  "iss":        "SkcKmsApi",
  "aud":        "SkcKmsClient",
  "exp":        1735689600,
  "iat":        1735686000
}
```

### ดึงข้อมูลใน Endpoint

```csharp
// ดึง user id จาก token ที่ validated แล้ว
var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
var email  = context.User.FindFirstValue(ClaimTypes.Email)!;
var roles  = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);

// ตรวจ role เฉพาะ
var isFaculty = context.User.IsInRole("Faculty");
var isAdmin   = context.User.IsInRole("Admin");
```

---

## Quick Troubleshooting

| ปัญหา | สาเหตุ | วิธีแก้ |
|-------|--------|--------|
| Lock icon ไม่ขึ้น | OperationFilter ไม่ได้ register | เพิ่ม `c.OperationFilter<SecurityRequirementsOperationFilter>()` |
| Icon แสดงผิด (unlock ก่อน login) | `syncOperationLockIcons()` swap แค่ CSS class แต่ไม่เปลี่ยน SVG `href` | ต้องเปลี่ยนทั้ง class และ `useEl.setAttribute("href", "#locked")` |
| JS เวอร์ชันเก่า — แก้แล้วไม่เห็นผล | Browser cache | Bump `?v=` ใน `InjectJavascript(...)` แล้ว `Ctrl+Shift+R` |
| Refresh หน้าแล้ว token หาย | `applyStoredToken()` ถูก disable | ให้ `applyStoredToken()` อ่านจาก `localStorage.getItem(STORAGE_KEY)` |
| Login panel ไม่โชว์ใน modal | `.auth-container` ยังไม่เกิดใน DOM | ใช้ `MutationObserver` + polling `setInterval(300ms)` เพื่อ inject |
| 401 ทั้งที่ใส่ token แล้ว | `UseAuthentication()` มาหลัง `UseAuthorization()` | สลับลำดับ — `UseAuthentication()` ต้องมาก่อนเสมอ |
| Token ไม่หมดอายุตรงเวลา | ClockSkew default = 5 นาที | ตั้ง `ClockSkew = TimeSpan.Zero` |
| `swagger-auto-auth.js` โหลดไม่ได้ (404) | `app.UseStaticFiles()` ไม่ได้เพิ่ม หรืออยู่หลัง middleware อื่น | ใส่ `app.UseStaticFiles()` ก่อน `app.UseAuthentication()` |
| Swagger ไม่แสดงใน Production | เงื่อนไข `IsDevelopment()` | ตั้ง environment flag หรือเพิ่ม config `Swagger:Enabled` |
| `AllowAnonymous` แต่ยัง lock | Filter อ่าน metadata ผิด | ตรวจสอบ `IAllowAnonymous` อยู่ใน `endpointMetadata` |

---

*KMS — Knowledge Management System*  
*RMUTI Sakon Nakhon Campus — Stack: .NET 10 · ASP.NET Core Identity · JWT Bearer · Swashbuckle*
