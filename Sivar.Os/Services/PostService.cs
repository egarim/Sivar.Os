using System.Text.Json;
using Microsoft.Extensions.AI;
using Pgvector;
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
    private readonly IActivityService _activityService;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IReactionRepository reactionRepository,
        ICommentRepository commentRepository,
        IPostAttachmentRepository postAttachmentRepository,
        IFileStorageService fileStorageService,
        IActivityService activityService,
        IVectorEmbeddingService vectorEmbeddingService,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _postAttachmentRepository = postAttachmentRepository ?? throw new ArgumentNullException(nameof(postAttachmentRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _vectorEmbeddingService = vectorEmbeddingService ?? throw new ArgumentNullException(nameof(vectorEmbeddingService));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
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
            Tags = createPostDto.Tags?.ToArray() ?? Array.Empty<string>(),
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

            await _postRepository.AddAsync(post);
            await _postRepository.SaveChangesAsync();

            _logger.LogInformation("[CreatePostAsync] Post saved successfully: PostId={postId}", post.Id);

            // Generate and save content embedding
            try
            {
                if (_vectorEmbeddingService != null)
                {
                    _logger.LogInformation("[CreatePostAsync] Generating embedding for post: PostId={postId}", post.Id);
                    var embedding = await _vectorEmbeddingService.GenerateEmbeddingAsync(post.Content);
                    var vectorString = _vectorEmbeddingService.ToPostgresVector(embedding);
                    
                    var embeddingUpdated = await _postRepository.UpdateContentEmbeddingAsync(post.Id, vectorString);
                    if (embeddingUpdated)
                    {
                        _logger.LogInformation("[CreatePostAsync] Embedding saved successfully: PostId={postId}", post.Id);
                    }
                    else
                    {
                        _logger.LogWarning("[CreatePostAsync] Failed to save embedding for post: PostId={postId}", post.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreatePostAsync] Failed to generate/save embedding for post: PostId={postId}", post.Id);
                // Don't fail the post creation if embedding generation fails
            }

            // Record activity for the post creation
            try
            {
                await _activityService.RecordPostCreatedAsync(post);
                _logger.LogInformation("[CreatePostAsync] Activity recorded for post: PostId={postId}", post.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreatePostAsync] Failed to record activity for post: PostId={postId}", post.Id);
                // Don't fail the post creation if activity recording fails
            }

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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetPostByIdAsync] START - RequestId={RequestId}, PostId={PostId}, KeycloakId={KeycloakId}, IncludeReactions={IncludeReactions}, IncludeComments={IncludeComments}",
            requestId, postId, requestingKeycloakId ?? "NULL", includeReactions, includeComments);

        try
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("[PostService.GetPostByIdAsync] Post not found - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return null;
            }

            _logger.LogInformation("[PostService.GetPostByIdAsync] Post retrieved - RequestId={RequestId}, PostId={PostId}, ProfileId={ProfileId}, Visibility={Visibility}",
                requestId, postId, post.ProfileId, post.Visibility);

            // Check if user can view this post
            var canView = await CanUserViewPostAsync(postId, requestingKeycloakId);
            if (!canView)
            {
                _logger.LogWarning("[PostService.GetPostByIdAsync] User not authorized to view post - RequestId={RequestId}, PostId={PostId}, KeycloakId={KeycloakId}",
                    requestId, postId, requestingKeycloakId ?? "NULL");
                return null;
            }

            var postDto = await MapToPostDtoAsync(post, requestingKeycloakId, includeReactions, includeComments);
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPostByIdAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);

            return postDto;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPostByIdAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing post (only by the author)
    /// </summary>
    public async Task<PostDto?> UpdatePostAsync(Guid postId, string keycloakId, UpdatePostDto updatePostDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.UpdatePostAsync] START - RequestId={RequestId}, PostId={PostId}, KeycloakId={KeycloakId}",
            requestId, postId, keycloakId);

        try
        {
            if (string.IsNullOrWhiteSpace(keycloakId) || updatePostDto == null)
            {
                _logger.LogWarning("[PostService.UpdatePostAsync] Invalid parameters - RequestId={RequestId}, KeycloakIdEmpty={KeycloakIdEmpty}, DtoNull={DtoNull}",
                    requestId, string.IsNullOrWhiteSpace(keycloakId), updatePostDto == null);
                return null;
            }

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("[PostService.UpdatePostAsync] Post not found - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return null;
            }

            _logger.LogInformation("[PostService.UpdatePostAsync] Post found - RequestId={RequestId}, PostId={PostId}, ProfileId={ProfileId}",
                requestId, postId, post.ProfileId);

            // Check if user is the author
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user?.ActiveProfile?.Id != post.ProfileId)
            {
                _logger.LogWarning("[PostService.UpdatePostAsync] Authorization failed - RequestId={RequestId}, UserProfileId={UserProfileId}, PostProfileId={PostProfileId}",
                    requestId, user?.ActiveProfile?.Id.ToString() ?? "NULL", post.ProfileId);
                return null;
            }

            _logger.LogInformation("[PostService.UpdatePostAsync] User authorized for update - RequestId={RequestId}, KeycloakId={KeycloakId}",
                requestId, keycloakId);

            // Update post properties
            if (!string.IsNullOrWhiteSpace(updatePostDto.Content))
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating content - RequestId={RequestId}, OldLength={OldLength}, NewLength={NewLength}",
                    requestId, post.Content.Length, updatePostDto.Content.Length);

                post.Content = updatePostDto.Content.Trim();
                post.IsEdited = true;
                post.EditedAt = DateTime.UtcNow;
            }

            if (updatePostDto.Visibility.HasValue)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating visibility - RequestId={RequestId}, OldVisibility={OldVisibility}, NewVisibility={NewVisibility}",
                    requestId, post.Visibility, updatePostDto.Visibility.Value);
                post.Visibility = updatePostDto.Visibility.Value;
            }

            if (updatePostDto.Tags != null)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating tags - RequestId={RequestId}, TagCount={TagCount}",
                    requestId, updatePostDto.Tags.Count);
                post.Tags = updatePostDto.Tags.ToArray();
            }

            if (updatePostDto.Location != null)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating location - RequestId={RequestId}, City={City}, Country={Country}",
                    requestId, updatePostDto.Location.City ?? "NULL", updatePostDto.Location.Country ?? "NULL");

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
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating business metadata - RequestId={RequestId}",
                    requestId);
                post.BusinessMetadata = updatePostDto.BusinessMetadata;
            }

            post.UpdatedAt = DateTime.UtcNow;

            // Vector embedding regeneration disabled until service is properly configured
            // TODO: Enable when vector embedding service is available

            _logger.LogInformation("[PostService.UpdatePostAsync] Saving post - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);

            await _postRepository.UpdateAsync(post);
            var result = await MapToPostDtoAsync(post, keycloakId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.UpdatePostAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.UpdatePostAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Deletes a post (only by the author)
    /// </summary>
    public async Task<bool> DeletePostAsync(Guid postId, string keycloakId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.DeletePostAsync] START - RequestId={RequestId}, PostId={PostId}, KeycloakId={KeycloakId}",
            requestId, postId, keycloakId);

        try
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
            {
                _logger.LogWarning("[PostService.DeletePostAsync] Invalid keycloak ID - RequestId={RequestId}",
                    requestId);
                return false;
            }

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("[PostService.DeletePostAsync] Post not found - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return false;
            }

            _logger.LogInformation("[PostService.DeletePostAsync] Post found - RequestId={RequestId}, PostId={PostId}, ProfileId={ProfileId}",
                requestId, postId, post.ProfileId);

            // Check if user is the author
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user?.ActiveProfile?.Id != post.ProfileId)
            {
                _logger.LogWarning("[PostService.DeletePostAsync] Authorization failed - RequestId={RequestId}, UserProfileId={UserProfileId}, PostProfileId={PostProfileId}",
                    requestId, user?.ActiveProfile?.Id.ToString() ?? "NULL", post.ProfileId);
                return false;
            }

            _logger.LogInformation("[PostService.DeletePostAsync] User authorized for deletion - RequestId={RequestId}, KeycloakId={KeycloakId}",
                requestId, keycloakId);

            // Delete attachments and their files first
            _logger.LogInformation("[PostService.DeletePostAsync] Deleting post attachments - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);

            await DeletePostAttachmentsAsync(postId);
            
            _logger.LogInformation("[PostService.DeletePostAsync] Attachments deleted - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);

            // Then delete the post
            _logger.LogInformation("[PostService.DeletePostAsync] Deleting post record - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);

            var result = await _postRepository.DeleteAsync(postId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.DeletePostAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Result={Result}, Duration={Duration}ms",
                requestId, postId, result, elapsed);

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.DeletePostAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetPostsByProfileAsync] START - RequestId={RequestId}, ProfileId={ProfileId}, KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}",
            requestId, profileId, requestingKeycloakId ?? "NULL", page, pageSize);

        try
        {
            var (posts, totalCount) = await _postRepository.GetByProfileAsync(profileId, page, pageSize);

            _logger.LogInformation("[PostService.GetPostsByProfileAsync] Repository returned {PostCount} posts (total: {TotalCount}) - RequestId={RequestId}",
                posts.Count(), totalCount, requestId);

            var postDtos = new List<PostDto>();
            var skippedCount = 0;

            foreach (var post in posts)
            {
                if (await CanUserViewPostAsync(post.Id, requestingKeycloakId))
                {
                    var dto = await MapToPostDtoAsync(post, requestingKeycloakId);
                    if (dto != null)
                        postDtos.Add(dto);
                }
                else
                {
                    skippedCount++;
                }
            }

            _logger.LogInformation("[PostService.GetPostsByProfileAsync] Mapped {DtoCount} DTOs, skipped {SkippedCount} unauthorized posts - RequestId={RequestId}",
                postDtos.Count, skippedCount, requestId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPostsByProfileAsync] SUCCESS - RequestId={RequestId}, ReturnedCount={ReturnedCount}, TotalCount={TotalCount}, Duration={Duration}ms",
                requestId, postDtos.Count, totalCount, elapsed);

            return (postDtos, totalCount);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPostsByProfileAsync] ERROR - RequestId={RequestId}, ProfileId={ProfileId}, Duration={Duration}ms",
                requestId, profileId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets posts by post type with pagination
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPostsByTypeAsync(PostType postType, string? requestingKeycloakId = null, int page = 1, int pageSize = 10)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetPostsByTypeAsync] START - RequestId={RequestId}, PostType={PostType}, KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}",
            requestId, postType, requestingKeycloakId ?? "NULL", page, pageSize);

        try
        {
            var (posts, totalCount) = await _postRepository.GetByPostTypeAsync(postType, page, pageSize);

            _logger.LogInformation("[PostService.GetPostsByTypeAsync] Repository returned {PostCount} posts (total: {TotalCount}) - RequestId={RequestId}",
                posts.Count(), totalCount, requestId);

            var postDtos = new List<PostDto>();
            var skippedCount = 0;

            foreach (var post in posts)
            {
                if (await CanUserViewPostAsync(post.Id, requestingKeycloakId))
                {
                    var dto = await MapToPostDtoAsync(post, requestingKeycloakId);
                    if (dto != null)
                        postDtos.Add(dto);
                }
                else
                {
                    skippedCount++;
                }
            }

            _logger.LogInformation("[PostService.GetPostsByTypeAsync] Mapped {DtoCount} DTOs, skipped {SkippedCount} unauthorized posts - RequestId={RequestId}",
                postDtos.Count, skippedCount, requestId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPostsByTypeAsync] SUCCESS - RequestId={RequestId}, PostType={PostType}, ReturnedCount={ReturnedCount}, TotalCount={TotalCount}, Duration={Duration}ms",
                requestId, postType, postDtos.Count, totalCount, elapsed);

            return (postDtos, totalCount);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPostsByTypeAsync] ERROR - RequestId={RequestId}, PostType={PostType}, Duration={Duration}ms",
                requestId, postType, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Searches posts by content with pagination
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> SearchPostsAsync(string searchTerm, string? requestingKeycloakId = null, int page = 1, int pageSize = 10)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.SearchPostsAsync] START - RequestId={RequestId}, SearchTerm={SearchTerm}, KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}",
            requestId, searchTerm ?? "NULL", requestingKeycloakId ?? "NULL", page, pageSize);

        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("[PostService.SearchPostsAsync] Empty search term - RequestId={RequestId}", requestId);
                return (Enumerable.Empty<PostDto>(), 0);
            }

            var (posts, totalCount) = await _postRepository.SearchPostsAsync(searchTerm.Trim(), null, page, pageSize);

            _logger.LogInformation("[PostService.SearchPostsAsync] Repository returned {PostCount} posts (total: {TotalCount}) for search term '{SearchTerm}' - RequestId={RequestId}",
                posts.Count(), totalCount, searchTerm, requestId);

            var postDtos = new List<PostDto>();
            var skippedCount = 0;

            foreach (var post in posts)
            {
                if (await CanUserViewPostAsync(post.Id, requestingKeycloakId))
                {
                    var dto = await MapToPostDtoAsync(post, requestingKeycloakId);
                    if (dto != null)
                        postDtos.Add(dto);
                }
                else
                {
                    skippedCount++;
                }
            }

            _logger.LogInformation("[PostService.SearchPostsAsync] Mapped {DtoCount} DTOs, skipped {SkippedCount} unauthorized posts - RequestId={RequestId}",
                postDtos.Count, skippedCount, requestId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.SearchPostsAsync] SUCCESS - RequestId={RequestId}, SearchTerm={SearchTerm}, ReturnedCount={ReturnedCount}, TotalCount={TotalCount}, Duration={Duration}ms",
                requestId, searchTerm, postDtos.Count, totalCount, elapsed);

            return (postDtos, totalCount);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.SearchPostsAsync] ERROR - RequestId={RequestId}, SearchTerm={SearchTerm}, Duration={Duration}ms",
                requestId, searchTerm ?? "NULL", elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets post engagement statistics (views, reactions, comments)
    /// </summary>
    public async Task<PostEngagementDto?> GetPostEngagementAsync(Guid postId, string keycloakId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetPostEngagementAsync] START - RequestId={RequestId}, PostId={PostId}, KeycloakId={KeycloakId}",
            requestId, postId, keycloakId);

        try
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
            {
                _logger.LogWarning("[PostService.GetPostEngagementAsync] Invalid keycloak ID - RequestId={RequestId}",
                    requestId);
                return null;
            }

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("[PostService.GetPostEngagementAsync] Post not found - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return null;
            }

            _logger.LogInformation("[PostService.GetPostEngagementAsync] Post found - RequestId={RequestId}, PostId={PostId}, ProfileId={ProfileId}",
                requestId, postId, post.ProfileId);

            // Check if user is the author
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user?.ActiveProfile?.Id != post.ProfileId)
            {
                _logger.LogWarning("[PostService.GetPostEngagementAsync] User is not the post author - RequestId={RequestId}, UserProfileId={UserProfileId}, PostProfileId={PostProfileId}",
                    requestId, user?.ActiveProfile?.Id.ToString() ?? "NULL", post.ProfileId);
                return null;
            }

            _logger.LogInformation("[PostService.GetPostEngagementAsync] User authorized - RequestId={RequestId}, KeycloakId={KeycloakId}",
                requestId, keycloakId);

            var reactionCounts = await _reactionRepository.GetReactionCountsByPostAsync(postId);
            var commentCount = await _commentRepository.GetCommentCountByPostAsync(postId);

            _logger.LogInformation("[PostService.GetPostEngagementAsync] Engagement stats retrieved - RequestId={RequestId}, PostId={PostId}, TotalReactions={TotalReactions}, CommentCount={CommentCount}",
                requestId, postId, reactionCounts.Sum(r => r.Value), commentCount);

            var engagement = new PostEngagementDto
            {
                PostId = postId,
                TotalReactions = reactionCounts.Sum(r => r.Value),
                ReactionsByType = reactionCounts,
                TotalComments = commentCount,
                TotalShares = 0, // TODO: Implement shares functionality
                EngagementRate = CalculateEngagementRate(reactionCounts.Sum(r => r.Value), commentCount, 0),
                TopReactionType = reactionCounts.OrderByDescending(r => r.Value).FirstOrDefault().Key
            };

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPostEngagementAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);

            return engagement;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPostEngagementAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Validates if a user can view a specific post based on visibility settings
    /// </summary>
    public async Task<bool> CanUserViewPostAsync(Guid postId, string? requestingKeycloakId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.CanUserViewPostAsync] START - RequestId={RequestId}, PostId={PostId}, KeycloakId={KeycloakId}",
            requestId, postId, requestingKeycloakId ?? "NULL");

        try
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("[PostService.CanUserViewPostAsync] Post not found - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return false;
            }

            _logger.LogInformation("[PostService.CanUserViewPostAsync] Post found - RequestId={RequestId}, PostId={PostId}, Visibility={Visibility}",
                requestId, postId, post.Visibility);

            // Public posts can be viewed by anyone
            if (post.Visibility == VisibilityLevel.Public)
            {
                _logger.LogInformation("[PostService.CanUserViewPostAsync] Post is public - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return true;
            }

            // Private posts require authentication
            if (string.IsNullOrWhiteSpace(requestingKeycloakId))
            {
                _logger.LogInformation("[PostService.CanUserViewPostAsync] Post is not public and no keycloak ID provided - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return false;
            }

            var requestingUser = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
            if (requestingUser?.ActiveProfile == null)
            {
                _logger.LogWarning("[PostService.CanUserViewPostAsync] Requesting user not found or has no active profile - RequestId={RequestId}, KeycloakId={KeycloakId}",
                    requestId, requestingKeycloakId);
                return false;
            }

            _logger.LogInformation("[PostService.CanUserViewPostAsync] User profile found - RequestId={RequestId}, UserProfileId={UserProfileId}",
                requestId, requestingUser.ActiveProfile.Id);

            // Author can always view their own posts
            if (requestingUser.ActiveProfile.Id == post.ProfileId)
            {
                _logger.LogInformation("[PostService.CanUserViewPostAsync] User is post author - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return true;
            }

            // TODO: Implement follower-based visibility when ProfileFollower system is integrated
            switch (post.Visibility)
            {
                case VisibilityLevel.ConnectionsOnly:
                    // Check if requesting user follows the post author
                    // For now, return false until follower system is integrated
                    _logger.LogInformation("[PostService.CanUserViewPostAsync] Post visibility is ConnectionsOnly - follower check not yet implemented - RequestId={RequestId}, PostId={PostId}",
                        requestId, postId);
                    return false;
                
                case VisibilityLevel.Private:
                    _logger.LogInformation("[PostService.CanUserViewPostAsync] Post visibility is Private - RequestId={RequestId}, PostId={PostId}",
                        requestId, postId);
                    return false;
                
                default:
                    _logger.LogWarning("[PostService.CanUserViewPostAsync] Unknown visibility level - RequestId={RequestId}, PostId={PostId}, Visibility={Visibility}",
                        requestId, postId, post.Visibility);
                    return false;
            }
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.CanUserViewPostAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
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
            Tags = post.Tags?.ToList() ?? new List<string>(),
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
        _logger.LogDebug("[PostService.MapToProfileDtoAsync] Mapping profile: Id={ProfileId}, DisplayName={DisplayName}, Handle={Handle}",
            profile.Id, profile.DisplayName, profile.Handle);
            
        // TODO: Use proper ProfileService mapping when available
        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Handle = profile.Handle,  // ⭐ CRITICAL: Include Handle for profile navigation
            Avatar = profile.Avatar ?? "",
            Bio = profile.Bio ?? "",
            UserId = profile.UserId,
            ProfileTypeId = profile.ProfileTypeId,
            IsActive = profile.IsActive,
            VisibilityLevel = profile.VisibilityLevel,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.ProcessPostAttachmentsAsync] START - RequestId={RequestId}, PostId={PostId}, AttachmentCount={AttachmentCount}",
            requestId, postId, attachments?.Count ?? 0);

        try
        {
            if (attachments == null || attachments.Count == 0)
            {
                _logger.LogInformation("[PostService.ProcessPostAttachmentsAsync] No attachments to process - RequestId={RequestId}",
                    requestId);
                return;
            }

            int successCount = 0;
            foreach (var attachmentDto in attachments)
            {
                try
                {
                    var attachment = new PostAttachment
                    {
                        Id = Guid.NewGuid(),
                        PostId = postId,
                        AttachmentType = attachmentDto.AttachmentType,
                        FileId = attachmentDto.FileId,
                        Url = $"blob://{attachmentDto.FileId}/{attachmentDto.OriginalFilename}", // ⭐ Placeholder URL - actual URL generated dynamically via GetFileUrlAsync
                        OriginalFileName = attachmentDto.OriginalFilename,
                        MimeType = attachmentDto.MimeType,
                        FileSizeBytes = attachmentDto.FileSize,
                        Description = attachmentDto.AltText,
                        DisplayOrder = attachmentDto.DisplayOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _logger.LogInformation("[PostService.ProcessPostAttachmentsAsync] Adding attachment - RequestId={RequestId}, PostId={PostId}, FileId={FileId}, Type={Type}, Size={Size}",
                        requestId, postId, attachmentDto.FileId, attachmentDto.AttachmentType, attachmentDto.FileSize);

                    await _postAttachmentRepository.AddAsync(attachment);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PostService.ProcessPostAttachmentsAsync] Failed to process attachment - RequestId={RequestId}, PostId={PostId}, FileId={FileId}",
                        requestId, postId, attachmentDto.FileId);
                }
            }

            // Save all attachments to database
            await _postAttachmentRepository.SaveChangesAsync();
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.ProcessPostAttachmentsAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, ProcessedCount={ProcessedCount}, TotalCount={TotalCount}, Duration={Duration}ms",
                requestId, postId, successCount, attachments.Count, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.ProcessPostAttachmentsAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Delete post attachments and their files from storage
    /// </summary>
    private async Task DeletePostAttachmentsAsync(Guid postId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.DeletePostAttachmentsAsync] START - RequestId={RequestId}, PostId={PostId}",
            requestId, postId);

        try
        {
            var attachments = await _postAttachmentRepository.GetByPostIdAsync(postId);

            _logger.LogInformation("[PostService.DeletePostAttachmentsAsync] Retrieved {AttachmentCount} attachments - RequestId={RequestId}, PostId={PostId}",
                attachments.Count, requestId, postId);

            // Delete files from storage
            var fileIds = attachments
                .Where(a => !string.IsNullOrEmpty(a.FileId))
                .Select(a => a.FileId!)
                .ToList();

            if (fileIds.Any())
            {
                _logger.LogInformation("[PostService.DeletePostAttachmentsAsync] Deleting {FileCount} files from storage - RequestId={RequestId}, PostId={PostId}",
                    fileIds.Count, requestId, postId);

                await _fileStorageService.DeleteFilesAsync(fileIds);

                _logger.LogInformation("[PostService.DeletePostAttachmentsAsync] Files deleted successfully - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
            }

            // Delete attachment records
            _logger.LogInformation("[PostService.DeletePostAttachmentsAsync] Deleting attachment records - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);

            await _postAttachmentRepository.DeleteByPostIdAsync(postId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.DeletePostAttachmentsAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, DeletedCount={DeletedCount}, Duration={Duration}ms",
                requestId, postId, attachments.Count, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.DeletePostAttachmentsAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Map post attachments to DTOs
    /// </summary>
    private async Task<List<PostAttachmentDto>> MapAttachmentsToDtosAsync(Guid postId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.MapAttachmentsToDtosAsync] START - RequestId={RequestId}, PostId={PostId}",
            requestId, postId);

        try
        {
            var attachments = await _postAttachmentRepository.GetByPostIdOrderedAsync(postId);

            _logger.LogInformation("[PostService.MapAttachmentsToDtosAsync] Retrieved {AttachmentCount} attachments - RequestId={RequestId}, PostId={PostId}",
                attachments.Count, requestId, postId);

            var attachmentDtos = new List<PostAttachmentDto>();

            foreach (var attachment in attachments)
            {
                string publicUrl;
                
                if (string.IsNullOrEmpty(attachment.FileId))
                {
                    // No FileId - use error placeholder
                    _logger.LogWarning("[PostService.MapAttachmentsToDtosAsync] Missing FileId - RequestId={RequestId}, AttachmentId={AttachmentId}",
                        requestId, attachment.Id);
                    publicUrl = $"/api/file-missing/{attachment.Id}";
                }
                else
                {
                    try
                    {
                        // ⭐ CRITICAL: Always generate URL dynamically from FileId - NEVER use stored URL
                        publicUrl = await _fileStorageService.GetFileUrlAsync(attachment.FileId);
                        
                        _logger.LogDebug("[PostService.MapAttachmentsToDtosAsync] Generated public URL - RequestId={RequestId}, FileId={FileId}, URL={URL}",
                            requestId, attachment.FileId, publicUrl);
                    }
                    catch (FileNotFoundException ex)
                    {
                        // File not found in blob storage - log error and use error placeholder
                        _logger.LogError(ex, "[PostService.MapAttachmentsToDtosAsync] File not found in blob storage - RequestId={RequestId}, FileId={FileId}, OriginalFileName={FileName}",
                            requestId, attachment.FileId, attachment.OriginalFileName);
                        publicUrl = $"/api/file-not-found/{attachment.FileId}"; // Error placeholder
                    }
                    catch (Exception ex)
                    {
                        // Other error generating URL - log and use error placeholder
                        _logger.LogError(ex, "[PostService.MapAttachmentsToDtosAsync] Error generating public URL - RequestId={RequestId}, FileId={FileId}, OriginalFileName={FileName}",
                            requestId, attachment.FileId, attachment.OriginalFileName);
                        publicUrl = $"/api/file-error/{attachment.FileId}"; // Error placeholder
                    }
                }

                var dto = new PostAttachmentDto
                {
                    Id = attachment.Id,
                    AttachmentType = attachment.AttachmentType,
                    FileId = attachment.FileId,
                    FilePath = publicUrl, // ⭐ Dynamically generated URL (proxy or public)
                    OriginalFilename = attachment.OriginalFileName ?? "",
                    MimeType = attachment.MimeType ?? "",
                    FileSize = attachment.FileSizeBytes ?? 0,
                    AltText = attachment.Description,
                    DisplayOrder = attachment.DisplayOrder,
                    CreatedAt = attachment.CreatedAt
                };

                attachmentDtos.Add(dto);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.MapAttachmentsToDtosAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, MappedCount={MappedCount}, Duration={Duration}ms",
                requestId, postId, attachmentDtos.Count, elapsed);

            return attachmentDtos;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.MapAttachmentsToDtosAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms",
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets all posts that have vector embeddings for semantic search
    /// </summary>
    public async Task<List<PostDto>> GetAllPostsWithEmbeddingsAsync()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetAllPostsWithEmbeddingsAsync] START - RequestId={RequestId}",
            requestId);

        try
        {
            var posts = await _postRepository.GetAllWithEmbeddingsAsync();

            _logger.LogInformation("[PostService.GetAllPostsWithEmbeddingsAsync] Repository returned {PostCount} posts with embeddings - RequestId={RequestId}",
                posts.Count, requestId);

            var postDtos = new List<PostDto>();
            var successCount = 0;
            var embeddingFailures = 0;
            
            foreach (var post in posts)
            {
                var postDto = await MapToPostDtoAsync(post, null, false, false);
                if (postDto != null)
                {
                    // Convert PostgreSQL vector string to float[] for the DTO
                    // Format: "[0.1,0.2,0.3,...]" -> float[]
                    if (post.ContentEmbedding != null)
                    {
                        try
                        {
                            // Remove brackets and split by comma
                            var vectorString = post.ContentEmbedding.Trim('[', ']');
                            var embeddingArray = vectorString.Split(',')
                                .Select(s => float.Parse(s.Trim()))
                                .ToArray();
                            
                            postDto = postDto with { ContentEmbedding = embeddingArray };
                            successCount++;
                        }
                        catch (Exception embeddingEx)
                        {
                            embeddingFailures++;
                            _logger.LogWarning(embeddingEx, "[PostService.GetAllPostsWithEmbeddingsAsync] Failed to convert embedding - RequestId={RequestId}, PostId={PostId}",
                                requestId, post.Id);
                        }
                    }
                    
                    postDtos.Add(postDto);
                }
            }

            _logger.LogInformation("[PostService.GetAllPostsWithEmbeddingsAsync] Processed {SuccessCount} embeddings, {FailureCount} failures - RequestId={RequestId}",
                successCount, embeddingFailures, requestId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetAllPostsWithEmbeddingsAsync] SUCCESS - RequestId={RequestId}, TotalPosts={TotalPosts}, SuccessfulEmbeddings={SuccessfulEmbeddings}, Duration={Duration}ms",
                requestId, postDtos.Count, successCount, elapsed);

            return postDtos;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetAllPostsWithEmbeddingsAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets all post entities that have vector embeddings (for internal use)
    /// </summary>
    public async Task<List<Post>> GetAllPostEntitiesWithEmbeddingsAsync()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetAllPostEntitiesWithEmbeddingsAsync] START - RequestId={RequestId}",
            requestId);

        try
        {
            var posts = await _postRepository.GetAllWithEmbeddingsAsync();

            _logger.LogInformation("[PostService.GetAllPostEntitiesWithEmbeddingsAsync] SUCCESS - RequestId={RequestId}, PostCount={PostCount}",
                requestId, posts.Count);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetAllPostEntitiesWithEmbeddingsAsync] Duration={Duration}ms",
                elapsed);

            return posts;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetAllPostEntitiesWithEmbeddingsAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    #endregion
}