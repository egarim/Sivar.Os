using System.ComponentModel;
using System.Text.Json;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Services;

/// <summary>
/// Service that provides AI-callable functions for social platform actions
/// </summary>
public class ChatFunctionService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IPostRepository _postRepository;
    private readonly IProfileFollowerRepository _followerRepository;
    private readonly ILogger<ChatFunctionService> _logger;
    private Guid _currentProfileId; // Set before each AI call

    public ChatFunctionService(
        IProfileRepository profileRepository,
        IPostRepository postRepository,
        IProfileFollowerRepository followerRepository,
        ILogger<ChatFunctionService> logger)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _followerRepository = followerRepository ?? throw new ArgumentNullException(nameof(followerRepository));
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

            var matchingProfiles = allProfiles
                .Where(p => !p.IsDeleted &&
                    (p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     p.Bio.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     p.ProfileType?.Name.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                .Take(maxResults)
                .Select(p => new
                {
                    id = p.Id,
                    displayName = p.DisplayName,
                    profileType = p.ProfileType?.Name ?? "Unknown",
                    bio = p.Bio.Length > 100 ? p.Bio.Substring(0, 100) + "..." : p.Bio,
                    isFollowing = _followerRepository.IsFollowingAsync(_currentProfileId, p.Id).Result
                })
                .ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.SearchProfiles] SUCCESS - RequestId={RequestId}, MatchCount={MatchCount}, Duration={Duration}ms",
                requestId, matchingProfiles.Count, elapsed);

            if (!matchingProfiles.Any())
            {
                return $"No profiles found matching '{query}'";
            }

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
    /// Search for posts by content or author
    /// </summary>
    [Description("Search for posts on the social network. Returns posts matching the search query.")]
    public async Task<string> SearchPosts(
        [Description("The search query - can be post content keywords or author name")]
        string query,
        [Description("Maximum number of results to return (default 5, max 10)")]
        int maxResults = 5)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ChatFunctionService.SearchPosts] START - RequestId={RequestId}, Timestamp={Timestamp}, Query={Query}, MaxResults={MaxResults}",
            requestId, startTime, query, maxResults);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogError("[ChatFunctionService.SearchPosts] VALIDATION ERROR - RequestId={RequestId}, QueryNull=true",
                    requestId);
                throw new ArgumentException("Query cannot be null or empty", nameof(query));
            }

            _logger.LogInformation("AI searching for posts with query: {Query}", query);

            // Limit max results
            maxResults = Math.Min(maxResults, 10);

            // Get posts with profiles
            var allPosts = await _postRepository.GetAllAsync();
            
            var matchingPosts = allPosts
                .Where(p => !p.IsDeleted &&
                    (p.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     p.Profile?.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                .OrderByDescending(p => p.CreatedAt)
                .Take(maxResults)
                .Select(p => new
                {
                    id = p.Id,
                    content = p.Content.Length > 150 ? p.Content.Substring(0, 150) + "..." : p.Content,
                    authorName = p.Profile?.DisplayName ?? "Unknown",
                    createdAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    postType = p.PostType
                })
                .ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatFunctionService.SearchPosts] SUCCESS - RequestId={RequestId}, MatchCount={MatchCount}, Duration={Duration}ms",
                requestId, matchingPosts.Count, elapsed);

            if (!matchingPosts.Any())
            {
                return $"No posts found matching '{query}'";
            }

            return JsonSerializer.Serialize(new
            {
                query,
                count = matchingPosts.Count,
                posts = matchingPosts
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchPosts] VALIDATION ERROR - RequestId={RequestId}, Query={Query}, Duration={Duration}ms",
                requestId, query, elapsed);
            return $"Validation error searching posts: {ex.Message}";
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatFunctionService.SearchPosts] EXCEPTION - RequestId={RequestId}, Query={Query}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, query, ex.GetType().Name, elapsed);
            return $"Error searching posts: {ex.Message}";
        }
    }

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
}
