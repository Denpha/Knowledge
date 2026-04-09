namespace KMS.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(byte[] fileData, string fileName, string contentType, string? path = null, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string> GenerateThumbnailAsync(string filePath, int width, int height, CancellationToken cancellationToken = default);
    Task<string> GeneratePresignedUrlAsync(string filePath, TimeSpan expiry, CancellationToken cancellationToken = default);
    
    // File operations
    Task<string> MoveFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
    Task<string> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
    Task CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default);
    
    // Storage information
    Task<long> GetStorageUsageAsync(string? directoryPath = null, CancellationToken cancellationToken = default);
    Task<List<string>> ListFilesAsync(string directoryPath, string pattern = "*", CancellationToken cancellationToken = default);
}