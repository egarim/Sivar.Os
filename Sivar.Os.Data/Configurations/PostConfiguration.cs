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
            
        // Tags - using PostgreSQL array for better performance (Phase 4)
        builder.Property(p => p.Tags)
            .HasColumnType("text[]")
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
        
        // Blog-specific fields
        builder.Property(p => p.BlogContent)
            .HasColumnType("text"); // Unlimited length for long-form content
            
        builder.Property(p => p.CoverImageUrl)
            .HasMaxLength(500);
            
        builder.Property(p => p.CoverImageFileId)
            .HasMaxLength(255);
            
        builder.Property(p => p.Subtitle)
            .HasMaxLength(500);
            
        builder.Property(p => p.CanonicalUrl)
            .HasMaxLength(500);
            
        builder.Property(p => p.IsDraft)
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
        
        // Blog-specific indexes
        builder.HasIndex(p => new { p.IsDraft, p.ProfileId })
            .HasDatabaseName("IX_Posts_Blog_Drafts");
        builder.HasIndex(p => p.PublishedAt)
            .HasDatabaseName("IX_Posts_Blog_PublishedAt");
        
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
        
        // Vector embedding configuration (Phase 5: pgvector for Semantic Search)
        // CRITICAL: ContentEmbedding MUST be IGNORED by EF Core due to EF Core 9.0 incompatibility
        // EF Core 9.0 cannot properly handle pgvector types - it sends character varying instead of vector
        // Column is created manually in database via SQL script (see ConvertContentEmbeddingToVector.sql)
        // Updates are done via raw SQL in PostRepository.UpdateContentEmbeddingAsync()
        builder.Ignore(p => p.ContentEmbedding);
        
        // HNSW index for fast similarity search
        // Note: Both column and index are created manually via ConvertContentEmbeddingToVector.sql
        // CREATE INDEX IF NOT EXISTS IX_Posts_ContentEmbedding_Hnsw ON "Sivar_Posts" USING hnsw ("ContentEmbedding" vector_cosine_ops);
        
        // Full-text search configuration (Phase 3: PostgreSQL Full-Text Search)
        // Dual-column approach for multi-language support
        
        // SearchVector: Language-aware full-text search (with stemming)
        // SearchVectorSimple: Language-agnostic full-text search (no stemming)
        // Both columns are created as GENERATED ALWAYS AS ... STORED via SQL script
        // See: Sivar.Os.Data/Scripts/AddFullTextSearchColumns.sql
        
        // Note: These columns are database-generated, EF Core should ignore them in inserts/updates
        // but can read them. We ignore them to prevent EF Core from trying to manage them.
        builder.Ignore(p => p.SearchVector);
        builder.Ignore(p => p.SearchVectorSimple);
        
        // ⚠️ CRITICAL: PostGIS columns - IGNORED by EF Core (following pgvector pattern)
        // These columns exist in database but are managed via raw SQL only
        // Reason: NetTopologySuite types incompatible with EF Core 9.0
        // See: Database/Scripts/003_AddPostGISLocationSupport.sql
        builder.Ignore(p => p.GeoLocation);
        builder.Ignore(p => p.GeoLocationUpdatedAt);
        builder.Ignore(p => p.GeoLocationSource);
        
        // GIN indexes are created via SQL script for performance
        // CREATE INDEX IX_Posts_SearchVector_Gin ON "Sivar_Posts" USING gin("SearchVector");
        // CREATE INDEX IX_Posts_SearchVectorSimple_Gin ON "Sivar_Posts" USING gin("SearchVectorSimple");
    }
}