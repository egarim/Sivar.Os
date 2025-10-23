using Sivar.Os.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Media attachment for posts (images, videos, links, documents)
/// </summary>
public class PostAttachment : BaseEntity
{
    /// <summary>
    /// The post this attachment belongs to
    /// </summary>
    public virtual Guid PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    /// <summary>
    /// Type of attachment
    /// </summary>
    public virtual AttachmentType AttachmentType { get; set; }

    /// <summary>
    /// File ID from the file storage service (for uploaded files)
    /// </summary>
    [StringLength(255)]
    public virtual string? FileId { get; set; }

    /// <summary>
    /// URL or path to the attachment
    /// </summary>
    [Required]
    [StringLength(1000)]
    public virtual string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional title or caption for the attachment
    /// </summary>
    [StringLength(500)]
    public virtual string? Title { get; set; }

    /// <summary>
    /// Optional description or alt text
    /// </summary>
    [StringLength(1000)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [StringLength(100)]
    public virtual string? MimeType { get; set; }

    /// <summary>
    /// File size in bytes (for uploaded files)
    /// </summary>
    public virtual long? FileSizeBytes { get; set; }

    /// <summary>
    /// Original filename (for uploaded files)
    /// </summary>
    [StringLength(255)]
    public virtual string? OriginalFileName { get; set; }

    /// <summary>
    /// Thumbnail URL for images and videos
    /// </summary>
    [StringLength(1000)]
    public virtual string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Display order for multiple attachments
    /// </summary>
    public virtual int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Duration in seconds (for video/audio files)
    /// </summary>
    public virtual int? DurationSeconds { get; set; }

    /// <summary>
    /// Image dimensions (for images)
    /// </summary>
    public virtual int? Width { get; set; }
    public virtual int? Height { get; set; }

    /// <summary>
    /// External link metadata (for link attachments)
    /// </summary>
    public virtual string? LinkMetadata { get; set; } // JSON: { title, description, favicon, etc. }
}