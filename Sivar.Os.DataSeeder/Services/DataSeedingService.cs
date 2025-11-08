using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.DataSeeder.Services;

/// <summary>
/// Service for seeding realistic data into the Sivar.Os database
/// Following the same patterns as the authentication flow
/// </summary>
public class DataSeedingService
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileTypeRepository _profileTypeRepository;
    private readonly IPostRepository _postRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IReactionRepository _reactionRepository;
    private readonly IProfileFollowerRepository _followerRepository;
    private readonly ILogger<DataSeedingService> _logger;

    // Predefined ProfileType IDs (matching the seeded ones in Updater.cs)
    private readonly Guid PersonalProfileTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid BusinessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid OrganizationProfileTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public DataSeedingService(
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IProfileTypeRepository profileTypeRepository,
        IPostRepository postRepository,
        ICommentRepository commentRepository,
        IReactionRepository reactionRepository,
        IProfileFollowerRepository followerRepository,
        ILogger<DataSeedingService> logger)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _profileTypeRepository = profileTypeRepository;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
        _followerRepository = followerRepository;
        _logger = logger;
    }

    /// <summary>
    /// Main seeding method - creates users, profiles, and content
    /// </summary>
    public async Task SeedDataAsync()
    {
        _logger.LogInformation("🌱 Starting data seeding process...");

        try
        {
            // Step 1: Verify ProfileTypes exist
            await VerifyProfileTypesAsync();

            // Step 2: Create users and profiles
            var createdProfiles = await SeedUsersAndProfilesAsync();

            // Step 3: Create posts for each profile
            await SeedPostsAsync(createdProfiles);

            // Step 4: Create social interactions
            await SeedSocialInteractionsAsync(createdProfiles);

            _logger.LogInformation("✅ Data seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during data seeding");
            throw;
        }
    }

    /// <summary>
    /// Verifies that required ProfileTypes exist in the database
    /// </summary>
    private async Task VerifyProfileTypesAsync()
    {
        _logger.LogInformation("🔍 Verifying ProfileTypes...");

        var personalType = await _profileTypeRepository.GetByIdAsync(PersonalProfileTypeId);
        var businessType = await _profileTypeRepository.GetByIdAsync(BusinessProfileTypeId);
        var orgType = await _profileTypeRepository.GetByIdAsync(OrganizationProfileTypeId);

        if (personalType == null || businessType == null || orgType == null)
        {
            throw new InvalidOperationException("Required ProfileTypes not found. Please run database migrations first.");
        }

        _logger.LogInformation("✅ ProfileTypes verified successfully");
    }

    /// <summary>
    /// Creates users and profiles following the authentication flow pattern
    /// </summary>
    private async Task<List<Profile>> SeedUsersAndProfilesAsync()
    {
        _logger.LogInformation("👥 Creating users and profiles...");

        var createdProfiles = new List<Profile>();

        // Define realistic user data
        var usersData = new[]
        {
            new {
                KeycloakId = "20b52564-e505-404a-bd7a-be5916c8e0a4",
                Email = "guz@sivaros.com",
                FirstName = "Gustavo",
                LastName = "Martinez",
                ProfileType = PersonalProfileTypeId,
                Bio = "Software architect passionate about distributed systems and AI. Love exploring new technologies!",
                Tags = new[] { "software", "ai", "architecture", "tech-enthusiast" },
                Location = new Location { City = "San Salvador", State = "San Salvador", Country = "El Salvador", Latitude = 13.6929, Longitude = -89.2182 }
            },
            new {
                KeycloakId = "b65fd3b2-e181-4830-8678-fff5f96492b9",
                Email = "jaime@sivaros.com", 
                FirstName = "Jaime",
                LastName = "Rodriguez",
                ProfileType = BusinessProfileTypeId,
                Bio = "Entrepreneur and business consultant. Helping startups scale and grow in Central America.",
                Tags = new[] { "business", "consulting", "startups", "entrepreneurship" },
                Location = new Location { City = "Guatemala City", State = "Guatemala", Country = "Guatemala", Latitude = 14.6349, Longitude = -90.5069 }
            },
            new {
                KeycloakId = "28b46a88-d191-4c63-8812-1bb8f3332228",
                Email = "joche@sivaros.com",
                FirstName = "Jose",
                LastName = "Ojeda", 
                ProfileType = PersonalProfileTypeId,
                Bio = "Full-stack developer and tech lead. Building the future of social platforms in Central America.",
                Tags = new[] { "fullstack", "leadership", "innovation", "social-tech" },
                Location = new Location { City = "Managua", State = "Managua", Country = "Nicaragua", Latitude = 12.1364, Longitude = -86.2514 }
            },
            new {
                KeycloakId = "ea06c2da-07f3-4606-aa65-46a67cb0a471",
                Email = "oscar@sivaros.com",
                FirstName = "Oscar",
                LastName = "Fernandez",
                ProfileType = BusinessProfileTypeId,
                Bio = "Digital marketing expert and content creator. Specializing in social media strategy for tech companies.",
                Tags = new[] { "marketing", "content", "social-media", "strategy" },
                Location = new Location { City = "San Jose", State = "San Jose", Country = "Costa Rica", Latitude = 9.9281, Longitude = -84.0907 }
            }
        };

        foreach (var userData in usersData)
        {
            try
            {
                // Check if user already exists (idempotent)
                var existingUser = await _userRepository.GetByKeycloakIdAsync(userData.KeycloakId);
                if (existingUser != null)
                {
                    _logger.LogInformation($"👤 User {userData.FirstName} {userData.LastName} already exists, skipping...");
                    
                    // Get existing profiles
                    var existingProfiles = await _profileRepository.GetProfilesByUserIdAsync(existingUser.Id, includeInactive: true);
                    createdProfiles.AddRange(existingProfiles);
                    continue;
                }

                // Create new user (following authentication flow pattern)
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = userData.KeycloakId,
                    Email = userData.Email,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    Role = UserRole.RegisteredUser,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PreferredLanguage = "en",
                    TimeZone = "America/El_Salvador"
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation($"👤 Created user: {user.FirstName} {user.LastName} ({user.Email})");

                // Create default profile (following CreateDefaultProfileAsync pattern)
                var profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ProfileTypeId = userData.ProfileType,
                    DisplayName = $"{userData.FirstName} {userData.LastName}",
                    Handle = GenerateHandle($"{userData.FirstName} {userData.LastName}"),
                    Bio = userData.Bio,
                    Avatar = "", // Will be populated later if needed
                    Location = userData.Location,
                    Tags = userData.Tags.ToList(),
                    Metadata = "{}",
                    SocialMediaLinks = "{}",
                    AllowedViewers = new List<Guid>(),
                    VisibilityLevel = VisibilityLevel.Public,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _profileRepository.AddAsync(profile);
                await _profileRepository.SaveChangesAsync();

                // Set as active profile
                user.ActiveProfileId = profile.Id;
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation($"📝 Created profile: {profile.DisplayName} (@{profile.Handle})");

                createdProfiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to create user/profile for {userData.FirstName} {userData.LastName}");
                throw;
            }
        }

        return createdProfiles;
    }

    /// <summary>
    /// Creates diverse posts for each profile
    /// </summary>
    private async Task SeedPostsAsync(List<Profile> profiles)
    {
        _logger.LogInformation("📝 Creating posts...");

        var random = new Random();
        var postTemplates = GetPostTemplates();

        foreach (var profile in profiles)
        {
            try
            {
                // Create 3-5 posts per profile
                var postCount = random.Next(3, 6);
                
                for (int i = 0; i < postCount; i++)
                {
                    var template = postTemplates[random.Next(postTemplates.Length)];
                    
                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = profile.Id,
                        PostType = template.PostType,
                        Content = template.Content.Replace("{name}", profile.DisplayName.Split(' ')[0]),
                        Title = template.Title,
                        Location = ShouldIncludeLocation() ? profile.Location : null,
                        Tags = template.Tags,
                        Visibility = VisibilityLevel.Public,
                        Language = "en",
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)), // Random date within last 30 days
                        UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    };

                    await _postRepository.AddAsync(post);
                }

                await _postRepository.SaveChangesAsync();
                _logger.LogInformation($"📝 Created {postCount} posts for {profile.DisplayName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to create posts for profile {profile.DisplayName}");
                throw;
            }
        }
    }

    /// <summary>
    /// Creates follow relationships and reactions
    /// </summary>
    private async Task SeedSocialInteractionsAsync(List<Profile> profiles)
    {
        _logger.LogInformation("🤝 Creating social interactions...");

        var random = new Random();

        // Create follow relationships (each user follows 1-2 others)
        foreach (var follower in profiles)
        {
            var followCount = random.Next(1, 3);
            var potentialFollows = profiles.Where(p => p.Id != follower.Id).ToList();
            
            for (int i = 0; i < Math.Min(followCount, potentialFollows.Count); i++)
            {
                var following = potentialFollows[random.Next(potentialFollows.Count)];
                potentialFollows.Remove(following);

                // Check if relationship already exists
                var existingFollow = await _followerRepository.GetFollowRelationshipAsync(follower.Id, following.Id);
                if (existingFollow != null) continue;

                var followRelationship = new ProfileFollower
                {
                    Id = Guid.NewGuid(),
                    FollowerProfileId = follower.Id,
                    FollowedProfileId = following.Id,
                    FollowedAt = DateTime.UtcNow.AddDays(-random.Next(1, 15)),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 15))
                };

                await _followerRepository.AddAsync(followRelationship);
            }
        }

        await _followerRepository.SaveChangesAsync();

        // Add reactions to posts
        var allPosts = await _postRepository.GetAllAsync();
        foreach (var post in allPosts.Take(20)) // Limit to avoid too many reactions
        {
            var reactionCount = random.Next(1, 4);
            var potentialReactors = profiles.Where(p => p.Id != post.ProfileId).ToList();

            for (int i = 0; i < Math.Min(reactionCount, potentialReactors.Count); i++)
            {
                var reactor = potentialReactors[random.Next(potentialReactors.Count)];
                potentialReactors.Remove(reactor);

                var reaction = new Reaction
                {
                    Id = Guid.NewGuid(),
                    ProfileId = reactor.Id,
                    PostId = post.Id,
                    ReactionType = (ReactionType)random.Next(1, 6), // Random reaction type
                    CreatedAt = post.CreatedAt.AddHours(random.Next(1, 24))
                };

                await _reactionRepository.AddAsync(reaction);
            }
        }

        await _reactionRepository.SaveChangesAsync();
        _logger.LogInformation("🤝 Social interactions created successfully");
    }

    #region Helper Methods

    /// <summary>
    /// Generates a URL-friendly handle from display name
    /// </summary>
    private string GenerateHandle(string displayName)
    {
        return displayName.ToLower()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace("_", "-");
    }

    /// <summary>
    /// Determines if a post should include location (30% chance)
    /// </summary>
    private bool ShouldIncludeLocation()
    {
        return new Random().NextDouble() < 0.3;
    }

    /// <summary>
    /// Gets variety of post templates for realistic content
    /// </summary>
    private PostTemplate[] GetPostTemplates()
    {
        return new[]
        {
            new PostTemplate 
            { 
                PostType = PostType.General, 
                Content = "Excited to be working on some amazing new features! The future of social networking in Central America looks bright 🌟",
                Tags = new[] { "tech", "innovation", "excited" }
            },
            new PostTemplate 
            { 
                PostType = PostType.General, 
                Content = "Just finished reading an incredible book on system architecture. Always learning! 📚 What's everyone else reading?",
                Tags = new[] { "learning", "books", "architecture" }
            },
            new PostTemplate 
            { 
                PostType = PostType.General, 
                Content = "Beautiful morning in Central America! ☀️ Ready to tackle some challenging problems today.",
                Tags = new[] { "morning", "motivation", "centralamerica" }
            },
            new PostTemplate 
            { 
                PostType = PostType.BusinessLocation, 
                Title = "Coffee & Code Session",
                Content = "Join us for our weekly Coffee & Code meetup! Great networking opportunity for developers and entrepreneurs.",
                Tags = new[] { "meetup", "networking", "coffee", "developers" }
            },
            new PostTemplate 
            { 
                PostType = PostType.General, 
                Content = "Working on improving our platform's performance. User experience is everything! 🚀",
                Tags = new[] { "performance", "ux", "development" }
            },
            new PostTemplate 
            { 
                PostType = PostType.Service, 
                Title = "Tech Consulting Services",
                Content = "Offering consulting services for startups looking to scale their technology infrastructure. DM me for details!",
                Tags = new[] { "consulting", "startups", "tech", "services" }
            }
        };
    }

    #endregion
}

/// <summary>
/// Template for generating diverse post content
/// </summary>
public class PostTemplate
{
    public PostType PostType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}