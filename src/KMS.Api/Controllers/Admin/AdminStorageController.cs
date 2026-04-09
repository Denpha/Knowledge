using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using KMS.Api.Models;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Admin;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>สรุป storage statistics ของ MinIO bucket</summary>
public class StorageStatsDto
{
    public long TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
    public Dictionary<string, long> FilesByType { get; set; } = new();
    public Dictionary<string, long> SizeByType { get; set; } = new();
    public string BucketName { get; set; } = string.Empty;
}

/// <summary>ข้อมูลไฟล์หนึ่งรายการใน MinIO</summary>
public class StorageFileDto
{
    public string Key { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
}

/// <summary>ผลลัพธ์ paginated ของ file listing</summary>
public class StorageFileListDto
{
    public List<StorageFileDto> Files { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>คำขอ delete หลายไฟล์พร้อมกัน</summary>
public class BulkDeleteRequest
{
    public List<string> Keys { get; set; } = new();
}

// ─── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Route("api/admin/storage")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminStorageController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly IMinioClient _minio;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminStorageController> _logger;
    private readonly string _bucket;
    private readonly string _publicUrl;

    public AdminStorageController(
        IFileStorageService storage,
        IConfiguration config,
        ILogger<AdminStorageController> logger)
    {
        _storage = storage;
        _config = config;
        _logger = logger;

        var section = config.GetSection("MinIO");
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

    // ─── GET /api/admin/storage/stats ─────────────────────────────────────────
    /// <summary>
    /// ดึงสถิติ storage ทั้งหมดของ MinIO bucket:
    /// จำนวนไฟล์, ขนาดรวม, แยกตาม file extension
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<StorageStatsDto>>> GetStats(CancellationToken ct)
    {
        try
        {
            var stats = new StorageStatsDto { BucketName = _bucket };
            var filesByExt = new Dictionary<string, long>();
            var sizeByExt = new Dictionary<string, long>();

            await foreach (var obj in _minio.ListObjectsEnumAsync(new ListObjectsArgs()
                .WithBucket(_bucket)
                .WithRecursive(true), ct))
            {
                stats.TotalFiles++;
                stats.TotalSizeBytes += (long)obj.Size;

                var ext = Path.GetExtension(obj.Key).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "other";

                filesByExt[ext] = filesByExt.GetValueOrDefault(ext) + 1;
                sizeByExt[ext] = sizeByExt.GetValueOrDefault(ext) + (long)obj.Size;
            }

            stats.TotalSizeFormatted = FormatBytes(stats.TotalSizeBytes);
            stats.FilesByType = filesByExt;
            stats.SizeByType = sizeByExt;

            return Ok(new ApiResponse<StorageStatsDto>(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve storage stats");
            return StatusCode(500, new ApiResponse("ไม่สามารถดึงข้อมูล storage stats ได้"));
        }
    }

    // ─── GET /api/admin/storage/files ─────────────────────────────────────────
    /// <summary>
    /// แสดงรายการไฟล์ใน MinIO bucket แบบ paginated
    /// รองรับ filter ด้วย prefix (path) และ extension
    /// </summary>
    [HttpGet("files")]
    public async Task<ActionResult<ApiResponse<StorageFileListDto>>> ListFiles(
        [FromQuery] string? prefix = null,
        [FromQuery] string? ext = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        try
        {
            var all = new List<StorageFileDto>();

            await foreach (var obj in _minio.ListObjectsEnumAsync(new ListObjectsArgs()
                .WithBucket(_bucket)
                .WithPrefix(prefix ?? "")
                .WithRecursive(true), ct))
            {
                // กรองตาม extension ถ้าระบุ
                if (!string.IsNullOrEmpty(ext))
                {
                    var objExt = Path.GetExtension(obj.Key).TrimStart('.').ToLowerInvariant();
                    if (!objExt.Equals(ext.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                all.Add(new StorageFileDto
                {
                    Key = obj.Key,
                    SizeBytes = (long)obj.Size,
                    SizeFormatted = FormatBytes((long)obj.Size),
                    Extension = Path.GetExtension(obj.Key).ToLowerInvariant(),
                    ContentType = GetContentType(obj.Key),
                    LastModified = obj.LastModifiedDateTime.HasValue
                        ? DateTime.SpecifyKind(obj.LastModifiedDateTime.Value, DateTimeKind.Utc)
                        : null,
                    PublicUrl = $"{_publicUrl.TrimEnd('/')}/{obj.Key}"
                });
            }

            var totalCount = all.Count;
            var paged = all
                .OrderByDescending(f => f.LastModified)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new ApiResponse<StorageFileListDto>(new StorageFileListDto
            {
                Files = paged,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = (page * pageSize) < totalCount
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files in prefix '{Prefix}'", prefix);
            return StatusCode(500, new ApiResponse("ไม่สามารถดึงรายการไฟล์ได้"));
        }
    }

    // ─── GET /api/admin/storage/presigned ─────────────────────────────────────
    /// <summary>
    /// สร้าง presigned URL สำหรับ download/preview ไฟล์
    /// URL มีอายุ 1 ชั่วโมง (สามารถปรับผ่าน query param)
    /// </summary>
    [HttpGet("presigned")]
    public async Task<ActionResult<ApiResponse<string>>> GetPresignedUrl(
        [FromQuery] string key,
        [FromQuery] int expiryMinutes = 60,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new ApiResponse("กรุณาระบุ key ของไฟล์"));

        if (expiryMinutes is < 1 or > 1440)
            expiryMinutes = 60;

        try
        {
            var url = await _storage.GeneratePresignedUrlAsync(key, TimeSpan.FromMinutes(expiryMinutes), ct);
            return Ok(new ApiResponse<string>(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for '{Key}'", key);
            return StatusCode(500, new ApiResponse("ไม่สามารถสร้าง presigned URL ได้"));
        }
    }

    // ─── DELETE /api/admin/storage/files ──────────────────────────────────────
    /// <summary>
    /// ลบไฟล์หนึ่งหรือหลายไฟล์จาก MinIO bucket
    /// รับ list ของ keys ที่ต้องการลบ
    /// </summary>
    [HttpDelete("files")]
    public async Task<ActionResult<ApiResponse>> DeleteFiles(
        [FromBody] BulkDeleteRequest request,
        CancellationToken ct)
    {
        if (request.Keys.Count == 0)
            return BadRequest(new ApiResponse("กรุณาระบุ keys ที่ต้องการลบ"));

        if (request.Keys.Count > 100)
            return BadRequest(new ApiResponse("ลบได้ไม่เกิน 100 ไฟล์ต่อครั้ง"));

        var deleted = 0;
        var failed = new List<string>();

        foreach (var key in request.Keys)
        {
            try
            {
                var success = await _storage.DeleteFileAsync(key, ct);
                if (success)
                    deleted++;
                else
                    failed.Add(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file '{Key}'", key);
                failed.Add(key);
            }
        }

        if (failed.Count > 0)
            return Ok(new ApiResponse(
                $"ลบสำเร็จ {deleted} ไฟล์, ล้มเหลว {failed.Count} ไฟล์",
                failed));

        return Ok(new ApiResponse { Success = true, Message = $"ลบสำเร็จ {deleted} ไฟล์" });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
}
