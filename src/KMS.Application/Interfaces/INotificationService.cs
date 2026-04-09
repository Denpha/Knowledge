using KMS.Application.DTOs;
using KMS.Application.DTOs.Logging;

namespace KMS.Application.Interfaces;

public interface INotificationService : IBaseService<NotificationDto, CreateNotificationDto, UpdateNotificationDto, NotificationSearchParams>
{
    // Get notifications for current user
    Task<PaginatedResult<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        NotificationSearchParams searchParams,
        CancellationToken cancellationToken = default);
    
    // Mark notification as read
    Task<NotificationDto> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    
    // Mark all notifications as read for user
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // Get unread notification count for user
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // Send notification to multiple users
    Task<List<NotificationDto>> SendToUsersAsync(
        CreateNotificationDto notificationDto,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
    
    // Send notification to all users in a role
    Task<List<NotificationDto>> SendToRoleAsync(
        CreateNotificationDto notificationDto,
        string roleName,
        CancellationToken cancellationToken = default);
    
    // Create notification for article events
    Task<NotificationDto> CreateArticleNotificationAsync(
        Guid articleId,
        string notificationType,
        Guid? triggeredByUserId,
        CancellationToken cancellationToken = default);
    
    // Create notification for comment events
    Task<NotificationDto> CreateCommentNotificationAsync(
        Guid commentId,
        string notificationType,
        Guid? triggeredByUserId,
        CancellationToken cancellationToken = default);
    
    // Delete old notifications (cleanup)
    Task<int> DeleteOldNotificationsAsync(int daysToKeep, CancellationToken cancellationToken = default);
}