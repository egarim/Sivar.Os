using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Comment entity
/// </summary>
public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        
        // Primary key
        builder.HasKey(c => c.Id);
        
        // Properties
        builder.Property(c => c.Content)
            .HasMaxLength(2000)
            .IsRequired();
        
        // Relationships
        builder.HasOne(c => c.Profile)
            .WithMany()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete loops
            
        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete loops
            
        builder.HasMany(c => c.Reactions)
            .WithOne(r => r.Comment)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(c => c.PostId);
        builder.HasIndex(c => c.ProfileId);
        builder.HasIndex(c => c.ParentCommentId);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => new { c.PostId, c.CreatedAt });
    }
}