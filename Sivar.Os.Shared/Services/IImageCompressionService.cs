namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for compressing and resizing images before upload.
/// Optimizes images for web/mobile viewing while maintaining acceptable quality.
/// </summary>
public interface IImageCompressionService
{
    /// <summary>
    /// Compresses an image stream, resizing if necessary and optimizing for web.
    /// </summary>
    /// <param name="inputStream">Original image stream</param>
    /// <param name="fileName">Original filename (used to determine format)</param>
    /// <param name="maxWidth">Maximum width (default 1920px for full HD)</param>
    /// <param name="maxHeight">Maximum height (default 1920px)</param>
    /// <param name="quality">JPEG/WebP quality 1-100 (default 80 - good balance)</param>
    /// <returns>Compressed image stream and the output content type</returns>
    Task<ImageCompressionResult> CompressImageAsync(
        Stream inputStream, 
        string fileName,
        int maxWidth = 1920,
        int maxHeight = 1920,
        int quality = 80);
}

/// <summary>
/// Result of image compression operation
/// </summary>
public class ImageCompressionResult
{
    public required MemoryStream CompressedStream { get; set; }
    public required string ContentType { get; set; }
    public required string NewFileName { get; set; }
    public long OriginalSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public double CompressionRatio => OriginalSizeBytes > 0 
        ? (1 - (double)CompressedSizeBytes / OriginalSizeBytes) * 100 
        : 0;
}
