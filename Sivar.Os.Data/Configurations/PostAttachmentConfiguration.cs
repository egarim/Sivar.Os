using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PostAttachment entity
/// </summary>
public class PostAttachmentConfiguration : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.ToTable("PostAttachments");
        
        // Primary key
        builder.HasKey(pa => pa.Id);
        
        // Properties
        builder.Property(pa => pa.AttachmentType)
            .IsRequired();
            
        builder.Property(pa => pa.Url)
            .HasMaxLength(1000)
            .IsRequired();
            
        builder.Property(pa => pa.Title)
            .HasMaxLength(500);
            
        builder.Property(pa => pa.Description)
            .HasMaxLength(1000);
            
        builder.Property(pa => pa.MimeType)
            .HasMaxLength(100);
            
        builder.Property(pa => pa.OriginalFileName)
            .HasMaxLength(255);
            
        builder.Property(pa => pa.ThumbnailUrl)
            .HasMaxLength(1000);
            
        builder.Property(pa => pa.DisplayOrder)
            .HasDefaultValue(0);
            
        builder.Property(pa => pa.LinkMetadata)
            .HasMaxLength(2000);
        
        // Relationships
        builder.HasOne(pa => pa.Post)
            .WithMany(p => p.Attachments)
            .HasForeignKey(pa => pa.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(pa => pa.PostId);
        builder.HasIndex(pa => pa.AttachmentType);
        builder.HasIndex(pa => new { pa.PostId, pa.DisplayOrder });
    }
}