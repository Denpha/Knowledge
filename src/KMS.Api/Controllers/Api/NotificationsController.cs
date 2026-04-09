using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Logging;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // GET: api/notifications
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<NotificationDto>>>> GetNotifications(
        [FromQuery] string? type,
        [FromQuery] bool? isRead,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<PaginatedResult<NotificationDto>>("Invalid user token.");
            }

            var searchParams = new NotificationSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending,
                SearchTerm = search,
                Type = type,
                IsRead = isRead,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                UserId = userId
            };

            var result = await _notificationService.GetUserNotificationsAsync(userId, searchParams, cancellationToken);
            return this.Ok(result, "Notifications retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return this.InternalServerError<PaginatedResult<NotificationDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/notifications/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotification(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<NotificationDto>("Invalid user token.");
            }

            var notification = await _notificationService.GetByIdAsync(id, cancellationToken);

            if (notification == null)
            {
                return this.NotFound<NotificationDto>($"Notification with ID {id} not found.");
            }

            // Check if notification belongs to current user
            if (notification.UserId != userId)
            {
                return this.Forbidden<NotificationDto>("You don't have permission to view this notification.");
            }

            return this.Ok(notification, "Notification retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId}", id);
            return this.InternalServerError<NotificationDto>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/notifications/unread/count
    [HttpGet("unread/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<int>("Invalid user token.");
            }

            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
            return this.Ok(count, "Unread notification count retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notification count");
            return this.InternalServerError<int>($"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/notifications/{id}/read
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<NotificationDto>("Invalid user token.");
            }

            var notification = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
            return this.Ok(notification, "Notification marked as read.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<NotificationDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return this.InternalServerError<NotificationDto>($"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<int>("Invalid user token.");
            }

            var count = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
            return this.Ok(count, $"Marked {count} notifications as read.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return this.InternalServerError<int>($"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/notifications/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteNotification(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized("Invalid user token.");
            }

            // Check if notification belongs to current user before deleting
            var notification = await _notificationService.GetByIdAsync(id, cancellationToken);
            if (notification == null)
            {
                return this.NotFound($"Notification with ID {id} not found.");
            }

            if (notification.UserId != userId)
            {
                return this.Forbidden("You don't have permission to delete this notification.");
            }

            var result = await _notificationService.DeleteAsync(id, userId, cancellationToken);

            if (result)
            {
                return this.Ok("Notification deleted successfully.");
            }
            else
            {
                return this.BadRequest("Failed to delete notification.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return this.InternalServerError($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/notifications/send
    [HttpPost("send")]
    [Authorize(Roles = "Admin,Faculty")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> SendNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var createDto = new CreateNotificationDto
            {
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                ReferenceUrl = request.ReferenceUrl,
                UserId = Guid.Empty // Will be set per user
            };

            List<NotificationDto> notifications;

            if (request.UserIds?.Any() == true)
            {
                notifications = await _notificationService.SendToUsersAsync(createDto, request.UserIds, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(request.RoleName))
            {
                notifications = await _notificationService.SendToRoleAsync(createDto, request.RoleName, cancellationToken);
            }
            else
            {
                return this.BadRequest<List<NotificationDto>>("Either UserIds or RoleName must be specified.");
            }

            return this.Ok(notifications, $"Sent {notifications.Count} notifications successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notifications");
            return this.InternalServerError<List<NotificationDto>>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/notifications/article/{articleId}
    [HttpPost("article/{articleId:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateArticleNotification(
        Guid articleId,
        [FromBody] CreateArticleNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var triggeredByUserId))
            {
                return this.Unauthorized<NotificationDto>("Invalid user token.");
            }

            var notification = await _notificationService.CreateArticleNotificationAsync(
                articleId,
                request.NotificationType,
                triggeredByUserId,
                cancellationToken);

            return this.Ok(notification, "Article notification created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<NotificationDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article notification for article {ArticleId}", articleId);
            return this.InternalServerError<NotificationDto>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/notifications/comment/{commentId}
    [HttpPost("comment/{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateCommentNotification(
        Guid commentId,
        [FromBody] CreateCommentNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var triggeredByUserId))
            {
                return this.Unauthorized<NotificationDto>("Invalid user token.");
            }

            var notification = await _notificationService.CreateCommentNotificationAsync(
                commentId,
                request.NotificationType,
                triggeredByUserId,
                cancellationToken);

            return this.Ok(notification, "Comment notification created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<NotificationDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment notification for comment {CommentId}", commentId);
            return this.InternalServerError<NotificationDto>($"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/notifications/cleanup
    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> CleanupOldNotifications(
        [FromQuery] int daysToKeep = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (daysToKeep < 1)
            {
                return this.BadRequest<int>("daysToKeep must be at least 1.");
            }

            var deletedCount = await _notificationService.DeleteOldNotificationsAsync(daysToKeep, cancellationToken);
            return this.Ok(deletedCount, $"Deleted {deletedCount} old notifications.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            return this.InternalServerError<int>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/notifications/test
    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> TestNotifications(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a test notification
            var createDto = new CreateNotificationDto
            {
                Type = "Test",
                Title = "Test Notification",
                Message = "This is a test notification to verify the service is working.",
                ReferenceUrl = "/test",
                UserId = Guid.Empty // Would need a valid user ID for real test
            };

            // Test search with empty params
            var searchParams = new NotificationSearchParams
            {
                PageNumber = 1,
                PageSize = 5
            };

            var searchResult = await _notificationService.SearchAsync(searchParams, cancellationToken);

            return this.Ok(
                $"Notification service is working. Total notifications in system: {searchResult.TotalCount}",
                "Notification service test completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing notification service");
            return this.InternalServerError<string>($"Notification service test failed: {ex.Message}");
        }
    }
}

public class SendNotificationRequest
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReferenceUrl { get; set; }
    public List<Guid>? UserIds { get; set; }
    public string? RoleName { get; set; }
}

public class CreateArticleNotificationRequest
{
    public string NotificationType { get; set; } = string.Empty;
}

public class CreateCommentNotificationRequest
{
    public string NotificationType { get; set; } = string.Empty;
}