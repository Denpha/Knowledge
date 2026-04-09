using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KMS.Application.Interfaces;

namespace KMS.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _basePath = configuration["FileStorage:Local:BasePath"] ?? "wwwroot/uploads";
        _baseUrl = configuration["FileStorage:Local:BaseUrl"] ?? "/uploads";
        
        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation($"Created storage directory: {_basePath}");
        }
    }

    public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string contentType, string? path = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique filename to prevent conflicts
            var uniqueFileName = GenerateUniqueFileName(fileName);
            var filePath = Path.Combine(_basePath, path ?? string.Empty, uniqueFileName);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            
            // Write file
            await File.WriteAllBytesAsync(filePath, fileData, cancellationToken);
            
            _logger.LogInformation($"File uploaded successfully: {filePath} ({fileData.Length} bytes)");
            
            // Return relative path
            return Path.GetRelativePath(_basePath, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file {fileName}");
            throw;
        }
    }

    public async Task<byte[]> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            
            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file {filePath}");
            throw;
        }
    }

    public Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/{filePath.Replace("\\", "/")}";
        return Task.FromResult(url);
    }

    public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(false);
            }
            
            File.Delete(fullPath);
            _logger.LogInformation($"File deleted: {filePath}");
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file {filePath}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        
        var fileInfo = new FileInfo(fullPath);
        return fileInfo.Length;
    }

    public Task<string> GenerateThumbnailAsync(string filePath, int width, int height, CancellationToken cancellationToken = default)
    {
        // For now, return the original file path
        // In a real implementation, this would generate thumbnails using ImageSharp or similar
        _logger.LogWarning($"Thumbnail generation not implemented for {filePath}");
        return Task.FromResult(filePath);
    }

    public Task<string> GeneratePresignedUrlAsync(string filePath, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        // Local storage doesn't need presigned URLs
        return GetFileUrlAsync(filePath, cancellationToken);
    }

    public async Task<string> MoveFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFullPath = Path.Combine(_basePath, sourcePath);
            var destFullPath = Path.Combine(_basePath, destinationPath);
            
            if (!File.Exists(sourceFullPath))
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }
            
            // Ensure destination directory exists
            var destDirectory = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory!);
            }
            
            File.Move(sourceFullPath, destFullPath);
            _logger.LogInformation($"File moved from {sourcePath} to {destinationPath}");
            
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error moving file from {sourcePath} to {destinationPath}");
            throw;
        }
    }

    public async Task<string> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFullPath = Path.Combine(_basePath, sourcePath);
            var destFullPath = Path.Combine(_basePath, destinationPath);
            
            if (!File.Exists(sourceFullPath))
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }
            
            // Ensure destination directory exists
            var destDirectory = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory!);
            }
            
            File.Copy(sourceFullPath, destFullPath);
            _logger.LogInformation($"File copied from {sourcePath} to {destinationPath}");
            
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error copying file from {sourcePath} to {destinationPath}");
            throw;
        }
    }

    public Task CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, directoryPath);
        
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            _logger.LogInformation($"Directory created: {directoryPath}");
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, directoryPath);
        return Task.FromResult(Directory.Exists(fullPath));
    }

    public Task<long> GetStorageUsageAsync(string? directoryPath = null, CancellationToken cancellationToken = default)
    {
        var searchPath = string.IsNullOrEmpty(directoryPath) ? _basePath : Path.Combine(_basePath, directoryPath);
        
        if (!Directory.Exists(searchPath))
        {
            return Task.FromResult(0L);
        }
        
        var files = Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories);
        var totalSize = files.Sum(file => new FileInfo(file).Length);
        
        return Task.FromResult(totalSize);
    }

    public Task<List<string>> ListFilesAsync(string directoryPath, string pattern = "*", CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, directoryPath);
        
        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(new List<string>());
        }
        
        var files = Directory.GetFiles(fullPath, pattern, SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetRelativePath(_basePath, file))
            .ToList();
        
        return Task.FromResult(files);
    }

    private string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        
        // Sanitize filename
        var sanitizedFileName = SanitizeFileName(fileNameWithoutExtension);
        
        // Add timestamp and GUID for uniqueness
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        
        return $"{sanitizedFileName}_{timestamp}_{uniqueId}{extension}";
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());
        
        // Replace spaces with underscores
        sanitized = sanitized.Replace(' ', '_');
        
        // Limit length
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100];
        }
        
        return sanitized.ToLowerInvariant();
    }
}