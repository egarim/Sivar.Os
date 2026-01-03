using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sivar.Os.Controllers;

/// <summary>
/// Public API controller for unauthenticated access to public content.
/// Used to attract new users by allowing them to view public posts and profiles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IProfileService _profileService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        IPostService postService,
        IProfileService profileService,
        ILogger<PublicController> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets public posts feed for unauthenticated users
    /// </summary>
    /// <remarks>
    /// Returns only posts with Visibility = Public, sorted by most recent.
    /// This endpoint is designed to showcase content to potential new users.
    /// </remarks>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page (max 50)</param>
    /// <param name="profileType">Optional filter by profile type (e.g., "Business", "Personal")</param>
    /// <returns>Paginated list of public posts</returns>
    [HttpGet("feed")]
    [SwaggerOperation(
        Summary = "Get public posts feed",
        Description = "Returns public posts for unauthenticated users to browse content",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Public feed retrieved successfully", typeof(PostFeedDto))]
    public async Task<ActionResult<PostFeedDto>> GetPublicFeed(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? profileType = null)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetPublicFeed] Fetching public feed - page={Page}, pageSize={PageSize}, profileType={ProfileType}",
                page, pageSize, profileType);

            // Limit page size to prevent abuse
            if (pageSize > 50)
                pageSize = 50;

            var (posts, totalCount) = await _postService.GetPublicFeedAsync(page + 1, pageSize, profileType);

            var feed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            _logger.LogInformation("[PublicController.GetPublicFeed] Returning {Count} public posts", feed.Posts.Count);
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetPublicFeed] Error fetching public feed");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific public post by ID
    /// </summary>
    /// <remarks>
    /// Returns post details only if the post has Visibility = Public.
    /// Private or followers-only posts will return 404.
    /// </remarks>
    /// <param name="id">Post ID</param>
    /// <returns>Post details if public, 404 otherwise</returns>
    [HttpGet("posts/{id:guid}")]
    [SwaggerOperation(
        Summary = "Get a public post by ID",
        Description = "Returns post details only if the post is publicly visible",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Post found", typeof(PostDto))]
    [SwaggerResponse(404, "Post not found or not public")]
    public async Task<ActionResult<PostDto>> GetPublicPost(Guid id)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetPublicPost] Fetching public post - PostId={PostId}", id);

            var post = await _postService.GetPublicPostByIdAsync(id);

            if (post == null)
            {
                _logger.LogInformation("[PublicController.GetPublicPost] Post not found or not public - PostId={PostId}", id);
                return NotFound("Post not found or not publicly accessible");
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetPublicPost] Error fetching post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a public profile by ID
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <returns>Profile details if public</returns>
    [HttpGet("profiles/{id:guid}")]
    [SwaggerOperation(
        Summary = "Get a public profile by ID",
        Description = "Returns profile summary for publicly visible profiles",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Profile found", typeof(ProfileDto))]
    [SwaggerResponse(404, "Profile not found")]
    public async Task<ActionResult<ProfileDto>> GetPublicProfile(Guid id)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetPublicProfile] Fetching public profile - ProfileId={ProfileId}", id);

            var profile = await _profileService.GetPublicProfileByIdAsync(id);

            if (profile == null)
            {
                _logger.LogInformation("[PublicController.GetPublicProfile] Profile not found - ProfileId={ProfileId}", id);
                return NotFound("Profile not found");
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetPublicProfile] Error fetching profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a public profile by handle/username
    /// </summary>
    /// <param name="handle">Profile handle (username)</param>
    /// <returns>Profile details if public</returns>
    [HttpGet("profiles/handle/{handle}")]
    [SwaggerOperation(
        Summary = "Get a public profile by handle",
        Description = "Returns profile summary for publicly visible profiles by their handle/username",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Profile found", typeof(ProfileDto))]
    [SwaggerResponse(404, "Profile not found")]
    public async Task<ActionResult<ProfileDto>> GetPublicProfileByHandle(string handle)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetPublicProfileByHandle] Fetching public profile - Handle={Handle}", handle);

            var profile = await _profileService.GetPublicProfileByHandleAsync(handle);

            if (profile == null)
            {
                _logger.LogInformation("[PublicController.GetPublicProfileByHandle] Profile not found - Handle={Handle}", handle);
                return NotFound("Profile not found");
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetPublicProfileByHandle] Error fetching profile by handle {Handle}", handle);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets posts by a specific public profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Public posts by the profile</returns>
    [HttpGet("profiles/{profileId:guid}/posts")]
    [SwaggerOperation(
        Summary = "Get public posts by profile",
        Description = "Returns public posts from a specific profile",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Posts retrieved", typeof(PostFeedDto))]
    public async Task<ActionResult<PostFeedDto>> GetPublicPostsByProfile(
        Guid profileId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetPublicPostsByProfile] Fetching posts for profile - ProfileId={ProfileId}", profileId);

            if (pageSize > 50)
                pageSize = 50;

            var (posts, totalCount) = await _postService.GetPublicPostsByProfileAsync(profileId, page + 1, pageSize);

            var feed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetPublicPostsByProfile] Error fetching posts for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets similar profiles based on tags, location, or profile type
    /// </summary>
    /// <param name="profileId">Profile ID to find similar profiles for</param>
    /// <param name="limit">Maximum number of similar profiles to return</param>
    /// <returns>List of similar profiles</returns>
    [HttpGet("profiles/{profileId:guid}/similar")]
    [SwaggerOperation(
        Summary = "Get similar profiles",
        Description = "Returns profiles similar to the given profile based on tags, type, and location",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Similar profiles retrieved", typeof(List<ProfileSummaryDto>))]
    public async Task<ActionResult<List<ProfileSummaryDto>>> GetSimilarProfiles(
        Guid profileId,
        [FromQuery] int limit = 4)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetSimilarProfiles] Finding similar profiles for ProfileId={ProfileId}", profileId);

            if (limit > 10)
                limit = 10;

            var similarProfiles = await _profileService.GetSimilarProfilesAsync(profileId, limit);
            return Ok(similarProfiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetSimilarProfiles] Error finding similar profiles for {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets trending posts for public display
    /// </summary>
    /// <param name="limit">Maximum number of trending posts to return</param>
    /// <returns>List of trending posts</returns>
    [HttpGet("posts/trending")]
    [SwaggerOperation(
        Summary = "Get trending posts",
        Description = "Returns trending public posts based on engagement",
        Tags = new[] { "Public" }
    )]
    [SwaggerResponse(200, "Trending posts retrieved", typeof(PostFeedDto))]
    public async Task<ActionResult<PostFeedDto>> GetTrendingPosts([FromQuery] int limit = 5)
    {
        try
        {
            _logger.LogInformation("[PublicController.GetTrendingPosts] Fetching trending posts - limit={Limit}", limit);

            if (limit > 20)
                limit = 20;

            var (posts, totalCount) = await _postService.GetTrendingPublicPostsAsync(limit);

            var feed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = 0,
                PageSize = limit,
                TotalCount = totalCount
            };

            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicController.GetTrendingPosts] Error fetching trending posts");
            return StatusCode(500, "Internal server error");
        }
    }
}
