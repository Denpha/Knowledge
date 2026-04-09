using Mapster;
using KMS.Application.DTOs.Identity;
using KMS.Application.DTOs.Interaction;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.DTOs.Logging;
using KMS.Application.DTOs.Media;
using KMS.Application.DTOs.System;
using KMS.Domain.Entities;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Entities.Interaction;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Entities.Logging;
using KMS.Domain.Entities.Media;

namespace KMS.Application.Common;

public class MappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // KnowledgeArticle -> ArticleDto
        config.NewConfig<KnowledgeArticle, ArticleDto>()
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : string.Empty)
            .Map(dest => dest.AuthorName, src => src.Author != null ? src.Author.FullNameTh : string.Empty)
            .Map(dest => dest.ReviewerName, src => src.Reviewer != null ? src.Reviewer.FullNameTh : null)
            .Map(dest => dest.Tags, src => src.ArticleTags.Select(at => at.Tag).ToList())
            .Map(dest => dest.ViewCount, src => src.Views != null ? src.Views.Count : 0)
            .Map(dest => dest.LikeCount, src => src.ArticleReactions != null ? src.ArticleReactions.Count(r => r.ReactionType == "Like") : 0)
            .Map(dest => dest.BookmarkCount, src => src.ArticleReactions != null ? src.ArticleReactions.Count(r => r.ReactionType == "Bookmark") : 0)
            .Map(dest => dest.CommentCount, src => src.Comments != null ? src.Comments.Count : 0);

        // Tag -> TagDto
        config.NewConfig<Tag, TagDto>()
            .Map(dest => dest.ArticleCount, src => src.ArticleTags != null ? src.ArticleTags.Count : 0);

        // Category -> CategoryDto
        config.NewConfig<Category, CategoryDto>()
            .Map(dest => dest.ParentName, src => src.Parent != null ? src.Parent.Name : null)
            .Map(dest => dest.ArticleCount, src => src.Articles != null ? src.Articles.Count : 0)
            .Map(dest => dest.SubCategoryCount, src => src.Children != null ? src.Children.Count : 0)
            .Map(dest => dest.Order, src => src.SortOrder); // Map SortOrder to Order

        // AppUser -> UserDto
        config.NewConfig<AppUser, UserDto>()
            .Map(dest => dest.FullName, src => src.FullNameTh) // Use FullNameTh
            .Map(dest => dest.FirstName, src => (string?)null) // Not available in entity
            .Map(dest => dest.LastName, src => (string?)null) // Not available in entity
            .Map(dest => dest.ArticleCount, src => src.CreatedArticles != null ? src.CreatedArticles.Count : 0)
            .Map(dest => dest.Roles, src => src.UserRoles != null ? src.UserRoles.Select(ur => ur.Role).ToList() : new List<Role>());

        // Role -> RoleDto
        config.NewConfig<Role, RoleDto>()
            .Map(dest => dest.UserCount, src => src.UserRoles != null ? src.UserRoles.Count : 0);

        // Comment -> CommentDto
        config.NewConfig<Comment, CommentDto>()
            .Map(dest => dest.ArticleTitle, src => src.Article != null ? src.Article.Title : string.Empty)
            .Map(dest => dest.AuthorName, src => src.Author != null ? src.Author.FullNameTh : "Anonymous")
            .Map(dest => dest.AuthorAvatarUrl, src => (string?)null) // Not available in entity
            .Map(dest => dest.LikeCount, src => 0) // TODO: Add comment likes if implemented
            .Map(dest => dest.Replies, src => src.Replies != null ? src.Replies.ToList() : null)
            .Map(dest => dest.IsAnonymous, src => false); // Not available in entity

        // ArticleReaction -> ReactionDto
        config.NewConfig<ArticleReaction, ReactionDto>()
            .Map(dest => dest.ArticleTitle, src => src.Article != null ? src.Article.Title : string.Empty)
            .Map(dest => dest.UserName, src => src.User != null ? src.User.FullNameTh : string.Empty);

        // MediaItem -> MediaItemDto
        config.NewConfig<MediaItem, MediaItemDto>()
            .Map(dest => dest.UploaderName, src => src.UploadedBy != null ? src.UploadedBy.FullNameTh : string.Empty)
            .Map(dest => dest.Url, src => $"/media/{src.Id}") // TODO: Update with actual URL generation
            .Map(dest => dest.ThumbnailUrl, src => src.MimeType.StartsWith("image/") ? $"/media/{src.Id}/thumbnail" : null)
            .Map(dest => dest.MediaType, src => GetMediaTypeFromMimeType(src.MimeType))
            .Map(dest => dest.FileName, src => src.Name)
            .Map(dest => dest.OriginalFileName, src => src.FileName)
            .Map(dest => dest.FilePath, src => src.Path)
            .Map(dest => dest.ContentType, src => src.MimeType)
            .Map(dest => dest.FileSize, src => src.Size)
            .Map(dest => dest.EntityType, src => src.ModelType)
            .Map(dest => dest.EntityId, src => src.ModelId);

        // AuditLog -> AuditLogDto
        config.NewConfig<AuditLog, AuditLogDto>()
            .Map(dest => dest.UserName, src => src.User != null ? src.User.FullNameTh : null);

        // Notification -> NotificationDto
        config.NewConfig<Notification, NotificationDto>()
            .Map(dest => dest.UserName, src => src.User != null ? src.User.FullNameTh : string.Empty);

        // SystemSetting -> SystemSettingDto
        config.NewConfig<SystemSetting, SystemSettingDto>();

        // View -> ViewDto
        config.NewConfig<View, ViewDto>()
            .Map(dest => dest.ArticleTitle, src => src.Article != null ? src.Article.Title : string.Empty)
            .Map(dest => dest.UserName, src => src.User != null ? src.User.FullNameTh : null);

        // ArticleVersion -> ArticleVersionDto
        config.NewConfig<ArticleVersion, ArticleVersionDto>()
            .Map(dest => dest.ArticleTitle, src => src.Article != null ? src.Article.Title : string.Empty)
            .Map(dest => dest.EditedByName, src => src.EditedBy != null ? src.EditedBy.FullNameTh : string.Empty);
    }

    private static string GetMediaTypeFromMimeType(string mimeType)
    {
        if (mimeType.StartsWith("image/")) return "Image";
        if (mimeType.StartsWith("video/")) return "Video";
        if (mimeType.StartsWith("audio/")) return "Audio";
        if (mimeType.StartsWith("application/pdf")) return "Document";
        if (mimeType.StartsWith("application/msword") || mimeType.StartsWith("application/vnd.openxmlformats-officedocument")) return "Document";
        return "Other";
    }

    private static string GetCategoryPath(Category category)
    {
        var path = new List<string>();
        var current = category;
        
        while (current != null)
        {
            path.Insert(0, current.Name);
            current = current.Parent;
        }
        
        return string.Join(" > ", path);
    }
}

public static class MappingExtensions
{
    public static TDestination MapTo<TDestination>(this object source)
    {
        return source.Adapt<TDestination>();
    }

    public static List<TDestination> MapToList<TDestination>(this IEnumerable<object> source)
    {
        return source.Adapt<List<TDestination>>();
    }
}