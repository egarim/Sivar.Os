using Microsoft.EntityFrameworkCore;
using PhotoBooking.Shared.Entities;

namespace PhotoBooking.Data;

public class PhotoBookingDbContext : DbContext
{
    static PhotoBookingDbContext()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public PhotoBookingDbContext(DbContextOptions<PhotoBookingDbContext> options) : base(options)
    {
    }

    public DbSet<BusinessProfile> BusinessProfiles { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<ServiceAvailability> ServiceAvailabilities { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BusinessProfile configuration
        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.ToTable("BusinessProfiles");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.Phone);
            entity.HasIndex(e => e.WhatsAppNumber);
            
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(2).IsRequired();
        });

        // Service configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Services");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.BusinessProfileId, e.IsActive });
            entity.HasIndex(e => e.Category);
            
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameEs).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.PricingType).HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(100);
            
            entity.Property(e => e.PhotoUrls)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            entity.HasOne(e => e.BusinessProfile)
                .WithMany(b => b.Services)
                .HasForeignKey(e => e.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceAvailability configuration
        modelBuilder.Entity<ServiceAvailability>(entity =>
        {
            entity.ToTable("ServiceAvailabilities");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.ServiceId, e.DayOfWeek });
            
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Availability)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.BusinessProfileId, e.BookingDate });
            entity.HasIndex(e => new { e.ServiceId, e.Status });
            entity.HasIndex(e => e.CustomerPhone);
            entity.HasIndex(e => e.WhatsAppConversationId);
            
            entity.Property(e => e.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CustomerPhone).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CustomerEmail).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BusinessProfile)
                .WithMany(b => b.Bookings)
                .HasForeignKey(e => e.BusinessProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
