using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Post entity
/// </summary>
public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        
        // Primary key
        builder.HasKey(p => p.Id);
        
        // Content and title
        builder.Property(p => p.Title)
            .HasMaxLength(200);
            
        builder.Property(p => p.Content)
            .HasMaxLength(5000)
            .IsRequired();
        
        // Post type
        builder.Property(p => p.PostType)
            .IsRequired();
        
        // JSON metadata fields
        builder.Property(p => p.PricingInfo)
            .HasMaxLength(1000);
            
        builder.Property(p => p.BusinessMetadata)
            .HasMaxLength(5000);
            
        builder.Property(p => p.Tags)
            .HasMaxLength(2000)
            .IsRequired();
            
        // Availability status
        builder.Property(p => p.AvailabilityStatus)
            .HasConversion<string>();
            
        // Statistics
        builder.Property(p => p.ViewCount)
            .HasDefaultValue(0);
            
        builder.Property(p => p.ShareCount)
            .HasDefaultValue(0);
            
        builder.Property(p => p.IsPinned)
            .HasDefaultValue(false);
            
        builder.Property(p => p.IsFeatured)
            .HasDefaultValue(false);
        
        // Location value object configuration
        builder.OwnsOne(p => p.Location, location =>
        {
            location.Property(l => l.City).HasMaxLength(100);
            location.Property(l => l.State).HasMaxLength(100);
            location.Property(l => l.Country).HasMaxLength(100);
            location.Property(l => l.Latitude).HasPrecision(18, 10);
            location.Property(l => l.Longitude).HasPrecision(18, 10);
        });
            
        // Relationships
        builder.HasOne(p => p.Profile)
            .WithMany()
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Reactions)
            .WithOne(r => r.Post)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Attachments)
            .WithOne(a => a.Post)
            .HasForeignKey(a => a.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(p => p.ProfileId);
        builder.HasIndex(p => p.PostType);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => new { p.ProfileId, p.PostType });
    }
}