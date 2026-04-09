using KMS.Application.DTOs;
using KMS.Application.DTOs.Media;

namespace KMS.Application.Interfaces;

public interface IMediaService : IBaseService<MediaItemDto, CreateMediaItemDto, UpdateMediaItemDto, MediaSearchParams>
{
    Task<MediaItemDto> UploadAsync(UploadMediaItemDto uploadDto, Guid uploadedById, byte[] fileData, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<string> GetDownloadUrlAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<string> GetThumbnailUrlAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<MediaItemDto> UpdateMetadataAsync(Guid mediaId, UpdateMediaItemDto updateDto, Guid updatedById, CancellationToken cancellationToken = default);
    Task<bool> DeletePermanentlyAsync(Guid mediaId, Guid deletedById, CancellationToken cancellationToken = default);
    Task<List<MediaItemDto>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<List<MediaItemDto>> GetByCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
    Task<long> GetStorageUsageAsync(Guid? uploaderId = null, CancellationToken cancellationToken = default);
    Task<MediaItemDto> GenerateThumbnailAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<MediaItemDto> ConvertFormatAsync(Guid mediaId, string targetFormat, CancellationToken cancellationToken = default);
}