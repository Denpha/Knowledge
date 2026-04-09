using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using KMS.Application.Interfaces;

namespace KMS.Infrastructure.Services;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly string _bucket;
    private readonly string _publicUrl;

    public MinioFileStorageService(IConfiguration configuration, ILogger<MinioFileStorageService> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("MinIO");
        var endpoint = section["Endpoint"] ?? "localhost:9000";
        var accessKey = section["AccessKey"] ?? "minioadmin";
        var secretKey = section["SecretKey"] ?? "minioadmin";
        var secure = bool.Parse(section["Secure"] ?? "false");
        _bucket = section["Bucket"] ?? "kms";
        _publicUrl = section["PublicUrl"] ?? $"http://{endpoint}/{_bucket}";

        _minio = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .Build();
    }

    // ─── Ensure bucket exists ──────────────────────────────────────────────────
    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), ct);
        if (!exists)
        {
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), ct);
            _logger.LogInformation("Created MinIO bucket: {Bucket}", _bucket);

            // Set public read policy for the bucket
            var policy = $$"""
                {
                  "Version":"2012-10-17",
                  "Statement":[{
                    "Effect":"Allow",
                    "Principal":{"AWS":["*"]},
                    "Action":["s3:GetObject"],
                    "Resource":["arn:aws:s3:::{{_bucket}}/*"]
                  }]
                }
                """;
            await _minio.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucket).WithPolicy(policy), ct);
        }
    }

    // ─── Upload ───────────────────────────────────────────────────────────────
    public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string contentType,
        string? path = null, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);
        var objectName = BuildObjectName(fileName, path);

        using var stream = new MemoryStream(fileData);
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(fileData.Length)
            .WithContentType(contentType), cancellationToken);

        _logger.LogInformation("Uploaded {Name} ({Size} bytes) to MinIO", objectName, fileData.Length);
        return objectName;
    }

    // ─── Download ─────────────────────────────────────────────────────────────
    public async Task<byte[]> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await _minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(filePath)
            .WithCallbackStream(s => s.CopyTo(ms)), cancellationToken);
        return ms.ToArray();
    }

    // ─── URL ──────────────────────────────────────────────────────────────────
    public Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default)
        => Task.FromResult($"{_publicUrl}/{filePath}");

    // ─── Presigned URL ────────────────────────────────────────────────────────
    public async Task<string> GeneratePresignedUrlAsync(string filePath, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        return await _minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(filePath)
            .WithExpiry((int)expiry.TotalSeconds));
    }

    // ─── Delete ───────────────────────────────────────────────────────────────
    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucket)
                .WithObject(filePath), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Path}", filePath);
            return false;
        }
    }

    // ─── Exists ───────────────────────────────────────────────────────────────
    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            await _minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucket)
                .WithObject(filePath), cancellationToken);
            return true;
        }
        catch { return false; }
    }

    // ─── Stat ─────────────────────────────────────────────────────────────────
    public async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var stat = await _minio.StatObjectAsync(new StatObjectArgs()
            .WithBucket(_bucket)
            .WithObject(filePath), cancellationToken);
        return stat.Size;
    }

    // ─── Thumbnail (passthrough — no server-side processing in MinIO) ─────────
    public Task<string> GenerateThumbnailAsync(string filePath, int width, int height,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Thumbnail generation not supported in MinIO mode; returning original: {Path}", filePath);
        return Task.FromResult(filePath);
    }

    // ─── Move / Copy ──────────────────────────────────────────────────────────
    public async Task<string> MoveFileAsync(string sourcePath, string destinationPath,
        CancellationToken cancellationToken = default)
    {
        await CopyFileAsync(sourcePath, destinationPath, cancellationToken);
        await DeleteFileAsync(sourcePath, cancellationToken);
        return destinationPath;
    }

    public async Task<string> CopyFileAsync(string sourcePath, string destinationPath,
        CancellationToken cancellationToken = default)
    {
        await _minio.CopyObjectAsync(new CopyObjectArgs()
            .WithBucket(_bucket)
            .WithObject(destinationPath)
            .WithCopyObjectSource(new CopySourceObjectArgs()
                .WithBucket(_bucket)
                .WithObject(sourcePath)), cancellationToken);
        return destinationPath;
    }

    // ─── Directory stubs (S3 uses prefixes, no real dirs) ────────────────────
    public Task CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
        => Task.FromResult(true); // prefixes always "exist"

    // ─── Storage usage ────────────────────────────────────────────────────────
    public async Task<long> GetStorageUsageAsync(string? directoryPath = null,
        CancellationToken cancellationToken = default)
    {
        long total = 0;
        var prefix = directoryPath ?? "";
        var tcs = new TaskCompletionSource<bool>();

        var observable = _minio.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(_bucket)
            .WithPrefix(prefix)
            .WithRecursive(true), cancellationToken);

        await foreach (var item in observable)
            total += (long)item.Size;

        return total;
    }

    // ─── List files ───────────────────────────────────────────────────────────
    public async Task<List<string>> ListFilesAsync(string directoryPath, string pattern = "*",
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await foreach (var item in _minio.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(_bucket)
            .WithPrefix(directoryPath)
            .WithRecursive(false), cancellationToken))
        {
            results.Add(item.Key);
        }
        return results;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static string BuildObjectName(string fileName, string? path)
    {
        var ext = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName)
            .ToLowerInvariant()
            .Replace(' ', '_');
        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uid = Guid.NewGuid().ToString("N")[..8];
        var unique = $"{name}_{ts}_{uid}{ext}";
        return string.IsNullOrEmpty(path) ? unique : $"{path.TrimEnd('/')}/{unique}";
    }
}
