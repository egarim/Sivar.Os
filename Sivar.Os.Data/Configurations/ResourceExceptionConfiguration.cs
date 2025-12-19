using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for ResourceException entity
/// </summary>
public class ResourceExceptionConfiguration : IEntityTypeConfiguration<ResourceException>
{
    public void Configure(EntityTypeBuilder<ResourceException> builder)
    {
        builder.ToTable("Sivar_ResourceExceptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.Property(e => e.IsRecurringAnnually)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(e => e.Resource)
            .WithMany(r => r.Exceptions)
            .HasForeignKey(e => e.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.ResourceId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => new { e.ResourceId, e.Date });
        builder.HasIndex(e => new { e.ResourceId, e.IsRecurringAnnually });
    }
}
