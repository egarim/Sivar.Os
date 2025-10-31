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
        
        // Vector embedding configuration (Phase 5: pgvector for Semantic Search)
        builder.Property(p => p.ContentEmbedding)
            .HasColumnType("vector(384)") // 384 dimensions for all-minilm model
            .HasConversion(
                // To database: string "[0.1,0.2,...]" -> needs ::vector cast (handled by interceptor)
                v => v,
                // From database: vector -> string
                v => v
            );
        
        // HNSW index for fast similarity search
        builder.HasIndex(p => p.ContentEmbedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("IX_Posts_ContentEmbedding_Hnsw");
        
        // Full-text search configuration (Phase 3: PostgreSQL Full-Text Search)
        // Dual-column approach for multi-language support
        
        // Note: These properties are ignored in EF Core model and added as database-generated columns
        // They will be created via OnModelCreating using raw SQL or manually in the database
        builder.Ignore(p => p.SearchVector);
        builder.Ignore(p => p.SearchVectorSimple);
    }
}