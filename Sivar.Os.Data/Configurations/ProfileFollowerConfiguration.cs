using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity configuration for ProfileFollower
/// </summary>
public class ProfileFollowerConfiguration : IEntityTypeConfiguration<ProfileFollower>
{
    public void Configure(EntityTypeBuilder<ProfileFollower> builder)
    {
        builder.ToTable("ProfileFollowers");
        
        builder.HasKey(pf => pf.Id);

        // Properties
        builder.Property(pf => pf.FollowerProfileId)
            .IsRequired();

        builder.Property(pf => pf.FollowedProfileId)
            .IsRequired();

        builder.Property(pf => pf.FollowedAt)
            .IsRequired();

        builder.Property(pf => pf.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(pf => pf.FollowerProfile)
            .WithMany() // A profile can follow many others
            .HasForeignKey(pf => pf.FollowerProfileId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        builder.HasOne(pf => pf.FollowedProfile)
            .WithMany() // A profile can be followed by many others
            .HasForeignKey(pf => pf.FollowedProfileId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Indexes
        builder.HasIndex(pf => new { pf.FollowerProfileId, pf.FollowedProfileId })
            .HasDatabaseName("IX_ProfileFollowers_FollowerProfile_FollowedProfile")
            .IsUnique(); // Prevent duplicate follow relationships

        builder.HasIndex(pf => pf.FollowerProfileId)
            .HasDatabaseName("IX_ProfileFollowers_FollowerProfileId");

        builder.HasIndex(pf => pf.FollowedProfileId)
            .HasDatabaseName("IX_ProfileFollowers_FollowedProfileId");

        builder.HasIndex(pf => pf.IsActive)
            .HasDatabaseName("IX_ProfileFollowers_IsActive");

        builder.HasIndex(pf => pf.FollowedAt)
            .HasDatabaseName("IX_ProfileFollowers_FollowedAt");

        // Check constraint to prevent self-following (PostgreSQL compatible syntax)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ProfileFollowers_NoSelfFollow", 
            "\"FollowerProfileId\" != \"FollowedProfileId\""));
    }
}