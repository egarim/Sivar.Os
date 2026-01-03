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

    /// <summary>
    /// AI chat token usage tracking for auditing and billing
    /// </summary>
    public DbSet<ChatTokenUsage> ChatTokenUsages { get; set; } = null!;

    /// <summary>
    /// AI model pricing for cost calculation and tracking
    /// </summary>
    public DbSet<AiModelPricing> AiModelPricings { get; set; } = null!;

    /// <summary>
    /// Structured search results from AI chat
    /// </summary>
    public DbSet<SearchResult> SearchResults { get; set; } = null!;

    /// <summary>
    /// Activity stream events (Actor-Verb-Object pattern)
    /// </summary>
    public DbSet<Activity> Activities { get; set; } = null!;

    /// <summary>
    /// Profile emotion summaries for sentiment analytics
    /// </summary>
    public DbSet<ProfileEmotionSummary> ProfileEmotionSummaries { get; set; } = null!;

    /// <summary>
    /// Contact type catalog (phone, whatsapp, email, etc.)
    /// </summary>
    public DbSet<ContactType> ContactTypes { get; set; } = null!;

    /// <summary>
    /// Business contact information linked to profiles
    /// </summary>
    public DbSet<BusinessContactInfo> BusinessContactInfos { get; set; } = null!;

    /// <summary>
    /// Chat bot settings for configurable welcome messages and prompts
    /// Phase 0.5: Database-driven chat configuration
    /// </summary>
    public DbSet<ChatBotSettings> ChatBotSettings { get; set; } = null!;

    /// <summary>
    /// Agent capabilities - defines what AI functions are available
    /// </summary>
    public DbSet<AgentCapability> AgentCapabilities { get; set; } = null!;

    /// <summary>
    /// Capability parameters - defines parameters for AI functions
    /// </summary>
    public DbSet<CapabilityParameter> CapabilityParameters { get; set; } = null!;

    /// <summary>
    /// Quick actions - buttons shown in chat interface
    /// </summary>
    public DbSet<QuickAction> QuickActions { get; set; } = null!;

    /// <summary>
    /// Profile bookmarks - saved posts by profiles
    /// </summary>
    public DbSet<ProfileBookmark> ProfileBookmarks { get; set; } = null!;

    /// <summary>
    /// Category definitions - multilingual synonyms for normalized search
    /// Phase 6: English-First Query Pattern
    /// </summary>
    public DbSet<CategoryDefinition> CategoryDefinitions { get; set; } = null!;

    /// <summary>
    /// Agent configurations - database-driven AI agent settings
    /// Phase 10: Multi-Agent Configuration
    /// </summary>
    public DbSet<AgentConfiguration> AgentConfigurations { get; set; } = null!;

    /// <summary>
    /// Agent tools - registry of available functions for agents
    /// Phase 10: Multi-Agent Configuration
    /// </summary>
    public DbSet<AgentTool> AgentTools { get; set; } = null!;

    /// <summary>
    /// User search behavior tracking for personalization
    /// Phase 11: Results Ranking & Personalization
    /// </summary>
    public DbSet<UserSearchBehavior> UserSearchBehaviors { get; set; } = null!;

    /// <summary>
    /// Ranking configurations (weights per category)
    /// Phase 11: Results Ranking & Personalization
    /// </summary>
    public DbSet<RankingConfiguration> RankingConfigurations { get; set; } = null!;

    /// <summary>
    /// Ad transactions - audit trail for ad budget changes
    /// Search Ads System
    /// </summary>
    public DbSet<AdTransaction> AdTransactions { get; set; } = null!;

    /// <summary>
    /// Schedule events - calendar events and appointments
    /// Event/Appointment Scheduling System
    /// </summary>
    public DbSet<ScheduleEvent> ScheduleEvents { get; set; } = null!;

    /// <summary>
    /// Event attendees - people attending events
    /// Event/Appointment Scheduling System
    /// </summary>
    public DbSet<EventAttendee> EventAttendees { get; set; } = null!;

    /// <summary>
    /// Event reminders - notifications for upcoming events
    /// Event/Appointment Scheduling System
    /// </summary>
    public DbSet<EventReminder> EventReminders { get; set; } = null!;

    /// <summary>
    /// Recurrence rules - patterns for repeating events
    /// Event/Appointment Scheduling System
    /// </summary>
    public DbSet<RecurrenceRule> RecurrenceRules { get; set; } = null!;

    /// <summary>
    /// Bookable resources - people, tables, rooms, equipment that can be booked
    /// Resource Booking System
    /// </summary>
    public DbSet<BookableResource> BookableResources { get; set; } = null!;

    /// <summary>
    /// Resource services - specific services offered by a resource (e.g., haircut, massage)
    /// Resource Booking System
    /// </summary>
    public DbSet<ResourceService> ResourceServices { get; set; } = null!;

    /// <summary>
    /// Resource availability - weekly schedule for when resources are available
    /// Resource Booking System
    /// </summary>
    public DbSet<ResourceAvailability> ResourceAvailabilities { get; set; } = null!;

    /// <summary>
    /// Resource exceptions - holidays, special hours, blocked dates
    /// Resource Booking System
    /// </summary>
    public DbSet<ResourceException> ResourceExceptions { get; set; } = null!;

    /// <summary>
    /// Resource bookings - customer reservations/appointments
    /// Resource Booking System
    /// </summary>
    public DbSet<ResourceBooking> ResourceBookings { get; set; } = null!;

    /// <summary>
    /// Waiting list entries - users waiting for app access
    /// Waiting List System
    /// </summary>
    public DbSet<WaitingListEntry> WaitingListEntries { get; set; } = null!;

    /// <summary>
    /// Phone verification attempts - OTP tracking via Twilio
    /// Waiting List System
    /// </summary>
    public DbSet<PhoneVerification> PhoneVerifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostgreSQL extensions
        modelBuilder.HasPostgresExtension("vector");  // Phase 5: pgvector for semantic search

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
        modelBuilder.ApplyConfiguration(new ChatTokenUsageConfiguration());
        modelBuilder.ApplyConfiguration(new AiModelPricingConfiguration());
        modelBuilder.ApplyConfiguration(new SearchResultConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileEmotionSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new ContactTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BusinessContactInfoConfiguration());
        modelBuilder.ApplyConfiguration(new ChatBotSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new AgentCapabilityConfiguration());
        modelBuilder.ApplyConfiguration(new CapabilityParameterConfiguration());
        modelBuilder.ApplyConfiguration(new QuickActionConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileBookmarkConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new AdTransactionConfiguration()); // Search Ads System
        
        // Event/Appointment Scheduling System
        modelBuilder.ApplyConfiguration(new ScheduleEventConfiguration());
        modelBuilder.ApplyConfiguration(new EventAttendeeConfiguration());
        modelBuilder.ApplyConfiguration(new EventReminderConfiguration());
        modelBuilder.ApplyConfiguration(new RecurrenceRuleConfiguration());

        // Resource Booking System
        modelBuilder.ApplyConfiguration(new BookableResourceConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceServiceConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceAvailabilityConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceExceptionConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceBookingConfiguration());

        // Waiting List System
        modelBuilder.ApplyConfiguration(new WaitingListEntryConfiguration());
        modelBuilder.ApplyConfiguration(new PhoneVerificationConfiguration());

        // Configure value objects
        ConfigureValueObjects(modelBuilder);

        // Apply seed data
        //SeedData.Configure(modelBuilder);
        
        // Phase 3: Add tsvector columns for full-text search
        // These are created as computed columns in PostgreSQL
        ConfigureFullTextSearch(modelBuilder);
    }

    /// <summary>
    /// Configures full-text search columns for Posts table
    /// </summary>
    private static void ConfigureFullTextSearch(ModelBuilder modelBuilder)
    {
        // Note: SearchVector and SearchVectorSimple properties are ignored in PostConfiguration
        // The actual columns need to be created via raw SQL after database creation
        
        // Run this SQL after recreating the database:
        /*
        -- Add language-specific search vector (with stemming)
        ALTER TABLE "Sivar_Posts" 
        ADD COLUMN "SearchVector" tsvector 
        GENERATED ALWAYS AS (
            to_tsvector(
                CASE 
                    WHEN "Language" = 'en' THEN 'english'::regconfig
                    WHEN "Language" = 'es' THEN 'spanish'::regconfig
                    WHEN "Language" = 'fr' THEN 'french'::regconfig
                    WHEN "Language" = 'de' THEN 'german'::regconfig
                    WHEN "Language" = 'pt' THEN 'portuguese'::regconfig
                    WHEN "Language" = 'it' THEN 'italian'::regconfig
                    WHEN "Language" = 'nl' THEN 'dutch'::regconfig
                    WHEN "Language" = 'ru' THEN 'russian'::regconfig
                    WHEN "Language" = 'sv' THEN 'swedish'::regconfig
                    WHEN "Language" = 'no' THEN 'norwegian'::regconfig
                    WHEN "Language" = 'da' THEN 'danish'::regconfig
                    WHEN "Language" = 'fi' THEN 'finnish'::regconfig
                    WHEN "Language" = 'tr' THEN 'turkish'::regconfig
                    WHEN "Language" = 'ro' THEN 'romanian'::regconfig
                    WHEN "Language" = 'ar' THEN 'arabic'::regconfig
                    ELSE 'simple'::regconfig
                END,
                coalesce("Title", '') || ' ' || "Content"
            )
        ) STORED;

        -- Add universal/simple search vector (no stemming, works for all languages)
        ALTER TABLE "Sivar_Posts" 
        ADD COLUMN "SearchVectorSimple" tsvector 
        GENERATED ALWAYS AS (
            to_tsvector('simple', coalesce("Title", '') || ' ' || "Content")
        ) STORED;

        -- Create GIN indexes for fast full-text search
        CREATE INDEX "IX_Posts_SearchVector_Gin" ON "Sivar_Posts" USING gin("SearchVector");
        CREATE INDEX "IX_Posts_SearchVectorSimple_Gin" ON "Sivar_Posts" USING gin("SearchVectorSimple");
        */
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