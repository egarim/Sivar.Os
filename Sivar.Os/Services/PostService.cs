using System.Text.Json;
using Microsoft.Extensions.AI;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;


namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for Post management in the activity stream
/// Provides business logic layer for post operations with validation and error handling
/// </summary>
public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IReactionRepository _reactionRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IPostAttachmentRepository _postAttachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IVectorEmbeddingService? _vectorEmbeddingService;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IReactionRepository reactionRepository,
        ICommentRepository commentRepository,
        IPostAttachmentRepository postAttachmentRepository,
        IFileStorageService fileStorageService,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _postAttachmentRepository = postAttachmentRepository ?? throw new ArgumentNullException(nameof(postAttachmentRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _vectorEmbeddingService = null; // Disabled until properly configured
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new post for the authenticated user's active profile
    /// </summary>
    public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto createPostDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || createPostDto == null)
            return null;

        _logger.LogInformation("[CreatePostAsync] START - KeycloakId={keycloakId}, ProfileIdFromRequest={profileId}", 
            keycloakId, createPostDto.ProfileId);

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        _logger.LogInformation("[CreatePostAsync] Retrieved user: UserId={userId}, ActiveProfileId={activeProfileId}", 
            user?.Id.ToString() ?? "NULL", 
            user?.ActiveProfileId.ToString() ?? "NULL");
        
        if (user == null)
        {
            _logger.LogWarning("[CreatePostAsync] User not found for keycloakId={keycloakId}", keycloakId);
            return null;
        }

        // Use ProfileId from request (sent by client) - this is the authoritative source
        if (createPostDto.ProfileId == Guid.Empty)
        {
            _logger.LogWarning("[CreatePostAsync] ProfileId from request is empty");
            return null;
        }

        _logger.LogInformation("[CreatePostAsync] Using ProfileId from request: {profileId}", createPostDto.ProfileId);

        // Get the profile using the ID sent by client
        var activeProfile = await _profileRepository.GetByIdIgnoringFiltersAsync(createPostDto.ProfileId);
        _logger.LogInformation("[CreatePostAsync] Profile lookup result: ProfileId={profileId}, FoundProfile={found}, UserId={userId}", 
            createPostDto.ProfileId, 
            activeProfile != null ? "YES" : "NO",
            activeProfile?.UserId.ToString() ?? "NULL");

        if (activeProfile == null)
        {
            _logger.LogWarning("[CreatePostAsync] Profile not found: {profileId}", createPostDto.ProfileId);
            return null;
        }

        // Verify that this profile belongs to the current user
        if (activeProfile.UserId != user.Id)
        {
            _logger.LogWarning("[CreatePostAsync] Profile {profileId} belongs to UserId={profileUserId}, not current user {userId}", 
                createPostDto.ProfileId, activeProfile.UserId, user.Id);
            return null;
        }

        // Validate content
        if (string.IsNullOrWhiteSpace(createPostDto.Content))
            return null;

        _logger.LogInformation("[CreatePostAsync] Content validated: Length={length}", createPostDto.Content.Length);

        // Create post entity
        var post = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = activeProfile.Id,
            Content = createPostDto.Content.Trim(),
            PostType = createPostDto.PostType,
            Visibility = createPostDto.Visibility,
            Language = createPostDto.Language ?? "en",
            Tags = JsonSerializer.Serialize(createPostDto.Tags ?? new List<string>()),
            BusinessMetadata = createPostDto.BusinessMetadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set location if provided
        if (createPostDto.Location != null)
        {
            post.Location = new Location
            {
                City = createPostDto.Location.City,
                State = createPostDto.Location.State,
                Country = createPostDto.Location.Country,
                Latitude = createPostDto.Location.Latitude,
                Longitude = createPostDto.Location.Longitude
            };
        }

        try
        {
            _logger.LogInformation("[CreatePostAsync] Saving post: PostId={postId}, ProfileId={profileId}", 
                post.Id, post.ProfileId);

            // Vector embedding generation disabled until service is properly configured
            // TODO: Enable when vector embedding service is available

            await _postRepository.AddAsync(post);
            await _postRepository.SaveChangesAsync();

            _logger.LogInformation("[CreatePostAsync] Post saved successfully: PostId={postId}", post.Id);

            // Process attachments if provided
            if (createPostDto.Attachments?.Any() == true)
            {
                await ProcessPostAttachmentsAsync(post.Id, createPostDto.Attachments);
            }

            return await MapToPostDtoAsync(post, keycloakId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a post by ID with permission validation
    /// </summary>
    public async Task<PostDto?> GetPostByIdAsync(Guid postId, string? requestingKeycloakId = null, bool includeReactions = true, bool includeComments = true)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return null;

        // Check if user can view this post
        if (!await CanUserViewPostAsync(postId, requestingKeycloakId))
            return null;

        return await MapToPostDtoAsync(post, requestingKeycloakId, includeReactions, includeComments);
    }

    /// <summary>
    /// Updates an existing post (only by the author)
    /// </summary>
    public async Task<PostDto?> UpdatePostAsync(Guid postId, string keycloakId, UpdatePostDto updatePostDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || updatePostDto == null)
            return null;

        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return null;

        // Check if user is the author
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != post.ProfileId)
            return null;

        // Update post properties
        if (!string.IsNullOrWhiteSpace(updatePostDto.Content))
        {
            post.Content = updatePostDto.Content.Trim();
            post.IsEdited = true;
            post.EditedAt = DateTime.UtcNow;
        }

        if (updatePostDto.Visibility.HasValue)
            post.Visibility = updatePostDto.Visibility.Value;

        if (updatePostDto.Tags != null)
            post.Tags = JsonSerializer.Serialize(updatePostDto.Tags);

        if (updatePostDto.Location != null)
        {
            post.Location = new Location
            {
                City = updatePostDto.Location.City,
                State = updatePostDto.Location.State,
                Country = updatePostDto.Location.Country,
                Latitude = updatePostDto.Location.Latitude,
                Longitude = updatePostDto.Location.Longitude
            };
        }

        if (updatePostDto.BusinessMetadata != null)
            post.BusinessMetadata = updatePostDto.BusinessMetadata;

        post.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Vector embedding regeneration disabled until service is properly configured
            // TODO: Enable when vector embedding service is available

            await _postRepository.UpdateAsync(post);
            return await MapToPostDtoAsync(post, keycloakId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes a post (only by the author)
    /// </summary>
    public async Task<bool> DeletePostAsync(Guid postId, string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return false;

        // Check if user is the author
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != post.ProfileId)
            return false;

        try
        {
            // Delete attachments and their files first
            await DeletePostAttachmentsAsync(postId);
            
            // Then delete the post
            return await _postRepository.DeleteAsync(postId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets posts for a user's activity feed with pagination
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetActivityFeedAsync(string keycloakId, int page = 1, int pageSize = 10, string? profileType = null)
    {
        _logger.LogInformation("[PostService.GetActivityFeedAsync] START - KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}, ProfileType={ProfileType}", 
            keycloakId, page, pageSize, profileType);

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[PostService.GetActivityFeedAsync] Empty Keycloak ID provided");
            return (Enumerable.Empty<PostDto>(), 0);
        }

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        _logger.LogInformation("[PostService.GetActivityFeedAsync] User lookup result - User found: {UserFound}, UserId: {UserId}, ActiveProfile: {ActiveProfileId}", 
            user != null, user?.Id, user?.ActiveProfile?.Id);

        if (user == null)
        {
            _logger.LogWarning("[PostService.GetActivityFeedAsync] User not found for KeycloakId={KeycloakId}", keycloakId);
            return (Enumerable.Empty<PostDto>(), 0);
        }

        // Handle case where ActiveProfile is not set - get user's first profile
        Guid profileId;
        if (user.ActiveProfile != null)
        {
            profileId = user.ActiveProfile.Id;
            _logger.LogInformation("[PostService.GetActivityFeedAsync] Using ActiveProfile: {ProfileId}", profileId);
        }
        else
        {
            _logger.LogWarning("[PostService.GetActivityFeedAsync] ActiveProfile is NULL, fetching user's profiles for UserId={UserId}", user.Id);
            var userProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: false);
            var firstProfile = userProfiles.FirstOrDefault();
            
            if (firstProfile == null)
            {
                _logger.LogWarning("[PostService.GetActivityFeedAsync] No profiles found for UserId={UserId}", user.Id);
                return (Enumerable.Empty<PostDto>(), 0);
            }
            
            profileId = firstProfile.Id;
            _logger.LogInformation("[PostService.GetActivityFeedAsync] Using first available profile: {ProfileId}, DisplayName={DisplayName}", 
                profileId, firstProfile.DisplayName);
        }

        _logger.LogInformation("[PostService.GetActivityFeedAsync] Calling repository with ProfileId={ProfileId}", profileId);

        // Use the enhanced repository method with profile type filtering
        var (posts, totalCount) = await _postRepository.GetActivityFeedAsync(profileId, page, pageSize, profileType: profileType);
        
        _logger.LogInformation("[PostService.GetActivityFeedAsync] Repository returned {PostCount} posts (total: {TotalCount})", 
            posts.Count(), totalCount);

        var postDtos = new List<PostDto>();
        foreach (var post in posts)
        {
            var dto = await MapToPostDtoAsync(post, keycloakId);
            if (dto != null)
                postDtos.Add(dto);
        }

        _logger.LogInformation("[PostService.GetActivityFeedAsync] Mapped {DtoCount} DTOs from {PostCount} posts", 
            postDtos.Count, posts.Count());

        return (postDtos, totalCount);
    }

    /// <summary>
    /// Gets posts by a specific profile with pagination
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPostsByProfileAsync(Guid profileId, string? requestingKeycloakId = null, int page = 1, int pageSize = 10)
    {
        var (posts, totalCount) = await _postRepository.GetByProfileAsync(profileId, page, pageSize);
        
        var postDtos = new List<PostDto>();
        foreach (var post in posts)
        {
            if (await CanUserViewPostAsync(post.Id, requestingKeycloakId))
            {
                var dto = await MapToPostDtoAsync(post, requestingKeycloakId);
                if (dto != null)
                    postDtos.Add(dto);
            }
        }

        return (postDtos, totalCount);
    }

    /// <summary>
    /// Gets posts by post type with pagination
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPostsByTypeAsync(PostType postType, string? requestingKeycloakId = null, int page = 1, int pageSize = 10)
    {
        var (posts, totalCount) = await _postRepository.GetByPostTypeAsync(postType, page, pageSize);
        
        var postDtos = new List<PostDto>();
        foreach (var post in posts)
        {
            if (await CanUserViewPostAsync(post.Id, requestingKeycloakId))
            {
                var dto = await MapToPostDtoAsync(post, requestingKeycloakId);
                if (dto != null)
                    postDtos.Add(dto);
            }
        }

        return (postDtos, totalCount);
    }

    /// <summary>
    /// Searches posts by content with pagination
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> SearchPostsAsync(string searchTerm, string? requestingKeycloakId = null, int page = 1, int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return (Enumerable.Empty<PostDto>(), 0);

        var (posts, totalCount) = await _postRepository.SearchPostsAsync(searchTerm.Trim(), null, page, pageSize);
        
        var postDtos = new List<PostDto>();
        foreach (var post in posts)
        {
            if (await CanUserViewPostAsync(post.Id, requestingKeycloakId))
            {
                var dto = await MapToPostDtoAsync(post, requestingKeycloakId);
                if (dto != null)
                    postDtos.Add(dto);
            }
        }

        return (postDtos, totalCount);
    }

    /// <summary>
    /// Gets post engagement statistics (views, reactions, comments)
    /// </summary>
    public async Task<PostEngagementDto?> GetPostEngagementAsync(Guid postId, string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return null;

        // Check if user is the author
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != post.ProfileId)
            return null;

        var reactionCounts = await _reactionRepository.GetReactionCountsByPostAsync(postId);
        var commentCount = await _commentRepository.GetCommentCountByPostAsync(postId);

        return new PostEngagementDto
        {
            PostId = postId,
            TotalReactions = reactionCounts.Sum(r => r.Value),
            ReactionsByType = reactionCounts,
            TotalComments = commentCount,
            TotalShares = 0, // TODO: Implement shares functionality
            EngagementRate = CalculateEngagementRate(reactionCounts.Sum(r => r.Value), commentCount, 0),
            TopReactionType = reactionCounts.OrderByDescending(r => r.Value).FirstOrDefault().Key
        };
    }

    /// <summary>
    /// Validates if a user can view a specific post based on visibility settings
    /// </summary>
    public async Task<bool> CanUserViewPostAsync(Guid postId, string? requestingKeycloakId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return false;

        // Public posts can be viewed by anyone
        if (post.Visibility == VisibilityLevel.Public)
            return true;

        // Private posts require authentication
        if (string.IsNullOrWhiteSpace(requestingKeycloakId))
            return false;

        var requestingUser = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
        if (requestingUser?.ActiveProfile == null)
            return false;

        // Author can always view their own posts
        if (requestingUser.ActiveProfile.Id == post.ProfileId)
            return true;

        // TODO: Implement follower-based visibility when ProfileFollower system is integrated
        switch (post.Visibility)
        {
            case VisibilityLevel.ConnectionsOnly:
                // Check if requesting user follows the post author
                // For now, return false until follower system is integrated
                return false;
            
            case VisibilityLevel.Private:
                return false;
            
            default:
                return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Post entity to PostDto
    /// </summary>
    private async Task<PostDto?> MapToPostDtoAsync(Post post, string? requestingKeycloakId = null, bool includeReactions = true, bool includeComments = true)
    {
        if (post.Profile == null)
        {
            // Load profile if not included
            var profile = await _profileRepository.GetByIdAsync(post.ProfileId);
            if (profile == null)
                return null;
            post.Profile = profile;
        }

        var postDto = new PostDto
        {
            Id = post.Id,
            Profile = await MapToProfileDtoAsync(post.Profile),
            Content = post.Content,
            PostType = post.PostType,
            Visibility = post.Visibility,
            Language = post.Language,
            Tags = string.IsNullOrEmpty(post.Tags) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(post.Tags) ?? new List<string>(),
            BusinessMetadata = post.BusinessMetadata,
            Attachments = await MapAttachmentsToDtosAsync(post.Id),
            CommentCount = includeComments ? await _commentRepository.GetCommentCountByPostAsync(post.Id) : 0,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsEdited = post.IsEdited,
            EditedAt = post.EditedAt
        };

        // Add location if present
        if (post.Location != null)
        {
            postDto = postDto with
            {
                Location = new LocationDto
                {
                    City = post.Location.City,
                    State = post.Location.State,
                    Country = post.Location.Country,
                    Latitude = post.Location.Latitude,
                    Longitude = post.Location.Longitude
                }
            };
        }

        // Add reaction summary if requested
        if (includeReactions)
        {
            var reactionCounts = await _reactionRepository.GetReactionCountsByPostAsync(post.Id);
            ReactionType? userReaction = null;

            if (!string.IsNullOrWhiteSpace(requestingKeycloakId))
            {
                var user = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
                if (user?.ActiveProfile != null)
                {
                    var reaction = await _reactionRepository.GetUserReactionToPostAsync(post.Id, user.ActiveProfile.Id);
                    userReaction = reaction?.ReactionType;
                }
            }

            postDto = postDto with
            {
                ReactionSummary = new PostReactionSummaryDto
                {
                    PostId = post.Id,
                    TotalReactions = reactionCounts.Sum(r => r.Value),
                    ReactionCounts = reactionCounts,
                    UserReaction = userReaction,
                    TopReactionType = reactionCounts.OrderByDescending(r => r.Value).FirstOrDefault().Key,
                    HasUserReacted = userReaction.HasValue
                }
            };
        }

        // TODO: Add comments if requested and when CommentService is implemented

        return postDto;
    }

    /// <summary>
    /// Maps a Profile entity to ProfileDto (simplified version)
    /// </summary>
    private async Task<ProfileDto> MapToProfileDtoAsync(Profile profile)
    {
        // TODO: Use proper ProfileService mapping when available
        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Avatar = profile.Avatar ?? "",
            Bio = profile.Bio ?? "",
            // Add other profile properties as needed
        };
    }

    /// <summary>
    /// Calculates engagement rate based on reactions, comments, and shares
    /// </summary>
    private static double CalculateEngagementRate(int reactions, int comments, int shares)
    {
        var totalEngagement = reactions + comments + shares;
        // TODO: Calculate based on reach/impressions when analytics are implemented
        // For now, return simple engagement count
        return totalEngagement;
    }

    #endregion

    #region Attachment Processing

    /// <summary>
    /// Process and create post attachments
    /// </summary>
    private async Task ProcessPostAttachmentsAsync(Guid postId, List<CreatePostAttachmentDto> attachments)
    {
        foreach (var attachmentDto in attachments)
        {
            var attachment = new PostAttachment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                AttachmentType = attachmentDto.AttachmentType,
                FileId = attachmentDto.FileId,
                Url = attachmentDto.FilePath,
                OriginalFileName = attachmentDto.OriginalFilename,
                MimeType = attachmentDto.MimeType,
                FileSizeBytes = attachmentDto.FileSize,
                Description = attachmentDto.AltText,
                DisplayOrder = attachmentDto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _postAttachmentRepository.AddAsync(attachment);
        }
    }

    /// <summary>
    /// Delete post attachments and their files from storage
    /// </summary>
    private async Task DeletePostAttachmentsAsync(Guid postId)
    {
        var attachments = await _postAttachmentRepository.GetByPostIdAsync(postId);
        
        // Delete files from storage
        var fileIds = attachments
            .Where(a => !string.IsNullOrEmpty(a.FileId))
            .Select(a => a.FileId!)
            .ToList();

        if (fileIds.Any())
        {
            await _fileStorageService.DeleteFilesAsync(fileIds);
        }

        // Delete attachment records
        await _postAttachmentRepository.DeleteByPostIdAsync(postId);
    }

    /// <summary>
    /// Map post attachments to DTOs
    /// </summary>
    private async Task<List<PostAttachmentDto>> MapAttachmentsToDtosAsync(Guid postId)
    {
        var attachments = await _postAttachmentRepository.GetByPostIdOrderedAsync(postId);
        
        return attachments.Select(attachment => new PostAttachmentDto
        {
            Id = attachment.Id,
            AttachmentType = attachment.AttachmentType,
            FileId = attachment.FileId,
            FilePath = attachment.Url,
            OriginalFilename = attachment.OriginalFileName ?? "",
            MimeType = attachment.MimeType ?? "",
            FileSize = attachment.FileSizeBytes ?? 0,
            AltText = attachment.Description,
            DisplayOrder = attachment.DisplayOrder,
            CreatedAt = attachment.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// Gets all posts that have vector embeddings for semantic search
    /// </summary>
    public async Task<List<PostDto>> GetAllPostsWithEmbeddingsAsync()
    {
        var posts = await _postRepository.GetAllWithEmbeddingsAsync();
        
        var postDtos = new List<PostDto>();
        
        foreach (var post in posts)
        {
            var postDto = await MapToPostDtoAsync(post, null, false, false);
            if (postDto != null)
            {
                // Deserialize the embedding data for the DTO
                if (!string.IsNullOrEmpty(post.ContentEmbedding))
                {
                    try
                    {
                        var embeddingArray = JsonSerializer.Deserialize<float[]>(post.ContentEmbedding);
                        postDto = postDto with { ContentEmbedding = embeddingArray };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize embedding for post {PostId}", post.Id);
                    }
                }
                
                postDtos.Add(postDto);
            }
        }
        
        return postDtos;
    }

    /// <summary>
    /// Gets all post entities that have vector embeddings (for internal use)
    /// </summary>
    public async Task<List<Post>> GetAllPostEntitiesWithEmbeddingsAsync()
    {
        return await _postRepository.GetAllWithEmbeddingsAsync();
    }

    #endregion
}