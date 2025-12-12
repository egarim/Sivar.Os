using System.ComponentModel;
using System.Text.Json;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service that provides AI-callable functions for social platform actions.
/// Includes PostGIS-powered spatial search for nearby profiles and posts.
/// Phase 2: Now captures structured search results for card-based UI rendering.
/// </summary>
public class ChatFunctionService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IPostRepository _postRepository;
    private readonly IProfileFollowerRepository _followerRepository;
    private readonly ILocationService _locationService;
    private readonly ILogger<ChatFunctionService> _logger;
    private Guid _currentProfileId; // Set before each AI call
    private (double Latitude, double Longitude)? _currentLocation; // Optional user location context
    
    /// <summary>
    /// Phase 2: Captures the last search results as structured DTOs for card rendering.
    /// Reset before each AI agent call and populated by search functions.
    /// </summary>
    public SearchResultsCollectionDto? LastSearchResults { get; private set; }
    
    /// <summary>
    /// Clears the last search results. Call before each AI agent invocation.
    /// </summary>
    public void ClearLastSearchResults()
    {
        LastSearchResults = null;
        _logger.LogDebug("[ChatFunctionService] LastSearchResults cleared");
    }

    public ChatFunctionService(
        IProfileRepository profileRepository,
        IPostRepository postRepository,
        IProfileFollowerRepository followerRepository,
        ILocationService locationService,
        ILogger<ChatFunctionService> logger)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _followerRepository = followerRepository ?? throw new ArgumentNullException(nameof(followerRepository));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Set the current profile ID for security context
    /// </summary>
    public void SetCurrentProfile(Guid profileId)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[ChatFunctionService.SetCurrentProfile] START - RequestId={RequestId}, ProfileId={ProfileId}",
            requestId, profileId);

        try
        {
            if (profileId == Guid.Empty)
            {
                _logger.LogError("[ChatFunctionService.SetCurrentProfile] VALIDATION ERROR - RequestId={RequestId}, ProfileIdEmpty=true",
                    requestId);
                throw new ArgumentException("Profile ID cannot be empty", nameof(profileId));
            }

            _currentProfileId = profileId;
            _logger.LogInformation("[ChatFunctionService.SetCurrentProfile] SUCCESS - RequestId={RequestId}, CurrentProfileId={CurrentProfileId}",
                requestId, _currentProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatFunctionService.SetCurrentProfile] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}",
                requestId, ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Set the current user's location for proximity-aware searches.
    /// When set, searches can prioritize nearby results and show distances.
    /// </summary>
    public void SetCurrentLocation(double? latitude, double? longitude)
    {
        if (latitude.HasValue && longitude.HasValue)
        {
            _currentLocation = (latitude.Value, longitude.Value);
            _logger.LogInformation("[ChatFunctionService.SetCurrentLocation] Location set - Lat={Lat}, Lng={Lng}",
                latitude, longitude);
        }
        else
        {
            _currentLocation = null;
            _logger.LogInformation("[ChatFunctionService.SetCurrentLocation] Location cleared");
        }
    }

    /// <summary>
    /// Search for profiles by name, type, or description
    /// </summary>
    [Description("Search for user profiles on the social network. Returns profiles matching the search query.")]
    public async Task<string> SearchProfiles(
        [Description("The search query - can be a name, profile type (Business, Personal, Organization), or description keywords")]
        string query,
        [Description("Maximum number of results to return (default 5, max 10)")]
        int maxResults = 5)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.SearchProfiles] START - RequestId={RequestId}, Timestamp={Timestamp}, Query={Query}, MaxResults={MaxResults}",
            requestId, startTime, query, maxResults);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogError("[ChatFunctionService.SearchProfiles] VALIDATION ERROR - RequestId={RequestId}, QueryNull=true",
                    requestId);
                throw new ArgumentException("Query cannot be null or empty", nameof(query));
            }

            _logger.LogInformation("AI searching for profiles with query: {Query}", query);

            // Limit max results
            maxResults = Math.Min(maxResults, 10);

            // Get all profiles and filter (in real app, this would be a DB query)
            var allProfiles = await _profileRepository.GetAllAsync();

            var matchingProfileEntities = allProfiles
                .Where(p => !p.IsDeleted &&
                    (p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     p.Bio.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     p.ProfileType?.Name.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                .Take(maxResults)
                .ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.SearchProfiles] SUCCESS - RequestId={RequestId}, MatchCount={MatchCount}, Duration={Duration}ms",
                requestId, matchingProfileEntities.Count, elapsed);

            if (!matchingProfileEntities.Any())
            {
                return $"No profiles found matching '{query}'";
            }

            // Phase 2: Populate structured results for card rendering
            var businessResults = matchingProfileEntities
                .Select((p, index) => MapProfileToBusinessResult(p, null, index))
                .ToList();
            
            LastSearchResults = CreateSearchResultsCollection(query, businessResults, (long)elapsed);
            _logger.LogInformation("[ChatFunctionService.SearchProfiles] Phase 2: LastSearchResults populated with {Count} business results", businessResults.Count);

            // Return JSON for AI agent (preserving existing behavior)
            var matchingProfiles = matchingProfileEntities
                .Select(p => new
                {
                    id = p.Id,
                    displayName = p.DisplayName,
                    profileType = p.ProfileType?.Name ?? "Unknown",
                    bio = p.Bio.Length > 100 ? p.Bio.Substring(0, 100) + "..." : p.Bio,
                    isFollowing = _followerRepository.IsFollowingAsync(_currentProfileId, p.Id).Result
                })
                .ToList();

            return JsonSerializer.Serialize(new
            {
                query,
                count = matchingProfiles.Count,
                profiles = matchingProfiles
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchProfiles] VALIDATION ERROR - RequestId={RequestId}, Query={Query}, Duration={Duration}ms",
                requestId, query, elapsed);
            return $"Validation error searching profiles: {ex.Message}";
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchProfiles] EXCEPTION - RequestId={RequestId}, Query={Query}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, query, ex.GetType().Name, elapsed);
            return $"Error searching profiles: {ex.Message}";
        }
    }

    /// <summary>
    /// Search for posts by content, author, or location
    /// </summary>
    [Description("Search for posts on the social network. Searches content, author names, and location (city/country). Use this for queries like 'pizzerias in San Salvador' or 'events near downtown'. When user asks for 'near me' or 'cerca de mi', the search will use their current location.")]
    public async Task<string> SearchPosts(
        [Description("Search keywords - can be post content, business name, or type of place")]
        string query,
        [Description("Optional: filter by city name (e.g., 'San Salvador')")]
        string? city = null,
        [Description("Optional: filter by country (e.g., 'El Salvador')")]
        string? country = null,
        [Description("Maximum number of results to return (default 10, max 20)")]
        int maxResults = 10)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.SearchPosts] START - RequestId={RequestId}, Query={Query}, City={City}, Country={Country}, MaxResults={MaxResults}, HasLocation={HasLocation}",
            requestId, query, city, country, maxResults, _currentLocation.HasValue);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
            {
                _logger.LogError("[ChatFunctionService.SearchPosts] VALIDATION ERROR - RequestId={RequestId}, AllFieldsEmpty=true",
                    requestId);
                return "Please provide at least a search query, city, or country to search.";
            }

            // Limit max results
            maxResults = Math.Min(maxResults, 20);

            // Get all posts
            var allPosts = await _postRepository.GetAllAsync();
            
            var filteredPosts = allPosts
                .Where(p => !p.IsDeleted)
                .Where(p =>
                {
                    bool matches = false;

                    // Match content or author if query provided
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        matches = p.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                  p.Profile?.DisplayName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
                        
                        // Also check location fields for the query (e.g., "San Salvador" as query)
                        if (!matches && p.Location != null)
                        {
                            matches = p.Location.City?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                                      p.Location.Country?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                                      p.Location.State?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
                        }
                    }
                    else
                    {
                        matches = true; // No query filter, will filter by location
                    }

                    // Apply city filter if provided
                    if (!string.IsNullOrWhiteSpace(city) && matches)
                    {
                        matches = p.Location?.City?.Contains(city, StringComparison.OrdinalIgnoreCase) == true;
                    }

                    // Apply country filter if provided
                    if (!string.IsNullOrWhiteSpace(country) && matches)
                    {
                        matches = p.Location?.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) == true;
                    }

                    return matches;
                })
                .ToList();

            // If user has location, calculate distances and sort by proximity
            IEnumerable<dynamic> matchingPosts;
            
            if (_currentLocation.HasValue)
            {
                var (userLat, userLng) = _currentLocation.Value;
                
                matchingPosts = filteredPosts
                    .Select(p => new
                    {
                        Post = p,
                        DistanceKm = p.Location?.Latitude.HasValue == true && p.Location?.Longitude.HasValue == true
                            ? CalculateDistanceKm(userLat, userLng, p.Location.Latitude.Value, p.Location.Longitude.Value)
                            : (double?)null
                    })
                    .OrderBy(x => x.DistanceKm ?? double.MaxValue) // Sort by distance (nearest first)
                    .ThenByDescending(x => x.Post.CreatedAt)
                    .Take(maxResults)
                    .Select(x => (dynamic)new
                    {
                        id = x.Post.Id,
                        content = x.Post.Content.Length > 200 ? x.Post.Content.Substring(0, 200) + "..." : x.Post.Content,
                        authorName = x.Post.Profile?.DisplayName ?? "Unknown",
                        authorHandle = x.Post.Profile?.Handle,
                        authorType = x.Post.Profile?.ProfileType?.Name ?? "Unknown",
                        postType = x.Post.PostType.ToString(),
                        location = x.Post.Location != null ? new
                        {
                            city = x.Post.Location.City,
                            state = x.Post.Location.State,
                            country = x.Post.Location.Country,
                            hasCoordinates = x.Post.Location.Latitude.HasValue && x.Post.Location.Longitude.HasValue
                        } : null,
                        distanceKm = x.DistanceKm.HasValue ? Math.Round(x.DistanceKm.Value, 1) : (double?)null,
                        distanceText = x.DistanceKm.HasValue 
                            ? (x.DistanceKm.Value < 1 ? $"{(int)(x.DistanceKm.Value * 1000)}m" : $"{x.DistanceKm.Value:F1}km")
                            : null,
                        createdAt = x.Post.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        link = $"/post/{x.Post.Id}"
                    })
                    .ToList();
            }
            else
            {
                // No location context - sort by recency
                matchingPosts = filteredPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(maxResults)
                    .Select(p => (dynamic)new
                    {
                        id = p.Id,
                        content = p.Content.Length > 200 ? p.Content.Substring(0, 200) + "..." : p.Content,
                        authorName = p.Profile?.DisplayName ?? "Unknown",
                        authorHandle = p.Profile?.Handle,
                        authorType = p.Profile?.ProfileType?.Name ?? "Unknown",
                        postType = p.PostType.ToString(),
                        location = p.Location != null ? new
                        {
                            city = p.Location.City,
                            state = p.Location.State,
                            country = p.Location.Country,
                            hasCoordinates = p.Location.Latitude.HasValue && p.Location.Longitude.HasValue
                        } : null,
                        distanceKm = (double?)null,
                        distanceText = (string?)null,
                        createdAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        link = $"/post/{p.Id}"
                    })
                    .ToList();
            }

            var resultsList = matchingPosts.ToList();
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.SearchPosts] SUCCESS - RequestId={RequestId}, MatchCount={MatchCount}, Duration={Duration}ms, SortedByDistance={SortedByDistance}",
                requestId, resultsList.Count, elapsed, _currentLocation.HasValue);

            if (!resultsList.Any())
            {
                var searchCriteria = new List<string>();
                if (!string.IsNullOrWhiteSpace(query)) searchCriteria.Add($"'{query}'");
                if (!string.IsNullOrWhiteSpace(city)) searchCriteria.Add($"city '{city}'");
                if (!string.IsNullOrWhiteSpace(country)) searchCriteria.Add($"country '{country}'");
                
                return $"No posts found matching {string.Join(" in ", searchCriteria)}. Try broader search terms or check if locations are registered on the platform.";
            }

            // Phase 2: Populate structured results for card rendering
            var postsWithDistance = _currentLocation.HasValue
                ? filteredPosts
                    .Select(p => (Post: p, Distance: p.Location?.Latitude.HasValue == true && p.Location?.Longitude.HasValue == true
                        ? CalculateDistanceKm(_currentLocation.Value.Latitude, _currentLocation.Value.Longitude, 
                            p.Location.Latitude.Value, p.Location.Longitude.Value)
                        : (double?)null))
                    .OrderBy(x => x.Distance ?? double.MaxValue)
                    .Take(maxResults)
                    .ToList()
                : filteredPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(maxResults)
                    .Select(p => (Post: p, Distance: (double?)null))
                    .ToList();

            var businessResults = postsWithDistance
                .Select((x, index) => MapPostToBusinessResult(x.Post, x.Distance, index))
                .ToList();
            
            var combinedQuery = string.Join(" ", new[] { query, city, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
            LastSearchResults = CreateSearchResultsCollection(combinedQuery, businessResults, (long)elapsed);
            _logger.LogInformation("[ChatFunctionService.SearchPosts] Phase 2: LastSearchResults populated with {Count} business results", businessResults.Count);

            return JsonSerializer.Serialize(new
            {
                searchCriteria = new { query, city, country },
                count = resultsList.Count,
                sortedByDistance = _currentLocation.HasValue,
                posts = resultsList
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchPosts] EXCEPTION - RequestId={RequestId}, Query={Query}, Duration={Duration}ms",
                requestId, query, elapsed);
            return $"Error searching posts: {ex.Message}";
        }
    }

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Get detailed information about a specific post
    /// </summary>
    [Description("Get detailed information about a specific post including content, author, reactions, and comments count.")]
    public async Task<string> GetPostDetails(
        [Description("The ID of the post to get details for")]
        string postId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.GetPostDetails] START - RequestId={RequestId}, Timestamp={Timestamp}, PostId={PostId}",
            requestId, startTime, postId);

        try
        {
            // Validate input
            if (!Guid.TryParse(postId, out var postGuid))
            {
                _logger.LogError("[ChatFunctionService.GetPostDetails] VALIDATION ERROR - RequestId={RequestId}, InvalidPostIdFormat=true",
                    requestId);
                return $"Invalid post ID format: {postId}";
            }

            var post = await _postRepository.GetByIdAsync(postGuid);
            if (post == null || post.IsDeleted)
            {
                _logger.LogWarning("[ChatFunctionService.GetPostDetails] POST NOT FOUND - RequestId={RequestId}, PostId={PostId}",
                    requestId, postGuid);
                return $"Post not found: {postId}";
            }

            var postDetails = new
            {
                id = post.Id,
                content = post.Content,
                postType = post.PostType,
                author = new
                {
                    id = post.Profile?.Id,
                    name = post.Profile?.DisplayName ?? "Unknown",
                    profileType = post.Profile?.ProfileType?.Name ?? "Unknown"
                },
                createdAt = post.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                stats = new
                {
                    comments = post.Comments?.Count ?? 0,
                    reactions = post.Reactions?.Count ?? 0
                },
                hasAttachments = post.Attachments?.Any() == true,
                attachmentCount = post.Attachments?.Count ?? 0
            };

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.GetPostDetails] SUCCESS - RequestId={RequestId}, PostId={PostId}, Comments={CommentCount}, Reactions={ReactionCount}, Duration={Duration}ms",
                requestId, postGuid, post.Comments?.Count ?? 0, post.Reactions?.Count ?? 0, elapsed);

            return JsonSerializer.Serialize(postDetails, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.GetPostDetails] EXCEPTION - RequestId={RequestId}, PostId={PostId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, postId, ex.GetType().Name, elapsed);
            return $"Error getting post details: {ex.Message}";
        }
    }

    /// <summary>
    /// Follow a profile
    /// </summary>
    [Description("Follow another user's profile. The current user will start seeing posts from this profile in their feed.")]
    public async Task<string> FollowProfile(
        [Description("The ID of the profile to follow")]
        string profileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.FollowProfile] START - RequestId={RequestId}, Timestamp={Timestamp}, ProfileId={ProfileId}, CurrentProfileId={CurrentProfileId}",
            requestId, startTime, profileId, _currentProfileId);

        try
        {
            // Validate input
            if (!Guid.TryParse(profileId, out var profileGuid))
            {
                _logger.LogError("[ChatFunctionService.FollowProfile] VALIDATION ERROR - RequestId={RequestId}, InvalidProfileIdFormat=true",
                    requestId);
                return $"Invalid profile ID format: {profileId}";
            }

            // Check if profile exists
            var targetProfile = await _profileRepository.GetByIdAsync(profileGuid);
            if (targetProfile == null || targetProfile.IsDeleted)
            {
                _logger.LogWarning("[ChatFunctionService.FollowProfile] PROFILE NOT FOUND - RequestId={RequestId}, ProfileId={ProfileId}",
                    requestId, profileGuid);
                return $"Profile not found: {profileId}";
            }

            // Can't follow yourself
            if (profileGuid == _currentProfileId)
            {
                _logger.LogWarning("[ChatFunctionService.FollowProfile] SELF FOLLOW ATTEMPT - RequestId={RequestId}, ProfileId={ProfileId}",
                    requestId, profileGuid);
                return "You cannot follow yourself.";
            }

            // Check if already following
            var isFollowing = await _followerRepository.IsFollowingAsync(_currentProfileId, profileGuid);
            if (isFollowing)
            {
                _logger.LogInformation("[ChatFunctionService.FollowProfile] ALREADY FOLLOWING - RequestId={RequestId}, ProfileId={ProfileId}",
                    requestId, profileGuid);
                return $"You are already following {targetProfile.DisplayName}.";
            }

            // Create follow relationship
            var follower = new ProfileFollower
            {
                FollowerProfileId = _currentProfileId,
                FollowedProfileId = profileGuid
            };

            await _followerRepository.AddAsync(follower);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.FollowProfile] SUCCESS - RequestId={RequestId}, ProfileId={ProfileId}, ProfileName={ProfileName}, Duration={Duration}ms",
                requestId, profileGuid, targetProfile.DisplayName, elapsed);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "follow",
                message = $"Successfully followed {targetProfile.DisplayName}",
                profile = new
                {
                    id = targetProfile.Id,
                    name = targetProfile.DisplayName,
                    type = targetProfile.ProfileType?.Name ?? "Unknown"
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.FollowProfile] EXCEPTION - RequestId={RequestId}, ProfileId={ProfileId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, profileId, ex.GetType().Name, elapsed);
            return $"Error following profile: {ex.Message}";
        }
    }

    /// <summary>
    /// Unfollow a profile
    /// </summary>
    [Description("Unfollow a user's profile. You will stop seeing their posts in your feed.")]
    public async Task<string> UnfollowProfile(
        [Description("The ID of the profile to unfollow")]
        string profileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.UnfollowProfile] START - RequestId={RequestId}, Timestamp={Timestamp}, ProfileId={ProfileId}, CurrentProfileId={CurrentProfileId}",
            requestId, startTime, profileId, _currentProfileId);

        try
        {
            // Validate input
            if (!Guid.TryParse(profileId, out var profileGuid))
            {
                _logger.LogError("[ChatFunctionService.UnfollowProfile] VALIDATION ERROR - RequestId={RequestId}, InvalidProfileIdFormat=true",
                    requestId);
                return $"Invalid profile ID format: {profileId}";
            }

            var targetProfile = await _profileRepository.GetByIdAsync(profileGuid);
            if (targetProfile == null || targetProfile.IsDeleted)
            {
                _logger.LogWarning("[ChatFunctionService.UnfollowProfile] PROFILE NOT FOUND - RequestId={RequestId}, ProfileId={ProfileId}",
                    requestId, profileGuid);
                return $"Profile not found: {profileId}";
            }

            // Check if following
            var isFollowing = await _followerRepository.IsFollowingAsync(_currentProfileId, profileGuid);
            if (!isFollowing)
            {
                _logger.LogInformation("[ChatFunctionService.UnfollowProfile] NOT FOLLOWING - RequestId={RequestId}, ProfileId={ProfileId}",
                    requestId, profileGuid);
                return $"You are not following {targetProfile.DisplayName}.";
            }

            // Remove follow relationship
            var follower = await _followerRepository.GetFollowRelationshipAsync(_currentProfileId, profileGuid);
            if (follower != null)
            {
                await _followerRepository.DeleteAsync(follower.Id);
                _logger.LogInformation("[ChatFunctionService.UnfollowProfile] RELATIONSHIP DELETED - RequestId={RequestId}, RelationshipId={RelationshipId}",
                    requestId, follower.Id);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.UnfollowProfile] SUCCESS - RequestId={RequestId}, ProfileId={ProfileId}, ProfileName={ProfileName}, Duration={Duration}ms",
                requestId, profileGuid, targetProfile.DisplayName, elapsed);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "unfollow",
                message = $"Successfully unfollowed {targetProfile.DisplayName}",
                profile = new
                {
                    id = targetProfile.Id,
                    name = targetProfile.DisplayName
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.UnfollowProfile] EXCEPTION - RequestId={RequestId}, ProfileId={ProfileId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, profileId, ex.GetType().Name, elapsed);
            return $"Error unfollowing profile: {ex.Message}";
        }
    }

    /// <summary>
    /// Get user's current profile information
    /// </summary>
    [Description("Get information about the current user's active profile.")]
    public async Task<string> GetMyProfile()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.GetMyProfile] START - RequestId={RequestId}, Timestamp={Timestamp}, CurrentProfileId={CurrentProfileId}",
            requestId, startTime, _currentProfileId);

        try
        {
            var profile = await _profileRepository.GetByIdAsync(_currentProfileId);
            if (profile == null || profile.IsDeleted)
            {
                _logger.LogWarning("[ChatFunctionService.GetMyProfile] PROFILE NOT FOUND - RequestId={RequestId}, ProfileId={ProfileId}",
                    requestId, _currentProfileId);
                return "Current profile not found.";
            }

            // Get follower stats
            var followerCount = await _followerRepository.GetFollowerCountAsync(_currentProfileId);
            var followingCount = await _followerRepository.GetFollowingCountAsync(_currentProfileId);

            // Get post count
            var (posts, totalPosts) = await _postRepository.GetByProfileAsync(_currentProfileId, 1, 1, false);

            _logger.LogInformation("[ChatFunctionService.GetMyProfile] PROFILE LOADED - RequestId={RequestId}, ProfileId={ProfileId}, DisplayName={DisplayName}, Followers={FollowerCount}, Following={FollowingCount}, Posts={PostCount}",
                requestId, _currentProfileId, profile.DisplayName, followerCount, followingCount, totalPosts);

            var profileInfo = new
            {
                id = profile.Id,
                displayName = profile.DisplayName,
                profileType = profile.ProfileType?.Name ?? "Unknown",
                bio = profile.Bio,
                stats = new
                {
                    followers = followerCount,
                    following = followingCount,
                    posts = totalPosts
                },
                createdAt = profile.CreatedAt.ToString("yyyy-MM-dd")
            };

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.GetMyProfile] SUCCESS - RequestId={RequestId}, ProfileId={ProfileId}, Duration={Duration}ms",
                requestId, _currentProfileId, elapsed);

            return JsonSerializer.Serialize(profileInfo, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.GetMyProfile] EXCEPTION - RequestId={RequestId}, ProfileId={ProfileId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, _currentProfileId, ex.GetType().Name, elapsed);
            return $"Error getting profile: {ex.Message}";
        }
    }

    /// <summary>
    /// Search for business locations by type and city.
    /// Specifically designed for queries like "pizzerias in San Salvador" or "restaurants near me".
    /// </summary>
    [Description("Find businesses and locations by type and city. Best for queries like 'pizzerias in San Salvador', 'restaurants in the city', or 'cafes near downtown'. Searches both content and location data.")]
    public async Task<string> FindBusinesses(
        [Description("Type of business or place to find (e.g., 'pizzeria', 'restaurant', 'cafe', 'hotel')")]
        string businessType,
        [Description("City name to search in (e.g., 'San Salvador', 'Santa Ana')")]
        string? city = null,
        [Description("Country to search in (default: El Salvador)")]
        string? country = null,
        [Description("Maximum results (default 10)")]
        int maxResults = 10)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.FindBusinesses] START - RequestId={RequestId}, Type={Type}, City={City}, Country={Country}",
            requestId, businessType, city, country);

        try
        {
            if (string.IsNullOrWhiteSpace(businessType))
            {
                return "Please specify what type of business you're looking for (e.g., 'pizzeria', 'restaurant', 'cafe').";
            }

            maxResults = Math.Min(maxResults, 20);

            // Get all posts
            var allPosts = await _postRepository.GetAllAsync();

            // Search for matching businesses
            var matchingPosts = allPosts
                .Where(p => !p.IsDeleted)
                .Where(p =>
                {
                    // Check content for business type
                    bool contentMatch = p.Content.Contains(businessType, StringComparison.OrdinalIgnoreCase);
                    
                    // Also check if it's a BusinessLocation post type
                    bool isBusinessLocation = p.PostType == Sivar.Os.Shared.Enums.PostType.BusinessLocation;

                    if (!contentMatch && !isBusinessLocation)
                        return false;

                    // Apply city filter if provided
                    if (!string.IsNullOrWhiteSpace(city))
                    {
                        if (p.Location?.City?.Contains(city, StringComparison.OrdinalIgnoreCase) != true)
                            return false;
                    }

                    // Apply country filter if provided
                    if (!string.IsNullOrWhiteSpace(country))
                    {
                        if (p.Location?.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) != true)
                            return false;
                    }

                    return true;
                })
                .OrderByDescending(p => p.CreatedAt)
                .Take(maxResults)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Content.Length > 100 ? p.Content.Substring(0, 100) + "..." : p.Content,
                    businessType = p.PostType.ToString(),
                    owner = p.Profile?.DisplayName ?? "Unknown",
                    ownerHandle = p.Profile?.Handle,
                    location = p.Location != null ? new
                    {
                        city = p.Location.City,
                        state = p.Location.State,
                        country = p.Location.Country,
                        coordinates = p.Location.Latitude.HasValue && p.Location.Longitude.HasValue
                            ? new { lat = p.Location.Latitude, lng = p.Location.Longitude }
                            : null
                    } : null,
                    link = $"/post/{p.Id}"
                })
                .ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.FindBusinesses] SUCCESS - RequestId={RequestId}, Found={Count}, Duration={Duration}ms",
                requestId, matchingPosts.Count, elapsed);

            if (!matchingPosts.Any())
            {
                var locationInfo = string.Join(", ", new[] { city, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return $"No {businessType}s found{(string.IsNullOrEmpty(locationInfo) ? "" : $" in {locationInfo}")}. This could mean:\n" +
                       $"1. No {businessType}s have been registered on the platform yet\n" +
                       $"2. Try different keywords or check the spelling\n" +
                       $"3. Try a broader location search";
            }

            // Phase 2: Populate structured results for card rendering
            // Re-fetch to get actual Post entities for mapping
            var matchingPostEntities = allPosts
                .Where(p => !p.IsDeleted)
                .Where(p =>
                {
                    bool contentMatch = p.Content.Contains(businessType, StringComparison.OrdinalIgnoreCase);
                    bool isBusinessLocation = p.PostType == Sivar.Os.Shared.Enums.PostType.BusinessLocation;

                    if (!contentMatch && !isBusinessLocation)
                        return false;

                    if (!string.IsNullOrWhiteSpace(city))
                    {
                        if (p.Location?.City?.Contains(city, StringComparison.OrdinalIgnoreCase) != true)
                            return false;
                    }

                    if (!string.IsNullOrWhiteSpace(country))
                    {
                        if (p.Location?.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) != true)
                            return false;
                    }

                    return true;
                })
                .OrderByDescending(p => p.CreatedAt)
                .Take(maxResults)
                .ToList();

            var businessResults = matchingPostEntities
                .Select((p, index) => MapPostToBusinessResult(p, null, index))
                .ToList();
            
            var combinedQuery = string.Join(" ", new[] { businessType, city, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
            LastSearchResults = CreateSearchResultsCollection(combinedQuery, businessResults, (long)elapsed);
            _logger.LogInformation("[ChatFunctionService.FindBusinesses] Phase 2: LastSearchResults populated with {Count} business results", businessResults.Count);

            return JsonSerializer.Serialize(new
            {
                searchCriteria = new { businessType, city, country },
                count = matchingPosts.Count,
                businesses = matchingPosts,
                tip = matchingPosts.Any(p => p.location?.coordinates != null) 
                    ? "Some results have GPS coordinates - you can view them on a map!" 
                    : null
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatFunctionService.FindBusinesses] EXCEPTION - RequestId={RequestId}", requestId);
            return $"Error finding businesses: {ex.Message}";
        }
    }

    // ==================== POSTGIS-POWERED SPATIAL SEARCH ====================

    /// <summary>
    /// Search for profiles/businesses near a specific location using PostGIS.
    /// Returns profiles ordered by distance with distance information.
    /// </summary>
    [Description("Find profiles and businesses near a geographic location. Uses GPS coordinates to find nearby users, businesses, or organizations. Perfect for 'Find pizza near me' or 'Who's nearby?' queries.")]
    public async Task<string> SearchNearbyProfiles(
        [Description("Latitude coordinate of the center point (e.g., 13.6969 for San Salvador)")]
        double latitude,
        [Description("Longitude coordinate of the center point (e.g., -89.2182 for San Salvador)")]
        double longitude,
        [Description("Search radius in kilometers (default 10km, max 100km)")]
        double radiusKm = 10,
        [Description("Optional: filter by profile type (Business, Personal, Organization)")]
        string? profileType = null,
        [Description("Maximum number of results (default 10, max 20)")]
        int maxResults = 10)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.SearchNearbyProfiles] START - RequestId={RequestId}, Lat={Lat}, Lng={Lng}, Radius={Radius}km, Type={Type}, MaxResults={MaxResults}",
            requestId, latitude, longitude, radiusKm, profileType, maxResults);

        try
        {
            // Validate inputs
            if (latitude < -90 || latitude > 90)
                return "Invalid latitude. Must be between -90 and 90.";
            if (longitude < -180 || longitude > 180)
                return "Invalid longitude. Must be between -180 and 180.";

            // Limit radius and results
            radiusKm = Math.Min(radiusKm, 100);
            maxResults = Math.Min(maxResults, 20);

            // Use PostGIS to find nearby profiles
            var nearbyProfiles = await _locationService.FindNearbyProfilesAsync(
                latitude, longitude, radiusKm, maxResults);

            // Filter by type if specified
            if (!string.IsNullOrWhiteSpace(profileType))
            {
                nearbyProfiles = nearbyProfiles
                    .Where(p => p.ProfileType?.Name?.Contains(profileType, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.SearchNearbyProfiles] SUCCESS - RequestId={RequestId}, FoundCount={Count}, Duration={Duration}ms",
                requestId, nearbyProfiles.Count, elapsed);

            if (!nearbyProfiles.Any())
            {
                return $"No profiles found within {radiusKm}km of the specified location.";
            }

            // Phase 2: Populate structured results for card rendering
            var businessResults = nearbyProfiles
                .Select((p, index) => MapProfileDtoToBusinessResult(p, index))
                .ToList();
            
            LastSearchResults = CreateSearchResultsCollection($"nearby {profileType ?? "profiles"}", businessResults, (long)elapsed);
            _logger.LogInformation("[ChatFunctionService.SearchNearbyProfiles] Phase 2: LastSearchResults populated with {Count} business results", businessResults.Count);

            var results = nearbyProfiles.Select(p => new
            {
                id = p.Id,
                displayName = p.DisplayName,
                profileType = p.ProfileType?.Name ?? "Unknown",
                bio = p.Bio?.Length > 100 ? p.Bio.Substring(0, 100) + "..." : p.Bio,
                distanceKm = p.DistanceKm.HasValue ? Math.Round(p.DistanceKm.Value, 2) : (double?)null,
                distanceText = p.DistanceKm.HasValue ? $"{p.DistanceKm.Value:F1} km away" : null,
                location = p.LocationDisplay,
                isFollowing = _followerRepository.IsFollowingAsync(_currentProfileId, p.Id).Result
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                query = new { latitude, longitude, radiusKm, profileType },
                count = results.Count,
                profiles = results
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchNearbyProfiles] EXCEPTION - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            return $"Error searching nearby profiles: {ex.Message}";
        }
    }

    /// <summary>
    /// Search for posts near a specific location using PostGIS.
    /// Returns posts ordered by distance with distance information.
    /// </summary>
    [Description("Find posts near a geographic location. Uses GPS coordinates to find nearby content, events, or updates. Perfect for 'What's happening around me?' or 'Events near downtown' queries.")]
    public async Task<string> SearchNearbyPosts(
        [Description("Latitude coordinate of the center point")]
        double latitude,
        [Description("Longitude coordinate of the center point")]
        double longitude,
        [Description("Search radius in kilometers (default 10km, max 100km)")]
        double radiusKm = 10,
        [Description("Maximum number of results (default 10, max 20)")]
        int maxResults = 10)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.SearchNearbyPosts] START - RequestId={RequestId}, Lat={Lat}, Lng={Lng}, Radius={Radius}km, MaxResults={MaxResults}",
            requestId, latitude, longitude, radiusKm, maxResults);

        try
        {
            // Validate inputs
            if (latitude < -90 || latitude > 90)
                return "Invalid latitude. Must be between -90 and 90.";
            if (longitude < -180 || longitude > 180)
                return "Invalid longitude. Must be between -180 and 180.";

            // Limit radius and results
            radiusKm = Math.Min(radiusKm, 100);
            maxResults = Math.Min(maxResults, 20);

            // Use PostGIS to find nearby posts
            var nearbyPosts = await _locationService.FindNearbyPostsAsync(
                latitude, longitude, radiusKm, page: 1, pageSize: maxResults);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.SearchNearbyPosts] SUCCESS - RequestId={RequestId}, FoundCount={Count}, Duration={Duration}ms",
                requestId, nearbyPosts.Count, elapsed);

            if (!nearbyPosts.Any())
            {
                return $"No posts found within {radiusKm}km of the specified location.";
            }

            // Phase 2: Populate structured results for card rendering
            var businessResults = nearbyPosts
                .Select((p, index) => MapPostDtoToBusinessResult(p, index))
                .ToList();
            
            LastSearchResults = CreateSearchResultsCollection("nearby posts", businessResults, (long)elapsed);
            _logger.LogInformation("[ChatFunctionService.SearchNearbyPosts] Phase 2: LastSearchResults populated with {Count} business results", businessResults.Count);

            var results = nearbyPosts.Select(p => new
            {
                id = p.Id,
                content = p.Content?.Length > 150 ? p.Content.Substring(0, 150) + "..." : p.Content,
                authorName = p.Profile?.DisplayName ?? "Unknown",
                authorHandle = p.Profile?.Handle,
                authorType = p.Profile?.ProfileType?.Name ?? "Unknown",
                createdAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                postType = p.PostType.ToString(),
                distanceKm = p.DistanceKm.HasValue ? Math.Round(p.DistanceKm.Value, 2) : (double?)null,
                distanceText = p.DistanceKm.HasValue ? $"{p.DistanceKm.Value:F1} km away" : null,
                hasAttachments = p.Attachments?.Any() == true,
                link = $"/post/{p.Id}"
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                query = new { latitude, longitude, radiusKm },
                count = results.Count,
                posts = results
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchNearbyPosts] EXCEPTION - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            return $"Error searching nearby posts: {ex.Message}";
        }
    }

    /// <summary>
    /// Calculate distance between two locations using PostGIS.
    /// </summary>
    [Description("Calculate the distance in kilometers between two geographic points. Useful for 'How far is X from Y?' queries.")]
    public async Task<string> CalculateDistance(
        [Description("First location latitude")]
        double lat1,
        [Description("First location longitude")]
        double lng1,
        [Description("Second location latitude")]
        double lat2,
        [Description("Second location longitude")]
        double lng2)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[ChatFunctionService.CalculateDistance] START - RequestId={RequestId}, From=({Lat1},{Lng1}), To=({Lat2},{Lng2})",
            requestId, lat1, lng1, lat2, lng2);

        try
        {
            var distanceKm = await _locationService.CalculateDistanceAsync(lat1, lng1, lat2, lng2);
            
            _logger.LogInformation("[ChatFunctionService.CalculateDistance] SUCCESS - RequestId={RequestId}, Distance={Distance}km",
                requestId, distanceKm);

            return JsonSerializer.Serialize(new
            {
                from = new { latitude = lat1, longitude = lng1 },
                to = new { latitude = lat2, longitude = lng2 },
                distanceKm = Math.Round(distanceKm, 2),
                distanceText = distanceKm < 1 
                    ? $"{Math.Round(distanceKm * 1000)} meters" 
                    : $"{distanceKm:F1} km"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatFunctionService.CalculateDistance] EXCEPTION - RequestId={RequestId}", requestId);
            return $"Error calculating distance: {ex.Message}";
        }
    }

    /// <summary>
    /// Get address information from coordinates (reverse geocoding).
    /// </summary>
    [Description("Convert GPS coordinates to a human-readable address. Useful for 'What place is at these coordinates?' queries.")]
    public async Task<string> GetAddressFromCoordinates(
        [Description("Latitude coordinate")]
        double latitude,
        [Description("Longitude coordinate")]
        double longitude)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[ChatFunctionService.GetAddressFromCoordinates] START - RequestId={RequestId}, Lat={Lat}, Lng={Lng}",
            requestId, latitude, longitude);

        try
        {
            var location = await _locationService.ReverseGeocodeAsync(latitude, longitude);
            
            if (location == null)
            {
                return "Could not find address for the specified coordinates.";
            }

            _logger.LogInformation("[ChatFunctionService.GetAddressFromCoordinates] SUCCESS - RequestId={RequestId}, City={City}, Country={Country}",
                requestId, location.City, location.Country);

            return JsonSerializer.Serialize(new
            {
                coordinates = new { latitude, longitude },
                address = new
                {
                    city = location.City,
                    state = location.State,
                    country = location.Country,
                    displayName = $"{location.City}, {location.State}, {location.Country}".Trim(',', ' ')
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatFunctionService.GetAddressFromCoordinates] EXCEPTION - RequestId={RequestId}", requestId);
            return $"Error getting address: {ex.Message}";
        }
    }

    /// <summary>
    /// Get coordinates from an address (geocoding).
    /// </summary>
    [Description("Convert an address or city name to GPS coordinates. Useful for 'Where is San Salvador?' or getting coordinates before searching nearby.")]
    public async Task<string> GetCoordinatesFromAddress(
        [Description("City name")]
        string city,
        [Description("State or province (optional)")]
        string? state = null,
        [Description("Country (optional)")]
        string? country = null)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[ChatFunctionService.GetCoordinatesFromAddress] START - RequestId={RequestId}, City={City}, State={State}, Country={Country}",
            requestId, city, state, country);

        try
        {
            if (string.IsNullOrWhiteSpace(city))
                return "City name is required.";

            var coords = await _locationService.GeocodeAsync(city, state, country);
            
            if (!coords.HasValue)
            {
                return $"Could not find coordinates for '{city}'.";
            }

            _logger.LogInformation("[ChatFunctionService.GetCoordinatesFromAddress] SUCCESS - RequestId={RequestId}, Lat={Lat}, Lng={Lng}",
                requestId, coords.Value.Latitude, coords.Value.Longitude);

            return JsonSerializer.Serialize(new
            {
                query = new { city, state, country },
                coordinates = new 
                { 
                    latitude = coords.Value.Latitude, 
                    longitude = coords.Value.Longitude 
                },
                hint = "You can use these coordinates with SearchNearbyProfiles or SearchNearbyPosts"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatFunctionService.GetCoordinatesFromAddress] EXCEPTION - RequestId={RequestId}", requestId);
            return $"Error getting coordinates: {ex.Message}";
        }
    }

    /// <summary>
    /// Search for profiles or posts near the user's current location (if set).
    /// Uses the location previously set via SetCurrentLocation().
    /// </summary>
    [Description("Find profiles or posts near your current location. Perfect for 'What's near me?' or 'Who's around?' queries. Requires location to be set first.")]
    public async Task<string> SearchNearMe(
        [Description("What to search for: 'profiles', 'posts', or 'all' (default)")]
        string searchType = "all",
        [Description("Search radius in kilometers (default 5km, max 50km)")]
        double radiusKm = 5,
        [Description("Filter by profile type (e.g., 'Business', 'Personal') - only for profile searches")]
        string? profileType = null)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[ChatFunctionService.SearchNearMe] START - RequestId={RequestId}, Type={SearchType}, Radius={Radius}km",
            requestId, searchType, radiusKm);

        try
        {
            // Check if we have the user's location
            if (!_currentLocation.HasValue)
            {
                return "I don't know your current location. Please share your location first, or use SearchNearbyProfiles/SearchNearbyPosts with specific coordinates.";
            }

            var lat = _currentLocation.Value.Latitude;
            var lng = _currentLocation.Value.Longitude;
            radiusKm = Math.Min(radiusKm, 50);
            searchType = searchType.ToLowerInvariant();

            var results = new Dictionary<string, object>
            {
                ["yourLocation"] = new { latitude = lat, longitude = lng },
                ["searchRadius"] = $"{radiusKm} km"
            };

            // Search for profiles if requested
            if (searchType == "profiles" || searchType == "all")
            {
                var profilesJson = await SearchNearbyProfiles(lat, lng, radiusKm, profileType, 5);
                results["nearbyProfiles"] = profilesJson;
            }

            // Search for posts if requested
            if (searchType == "posts" || searchType == "all")
            {
                var postsJson = await SearchNearbyPosts(lat, lng, radiusKm, 5);
                results["nearbyPosts"] = postsJson;
            }

            _logger.LogInformation("[ChatFunctionService.SearchNearMe] SUCCESS - RequestId={RequestId}", requestId);

            return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatFunctionService.SearchNearMe] EXCEPTION - RequestId={RequestId}", requestId);
            return $"Error searching near your location: {ex.Message}";
        }
    }

    /// <summary>
    /// Get the user's current location status.
    /// </summary>
    [Description("Check if the user's location is set and show their current coordinates if available.")]
    public string GetCurrentLocationStatus()
    {
        if (_currentLocation.HasValue)
        {
            return JsonSerializer.Serialize(new
            {
                hasLocation = true,
                latitude = _currentLocation.Value.Latitude,
                longitude = _currentLocation.Value.Longitude,
                message = "Location is set. You can use SearchNearMe or location-based functions."
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        else
        {
            return JsonSerializer.Serialize(new
            {
                hasLocation = false,
                message = "No location set. The user needs to share their location first, or you can search using specific coordinates."
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    // ==================== PHASE 2: STRUCTURED RESULT MAPPING ====================

    /// <summary>
    /// Maps a Profile entity to a BusinessSearchResultDto for structured card rendering
    /// </summary>
    private BusinessSearchResultDto MapProfileToBusinessResult(Profile profile, double? distanceKm = null, int displayOrder = 0)
    {
        return new BusinessSearchResultDto
        {
            Id = profile.Id,
            ProfileId = profile.Id,
            ResultType = SearchResultType.Business,
            MatchSource = SearchMatchSource.Hybrid, // Function calls use hybrid matching
            RelevanceScore = 1.0 - (distanceKm ?? 0) / 100, // Simple distance-based relevance
            DisplayOrder = displayOrder,
            Title = profile.DisplayName,
            Description = profile.Bio?.Length > 150 ? profile.Bio.Substring(0, 150) + "..." : profile.Bio,
            Handle = profile.Handle,
            Category = profile.ProfileType?.Name ?? "Business",
            ImageUrl = profile.Avatar,
            City = profile.Location?.City,
            Department = profile.Location?.State,
            Latitude = profile.Location?.Latitude,
            Longitude = profile.Location?.Longitude,
            DistanceKm = distanceKm,
            Tags = null // Could extract from profile metadata
        };
    }

    /// <summary>
    /// Maps a ProfileDto (from location service) to a BusinessSearchResultDto for structured card rendering
    /// </summary>
    private BusinessSearchResultDto MapProfileDtoToBusinessResult(ProfileDto profile, int displayOrder = 0)
    {
        return new BusinessSearchResultDto
        {
            Id = profile.Id,
            ProfileId = profile.Id,
            ResultType = SearchResultType.Business,
            MatchSource = SearchMatchSource.Geographic, // Location-based searches use geographic matching
            RelevanceScore = 1.0 - (profile.DistanceKm ?? 0) / 100,
            DisplayOrder = displayOrder,
            Title = profile.DisplayName,
            Description = profile.Bio?.Length > 150 ? profile.Bio.Substring(0, 150) + "..." : profile.Bio,
            Handle = profile.Handle,
            Category = profile.ProfileType?.Name ?? "Business",
            ImageUrl = profile.Avatar,
            City = profile.Location?.City,
            Department = profile.Location?.State,
            Latitude = profile.Location?.Latitude,
            Longitude = profile.Location?.Longitude,
            DistanceKm = profile.DistanceKm,
            Tags = profile.Tags?.ToArray()
        };
    }

    /// <summary>
    /// Maps a Post entity to a BusinessSearchResultDto for structured card rendering
    /// </summary>
    private BusinessSearchResultDto MapPostToBusinessResult(Post post, double? distanceKm = null, int displayOrder = 0)
    {
        return new BusinessSearchResultDto
        {
            Id = post.Id,
            ProfileId = post.ProfileId,
            ResultType = SearchResultType.Business,
            MatchSource = SearchMatchSource.Hybrid,
            RelevanceScore = 1.0 - (distanceKm ?? 0) / 100,
            DisplayOrder = displayOrder,
            Title = post.Profile?.DisplayName ?? "Business",
            Description = post.Content?.Length > 150 ? post.Content.Substring(0, 150) + "..." : post.Content,
            Handle = post.Profile?.Handle,
            Category = post.Profile?.ProfileType?.Name ?? "Business",
            ImageUrl = post.Attachments?.FirstOrDefault()?.Url ?? post.Profile?.Avatar,
            City = post.Location?.City,
            Department = post.Location?.State,
            Latitude = post.Location?.Latitude,
            Longitude = post.Location?.Longitude,
            DistanceKm = distanceKm,
            Tags = null
        };
    }

    /// <summary>
    /// Maps a PostDto (from location service) to a BusinessSearchResultDto for structured card rendering
    /// </summary>
    private BusinessSearchResultDto MapPostDtoToBusinessResult(PostDto post, int displayOrder = 0)
    {
        return new BusinessSearchResultDto
        {
            Id = post.Id,
            ProfileId = post.Profile?.Id ?? Guid.Empty,
            ResultType = SearchResultType.Business,
            MatchSource = SearchMatchSource.Geographic,
            RelevanceScore = 1.0 - (post.DistanceKm ?? 0) / 100,
            DisplayOrder = displayOrder,
            Title = post.Profile?.DisplayName ?? "Business",
            Description = post.Content?.Length > 150 ? post.Content.Substring(0, 150) + "..." : post.Content,
            Handle = post.Profile?.Handle,
            Category = post.Profile?.ProfileType?.Name ?? "Business",
            ImageUrl = post.Attachments?.FirstOrDefault()?.FilePath ?? post.Profile?.Avatar,
            City = post.Location?.City,
            Department = post.Location?.State,
            Latitude = post.Location?.Latitude,
            Longitude = post.Location?.Longitude,
            DistanceKm = post.DistanceKm,
            Tags = post.Tags?.ToArray()
        };
    }

    /// <summary>
    /// Creates a SearchResultsCollectionDto from a list of business results
    /// </summary>
    private SearchResultsCollectionDto CreateSearchResultsCollection(
        string query,
        List<BusinessSearchResultDto> businesses,
        long searchTimeMs)
    {
        return new SearchResultsCollectionDto
        {
            Query = query,
            TotalCount = businesses.Count,
            SearchTimeMs = searchTimeMs,
            Businesses = businesses,
            Events = [],
            Procedures = [],
            Tourism = [],
            Products = [],
            Services = []
        };
    }

    /// <summary>
    /// Creates a SearchResultsCollectionDto from multiple result types
    /// </summary>
    private SearchResultsCollectionDto CreateMixedSearchResultsCollection(
        string query,
        List<BusinessSearchResultDto>? businesses = null,
        List<EventSearchResultDto>? events = null,
        List<ProcedureSearchResultDto>? procedures = null,
        List<TourismSearchResultDto>? tourism = null,
        List<ProductSearchResultDto>? products = null,
        List<ServiceSearchResultDto>? services = null,
        long searchTimeMs = 0)
    {
        var businessList = businesses ?? [];
        var eventList = events ?? [];
        var procedureList = procedures ?? [];
        var tourismList = tourism ?? [];
        var productList = products ?? [];
        var serviceList = services ?? [];

        return new SearchResultsCollectionDto
        {
            Query = query,
            TotalCount = businessList.Count + eventList.Count + procedureList.Count + 
                        tourismList.Count + productList.Count + serviceList.Count,
            SearchTimeMs = searchTimeMs,
            Businesses = businessList,
            Events = eventList,
            Procedures = procedureList,
            Tourism = tourismList,
            Products = productList,
            Services = serviceList
        };
    }
}
