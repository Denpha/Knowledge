using Mapster;
using Microsoft.EntityFrameworkCore;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Media;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Media;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class MediaService : IMediaService
{
    private readonly IRepository<MediaItem> _mediaRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMediaProcessor? _mediaProcessor;

    public MediaService(
        IRepository<MediaItem> mediaRepository,
        IFileStorageService fileStorageService,
        IMediaProcessor? mediaProcessor = null)
    {
        _mediaRepository = mediaRepository;
        _fileStorageService = fileStorageService;
        _mediaProcessor = mediaProcessor;
    }

    public async Task<MediaItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(id, cancellationToken);
        return mediaItem?.Adapt<MediaItemDto>();
    }

    public async Task<PaginatedResult<MediaItemDto>> SearchAsync(MediaSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        var query = _mediaRepository.Query;

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchParams.MediaType))
        {
            // Map media type to mime type patterns
            var mimeTypePatterns = GetMimeTypePatterns(searchParams.MediaType);
            query = query.Where(m => mimeTypePatterns.Any(pattern => m.MimeType.StartsWith(pattern)));
        }

        if (!string.IsNullOrWhiteSpace(searchParams.CollectionName))
        {
            query = query.Where(m => m.CollectionName == searchParams.CollectionName);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.EntityType))
        {
            query = query.Where(m => m.ModelType == searchParams.EntityType);
        }

        if (searchParams.EntityId.HasValue)
        {
            query = query.Where(m => m.ModelId == searchParams.EntityId.Value);
        }

        if (searchParams.UploaderId.HasValue)
        {
            query = query.Where(m => m.UploadedById == searchParams.UploaderId.Value);
        }

        if (searchParams.IsPublic.HasValue)
        {
            // Note: MediaItem doesn't have IsPublic field, this would need to be implemented
            // For now, we'll skip this filter
        }

        if (searchParams.UploadedFrom.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= searchParams.UploadedFrom.Value);
        }

        if (searchParams.UploadedTo.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= searchParams.UploadedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
        {
            query = query.Where(m => 
                m.Name.Contains(searchParams.SearchTerm) || 
                m.FileName.Contains(searchParams.SearchTerm));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (searchParams.SortDescending)
        {
            query = searchParams.SortBy switch
            {
                "name" => query.OrderByDescending(m => m.Name),
                "size" => query.OrderByDescending(m => m.Size),
                "createdAt" => query.OrderByDescending(m => m.CreatedAt),
                _ => query.OrderByDescending(m => m.CreatedAt)
            };
        }
        else
        {
            query = searchParams.SortBy switch
            {
                "name" => query.OrderBy(m => m.Name),
                "size" => query.OrderBy(m => m.Size),
                "createdAt" => query.OrderBy(m => m.CreatedAt),
                _ => query.OrderBy(m => m.CreatedAt)
            };
        }

        // Apply pagination
        var mediaItems = await query
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(cancellationToken);

        var mediaItemDtos = mediaItems.Adapt<List<MediaItemDto>>();

        return new PaginatedResult<MediaItemDto>
        {
            Items = mediaItemDtos,
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<MediaItemDto> CreateAsync(CreateMediaItemDto createDto, Guid createdById, CancellationToken cancellationToken = default)
    {
        // This method is for creating metadata only
        // For file uploads, use UploadAsync
        var mediaItem = new MediaItem
        {
            Id = Guid.NewGuid(),
            Name = createDto.Title ?? "Untitled",
            CollectionName = createDto.CollectionName ?? "default",
            ModelType = createDto.EntityType ?? string.Empty,
            ModelId = createDto.EntityId ?? Guid.Empty,
            UploadedById = createdById,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdById.ToString()
        };

        await _mediaRepository.AddAsync(mediaItem, cancellationToken);
        await _mediaRepository.SaveChangesAsync(cancellationToken);

        return mediaItem.Adapt<MediaItemDto>();
    }

    public async Task<MediaItemDto> UpdateAsync(Guid id, UpdateMediaItemDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(id, cancellationToken);
        if (mediaItem == null)
        {
            throw new KeyNotFoundException($"Media item with ID {id} not found.");
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(updateDto.Title))
        {
            mediaItem.Name = updateDto.Title;
        }

        if (!string.IsNullOrEmpty(updateDto.AltText))
        {
            // Store in custom properties JSON
            // For simplicity, we'll store in Name if no title provided
            if (string.IsNullOrEmpty(updateDto.Title))
            {
                mediaItem.Name = updateDto.AltText;
            }
        }

        mediaItem.UpdatedAt = DateTime.UtcNow;
        mediaItem.UpdatedBy = updatedById.ToString();

        _mediaRepository.Update(mediaItem);
        await _mediaRepository.SaveChangesAsync(cancellationToken);

        return mediaItem.Adapt<MediaItemDto>();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(id, cancellationToken);
        if (mediaItem == null)
        {
            throw new KeyNotFoundException($"Media item with ID {id} not found.");
        }

        // Delete the physical file if it exists
        if (!string.IsNullOrEmpty(mediaItem.Path))
        {
            await _fileStorageService.DeleteFileAsync(mediaItem.Path, cancellationToken);
        }

        // Delete the database record
        _mediaRepository.Remove(mediaItem);
        await _mediaRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<MediaItemDto> UploadAsync(UploadMediaItemDto uploadDto, Guid uploadedById, byte[] fileData, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        try
        {
            // Determine storage path based on entity type and collection
            var storagePath = GetStoragePath(uploadDto.EntityType, uploadDto.CollectionName);

            // Upload file to storage
            var filePath = await _fileStorageService.UploadFileAsync(
                fileData,
                fileName,
                contentType,
                storagePath,
                cancellationToken);

            // Get file URL
            var fileUrl = await _fileStorageService.GetFileUrlAsync(filePath, cancellationToken);

            // Create media item record
            var mediaItem = new MediaItem
            {
                Id = Guid.NewGuid(),
                ModelType = uploadDto.EntityType ?? string.Empty,
                ModelId = uploadDto.EntityId ?? Guid.Empty,
                CollectionName = uploadDto.CollectionName ?? "default",
                Name = uploadDto.Title ?? Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                MimeType = contentType,
                Extension = Path.GetExtension(fileName).TrimStart('.'),
                Disk = "local",
                Path = filePath,
                Size = fileSize,
                UploadedById = uploadedById,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = uploadedById.ToString()
            };

            // Process image if it's an image and we have a processor
            if (_mediaProcessor != null && _mediaProcessor.IsImageMimeType(contentType))
            {
                try
                {
                    await _mediaProcessor.ProcessImageAsync(mediaItem, fileData, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the upload if image processing fails
                    // The original file is still uploaded successfully
                    Console.WriteLine($"Image processing failed: {ex.Message}");
                }
            }

            await _mediaRepository.AddAsync(mediaItem, cancellationToken);
            await _mediaRepository.SaveChangesAsync(cancellationToken);

            return mediaItem.Adapt<MediaItemDto>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload media: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DownloadAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (mediaItem == null)
        {
            throw new KeyNotFoundException($"Media item with ID {mediaId} not found.");
        }

        if (string.IsNullOrEmpty(mediaItem.Path))
        {
            throw new InvalidOperationException($"Media item with ID {mediaId} has no associated file.");
        }

        return await _fileStorageService.DownloadFileAsync(mediaItem.Path, cancellationToken);
    }

    public async Task<string> GetDownloadUrlAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (mediaItem == null)
        {
            throw new KeyNotFoundException($"Media item with ID {mediaId} not found.");
        }

        if (string.IsNullOrEmpty(mediaItem.Path))
        {
            throw new InvalidOperationException($"Media item with ID {mediaId} has no associated file.");
        }

        return await _fileStorageService.GetFileUrlAsync(mediaItem.Path, cancellationToken);
    }

    public Task<string> GetThumbnailUrlAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        // For now, return the original file URL
        // In a real implementation, this would check if a thumbnail exists and generate one if not
        return GetDownloadUrlAsync(mediaId, cancellationToken);
    }

    public async Task<MediaItemDto> UpdateMetadataAsync(Guid mediaId, UpdateMediaItemDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(mediaId, updateDto, updatedById, cancellationToken);
    }

    public async Task<bool> DeletePermanentlyAsync(Guid mediaId, Guid deletedById, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(mediaId, deletedById, cancellationToken);
    }

    public async Task<List<MediaItemDto>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var mediaItems = await _mediaRepository.FindAsync(
            m => m.ModelType == entityType && m.ModelId == entityId, 
            cancellationToken);

        return mediaItems.Adapt<List<MediaItemDto>>();
    }

    public async Task<List<MediaItemDto>> GetByCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        var mediaItems = await _mediaRepository.FindAsync(
            m => m.CollectionName == collectionName, 
            cancellationToken);

        return mediaItems.Adapt<List<MediaItemDto>>();
    }

    public async Task<long> GetStorageUsageAsync(Guid? uploaderId = null, CancellationToken cancellationToken = default)
    {
        var query = _mediaRepository.Query;

        if (uploaderId.HasValue)
        {
            query = query.Where(m => m.UploadedById == uploaderId.Value);
        }

        return await query.SumAsync(m => m.Size, cancellationToken);
    }

    public async Task<MediaItemDto> GenerateThumbnailAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (mediaItem == null)
        {
            throw new KeyNotFoundException($"Media item with ID {mediaId} not found.");
        }

        // Check if it's an image
        if (!mediaItem.MimeType.StartsWith("image/"))
        {
            throw new InvalidOperationException($"Thumbnail generation only supported for images. Media type: {mediaItem.MimeType}");
        }

        // Use media processor if available
        if (_mediaProcessor != null)
        {
            try
            {
                // Download the original file
                var fileData = await _fileStorageService.DownloadFileAsync(mediaItem.Path, cancellationToken);
                
                // Generate thumbnail
                var thumbnailData = await _mediaProcessor.GenerateThumbnailAsync(fileData, ThumbnailSize.Medium, cancellationToken);
                
                // Save thumbnail
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(mediaItem.FileName)}_thumb.webp";
                var thumbnailPath = await _fileStorageService.UploadFileAsync(
                    thumbnailData,
                    thumbnailFileName,
                    "image/webp",
                    Path.GetDirectoryName(mediaItem.Path),
                    cancellationToken);

                // Update media item with thumbnail information
                var generatedConversions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(mediaItem.GeneratedConversions) ?? new Dictionary<string, bool>();
                generatedConversions["thumb_medium"] = true;
                mediaItem.GeneratedConversions = System.Text.Json.JsonSerializer.Serialize(generatedConversions);

                _mediaRepository.Update(mediaItem);
                await _mediaRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate thumbnail: {ex.Message}", ex);
            }
        }

        return mediaItem.Adapt<MediaItemDto>();
    }

    public async Task<MediaItemDto> ConvertFormatAsync(Guid mediaId, string targetFormat, CancellationToken cancellationToken = default)
    {
        var mediaItem = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (mediaItem == null)
        {
            throw new KeyNotFoundException($"Media item with ID {mediaId} not found.");
        }

        // Check if it's an image
        if (!mediaItem.MimeType.StartsWith("image/"))
        {
            throw new InvalidOperationException($"Format conversion only supported for images. Media type: {mediaItem.MimeType}");
        }

        // Use media processor if available and target format is WebP
        if (_mediaProcessor != null && targetFormat.ToLowerInvariant() == "webp")
        {
            try
            {
                // Download the original file
                var fileData = await _fileStorageService.DownloadFileAsync(mediaItem.Path, cancellationToken);
                
                // Convert to WebP
                var webpData = await _mediaProcessor.ConvertToWebPAsync(fileData, quality: 85, cancellationToken);
                
                // Save WebP version
                var webpFileName = $"{Path.GetFileNameWithoutExtension(mediaItem.FileName)}.webp";
                var webpPath = await _fileStorageService.UploadFileAsync(
                    webpData,
                    webpFileName,
                    "image/webp",
                    Path.GetDirectoryName(mediaItem.Path),
                    cancellationToken);

                // Update media item with conversion information
                var generatedConversions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(mediaItem.GeneratedConversions) ?? new Dictionary<string, bool>();
                generatedConversions["webp"] = true;
                mediaItem.GeneratedConversions = System.Text.Json.JsonSerializer.Serialize(generatedConversions);

                _mediaRepository.Update(mediaItem);
                await _mediaRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert to {targetFormat}: {ex.Message}", ex);
            }
        }

        return mediaItem.Adapt<MediaItemDto>();
    }

    private string GetStoragePath(string? entityType, string? collectionName)
    {
        var pathParts = new List<string>();

        if (!string.IsNullOrEmpty(entityType))
        {
            pathParts.Add(entityType.ToLowerInvariant());
        }

        if (!string.IsNullOrEmpty(collectionName))
        {
            pathParts.Add(collectionName.ToLowerInvariant());
        }

        // Add date-based subdirectory
        var datePart = DateTime.UtcNow.ToString("yyyy/MM/dd");
        pathParts.Add(datePart);

        return string.Join("/", pathParts);
    }

    private List<string> GetMimeTypePatterns(string mediaType)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "image" => new List<string> { "image/" },
            "video" => new List<string> { "video/" },
            "audio" => new List<string> { "audio/" },
            "document" => new List<string> 
            { 
                "application/pdf", 
                "application/msword", 
                "application/vnd.openxmlformats-officedocument",
                "text/"
            },
            _ => new List<string> { string.Empty }
        };
    }
}