namespace KMS.Application.Interfaces;

public interface IMediaProcessor
{
    Task<bool> ProcessImageAsync(Domain.Entities.Media.MediaItem mediaItem, byte[] imageData, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateThumbnailAsync(byte[] imageData, ThumbnailSize size, CancellationToken cancellationToken = default);
    Task<byte[]> ConvertToWebPAsync(byte[] imageData, int quality = 80, CancellationToken cancellationToken = default);
    Task<byte[]> ResizeImageAsync(byte[] imageData, int width, int height, CancellationToken cancellationToken = default);
    bool IsImageMimeType(string mimeType);
    bool SupportsFormat(string mimeType);
}

public enum ThumbnailSize
{
    Small = 1,
    Medium = 2,
    Large = 3
}