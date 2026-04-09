namespace KMS.Application.DTOs.Media;

public class MediaItemDto : BaseDto
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MediaType { get; set; } = string.Empty; // Image, Document, Video, Audio

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AltText { get; set; }

    public string? CollectionName { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }

    public bool IsPublic { get; set; } = true;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Duration { get; set; } // in seconds

    public Guid UploaderId { get; set; }
    public string UploaderName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

public class CreateMediaItemDto : CreateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AltText { get; set; }
    public string? CollectionName { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class UpdateMediaItemDto : UpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AltText { get; set; }
    public bool? IsPublic { get; set; }
}

public class UploadMediaItemDto : CreateMediaItemDto
{
    // Note: IFormFile is not included here as it's a web-specific dependency
    // File handling should be done in the API layer
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public byte[]? FileData { get; set; }
}

public class MediaSearchParams : SearchParams
{
    public string? MediaType { get; set; }
    public string? CollectionName { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? UploaderId { get; set; }
    public bool? IsPublic { get; set; }
    public DateTime? UploadedFrom { get; set; }
    public DateTime? UploadedTo { get; set; }
}