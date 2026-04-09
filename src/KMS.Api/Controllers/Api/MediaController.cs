using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Media;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly IValidator<CreateMediaItemDto> _createMediaValidator;
    private readonly IValidator<UpdateMediaItemDto> _updateMediaValidator;

    public MediaController(
        IMediaService mediaService,
        IValidator<CreateMediaItemDto> createMediaValidator,
        IValidator<UpdateMediaItemDto> updateMediaValidator)
    {
        _mediaService = mediaService;
        _createMediaValidator = createMediaValidator;
        _updateMediaValidator = updateMediaValidator;
    }

    // GET: api/media
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<MediaItemDto>>>> GetMedia(
        [FromQuery] string? mediaType,
        [FromQuery] string? collectionName,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? uploaderId,
        [FromQuery] bool? isPublic,
        [FromQuery] DateTime? uploadedFrom,
        [FromQuery] DateTime? uploadedTo,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new MediaSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                MediaType = mediaType,
                CollectionName = collectionName,
                EntityType = entityType,
                EntityId = entityId,
                UploaderId = uploaderId,
                IsPublic = isPublic,
                UploadedFrom = uploadedFrom,
                UploadedTo = uploadedTo
            };

            var result = await _mediaService.SearchAsync(searchParams, cancellationToken);
            return this.Ok(result, "Media items retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<PaginatedResult<MediaItemDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/media/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MediaItemDto>>> GetMediaItem(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _mediaService.GetByIdAsync(id, cancellationToken);

            if (mediaItem == null)
            {
                return this.NotFound<MediaItemDto>($"Media item with ID {id} not found.");
            }

            return this.Ok(mediaItem, "Media item retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<MediaItemDto>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/media/{id}/download
    [HttpGet("{id:guid}/download")]
    public async Task<ActionResult> DownloadMediaItem(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _mediaService.GetByIdAsync(id, cancellationToken);
            if (mediaItem == null)
            {
                return this.NotFound($"Media item with ID {id} not found.");
            }

            var fileData = await _mediaService.DownloadAsync(id, cancellationToken);

            // Determine content type
            var contentType = mediaItem.ContentType ?? "application/octet-stream";

            // Set content disposition for download
            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = mediaItem.OriginalFileName ?? mediaItem.FileName,
                Inline = false // Force download
            };

            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

            return File(fileData, contentType);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse($"Internal server error: {ex.Message}"));
        }
    }

    // GET: api/media/{id}/url
    [HttpGet("{id:guid}/url")]
    public async Task<ActionResult<ApiResponse<string>>> GetMediaUrl(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = await _mediaService.GetDownloadUrlAsync(id, cancellationToken);
            return this.Ok(url, "Media URL retrieved successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<string>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<string>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/media/{id}/thumbnail
    [HttpGet("{id:guid}/thumbnail")]
    public async Task<ActionResult<ApiResponse<string>>> GetMediaThumbnailUrl(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = await _mediaService.GetThumbnailUrlAsync(id, cancellationToken);
            return this.Ok(url, "Media thumbnail URL retrieved successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<string>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<string>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/media/upload
    [HttpPost("upload")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MediaItemDto>>> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] string? altText,
        [FromForm] string? collectionName,
        [FromForm] string? entityType,
        [FromForm] Guid? entityId,
        [FromForm] bool isPublic = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<MediaItemDto>("Invalid user token.");
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return this.BadRequest<MediaItemDto>("No file uploaded.");
            }

            // Check file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return this.BadRequest<MediaItemDto>("File size exceeds 10MB limit.");
            }

            // Read file data
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileData = memoryStream.ToArray();

            // Create upload DTO
            var uploadDto = new UploadMediaItemDto
            {
                Title = title,
                Description = description,
                AltText = altText,
                CollectionName = collectionName,
                EntityType = entityType,
                EntityId = entityId,
                IsPublic = isPublic,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileData = fileData
            };

            // Upload media
            var mediaItem = await _mediaService.UploadAsync(
                uploadDto, 
                userId, 
                fileData, 
                file.FileName, 
                file.ContentType, 
                file.Length, 
                cancellationToken);

            return this.Ok(mediaItem, "Media uploaded successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<MediaItemDto>($"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/media/{id}
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MediaItemDto>>> UpdateMediaItem(
        Guid id,
        UpdateMediaItemDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized<MediaItemDto>("Invalid user token.");
            }

            // Validate the request
            var validationResult = await _updateMediaValidator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return this.BadRequest<MediaItemDto>("Validation failed.", errors);
            }

            var mediaItem = await _mediaService.UpdateMetadataAsync(id, updateDto, userId, cancellationToken);

            return this.Ok(mediaItem, "Media item updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<MediaItemDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<MediaItemDto>($"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/media/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteMediaItem(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return this.Unauthorized("Invalid user token.");
            }

            var result = await _mediaService.DeletePermanentlyAsync(id, userId, cancellationToken);

            if (result)
            {
                return this.Ok("Media item deleted successfully.");
            }
            else
            {
                return this.BadRequest("Failed to delete media item.");
            }
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/media/entity/{entityType}/{entityId}
    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<ActionResult<ApiResponse<List<MediaItemDto>>>> GetMediaByEntity(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItems = await _mediaService.GetByEntityAsync(entityType, entityId, cancellationToken);
            return this.Ok(mediaItems, "Media items retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<List<MediaItemDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/media/collection/{collectionName}
    [HttpGet("collection/{collectionName}")]
    public async Task<ActionResult<ApiResponse<List<MediaItemDto>>>> GetMediaByCollection(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItems = await _mediaService.GetByCollectionAsync(collectionName, cancellationToken);
            return this.Ok(mediaItems, "Media items retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<List<MediaItemDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/media/storage/usage
    [HttpGet("storage/usage")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<long>>> GetStorageUsage(
        [FromQuery] Guid? uploaderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If uploaderId is not provided, use current user's ID
            if (!uploaderId.HasValue)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return this.Unauthorized<long>("Invalid user token.");
                }
                uploaderId = userId;
            }

            var usage = await _mediaService.GetStorageUsageAsync(uploaderId, cancellationToken);
            return this.Ok(usage, "Storage usage retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<long>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/media/{id}/thumbnail/generate
    [HttpPost("{id:guid}/thumbnail/generate")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MediaItemDto>>> GenerateThumbnail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _mediaService.GenerateThumbnailAsync(id, cancellationToken);
            return this.Ok(mediaItem, "Thumbnail generated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<MediaItemDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<MediaItemDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<MediaItemDto>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/media/{id}/convert
    [HttpPost("{id:guid}/convert")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MediaItemDto>>> ConvertMediaFormat(
        Guid id,
        [FromForm] string targetFormat,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaItem = await _mediaService.ConvertFormatAsync(id, targetFormat, cancellationToken);
            return this.Ok(mediaItem, "Media converted successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<MediaItemDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<MediaItemDto>($"Internal server error: {ex.Message}");
        }
    }
}