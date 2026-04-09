using System.Text;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pgvector.EntityFrameworkCore;
using System.Threading.RateLimiting;
using KMS.Api.Filters;
using KMS.Api.Middleware;
using KMS.Application.Common;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Application.Services;
using KMS.Application.Services.Ai;
using KMS.Application.Validators;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Interfaces;
using KMS.Infrastructure.Data;
using KMS.Infrastructure.Repositories;
using KMS.Infrastructure.Services;
using KMS.Infrastructure.Services.Ai;
using KMS.Infrastructure.Services.Media;
using Serilog;
using Serilog.Events;
using Hangfire;
using Hangfire.PostgreSql;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();
builder.Host.UseSystemd();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── API Versioning ────────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add CORS for frontend dev hosts
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        var origins = configuredOrigins
            .Concat(new[] {
                "http://localhost:5173", "http://localhost:5174",
                "http://127.0.0.1:5173", "http://127.0.0.1:5174",
                "http://172.28.26.249:5173", "http://172.28.26.249:5174",
                "http://172.29.65.45:5173", "http://172.29.65.45:5174",
                "http://192.168.1.176:5173", "http://192.168.1.176:5174"
            })
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
    });
});

// Add repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();

// Add Application services
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ICommentService, CommentService>();

// Add Phase 4 services
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Add AI Services
builder.Services.Configure<AiConfig>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<LineOaConfig>(builder.Configuration.GetSection("LineOA"));

// Register HttpClient for AI services (named clients for different providers)
builder.Services.AddHttpClient("OpenRouter");
builder.Services.AddHttpClient("XiaomiMiMo");
builder.Services.AddHttpClient("LineOA");
builder.Services.AddHttpClient("AlertWebhook");

// Register individual AI services
builder.Services.AddScoped<OpenRouterChatService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenRouter");
    var aiConfig = sp.GetRequiredService<IOptions<AiConfig>>();
    var logger = sp.GetRequiredService<ILogger<OpenRouterChatService>>();
    return new OpenRouterChatService(httpClient, aiConfig, logger);
});

builder.Services.AddScoped<XiaomiMimoChatService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("XiaomiMiMo");
    var aiConfig = sp.GetRequiredService<IOptions<AiConfig>>();
    var logger = sp.GetRequiredService<ILogger<XiaomiMimoChatService>>();
    return new XiaomiMimoChatService(httpClient, aiConfig, logger);
});

builder.Services.AddScoped<IAiChatService, FallbackChatService>();

// Register embedding services
builder.Services.AddScoped<OpenRouterEmbeddingService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenRouter");
    var aiConfig = sp.GetRequiredService<IOptions<AiConfig>>();
    var logger = sp.GetRequiredService<ILogger<OpenRouterEmbeddingService>>();
    return new OpenRouterEmbeddingService(httpClient, aiConfig, logger);
});

builder.Services.AddScoped<IEmbeddingService, OpenRouterEmbeddingService>();
// Also register as IAiEmbeddingService for AiWritingService
builder.Services.AddScoped<IAiEmbeddingService>(sp => sp.GetRequiredService<OpenRouterEmbeddingService>());
// Keep MockEmbeddingService as fallback
builder.Services.AddScoped<MockEmbeddingService>();

// Register AI Writing Service
builder.Services.AddScoped<IAiWritingService, AiWritingService>();
builder.Services.AddScoped<IAlertChannelService, WebhookAlertChannelService>();

// Register Media Processor
builder.Services.AddScoped<IMediaProcessor, ImageSharpMediaProcessor>();

// File Storage: switch between Local and MinIO via FileStorage:Provider config
var storageProvider = builder.Configuration["FileStorage:Provider"]?.ToLower() ?? "local";
if (storageProvider == "minio")
{
    builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}

// Register Publish Workflow Service
builder.Services.AddScoped<IPublishWorkflowService, PublishWorkflowService>();

// Email service
builder.Services.Configure<KMS.Infrastructure.Services.Email.EmailOptions>(
    builder.Configuration.GetSection(KMS.Infrastructure.Services.Email.EmailOptions.SectionName));
builder.Services.AddScoped<KMS.Application.Interfaces.IEmailService,
    KMS.Infrastructure.Services.Email.EmailService>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    // AI endpoints: 20 requests per minute per user/IP
    options.AddFixedWindowLimiter("ai", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Line webhook: 100 requests per minute per IP
    options.AddFixedWindowLimiter("line", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Auth endpoints: 10 requests per minute per IP (brute force protection)
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "KMS:";
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "db", "ready" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "cache", "ready" });

// Add ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/ai/stream"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanWrite", policy =>
        policy.RequireRole("Faculty", "Researcher", "Admin"));

    options.AddPolicy("CanPublish", policy =>
        policy.RequireRole("Faculty", "Admin"));

    options.AddPolicy("CanReview", policy =>
        policy.RequireRole("Faculty", "Admin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});

// Add Mapster configuration
TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingProfile).Assembly);

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateArticleDtoValidator>();

// ── Hangfire ─────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer(opts =>
{
    opts.WorkerCount = 2;
    opts.Queues = new[] { "critical", "default", "low" };
});

// Add Swagger services
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = builder.Configuration["Swagger:Title"] ?? "KMS API",
        Version = builder.Configuration["Swagger:Version"] ?? "v1",
        Description = builder.Configuration["Swagger:Description"] ?? "Knowledge Management System API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token returned by POST /api/auth/login. Swagger will send it as Authorization: Bearer <token>."
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
    options.TagActionsBy(api =>
    {
        if (!string.IsNullOrWhiteSpace(api.GroupName))
        {
            return [api.GroupName];
        }

        if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName) &&
            !string.IsNullOrWhiteSpace(controllerName))
        {
            return [controllerName];
        }

        return ["Default"];
    });
    options.DocInclusionPredicate((_, _) => true);
});

var app = builder.Build();

// Seed database in development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        await KMS.Infrastructure.Data.Seeders.DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "KMS API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "KMS API Documentation";
        options.DisplayRequestDuration();
        options.EnableFilter();
        options.EnableDeepLinking();
        options.DefaultModelsExpandDepth(-1);
        options.InjectJavascript("/swagger-auto-auth.js?v=20260408-5");
    });
}

// Add exception handling middleware
app.UseExceptionHandling();
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    opts.GetLevel = (ctx, elapsed, ex) => ex != null || ctx.Response.StatusCode >= 500
        ? LogEventLevel.Error
        : ctx.Response.StatusCode >= 400
            ? LogEventLevel.Warning
            : LogEventLevel.Information;
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Hangfire Dashboard (admin only) ──────────────────────────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "KMS Background Jobs",
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
});

// Register recurring background jobs
RecurringJob.AddOrUpdate(
    "cleanup-expired-tokens",
    () => Console.WriteLine("[Hangfire] Cleanup expired refresh tokens - implement with DI"),
    Cron.Daily(3, 0),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok") });

RecurringJob.AddOrUpdate(
    "stats-snapshot",
    () => Console.WriteLine("[Hangfire] Daily stats snapshot - implement with DI"),
    Cron.Daily(1, 0),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok") });

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            }),
            duration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});

await app.RunAsync();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }