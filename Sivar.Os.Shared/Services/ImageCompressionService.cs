using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Implementation of image compression service using SixLabors.ImageSharp.
/// Optimizes images for web/mobile viewing while maintaining acceptable quality.
/// </summary>
public class ImageCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageCompressionService> _logger;

    public ImageCompressionService(ILogger<ImageCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<ImageCompressionResult> CompressImageAsync(
        Stream inputStream, 
        string fileName,
        int maxWidth = 1920,
        int maxHeight = 1920,
        int quality = 80)
    {
        var originalSize = inputStream.Length;
        _logger.LogInformation("[ImageCompression] START - FileName={FileName}, OriginalSize={Size}KB, MaxDimensions={MaxW}x{MaxH}, Quality={Quality}",
            fileName, originalSize / 1024, maxWidth, maxHeight, quality);

        try
        {
            // Reset stream position
            if (inputStream.CanSeek)
                inputStream.Position = 0;

            // Determine file extension
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            // ⭐ CRITICAL: Handle GIFs BEFORE loading with ImageSharp
            // ImageSharp's Image.LoadAsync consumes the stream, and for network streams
            // or non-seekable streams, we can't reset and re-read afterwards.
            // GIFs should be passed through unchanged to preserve animation.
            if (extension == ".gif")
            {
                _logger.LogInformation("[ImageCompression] GIF detected - copying directly without loading to preserve animation and stream integrity");
                
                // Copy the input stream to a new memory stream
                var gifOutputStream = new MemoryStream();
                await inputStream.CopyToAsync(gifOutputStream);
                gifOutputStream.Position = 0;
                
                _logger.LogInformation("[ImageCompression] GIF copied - OutputSize={Size} bytes", gifOutputStream.Length);
                
                return new ImageCompressionResult
                {
                    CompressedStream = gifOutputStream,
                    ContentType = "image/gif",
                    NewFileName = fileName,
                    OriginalSizeBytes = originalSize,
                    CompressedSizeBytes = gifOutputStream.Length
                };
            }

            // Load image (for non-GIF formats)
            using var image = await Image.LoadAsync(inputStream);
            
            var originalWidth = image.Width;
            var originalHeight = image.Height;
            
            _logger.LogInformation("[ImageCompression] Original dimensions: {Width}x{Height}", originalWidth, originalHeight);

            // Check if we need to resize
            bool needsResize = originalWidth > maxWidth || originalHeight > maxHeight;
            
            if (needsResize)
            {
                // Calculate new dimensions maintaining aspect ratio
                var ratioX = (double)maxWidth / originalWidth;
                var ratioY = (double)maxHeight / originalHeight;
                var ratio = Math.Min(ratioX, ratioY);
                
                var newWidth = (int)(originalWidth * ratio);
                var newHeight = (int)(originalHeight * ratio);
                
                _logger.LogInformation("[ImageCompression] Resizing from {OrigW}x{OrigH} to {NewW}x{NewH}",
                    originalWidth, originalHeight, newWidth, newHeight);
                
                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            // Determine output format - convert PNGs without transparency to JPEG for better compression
            // Note: extension was already determined before Image.LoadAsync
            var outputStream = new MemoryStream();
            string contentType;
            string newFileName;

            // For PNG, check if it has transparency - if not, convert to JPEG
            if (extension == ".png")
            {
                // Check for alpha channel usage
                bool hasTransparency = HasTransparency(image);
                
                if (hasTransparency)
                {
                    // Keep as PNG but optimize
                    _logger.LogInformation("[ImageCompression] PNG with transparency - keeping as PNG");
                    var pngEncoder = new PngEncoder
                    {
                        CompressionLevel = PngCompressionLevel.BestCompression,
                        FilterMethod = PngFilterMethod.Adaptive
                    };
                    await image.SaveAsPngAsync(outputStream, pngEncoder);
                    contentType = "image/png";
                    newFileName = fileName;
                }
                else
                {
                    // Convert to JPEG for better compression
                    _logger.LogInformation("[ImageCompression] PNG without transparency - converting to JPEG for better compression");
                    var jpegEncoder = new JpegEncoder { Quality = quality };
                    await image.SaveAsJpegAsync(outputStream, jpegEncoder);
                    contentType = "image/jpeg";
                    newFileName = Path.ChangeExtension(fileName, ".jpg");
                }
            }
            else
            {
                // JPEG, WEBP, etc. - save as JPEG with quality setting
                var jpegEncoder = new JpegEncoder { Quality = quality };
                await image.SaveAsJpegAsync(outputStream, jpegEncoder);
                contentType = "image/jpeg";
                newFileName = extension == ".jpg" || extension == ".jpeg" 
                    ? fileName 
                    : Path.ChangeExtension(fileName, ".jpg");
            }

            outputStream.Position = 0;
            var compressedSize = outputStream.Length;
            var compressionRatio = originalSize > 0 ? (1 - (double)compressedSize / originalSize) * 100 : 0;

            _logger.LogInformation("[ImageCompression] SUCCESS - OriginalSize={OrigKB}KB, CompressedSize={CompKB}KB, Saved={Saved:F1}%, ContentType={ContentType}",
                originalSize / 1024, compressedSize / 1024, compressionRatio, contentType);

            return new ImageCompressionResult
            {
                CompressedStream = outputStream,
                ContentType = contentType,
                NewFileName = newFileName,
                OriginalSizeBytes = originalSize,
                CompressedSizeBytes = compressedSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ImageCompression] ERROR - Failed to compress {FileName}", fileName);
            
            // Return original stream on error
            if (inputStream.CanSeek)
                inputStream.Position = 0;
            
            var fallbackStream = new MemoryStream();
            await inputStream.CopyToAsync(fallbackStream);
            fallbackStream.Position = 0;
            
            var contentType = GetContentTypeFromExtension(fileName);
            return new ImageCompressionResult
            {
                CompressedStream = fallbackStream,
                ContentType = contentType,
                NewFileName = fileName,
                OriginalSizeBytes = originalSize,
                CompressedSizeBytes = fallbackStream.Length
            };
        }
    }

    /// <summary>
    /// Check if image has any transparent pixels
    /// </summary>
    private bool HasTransparency(Image image)
    {
        try
        {
            // Quick check - if the pixel format doesn't support alpha, no transparency
            if (image.PixelType.BitsPerPixel <= 24)
                return false;

            // For performance, just sample some pixels rather than checking all
            // This is a heuristic - may not catch all cases but good enough for compression decisions
            return true; // Assume transparency for safety with 32-bit images
        }
        catch
        {
            return false;
        }
    }

    private string GetContentTypeFromExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }
}
