using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Media;

namespace KMS.Infrastructure.Services.Media;

public class ImageSharpMediaProcessor : IMediaProcessor
{
    private readonly ILogger<ImageSharpMediaProcessor> _logger;
    private readonly IFileStorageService _fileStorageService;

    public ImageSharpMediaProcessor(
        ILogger<ImageSharpMediaProcessor> logger,
        IFileStorageService fileStorageService)
    {
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    public async Task<bool> ProcessImageAsync(MediaItem mediaItem, byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing image: {FileName} ({MimeType})", mediaItem.FileName, mediaItem.MimeType);

            // Check if it's an image
            if (!IsImageMimeType(mediaItem.MimeType))
            {
                _logger.LogWarning("File is not an image: {MimeType}", mediaItem.MimeType);
                return false;
            }

            // Load the image
            using var image = await Image.LoadAsync(new MemoryStream(imageData), cancellationToken);

            // Generate conversions based on collection
            var conversions = await GenerateConversionsAsync(mediaItem, image, cancellationToken);

            // Update media item with conversions metadata
            UpdateMediaItemWithConversions(mediaItem, conversions);

            _logger.LogInformation("Successfully processed image with {Count} conversions", conversions.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image: {FileName}", mediaItem.FileName);
            return false;
        }
    }

    public async Task<byte[]> GenerateThumbnailAsync(byte[] imageData, ThumbnailSize size, CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync(new MemoryStream(imageData), cancellationToken);
            return await GenerateThumbnailAsync(image, size, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail");
            throw;
        }
    }

    public async Task<byte[]> ConvertToWebPAsync(byte[] imageData, int quality = 80, CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync(new MemoryStream(imageData), cancellationToken);
            return await ConvertToWebPAsync(image, quality, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert image to WebP");
            throw;
        }
    }

    public async Task<byte[]> ResizeImageAsync(byte[] imageData, int width, int height, CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync(new MemoryStream(imageData), cancellationToken);
            return await ResizeImageAsync(image, width, height, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize image");
            throw;
        }
    }

    public bool IsImageMimeType(string mimeType)
    {
        return mimeType.StartsWith("image/");
    }

    public bool SupportsFormat(string mimeType)
    {
        var supportedFormats = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/webp"
        };

        return supportedFormats.Contains(mimeType.ToLowerInvariant());
    }

    private async Task<List<MediaConversion>> GenerateConversionsAsync(MediaItem mediaItem, Image image, CancellationToken cancellationToken)
    {
        var conversions = new List<MediaConversion>();

        // Determine conversions based on collection
        var collectionConversions = GetConversionsForCollection(mediaItem.CollectionName);

        foreach (var conversion in collectionConversions)
        {
            try
            {
                var convertedImage = await GenerateConversionAsync(image, conversion, cancellationToken);
                
                // Save the converted image
                var conversionPath = await SaveConversionAsync(
                    mediaItem,
                    convertedImage.Data,
                    conversion.Name,
                    convertedImage.Format,
                    cancellationToken);

                conversions.Add(new MediaConversion
                {
                    Name = conversion.Name,
                    Path = conversionPath,
                    Width = convertedImage.Width,
                    Height = convertedImage.Height,
                    Size = convertedImage.Data.Length,
                    Format = convertedImage.Format,
                    Quality = conversion.Quality
                });

                _logger.LogDebug("Generated conversion: {Name} ({Width}x{Height}, {Format})",
                    conversion.Name, convertedImage.Width, convertedImage.Height, convertedImage.Format);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate conversion {Name} for {FileName}",
                    conversion.Name, mediaItem.FileName);
            }
        }

        return conversions;
    }

    private async Task<ConvertedImage> GenerateConversionAsync(Image originalImage, ConversionDefinition conversion, CancellationToken cancellationToken)
    {
        // Clone the image to avoid modifying the original
        using var image = originalImage.CloneAs<Rgba32>();

        // Apply resize
        if (conversion.Width > 0 && conversion.Height > 0)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(conversion.Width, conversion.Height),
                Mode = ResizeMode.Max,
                Compand = true
            }));
        }

        // Determine output format and encoder
        byte[] outputData;
        string format;

        if (conversion.Format == "webp")
        {
            var encoder = new WebpEncoder
            {
                Quality = conversion.Quality,
                Method = WebpEncodingMethod.Default
            };

            using var ms = new MemoryStream();
            await image.SaveAsync(ms, encoder, cancellationToken);
            outputData = ms.ToArray();
            format = "webp";
        }
        else if (conversion.Format == "jpeg" || conversion.Format == "jpg")
        {
            var encoder = new JpegEncoder
            {
                Quality = conversion.Quality
            };

            using var ms = new MemoryStream();
            await image.SaveAsync(ms, encoder, cancellationToken);
            outputData = ms.ToArray();
            format = "jpeg";
        }
        else // Default to PNG
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.DefaultCompression
            };

            using var ms = new MemoryStream();
            await image.SaveAsync(ms, encoder, cancellationToken);
            outputData = ms.ToArray();
            format = "png";
        }

        return new ConvertedImage
        {
            Data = outputData,
            Width = image.Width,
            Height = image.Height,
            Format = format
        };
    }

    private async Task<string> SaveConversionAsync(MediaItem mediaItem, byte[] data, string conversionName, string format, CancellationToken cancellationToken)
    {
        var originalPath = mediaItem.Path;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(mediaItem.FileName);
        var conversionFileName = $"{fileNameWithoutExt}_{conversionName}.{format}";

        // Create conversion path by appending conversion name to original directory
        var originalDir = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var conversionPath = Path.Combine(originalDir, conversionFileName).Replace('\\', '/');

        await _fileStorageService.UploadFileAsync(
            data,
            conversionFileName,
            GetMimeTypeForFormat(format),
            originalDir,
            cancellationToken);

        return conversionPath;
    }

    private void UpdateMediaItemWithConversions(MediaItem mediaItem, List<MediaConversion> conversions)
    {
        // Update generated conversions
        var generatedConversions = new JsonObject();
        foreach (var conversion in conversions)
        {
            generatedConversions[conversion.Name] = true;
        }
        mediaItem.GeneratedConversions = generatedConversions.ToJsonString();

        // Update responsive images
        var responsiveImages = new JsonObject();
        foreach (var conversion in conversions.Where(c => c.Name.StartsWith("thumb_")))
        {
            responsiveImages[conversion.Name] = new JsonObject
            {
                ["width"] = conversion.Width,
                ["height"] = conversion.Height,
                ["url"] = $"/conversions/{Path.GetFileName(conversion.Path)}"
            };
        }
        mediaItem.ResponsiveImages = responsiveImages.ToJsonString();

        // Update manipulations
        var manipulations = JsonSerializer.Deserialize<JsonObject>(mediaItem.Manipulations) ?? new JsonObject();
        manipulations["processed"] = true;
        manipulations["processed_at"] = DateTime.UtcNow.ToString("o");
        mediaItem.Manipulations = manipulations.ToJsonString();
    }

    private List<ConversionDefinition> GetConversionsForCollection(string collectionName)
    {
        return collectionName.ToLowerInvariant() switch
        {
            "cover" => GetCoverConversions(),
            "attachments" => GetAttachmentConversions(),
            "avatar" => GetAvatarConversions(),
            _ => GetDefaultConversions()
        };
    }

    private List<ConversionDefinition> GetCoverConversions()
    {
        return new List<ConversionDefinition>
        {
            new() { Name = "thumb_small", Width = 400, Height = 300, Format = "webp", Quality = 80 },
            new() { Name = "thumb_medium", Width = 800, Height = 450, Format = "webp", Quality = 85 },
            new() { Name = "thumb_large", Width = 1200, Height = 630, Format = "webp", Quality = 90 },
            new() { Name = "og", Width = 1200, Height = 630, Format = "jpeg", Quality = 90 },
            new() { Name = "card", Width = 400, Height = 300, Format = "jpeg", Quality = 85 }
        };
    }

    private List<ConversionDefinition> GetAttachmentConversions()
    {
        return new List<ConversionDefinition>
        {
            new() { Name = "thumb", Width = 300, Height = 300, Format = "webp", Quality = 75 },
            new() { Name = "preview", Width = 800, Height = 600, Format = "webp", Quality = 80 }
        };
    }

    private List<ConversionDefinition> GetAvatarConversions()
    {
        return new List<ConversionDefinition>
        {
            new() { Name = "thumb_small", Width = 64, Height = 64, Format = "webp", Quality = 80 },
            new() { Name = "thumb_medium", Width = 128, Height = 128, Format = "webp", Quality = 85 },
            new() { Name = "thumb_large", Width = 256, Height = 256, Format = "webp", Quality = 90 },
            new() { Name = "profile", Width = 512, Height = 512, Format = "jpeg", Quality = 90 }
        };
    }

    private List<ConversionDefinition> GetDefaultConversions()
    {
        return new List<ConversionDefinition>
        {
            new() { Name = "thumb", Width = 300, Height = 300, Format = "webp", Quality = 75 }
        };
    }

    private async Task<byte[]> GenerateThumbnailAsync(Image image, ThumbnailSize size, CancellationToken cancellationToken)
    {
        var (width, height) = size switch
        {
            ThumbnailSize.Small => (400, 300),
            ThumbnailSize.Medium => (800, 450),
            ThumbnailSize.Large => (1200, 630),
            _ => (400, 300)
        };

        using var clonedImage = image.CloneAs<Rgba32>();
        clonedImage.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max,
            Compand = true
        }));

        using var ms = new MemoryStream();
        await clonedImage.SaveAsync(ms, new WebpEncoder { Quality = 80 }, cancellationToken);
        return ms.ToArray();
    }

    private async Task<byte[]> ConvertToWebPAsync(Image image, int quality, CancellationToken cancellationToken)
    {
        using var clonedImage = image.CloneAs<Rgba32>();
        using var ms = new MemoryStream();
        await clonedImage.SaveAsync(ms, new WebpEncoder { Quality = quality }, cancellationToken);
        return ms.ToArray();
    }

    private async Task<byte[]> ResizeImageAsync(Image image, int width, int height, CancellationToken cancellationToken)
    {
        using var clonedImage = image.CloneAs<Rgba32>();
        clonedImage.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max,
            Compand = true
        }));

        using var ms = new MemoryStream();
        await clonedImage.SaveAsync(ms, image.Metadata.DecodedImageFormat!, cancellationToken);
        return ms.ToArray();
    }

    private string GetMimeTypeForFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    // Helper classes
    private class ConversionDefinition
    {
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = "webp";
        public int Quality { get; set; } = 80;
    }

    private class ConvertedImage
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
    }

    private class MediaConversion
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public long Size { get; set; }
        public string Format { get; set; } = string.Empty;
        public int Quality { get; set; }
    }
}