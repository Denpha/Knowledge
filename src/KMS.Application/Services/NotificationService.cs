using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Logging;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Entities.Logging;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IRepository<AppUser> _userRepository;
    private readonly IRepository<Domain.Entities.Knowledge.KnowledgeArticle> _articleRepository;
    private readonly IRepository<Domain.Entities.Interaction.Comment> _commentRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IRepository<Notification> notificationRepository,
        IRepository<AppUser> userRepository,
        IRepository<Domain.Entities.Knowledge.KnowledgeArticle> articleRepository,
        IRepository<Domain.Entities.Interaction.Comment> commentRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _articleRepository = articleRepository;
        _commentRepository = commentRepository;
        _logger = logger;
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.Query
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        return notification?.Adapt<NotificationDto>();
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto createDto, Guid createdById, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = createDto.UserId,
            Type = createDto.Type,
            Title = createDto.Title,
            Message = createDto.Message,
            ReferenceUrl = createDto.ReferenceUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return notification.Adapt<NotificationDto>();
    }

    public async Task<NotificationDto> UpdateAsync(Guid id, UpdateNotificationDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification == null)
        {
            throw new KeyNotFoundException($"Notification with ID {id} not found.");
        }

        if (updateDto.IsRead.HasValue && updateDto.IsRead.Value && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
        else if (updateDto.IsRead.HasValue && !updateDto.IsRead.Value && notification.IsRead)
        {
            notification.IsRead = false;
            notification.ReadAt = null;
        }

        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return notification.Adapt<NotificationDto>();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification == null)
        {
            return false;
        }

        _notificationRepository.Remove(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PaginatedResult<NotificationDto>> SearchAsync(NotificationSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        var query = _notificationRepository.Query
            .Include(n => n.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchParams.Type))
        {
            query = query.Where(n => n.Type == searchParams.Type);
        }

        if (searchParams.UserId.HasValue)
        {
            query = query.Where(n => n.UserId == searchParams.UserId.Value);
        }

        if (searchParams.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == searchParams.IsRead.Value);
        }

        if (searchParams.CreatedFrom.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= searchParams.CreatedFrom.Value);
        }

        if (searchParams.CreatedTo.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= searchParams.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
        {
            query = query.Where(n => 
                n.Title.Contains(searchParams.SearchTerm) || 
                n.Message.Contains(searchParams.SearchTerm));
        }

        // Apply sorting
        var orderedQuery = searchParams.SortDescending
            ? query.OrderByDescending(GetSortProperty(searchParams))
            : query.OrderBy(GetSortProperty(searchParams));

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await orderedQuery.AsQueryable()
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(n =>
        {
            var dto = n.Adapt<NotificationDto>();
            dto.UserName = n.User?.UserName ?? string.Empty;
            return dto;
        }).ToList();

        return new PaginatedResult<NotificationDto>
        {
            Items = dtos,
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PaginatedResult<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        NotificationSearchParams searchParams,
        CancellationToken cancellationToken = default)
    {
        searchParams.UserId = userId;
        return await SearchAsync(searchParams, cancellationToken);
    }

    public async Task<NotificationDto> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.Query
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification == null)
        {
            throw new KeyNotFoundException($"Notification with ID {notificationId} not found for user {userId}.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _notificationRepository.Update(notification);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return notification.Adapt<NotificationDto>();
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _notificationRepository.Query
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        if (unreadNotifications.Any())
        {
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return unreadNotifications.Count;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.Query
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<List<NotificationDto>> SendToUsersAsync(
        CreateNotificationDto notificationDto,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var notifications = new List<NotificationDto>();
        var validUserIds = userIds.Distinct().ToList();

        foreach (var userId in validUserIds)
        {
            try
            {
                var createDto = new CreateNotificationDto
                {
                    Type = notificationDto.Type,
                    Title = notificationDto.Title,
                    Message = notificationDto.Message,
                    ReferenceUrl = notificationDto.ReferenceUrl,
                    UserId = userId
                };

                var notification = await CreateAsync(createDto, userId, cancellationToken);
                notifications.Add(notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to user {UserId}", userId);
            }
        }

        return notifications;
    }

    public async Task<List<NotificationDto>> SendToRoleAsync(
        CreateNotificationDto notificationDto,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Note: This is a simplified implementation. In a real application,
        // you would need to query users by role using Identity framework.
        // For now, we'll return an empty list as a placeholder.
        
        _logger.LogInformation("Sending notification to role {RoleName} would require Identity role queries", roleName);
        return new List<NotificationDto>();
    }

    public async Task<NotificationDto> CreateArticleNotificationAsync(
        Guid articleId,
        string notificationType,
        Guid? triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.Query
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {articleId} not found.");
        }

        var (title, message, referenceUrl) = GetArticleNotificationDetails(
            notificationType, 
            article, 
            triggeredByUserId);

        var notificationDto = new CreateNotificationDto
        {
            Type = notificationType,
            Title = title,
            Message = message,
            ReferenceUrl = referenceUrl,
            UserId = article.AuthorId // Notify article author by default
        };

        return await CreateAsync(notificationDto, notificationDto.UserId, cancellationToken);
    }

    public async Task<NotificationDto> CreateCommentNotificationAsync(
        Guid commentId,
        string notificationType,
        Guid? triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.Query
            .Include(c => c.Author)
            .Include(c => c.Article)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            throw new KeyNotFoundException($"Comment with ID {commentId} not found.");
        }

        var (title, message, referenceUrl) = GetCommentNotificationDetails(
            notificationType,
            comment,
            triggeredByUserId);

        // Determine who to notify
        Guid userIdToNotify;
        if (notificationType == "CommentReply" && comment.ParentId.HasValue)
        {
            // Notify parent comment author
            var parentComment = await _commentRepository.Query
                .FirstOrDefaultAsync(c => c.Id == comment.ParentId.Value, cancellationToken);
            
            userIdToNotify = parentComment?.AuthorId ?? comment.Article.AuthorId;
        }
        else
        {
            // Notify article author
            userIdToNotify = comment.Article.AuthorId;
        }

        var notificationDto = new CreateNotificationDto
        {
            Type = notificationType,
            Title = title,
            Message = message,
            ReferenceUrl = referenceUrl,
            UserId = userIdToNotify
        };

        return await CreateAsync(notificationDto, notificationDto.UserId, cancellationToken);
    }

    public async Task<int> DeleteOldNotificationsAsync(int daysToKeep, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldNotifications = await _notificationRepository.Query
            .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
            .ToListAsync(cancellationToken);

        if (!oldNotifications.Any())
        {
            return 0;
        }

        foreach (var notification in oldNotifications)
        {
            _notificationRepository.Remove(notification);
        }

        await _notificationRepository.SaveChangesAsync(cancellationToken);
        return oldNotifications.Count;
    }

    #region Private Methods

    private static Func<Notification, object> GetSortProperty(NotificationSearchParams searchParams)
    {
        return searchParams.SortBy?.ToLower() switch
        {
            "type" => n => n.Type,
            "title" => n => n.Title,
            "createdat" => n => n.CreatedAt,
            "readat" => n => (object)(n.ReadAt ?? DateTimeOffset.MinValue),
            _ => n => n.CreatedAt
        };
    }

    private (string Title, string Message, string? ReferenceUrl) GetArticleNotificationDetails(
        string notificationType,
        Domain.Entities.Knowledge.KnowledgeArticle article,
        Guid? triggeredByUserId)
    {
        var articleUrl = $"/articles/{article.Slug}";
        var triggeredByUser = triggeredByUserId.HasValue ? $" by user {triggeredByUserId}" : "";

        return notificationType switch
        {
            "ArticlePublished" => (
                $"บทความของคุณได้รับการเผยแพร่แล้ว",
                $"บทความ \"{article.Title}\" ได้รับการเผยแพร่แล้ว{triggeredByUser}",
                articleUrl
            ),
            "ArticleReviewed" => (
                $"บทความของคุณได้รับการตรวจสอบ",
                $"บทความ \"{article.Title}\" ได้รับการตรวจสอบแล้ว{triggeredByUser}",
                articleUrl
            ),
            "ArticleRejected" => (
                $"บทความของคุณไม่ผ่านการอนุมัติ",
                $"บทความ \"{article.Title}\" ไม่ผ่านการอนุมัติ{triggeredByUser}",
                articleUrl
            ),
            "ArticleUpdated" => (
                $"บทความของคุณได้รับการอัปเดต",
                $"บทความ \"{article.Title}\" ได้รับการอัปเดตแล้ว{triggeredByUser}",
                articleUrl
            ),
            "ArticleCommentAdded" => (
                $"มีความคิดเห็นใหม่ในบทความของคุณ",
                $"มีผู้อื่นแสดงความคิดเห็นในบทความ \"{article.Title}\" ของคุณ{triggeredByUser}",
                articleUrl
            ),
            "ArticleLiked" => (
                $"มีคนชอบบทความของคุณ",
                $"มีผู้อื่นชอบบทความ \"{article.Title}\" ของคุณ{triggeredByUser}",
                articleUrl
            ),
            _ => (
                $"การแจ้งเตือนเกี่ยวกับบทความ",
                $"มีกิจกรรมเกี่ยวกับบทความ \"{article.Title}\" ของคุณ{triggeredByUser}",
                articleUrl
            )
        };
    }

    private (string Title, string Message, string? ReferenceUrl) GetCommentNotificationDetails(
        string notificationType,
        Domain.Entities.Interaction.Comment comment,
        Guid? triggeredByUserId)
    {
        var articleUrl = $"/articles/{comment.Article.Slug}#comment-{comment.Id}";
        var triggeredByUser = triggeredByUserId.HasValue ? $" by user {triggeredByUserId}" : "";

        return notificationType switch
        {
            "CommentAdded" => (
                $"มีความคิดเห็นใหม่ในบทความของคุณ",
                $"มีผู้อื่นแสดงความคิดเห็นในบทความ \"{comment.Article.Title}\" ของคุณ{triggeredByUser}",
                articleUrl
            ),
            "CommentReply" => (
                $"มีคนตอบกลับความคิดเห็นของคุณ",
                $"มีผู้อื่นตอบกลับความคิดเห็นของคุณในบทความ \"{comment.Article.Title}\"{triggeredByUser}",
                articleUrl
            ),
            "CommentLiked" => (
                $"มีคนชอบความคิดเห็นของคุณ",
                $"มีผู้อื่นชอบความคิดเห็นของคุณในบทความ \"{comment.Article.Title}\"{triggeredByUser}",
                articleUrl
            ),
            _ => (
                $"การแจ้งเตือนเกี่ยวกับความคิดเห็น",
                $"มีกิจกรรมเกี่ยวกับความคิดเห็นของคุณในบทความ \"{comment.Article.Title}\"{triggeredByUser}",
                articleUrl
            )
        };
    }

    #endregion
}