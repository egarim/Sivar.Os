using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for SearchResult entity
/// </summary>
public class SearchResultConfiguration : IEntityTypeConfiguration<SearchResult>
{
    public void Configure(EntityTypeBuilder<SearchResult> builder)
    {
        // Table name
        builder.ToTable("Sivar_SearchResults");

        // Primary key
        builder.HasKey(sr => sr.Id);

        // Properties
        builder.Property(sr => sr.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(sr => sr.ChatMessageId)
            .IsRequired();

        builder.Property(sr => sr.ResultType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(sr => sr.MatchSource)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(sr => sr.RelevanceScore)
            .IsRequired();

        builder.Property(sr => sr.SemanticScore);
        builder.Property(sr => sr.FullTextRank);
        builder.Property(sr => sr.DistanceKm);
        builder.Property(sr => sr.DisplayOrder).IsRequired();

        // Business/Profile Data
        builder.Property(sr => sr.PostId);
        builder.Property(sr => sr.ProfileId);

        builder.Property(sr => sr.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sr => sr.Description)
            .HasMaxLength(500);

        builder.Property(sr => sr.Handle)
            .HasMaxLength(100);

        builder.Property(sr => sr.Category)
            .HasMaxLength(50);

        builder.Property(sr => sr.SubCategory)
            .HasMaxLength(50);

        builder.Property(sr => sr.ImageUrl)
            .HasMaxLength(500);

        // Location Data
        builder.Property(sr => sr.City)
            .HasMaxLength(100);

        builder.Property(sr => sr.Department)
            .HasMaxLength(100);

        builder.Property(sr => sr.Address)
            .HasMaxLength(300);

        builder.Property(sr => sr.Latitude);
        builder.Property(sr => sr.Longitude);

        // Business Details
        builder.Property(sr => sr.Phone)
            .HasMaxLength(50);

        builder.Property(sr => sr.Website)
            .HasMaxLength(300);

        builder.Property(sr => sr.WorkingHours)
            .HasMaxLength(200);

        builder.Property(sr => sr.PriceRange)
            .HasMaxLength(10);

        builder.Property(sr => sr.Rating);
        builder.Property(sr => sr.ReviewCount);

        // Event Data
        builder.Property(sr => sr.EventDate);
        builder.Property(sr => sr.EventEndDate);

        builder.Property(sr => sr.Venue)
            .HasMaxLength(200);

        builder.Property(sr => sr.TicketPrice)
            .HasMaxLength(50);

        // Procedure Data
        builder.Property(sr => sr.Requirements)
            .HasColumnType("jsonb");

        builder.Property(sr => sr.ProcessingTime)
            .HasMaxLength(100);

        builder.Property(sr => sr.Cost)
            .HasMaxLength(100);

        builder.Property(sr => sr.WhereToGo)
            .HasMaxLength(200);

        builder.Property(sr => sr.OnlineUrl)
            .HasMaxLength(300);

        // Tags and Metadata
        builder.Property(sr => sr.Tags)
            .HasColumnType("text[]");

        builder.Property(sr => sr.Metadata)
            .HasColumnType("jsonb");

        // Base entity properties
        builder.Property(sr => sr.CreatedAt)
            .IsRequired();

        builder.Property(sr => sr.UpdatedAt)
            .IsRequired();

        builder.Property(sr => sr.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        // Note: ChatMessage has composite PK (Id, CreatedAt) for TimescaleDB hypertable
        // Use HasPrincipalKey to reference the alternate key (Id) instead of composite PK
        builder.HasOne(sr => sr.ChatMessage)
            .WithMany(cm => cm.SearchResults)
            .HasForeignKey(sr => sr.ChatMessageId)
            .HasPrincipalKey(cm => cm.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: Post has composite PK (Id, CreatedAt) for TimescaleDB hypertable
        // Use HasPrincipalKey to reference the alternate key (Id) instead of composite PK
        builder.HasOne(sr => sr.Post)
            .WithMany()
            .HasForeignKey(sr => sr.PostId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(sr => sr.Profile)
            .WithMany()
            .HasForeignKey(sr => sr.ProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(sr => sr.ChatMessageId);
        builder.HasIndex(sr => sr.ResultType);
        builder.HasIndex(sr => sr.PostId);
        builder.HasIndex(sr => sr.ProfileId);
        builder.HasIndex(sr => new { sr.ChatMessageId, sr.DisplayOrder });

        // Global query filter for soft delete
        builder.HasQueryFilter(sr => !sr.IsDeleted);
    }
}
