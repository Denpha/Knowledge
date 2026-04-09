namespace KMS.Domain.Entities.Media;

public class MediaItem : BaseEntity
{
    public string ModelType { get; set; } = string.Empty;
    public Guid ModelId { get; set; }
    
    public string CollectionName { get; set; } = "default";
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    
    public string Disk { get; set; } = "local";
    public string? ConversionsDisk { get; set; }
    public string Path { get; set; } = string.Empty;
    
    public long Size { get; set; }
    public string Manipulations { get; set; } = "{}"; // JSON
    public string CustomProperties { get; set; } = "{}"; // JSON
    public string GeneratedConversions { get; set; } = "{}"; // JSON
    public string ResponsiveImages { get; set; } = "{}"; // JSON
    
    public int? OrderColumn { get; set; }
    
    public Guid UploadedById { get; set; }
    
    // Navigation properties
    public virtual Identity.AppUser UploadedBy { get; set; } = null!;
}