using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for ProfileBookmark entity
/// </summary>
public class ProfileBookmarkConfiguration : IEntityTypeConfiguration<ProfileBookmark>
{
    public void Configure(EntityTypeBuilder<ProfileBookmark> builder)
    {
        builder.ToTable("Sivar_ProfileBookmarks");

        builder.HasKey(b => b.Id);

        // Unique constraint: one bookmark per profile-post combination
        builder.HasIndex(b => new { b.ProfileId, b.PostId })
            .IsUnique()
            .HasDatabaseName("IX_ProfileBookmarks_ProfileId_PostId");

        // Index for querying bookmarks by profile
        builder.HasIndex(b => b.ProfileId)
            .HasDatabaseName("IX_ProfileBookmarks_ProfileId");

        // Index for querying bookmarks by post
        builder.HasIndex(b => b.PostId)
            .HasDatabaseName("IX_ProfileBookmarks_PostId");

        // Index for recent bookmarks
        builder.HasIndex(b => b.CreatedAt)
            .HasDatabaseName("IX_ProfileBookmarks_CreatedAt");

        // Relationships
        builder.HasOne(b => b.Profile)
            .WithMany()
            .HasForeignKey(b => b.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Post)
            .WithMany()
            .HasForeignKey(b => b.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Property configurations
        builder.Property(b => b.Note)
            .HasMaxLength(500);
    }
}
