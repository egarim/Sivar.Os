using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for CategoryDefinition entity
/// Supports multilingual search with English-First Query Pattern
/// </summary>
public class CategoryDefinitionConfiguration : IEntityTypeConfiguration<CategoryDefinition>
{
    public void Configure(EntityTypeBuilder<CategoryDefinition> builder)
    {
        builder.ToTable("Sivar_CategoryDefinitions");
        
        // Primary key
        builder.HasKey(c => c.Id);
        
        // Key - unique normalized English identifier
        builder.Property(c => c.Key)
            .HasMaxLength(100)
            .IsRequired();
        
        // Unique index on Key for fast lookups
        builder.HasIndex(c => c.Key)
            .IsUnique()
            .HasDatabaseName("IX_CategoryDefinitions_Key");
        
        // Display names
        builder.Property(c => c.DisplayNameEn)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(c => c.DisplayNameEs)
            .HasMaxLength(200)
            .IsRequired();
        
        // Parent key for hierarchical categories
        builder.Property(c => c.ParentKey)
            .HasMaxLength(100);
        
        // Index on ParentKey for hierarchy lookups
        builder.HasIndex(c => c.ParentKey)
            .HasDatabaseName("IX_CategoryDefinitions_ParentKey");
        
        // Synonyms - PostgreSQL text[] array with GIN index for fast containment queries
        builder.Property(c => c.Synonyms)
            .HasColumnType("text[]")
            .IsRequired();
        
        // GIN index on Synonyms for efficient ANY() queries
        builder.HasIndex(c => c.Synonyms)
            .HasMethod("gin")
            .HasDatabaseName("IX_CategoryDefinitions_Synonyms_GIN");
        
        // Description
        builder.Property(c => c.Description)
            .HasMaxLength(500);
        
        // IsActive flag
        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);
        
        // Sort order
        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0);
        
        // Index for active categories sorted by order
        builder.HasIndex(c => new { c.IsActive, c.SortOrder })
            .HasDatabaseName("IX_CategoryDefinitions_Active_SortOrder");
    }
}
