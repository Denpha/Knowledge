using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using KMS.Domain.Entities;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Entities.Interaction;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Entities.Logging;
using KMS.Domain.Entities.Media;

namespace KMS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser, Role, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Knowledge
    public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }
    public DbSet<ArticleVersion> ArticleVersions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ArticleTag> ArticleTags { get; set; }
    
    // Interaction
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ArticleReaction> ArticleReactions { get; set; }
    public DbSet<View> Views { get; set; }
    
    // Media
    public DbSet<MediaItem> MediaItems { get; set; }
    
    // Logging
    public DbSet<AiWritingLog> AiWritingLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<KnowledgeSearchLog> KnowledgeSearchLogs { get; set; }
    
    // Identity
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // System
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure entity relationships and constraints
        ConfigureIdentityEntities(builder);
        ConfigureKnowledgeEntities(builder);
        ConfigureInteractionEntities(builder);
        ConfigureMediaEntities(builder);
        ConfigureLoggingEntities(builder);
        ConfigureSystemEntities(builder);
    }

    private void ConfigureIdentityEntities(ModelBuilder builder)
    {
        // RefreshToken configuration
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AppUser configuration
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.FullNameTh).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FullNameEn).HasMaxLength(200);
            entity.Property(e => e.Faculty).HasMaxLength(200);
            entity.Property(e => e.Department).HasMaxLength(200);
            entity.Property(e => e.Position).HasMaxLength(200);
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            
            entity.HasMany(e => e.UserRoles)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Role configuration
        builder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasMany(e => e.UserRoles)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRole configuration (join table)
        builder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureKnowledgeEntities(ModelBuilder builder)
    {
        // KnowledgeArticle configuration
        builder.Entity<KnowledgeArticle>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TitleEn).HasMaxLength(500);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(600);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SummaryEn).HasMaxLength(2000);
            entity.Property(e => e.KeywordsEn).HasMaxLength(500);
            
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Visibility).HasConversion<string>();
            
            entity.Property(e => e.Embedding).HasColumnType("vector(1024)");
            
            // Relationships
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Articles)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Author)
                .WithMany(e => e.CreatedArticles)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Reviewer)
                .WithMany(e => e.UpdatedArticles)
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.AuthorId);
            // Composite indexes for common query patterns
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_KnowledgeArticle_Status_CreatedAt");
            entity.HasIndex(e => new { e.CategoryId, e.Status }).HasDatabaseName("IX_KnowledgeArticle_CategoryId_Status");
            entity.HasIndex(e => new { e.AuthorId, e.Status }).HasDatabaseName("IX_KnowledgeArticle_AuthorId_Status");
            entity.HasIndex(e => new { e.Status, e.Visibility, e.PublishedAt }).HasDatabaseName("IX_KnowledgeArticle_Published_Feed");
        });

        // Category configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NameEn).HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(250);
            entity.Property(e => e.IconName).HasMaxLength(100);
            entity.Property(e => e.ColorHex).HasMaxLength(7);
            
            // Self-referencing relationship for hierarchical categories
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.SortOrder);
        });

        // ArticleVersion configuration
        builder.Entity<ArticleVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ChangeNote).HasMaxLength(500);
            
            entity.HasOne(e => e.Article)
                .WithMany(e => e.Versions)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.EditedBy)
                .WithMany()
                .HasForeignKey(e => e.EditedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.VersionNumber);
        });

        // Tag configuration
        builder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(120);
            
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // ArticleTag configuration (many-to-many)
        builder.Entity<ArticleTag>(entity =>
        {
            entity.HasKey(e => new { e.ArticleId, e.TagId });
            
            entity.HasOne(e => e.Article)
                .WithMany(e => e.ArticleTags)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Tag)
                .WithMany(e => e.ArticleTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureInteractionEntities(ModelBuilder builder)
    {
        // Comment configuration
        builder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Content).IsRequired();
            
            entity.HasOne(e => e.Article)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Author)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Self-referencing relationship for nested comments
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => e.ParentId);
        });

        // ArticleReaction configuration
        builder.Entity<ArticleReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ReactionType).IsRequired().HasMaxLength(20);
            
            entity.HasOne(e => e.Article)
                .WithMany(e => e.ArticleReactions)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.ArticleReactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.ArticleId, e.UserId, e.ReactionType }).IsUnique();
            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ReactionType);
        });

        // View configuration
        builder.Entity<View>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            
            entity.HasOne(e => e.Article)
                .WithMany(e => e.Views)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ViewedAt);
            entity.HasIndex(e => new { e.ArticleId, e.ViewedAt }).HasDatabaseName("IX_View_ArticleId_ViewedAt");
        });
    }

    private void ConfigureMediaEntities(ModelBuilder builder)
    {
        // MediaItem configuration
        builder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ModelType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CollectionName).IsRequired().HasMaxLength(100).HasDefaultValue("default");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Extension).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Disk).IsRequired().HasMaxLength(50).HasDefaultValue("local");
            entity.Property(e => e.ConversionsDisk).HasMaxLength(50);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            
            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.ModelType, e.ModelId });
            entity.HasIndex(e => e.CollectionName);
            entity.HasIndex(e => e.UploadedById);
        });
    }

    private void ConfigureLoggingEntities(ModelBuilder builder)
    {
        // AiWritingLog configuration
        builder.Entity<AiWritingLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FeatureType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ImprovementType).HasMaxLength(30);
            entity.Property(e => e.ModelUsed).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Article)
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // AuditLog configuration
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReferenceUrl).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
        });

        // KnowledgeSearchLog configuration
        builder.Entity<KnowledgeSearchLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Query).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.SearchType).IsRequired().HasMaxLength(20).HasDefaultValue("Keyword");
            entity.Property(e => e.QueryEmbedding).HasColumnType("vector(1024)");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ClickedArticle)
                .WithMany()
                .HasForeignKey(e => e.ClickedArticleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SearchType);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private void ConfigureSystemEntities(ModelBuilder builder)
    {
        // SystemSetting configuration
        builder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Group).HasMaxLength(100);
            
            entity.HasOne(e => e.UpdatedBy)
                .WithMany()
                .HasForeignKey(e => e.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Group);
        });

        // ApiKey configuration
        builder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Prefix).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ClientType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.RevokedBy)
                .WithMany()
                .HasForeignKey(e => e.RevokedById)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}