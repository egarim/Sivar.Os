using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Reaction entity
/// </summary>
public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Sivar_Reactions");
        
        // Primary key
        builder.HasKey(r => r.Id);
        
        // Properties
        builder.Property(r => r.ReactionType)
            .IsRequired();
        
        // Relationships
        builder.HasOne(r => r.Profile)
            .WithMany()
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(r => r.Post)
            .WithMany(p => p.Reactions)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(r => r.Comment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(r => r.ProfileId);
        builder.HasIndex(r => r.PostId);
        builder.HasIndex(r => r.CommentId);
        builder.HasIndex(r => r.ReactionType);
        
        // Unique indexes: one reaction per profile per post/comment
        // Note: Using separate unique indexes without filters for PostgreSQL compatibility
        builder.HasIndex(r => new { r.ProfileId, r.PostId })
            .IsUnique()
            .HasDatabaseName("IX_Reactions_ProfileId_PostId");
            
        builder.HasIndex(r => new { r.ProfileId, r.CommentId })
            .IsUnique()
            .HasDatabaseName("IX_Reactions_ProfileId_CommentId");
        
        // REMOVED: Check constraint - validation moved to application layer
        // For activity streams, we prefer flexible relationships over database constraints
        // Validation is handled in ReactionRepository.ValidateReactionAsync() and ReactionService
        
        // Note: If you need to enforce this rule, do it at the application level:
        // - ReactionRepository.ToggleReactionAsync validates before insert
        // - ReactionService enforces business rules
    }
}