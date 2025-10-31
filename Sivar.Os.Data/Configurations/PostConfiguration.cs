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
        builder.ToTable("Sivar_Posts");
        
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
        
        // JSON metadata fields - using PostgreSQL JSONB for better performance
        builder.Property(p => p.PricingInfo)
            .HasColumnType("jsonb")
            .HasMaxLength(1000);
            
        builder.Property(p => p.BusinessMetadata)
            .HasColumnType("jsonb")
            .HasMaxLength(5000);
            
        builder.Property(p => p.Tags)
            .HasColumnType("jsonb")
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
        
        // GIN indexes for JSONB columns (Phase 2: PostgreSQL optimization)
        builder.HasIndex(p => p.BusinessMetadata)
            .HasMethod("gin")
            .HasDatabaseName("IX_Posts_BusinessMetadata_Gin");
        
        builder.HasIndex(p => p.PricingInfo)
            .HasMethod("gin")
            .HasDatabaseName("IX_Posts_PricingInfo_Gin");
        
        builder.HasIndex(p => p.Tags)
            .HasMethod("gin")
            .HasDatabaseName("IX_Posts_Tags_Gin");
        
        // Full-text search configuration (Phase 3: PostgreSQL Full-Text Search)
        // Dual-column approach for multi-language support
        
        // Language-specific search vector - uses Post.Language for accurate stemming
        builder.Property(p => p.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                @"to_tsvector(
                    CASE 
                        WHEN ""Language"" = 'en' THEN 'english'::regconfig
                        WHEN ""Language"" = 'es' THEN 'spanish'::regconfig
                        WHEN ""Language"" = 'fr' THEN 'french'::regconfig
                        WHEN ""Language"" = 'de' THEN 'german'::regconfig
                        WHEN ""Language"" = 'pt' THEN 'portuguese'::regconfig
                        WHEN ""Language"" = 'it' THEN 'italian'::regconfig
                        WHEN ""Language"" = 'nl' THEN 'dutch'::regconfig
                        WHEN ""Language"" = 'ru' THEN 'russian'::regconfig
                        WHEN ""Language"" = 'sv' THEN 'swedish'::regconfig
                        WHEN ""Language"" = 'no' THEN 'norwegian'::regconfig
                        WHEN ""Language"" = 'da' THEN 'danish'::regconfig
                        WHEN ""Language"" = 'fi' THEN 'finnish'::regconfig
                        WHEN ""Language"" = 'tr' THEN 'turkish'::regconfig
                        WHEN ""Language"" = 'ro' THEN 'romanian'::regconfig
                        WHEN ""Language"" = 'ar' THEN 'arabic'::regconfig
                        ELSE 'simple'::regconfig
                    END,
                    coalesce(""Title"", '') || ' ' || ""Content""
                )", 
                stored: true);
        
        // Universal/simple search vector - works for ALL languages (no stemming)
        builder.Property(p => p.SearchVectorSimple)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('simple', coalesce(\"Title\", '') || ' ' || \"Content\")", 
                stored: true);
        
        // GIN indexes for both search vectors
        builder.HasIndex(p => p.SearchVector)
            .HasMethod("gin")
            .HasDatabaseName("IX_Posts_SearchVector_Gin");
        
        builder.HasIndex(p => p.SearchVectorSimple)
            .HasMethod("gin")
            .HasDatabaseName("IX_Posts_SearchVectorSimple_Gin");
    }
}