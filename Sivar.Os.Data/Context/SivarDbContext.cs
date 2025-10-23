using Microsoft.EntityFrameworkCore;

using Sivar.Os.Data.Configurations;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Context;

/// <summary>
/// Main database context for the Sivar application
/// </summary>
public class SivarDbContext : DbContext
{
    static SivarDbContext()
    {
        // Enable legacy timestamp behavior for PostgreSQL
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public SivarDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Users in the system
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Available profile types
    /// </summary>
    public DbSet<ProfileType> ProfileTypes { get; set; } = null!;

    /// <summary>
    /// User profiles
    /// </summary>
    public DbSet<Profile> Profiles { get; set; } = null!;

    /// <summary>
    /// Profile follower relationships
    /// </summary>
    public DbSet<ProfileFollower> ProfileFollowers { get; set; } = null!;

    /// <summary>
    /// Posts in the activity stream
    /// </summary>
    public DbSet<Post> Posts { get; set; } = null!;

    /// <summary>
    /// Post attachments (images, videos, files)
    /// </summary>
    public DbSet<PostAttachment> PostAttachments { get; set; } = null!;

    /// <summary>
    /// Comments on posts
    /// </summary>
    public DbSet<Comment> Comments { get; set; } = null!;

    /// <summary>
    /// Reactions on posts and comments
    /// </summary>
    public DbSet<Reaction> Reactions { get; set; } = null!;

    /// <summary>
    /// User notifications
    /// </summary>
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>
    /// AI chat conversations
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; } = null!;

    /// <summary>
    /// Messages within chat conversations
    /// </summary>
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    /// <summary>
    /// Saved AI chat results
    /// </summary>
    public DbSet<SavedResult> SavedResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileFollowerConfiguration());
        modelBuilder.ApplyConfiguration(new PostConfiguration());
        modelBuilder.ApplyConfiguration(new PostAttachmentConfiguration());
        modelBuilder.ApplyConfiguration(new CommentConfiguration());
        modelBuilder.ApplyConfiguration(new ReactionConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SavedResultConfiguration());

        // Configure value objects
        ConfigureValueObjects(modelBuilder);

        // Apply seed data
        //SeedData.Configure(modelBuilder);
    }

    /// <summary>
    /// Configures value objects for EF Core
    /// </summary>
    private static void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Configure Location value object as owned entity for Profile
        modelBuilder.Entity<Profile>()
            .OwnsOne(p => p.Location, location =>
            {
                location.Property(l => l.City)
                    .HasMaxLength(100)
                    .HasColumnName("LocationCity");

                location.Property(l => l.State)
                    .HasMaxLength(100)
                    .HasColumnName("LocationState");

                location.Property(l => l.Country)
                    .HasMaxLength(100)
                    .HasColumnName("LocationCountry");

                location.Property(l => l.Latitude)
                    .HasColumnName("LocationLatitude");

                location.Property(l => l.Longitude)
                    .HasColumnName("LocationLongitude");
            });


    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update timestamps
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates timestamps for modified entities
    /// </summary>
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
                    
                    // Initialize Conversation.LastMessageAt
                    if (entry.Entity is Conversation conversation && conversation.LastMessageAt == default)
                    {
                        conversation.LastMessageAt = DateTime.UtcNow;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}