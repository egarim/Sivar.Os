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
    private readonly IClientEmbeddingService _clientEmbeddingService;
    private readonly IVectorEmbeddingService _vectorEmbeddingService;
    private readonly IActivityService _activityService;
    private readonly ISentimentAnalysisService _sentimentService;
    private readonly ILocationService _locationService;
    private readonly IContentExtractionService _contentExtractionService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
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
        IClientEmbeddingService clientEmbeddingService,
        IVectorEmbeddingService vectorEmbeddingService,
        ISentimentAnalysisService sentimentService,
        ILocationService locationService,
        IContentExtractionService contentExtractionService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _postAttachmentRepository = postAttachmentRepository ?? throw new ArgumentNullException(nameof(postAttachmentRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _clientEmbeddingService = clientEmbeddingService ?? throw new ArgumentNullException(nameof(clientEmbeddingService));
        _vectorEmbeddingService = vectorEmbeddingService ?? throw new ArgumentNullException(nameof(vectorEmbeddingService));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _sentimentService = sentimentService ?? throw new ArgumentNullException(nameof(sentimentService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _contentExtractionService = contentExtractionService ?? throw new ArgumentNullException(nameof(contentExtractionService));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
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

        // ⚡ FIX: Use a new scope to avoid DbContext concurrency issues
        // Blazor Server can trigger multiple async operations that share the same scoped DbContext
        using var scope = _serviceScopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var profileRepository = scope.ServiceProvider.GetRequiredService<IProfileRepository>();
        var postRepository = scope.ServiceProvider.GetRequiredService<IPostRepository>();
        var postAttachmentRepository = scope.ServiceProvider.GetRequiredService<IPostAttachmentRepository>();
        var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();

        // Get user
        var user = await userRepository.GetByKeycloakIdAsync(keycloakId);
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

        // Get the profile using the ID sent by client (use scoped repository to avoid concurrency)
        var activeProfile = await profileRepository.GetByIdIgnoringFiltersAsync(createPostDto.ProfileId);
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

        // ⚡ OPTIMISTIC UI: Create post immediately, process AI enrichment in background
        // This gives instant feedback to the user while heavy AI processing happens async
        
        // Create post entity with user-provided data only (no AI extraction yet)
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

        // Set blog-specific fields if this is a blog post
        if (createPostDto.PostType == PostType.Blog)
        {
            post.BlogContent = createPostDto.BlogContent;
            post.CoverImageUrl = createPostDto.CoverImageUrl;
            post.CoverImageFileId = createPostDto.CoverImageFileId;
            post.Subtitle = createPostDto.Subtitle;
            post.CanonicalUrl = createPostDto.CanonicalUrl;
            post.IsDraft = createPostDto.IsDraft;
            post.ReadTimeMinutes = CalculateReadTimeMinutes(createPostDto.BlogContent);
            post.PublishedAt = createPostDto.IsDraft ? null : DateTime.UtcNow;
            
            _logger.LogInformation("[CreatePostAsync] Blog post created: IsDraft={IsDraft}, ReadTime={ReadTime}min", 
                post.IsDraft, post.ReadTimeMinutes);
        }

        // Set procedure-specific fields if this is a procedure post
        if (createPostDto.PostType == PostType.Procedure)
        {
            post.ProcedureMetadataJson = createPostDto.ProcedureMetadataJson;
            
            _logger.LogInformation("[CreatePostAsync] Procedure post created with metadata: {HasMetadata}", 
                !string.IsNullOrEmpty(createPostDto.ProcedureMetadataJson));
        }

        // Set location from user-provided data
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
            _logger.LogInformation("[CreatePostAsync] ⚡ FAST PATH: Saving post immediately: PostId={postId}, ProfileId={profileId}", 
                post.Id, post.ProfileId);

            await postRepository.AddAsync(post);
            await postRepository.SaveChangesAsync();

            _logger.LogInformation("[CreatePostAsync] ✓ Post saved successfully: PostId={postId}", post.Id);

            // Process attachments if provided (this is fast, ~100ms)
            if (createPostDto.Attachments?.Any() == true)
            {
                await ProcessPostAttachmentsAsync(post.Id, createPostDto.Attachments, postAttachmentRepository);
            }

            // ⚡ FAST PATH: Use data we already have instead of re-fetching from blob storage
            // This avoids ~500ms latency from MapAttachmentsToDtosAsync which fetches URLs
            var postDto = MapToPostDtoFast(post, activeProfile, createPostDto.Attachments);

            // Record activity for the post creation with snapshot for fast feed loading
            try
            {
                var postSnapshotJson = JsonSerializer.Serialize(postDto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
                await activityService.RecordPostCreatedAsync(post, postSnapshotJson);
                _logger.LogInformation("[CreatePostAsync] Activity recorded with snapshot for post: PostId={postId}", post.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreatePostAsync] Failed to record activity for post: PostId={postId}", post.Id);
            }

            // ⚡ BACKGROUND PROCESSING: Queue AI enrichment (content extraction, sentiment, embeddings)
            // This runs async after returning to the user - they see the post immediately
            var postId = post.Id;
            var content = post.Content;
            var language = post.Language ?? "en";
            
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    await ProcessPostAiEnrichmentAsync(scope.ServiceProvider, postId, content, language);
                }
                catch (Exception ex)
                {
                    // Log but don't fail - the post is already saved
                    _logger.LogError(ex, "[CreatePostAsync] Background AI enrichment failed for PostId={postId}", postId);
                }
            });

            _logger.LogInformation("[CreatePostAsync] ✓ Post returned to user, AI enrichment queued in background: PostId={postId}", post.Id);

            return postDto;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Background task: Process AI enrichment for a post (content extraction, sentiment, embeddings)
    /// This runs after the post is saved and returned to the user for instant feedback
    /// </summary>
    private async Task ProcessPostAiEnrichmentAsync(IServiceProvider serviceProvider, Guid postId, string content, string language)
    {
        _logger.LogInformation("[ProcessPostAiEnrichmentAsync] START background processing for PostId={postId}", postId);
        var startTime = DateTime.UtcNow;

        try
        {
            var postRepository = serviceProvider.GetRequiredService<IPostRepository>();
            var contentExtractionService = serviceProvider.GetRequiredService<IContentExtractionService>();
            var sentimentService = serviceProvider.GetRequiredService<ISentimentAnalysisService>();
            var clientEmbeddingService = serviceProvider.GetRequiredService<IClientEmbeddingService>();
            var vectorEmbeddingService = serviceProvider.GetRequiredService<IVectorEmbeddingService>();

            // Get the post
            var post = await postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("[ProcessPostAiEnrichmentAsync] Post not found: {postId}", postId);
                return;
            }

            // 1. AI CONTENT EXTRACTION (enriches tags, post type, location)
            try
            {
                _logger.LogInformation("[ProcessPostAiEnrichmentAsync] Starting AI content extraction for PostId={postId}", postId);
                var extractedMetadata = await contentExtractionService.ExtractMetadataAsync(content, language);
                
                if (extractedMetadata?.Success == true)
                {
                    // Update post with AI-extracted metadata
                    if (post.PostType == PostType.General && extractedMetadata.PostTypeConfidence > 0.6)
                    {
                        post.PostType = extractedMetadata.SuggestedPostType;
                    }
                    
                    if (extractedMetadata.Tags?.Any() == true)
                    {
                        post.Tags = MergeTagsWithAiExtracted(post.Tags, extractedMetadata.Tags.ToArray());
                    }
                    
                    if (post.Location == null && extractedMetadata.Location != null)
                    {
                        post.Location = new Location
                        {
                            City = extractedMetadata.Location.City,
                            State = extractedMetadata.Location.State,
                            Country = extractedMetadata.Location.Country ?? "El Salvador",
                            Latitude = extractedMetadata.Location.Latitude,
                            Longitude = extractedMetadata.Location.Longitude
                        };
                    }
                    
                    if (string.IsNullOrEmpty(post.BusinessMetadata) && extractedMetadata.BusinessMetadata != null)
                    {
                        post.BusinessMetadata = BuildBusinessMetadataFromExtraction(extractedMetadata);
                    }

                    post.UpdatedAt = DateTime.UtcNow;
                    await postRepository.UpdateAsync(post);
                    await postRepository.SaveChangesAsync();
                    
                    _logger.LogInformation("[ProcessPostAiEnrichmentAsync] ✓ AI extraction complete for PostId={postId}", postId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ProcessPostAiEnrichmentAsync] AI extraction failed for PostId={postId}", postId);
            }

            // 2. SENTIMENT ANALYSIS
            try
            {
                var sentimentResult = await sentimentService.AnalyzeAsync(content, language);
                if (sentimentResult != null)
                {
                    post.PrimaryEmotion = sentimentResult.PrimaryEmotion;
                    post.EmotionScore = sentimentResult.EmotionScore;
                    post.SentimentPolarity = sentimentResult.SentimentPolarity;
                    post.JoyScore = sentimentResult.EmotionScores.Joy;
                    post.SadnessScore = sentimentResult.EmotionScores.Sadness;
                    post.AngerScore = sentimentResult.EmotionScores.Anger;
                    post.FearScore = sentimentResult.EmotionScores.Fear;
                    post.HasAnger = sentimentResult.HasAnger;
                    post.NeedsReview = sentimentResult.NeedsReview;
                    post.AnalyzedAt = DateTime.UtcNow;
                    post.UpdatedAt = DateTime.UtcNow;

                    await postRepository.UpdateAsync(post);
                    await postRepository.SaveChangesAsync();
                    
                    _logger.LogInformation("[ProcessPostAiEnrichmentAsync] ✓ Sentiment analysis complete for PostId={postId}: {Emotion}", 
                        postId, post.PrimaryEmotion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ProcessPostAiEnrichmentAsync] Sentiment analysis failed for PostId={postId}", postId);
            }

            // 3. EMBEDDING GENERATION
            try
            {
                float[]? embedding = await clientEmbeddingService.TryGenerateEmbeddingAsync(content);
                string vectorString;

                if (embedding != null)
                {
                    vectorString = vectorEmbeddingService.ToPostgresVector(embedding);
                }
                else
                {
                    var serverEmbedding = await vectorEmbeddingService.GenerateEmbeddingAsync(content);
                    vectorString = vectorEmbeddingService.ToPostgresVector(serverEmbedding);
                }

                await postRepository.UpdateContentEmbeddingAsync(postId, vectorString);
                _logger.LogInformation("[ProcessPostAiEnrichmentAsync] ✓ Embedding saved for PostId={postId}", postId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ProcessPostAiEnrichmentAsync] Embedding generation failed for PostId={postId}", postId);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProcessPostAiEnrichmentAsync] ✅ COMPLETE background processing for PostId={postId} in {Duration}ms", 
                postId, elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProcessPostAiEnrichmentAsync] FAILED background processing for PostId={postId}", postId);
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

            // Track if content changed significantly (for re-extraction and embedding regeneration)
            bool contentChanged = false;
            string? oldContent = post.Content;

            // Update post properties
            if (!string.IsNullOrWhiteSpace(updatePostDto.Content))
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating content - RequestId={RequestId}, OldLength={OldLength}, NewLength={NewLength}",
                    requestId, post.Content.Length, updatePostDto.Content.Length);

                // Check if content changed significantly (more than 20% different or >50 chars changed)
                var newContent = updatePostDto.Content.Trim();
                contentChanged = IsContentSignificantlyDifferent(oldContent, newContent);

                post.Content = newContent;
                post.IsEdited = true;
                post.EditedAt = DateTime.UtcNow;
            }

            if (updatePostDto.Visibility.HasValue)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating visibility - RequestId={RequestId}, OldVisibility={OldVisibility}, NewVisibility={NewVisibility}",
                    requestId, post.Visibility, updatePostDto.Visibility.Value);
                post.Visibility = updatePostDto.Visibility.Value;
            }

            // ==================== AI CONTENT RE-EXTRACTION ====================
            // If content changed significantly and user didn't provide explicit metadata updates,
            // re-run AI extraction to update tags, location, and business metadata
            ExtractedContentMetadata? extractedMetadata = null;
            if (contentChanged)
            {
                try
                {
                    _logger.LogInformation("[PostService.UpdatePostAsync] Content changed significantly, running AI re-extraction - RequestId={RequestId}", requestId);
                    extractedMetadata = await _contentExtractionService.ExtractMetadataAsync(
                        post.Content, 
                        post.Language ?? "es");
                    
                    if (extractedMetadata?.Success == true)
                    {
                        _logger.LogInformation("[PostService.UpdatePostAsync] ✓ AI re-extraction successful - RequestId={RequestId}, Tags={TagCount}, HasLocation={HasLocation}",
                            requestId, extractedMetadata.Tags?.Count ?? 0, extractedMetadata.Location != null);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PostService.UpdatePostAsync] AI re-extraction failed, continuing with user updates - RequestId={RequestId}", requestId);
                }
            }

            // Update tags: user-provided takes priority, otherwise merge with AI-extracted
            if (updatePostDto.Tags != null)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating tags (user-provided) - RequestId={RequestId}, TagCount={TagCount}",
                    requestId, updatePostDto.Tags.Count);
                post.Tags = updatePostDto.Tags.ToArray();
            }
            else if (contentChanged && extractedMetadata?.Tags != null && extractedMetadata.Tags.Count > 0)
            {
                // Content changed but user didn't provide new tags - merge AI tags with existing
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating tags (AI-extracted) - RequestId={RequestId}, TagCount={TagCount}",
                    requestId, extractedMetadata.Tags.Count);
                post.Tags = MergeTagsWithAiExtracted(post.Tags, extractedMetadata.Tags.ToArray());
            }

            // Update location: user-provided takes priority, otherwise use AI-extracted
            if (updatePostDto.Location != null)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating location (user-provided) - RequestId={RequestId}, City={City}, Country={Country}",
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
            else if (contentChanged && extractedMetadata?.Location != null && post.Location == null)
            {
                // Content changed, AI found location, and post didn't have one before
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating location (AI-extracted) - RequestId={RequestId}, City={City}",
                    requestId, extractedMetadata.Location.City ?? "NULL");
                
                post.Location = new Location
                {
                    City = extractedMetadata.Location.City,
                    State = extractedMetadata.Location.State,
                    Country = extractedMetadata.Location.Country ?? "El Salvador",
                    Latitude = extractedMetadata.Location.Latitude,
                    Longitude = extractedMetadata.Location.Longitude
                };
            }

            // Update business metadata: user-provided takes priority
            if (updatePostDto.BusinessMetadata != null)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating business metadata (user-provided) - RequestId={RequestId}",
                    requestId);
                post.BusinessMetadata = updatePostDto.BusinessMetadata;
            }
            else if (contentChanged && string.IsNullOrEmpty(post.BusinessMetadata))
            {
                // Content changed and post didn't have business metadata - try AI extraction
                var aiBusinessMetadata = BuildBusinessMetadataFromExtraction(extractedMetadata);
                if (!string.IsNullOrEmpty(aiBusinessMetadata))
                {
                    _logger.LogInformation("[PostService.UpdatePostAsync] Updating business metadata (AI-extracted) - RequestId={RequestId}",
                        requestId);
                    post.BusinessMetadata = aiBusinessMetadata;
                }
            }

            // Update procedure metadata if provided
            if (updatePostDto.ProcedureMetadataJson != null)
            {
                _logger.LogInformation("[PostService.UpdatePostAsync] Updating procedure metadata - RequestId={RequestId}",
                    requestId);
                post.ProcedureMetadataJson = updatePostDto.ProcedureMetadataJson;
            }

            post.UpdatedAt = DateTime.UtcNow;

            // ==================== EMBEDDING REGENERATION ====================
            // Regenerate vector embedding if content changed
            if (contentChanged)
            {
                try
                {
                    _logger.LogInformation("[PostService.UpdatePostAsync] Regenerating embedding for updated content - RequestId={RequestId}", requestId);
                    
                    float[]? embedding = null;
                    string? vectorString = null;

                    // Try client-side first, fallback to server
                    embedding = await _clientEmbeddingService.TryGenerateEmbeddingAsync(post.Content);
                    if (embedding != null)
                    {
                        vectorString = _vectorEmbeddingService.ToPostgresVector(embedding);
                        _logger.LogInformation("[PostService.UpdatePostAsync] ✓ Client-side embedding regenerated - RequestId={RequestId}", requestId);
                    }
                    else
                    {
                        var serverEmbedding = await _vectorEmbeddingService.GenerateEmbeddingAsync(post.Content);
                        vectorString = _vectorEmbeddingService.ToPostgresVector(serverEmbedding);
                        _logger.LogInformation("[PostService.UpdatePostAsync] ✓ Server-side embedding regenerated - RequestId={RequestId}", requestId);
                    }

                    // Save embedding to database
                    if (!string.IsNullOrEmpty(vectorString))
                    {
                        var embeddingUpdated = await _postRepository.UpdateContentEmbeddingAsync(post.Id, vectorString);
                        if (embeddingUpdated)
                        {
                            _logger.LogInformation("[PostService.UpdatePostAsync] ✓ Embedding saved to database - RequestId={RequestId}", requestId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PostService.UpdatePostAsync] Embedding regeneration failed, keeping existing - RequestId={RequestId}", requestId);
                }
            }

            _logger.LogInformation("[PostService.UpdatePostAsync] Saving post - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);

            await _postRepository.UpdateAsync(post);
            var result = await MapToPostDtoAsync(post, keycloakId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.UpdatePostAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms, ContentChanged={ContentChanged}",
                requestId, postId, elapsed, contentChanged);

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
    /// Determines if content changed significantly enough to warrant re-extraction and embedding regeneration
    /// </summary>
    private static bool IsContentSignificantlyDifferent(string? oldContent, string? newContent)
    {
        if (string.IsNullOrEmpty(oldContent) || string.IsNullOrEmpty(newContent))
            return true;

        // If length difference is more than 20%, consider it significant
        var lengthDiff = Math.Abs(oldContent.Length - newContent.Length);
        var avgLength = (oldContent.Length + newContent.Length) / 2.0;
        if (lengthDiff / avgLength > 0.2)
            return true;

        // If more than 50 characters changed, consider it significant
        if (lengthDiff > 50)
            return true;

        // Quick check: if they're the same, no change
        if (oldContent == newContent)
            return false;

        // Count word-level differences
        var oldWords = oldContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var newWords = newContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commonWords = oldWords.Intersect(newWords, StringComparer.OrdinalIgnoreCase).Count();
        var totalWords = Math.Max(oldWords.Length, newWords.Length);

        // If less than 80% words in common, consider it significant
        return totalWords > 0 && (double)commonWords / totalWords < 0.8;
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
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPostsByProfileAsync(Guid profileId, string? requestingKeycloakId = null, int page = 1, int pageSize = 10, PostType? postType = null)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetPostsByProfileAsync] START - RequestId={RequestId}, ProfileId={ProfileId}, KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}, PostType={PostType}",
            requestId, profileId, requestingKeycloakId ?? "NULL", page, pageSize, postType?.ToString() ?? "ALL");

        try
        {
            var (posts, totalCount) = await _postRepository.GetByProfileAsync(profileId, page, pageSize, includeRelated: true, postType: postType);

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
    /// ⚡ FAST PATH: Maps Post to DTO using data we already have
    /// Used during post creation to avoid re-fetching blob URLs (~500ms savings)
    /// </summary>
    private PostDto MapToPostDtoFast(Post post, Profile profile, List<CreatePostAttachmentDto>? attachments)
    {
        _logger.LogInformation("[PostService.MapToPostDtoFast] ⚡ Fast mapping post: PostId={PostId}", post.Id);

        var postDto = new PostDto
        {
            Id = post.Id,
            Profile = new ProfileDto
            {
                Id = profile.Id,
                DisplayName = profile.DisplayName,
                Handle = profile.Handle,
                Avatar = profile.Avatar ?? "",
                Bio = profile.Bio ?? "",
                UserId = profile.UserId,
                ProfileTypeId = profile.ProfileTypeId,
                IsActive = profile.IsActive,
                VisibilityLevel = profile.VisibilityLevel,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            },
            Content = post.Content,
            PostType = post.PostType,
            Visibility = post.Visibility,
            Language = post.Language,
            Tags = post.Tags?.ToList() ?? new List<string>(),
            BusinessMetadata = post.BusinessMetadata,
            ProcedureMetadataJson = post.ProcedureMetadataJson,
            // ⚡ Use attachment data from CreatePostDto - no blob URL re-fetch needed!
            Attachments = attachments?.Select((a, index) => new PostAttachmentDto
            {
                Id = Guid.NewGuid(), // Placeholder - actual ID created in ProcessPostAttachmentsAsync
                AttachmentType = a.AttachmentType,
                FileId = a.FileId,
                FilePath = a.FilePath, // Already have the URL from upload
                OriginalFilename = a.OriginalFilename,
                MimeType = a.MimeType,
                FileSize = a.FileSize,
                AltText = a.AltText,
                DisplayOrder = a.DisplayOrder > 0 ? a.DisplayOrder : index,
                CreatedAt = DateTime.UtcNow
            }).ToList() ?? new List<PostAttachmentDto>(),
            CommentCount = 0, // New post has no comments
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsEdited = post.IsEdited,
            EditedAt = post.EditedAt,
            // Blog-specific fields
            BlogContent = post.BlogContent,
            CoverImageUrl = post.CoverImageUrl,
            CoverImageFileId = post.CoverImageFileId,
            Subtitle = post.Subtitle,
            ReadTimeMinutes = post.ReadTimeMinutes,
            IsDraft = post.IsDraft,
            PublishedAt = post.PublishedAt,
            CanonicalUrl = post.CanonicalUrl,
            // New post has no reactions
            ReactionSummary = new PostReactionSummaryDto
            {
                PostId = post.Id,
                TotalReactions = 0,
                ReactionCounts = new Dictionary<ReactionType, int>(),
                UserReaction = null,
                TopReactionType = null,
                HasUserReacted = false
            }
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

        _logger.LogInformation("[PostService.MapToPostDtoFast] ✓ Fast mapping complete: PostId={PostId}, AttachmentCount={AttachmentCount}", 
            post.Id, postDto.Attachments.Count);

        return postDto;
    }

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
            ProcedureMetadataJson = post.ProcedureMetadataJson,
            Attachments = await MapAttachmentsToDtosAsync(post.Id),
            CommentCount = includeComments ? await _commentRepository.GetCommentCountByPostAsync(post.Id) : 0,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsEdited = post.IsEdited,
            EditedAt = post.EditedAt,
            // Blog-specific fields
            BlogContent = post.BlogContent,
            CoverImageUrl = post.CoverImageUrl,
            CoverImageFileId = post.CoverImageFileId,
            Subtitle = post.Subtitle,
            ReadTimeMinutes = post.ReadTimeMinutes,
            IsDraft = post.IsDraft,
            PublishedAt = post.PublishedAt,
            CanonicalUrl = post.CanonicalUrl
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
        await ProcessPostAttachmentsAsync(postId, attachments, _postAttachmentRepository);
    }

    /// <summary>
    /// Process and create post attachments with explicit repository (for scoped scenarios)
    /// </summary>
    private async Task ProcessPostAttachmentsAsync(Guid postId, List<CreatePostAttachmentDto> attachments, IPostAttachmentRepository attachmentRepository)
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

                    await attachmentRepository.AddAsync(attachment);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PostService.ProcessPostAttachmentsAsync] Failed to process attachment - RequestId={RequestId}, PostId={PostId}, FileId={FileId}",
                        requestId, postId, attachmentDto.FileId);
                }
            }

            // Save all attachments to database
            await attachmentRepository.SaveChangesAsync();
            
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

            // ⚡ PERFORMANCE: Use stored URLs directly - no Azure API calls needed!
            // URLs are generated and stored during upload, so we can use them directly.
            // Browser caches images by URL, so consistent URLs = better caching.
            var attachmentDtos = new List<PostAttachmentDto>();
            
            foreach (var attachment in attachments)
            {
                string publicUrl;
                
                // Use stored URL if available (fast path - no API calls)
                if (!string.IsNullOrEmpty(attachment.Url) && !attachment.Url.StartsWith("blob://"))
                {
                    publicUrl = attachment.Url;
                    _logger.LogDebug("[PostService.MapAttachmentsToDtosAsync] Using stored URL - RequestId={RequestId}, AttachmentId={AttachmentId}, URL={URL}",
                        requestId, attachment.Id, publicUrl);
                }
                else if (!string.IsNullOrEmpty(attachment.FileId))
                {
                    // Fallback: Generate URL from FileId (slow path - makes Azure API calls)
                    try
                    {
                        publicUrl = await _fileStorageService.GetFileUrlAsync(attachment.FileId);
                        _logger.LogInformation("[PostService.MapAttachmentsToDtosAsync] Generated URL from FileId - RequestId={RequestId}, FileId={FileId}, URL={URL}",
                            requestId, attachment.FileId, publicUrl);
                    }
                    catch (FileNotFoundException)
                    {
                        publicUrl = $"/api/file-not-found/{attachment.FileId}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[PostService.MapAttachmentsToDtosAsync] Error generating URL - RequestId={RequestId}, FileId={FileId}",
                            requestId, attachment.FileId);
                        publicUrl = $"/api/file-error/{attachment.FileId}";
                    }
                }
                else
                {
                    publicUrl = $"/api/file-missing/{attachment.Id}";
                }
                
                attachmentDtos.Add(new PostAttachmentDto
                {
                    Id = attachment.Id,
                    AttachmentType = attachment.AttachmentType,
                    FileId = attachment.FileId,
                    FilePath = publicUrl,
                    OriginalFilename = attachment.OriginalFileName ?? "",
                    MimeType = attachment.MimeType ?? "",
                    FileSize = attachment.FileSizeBytes ?? 0,
                    AltText = attachment.Description,
                    DisplayOrder = attachment.DisplayOrder,
                    CreatedAt = attachment.CreatedAt
                });
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
    /// Gets multiple posts by their IDs in a single batch query (for feed optimization)
    /// </summary>
    public async Task<Dictionary<Guid, PostDto?>> GetPostsByIdsAsync(IEnumerable<Guid> postIds, string? requestingKeycloakId = null)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var idList = postIds.ToList();
        
        _logger.LogInformation("[PostService.GetPostsByIdsAsync] START - RequestId={RequestId}, PostCount={PostCount}",
            requestId, idList.Count);

        var result = new Dictionary<Guid, PostDto?>();
        
        if (idList.Count == 0)
        {
            return result;
        }

        try
        {
            // Batch fetch all posts in a single query
            var posts = await _postRepository.GetByIdsAsync(idList);
            
            _logger.LogInformation("[PostService.GetPostsByIdsAsync] Fetched {Found}/{Requested} posts from DB - RequestId={RequestId}, Duration={Elapsed}ms",
                posts.Count, idList.Count, requestId, (DateTime.UtcNow - startTime).TotalMilliseconds);

            // Map each post to DTO (sequential to avoid DbContext concurrency issues)
            foreach (var post in posts)
            {
                try
                {
                    // Skip permission checks for public posts (optimization for feed)
                    // Most feed posts are public anyway
                    var postDto = await MapToPostDtoAsync(post, requestingKeycloakId, includeReactions: true, includeComments: false);
                    result[post.Id] = postDto;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PostService.GetPostsByIdsAsync] Failed to map post {PostId} - RequestId={RequestId}",
                        post.Id, requestId);
                    result[post.Id] = null;
                }
            }

            // Add null entries for posts that weren't found
            foreach (var id in idList)
            {
                if (!result.ContainsKey(id))
                {
                    result[id] = null;
                }
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPostsByIdsAsync] SUCCESS - RequestId={RequestId}, PostCount={PostCount}, MappedCount={MappedCount}, Duration={Duration}ms",
                requestId, idList.Count, result.Count(kv => kv.Value != null), elapsed);

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPostsByIdsAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
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

    /// <summary>
    /// Finds posts near a geographic location using PostGIS
    /// </summary>
    public async Task<PostFeedDto> FindNearbyPostsAsync(
        double latitude, 
        double longitude, 
        double radiusKm = 10, 
        int pageSize = 20, 
        int pageNumber = 1)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation(
            "[PostService.FindNearbyPostsAsync] START - Lat={Lat}, Lon={Lon}, Radius={Radius}km, PageSize={PageSize}, PageNumber={PageNumber}, RequestId={RequestId}",
            latitude, longitude, radiusKm, pageSize, pageNumber, requestId);

        try
        {
            // Delegate to LocationService which uses PostGIS
            var postDtos = await _locationService.FindNearbyPostsAsync(
                latitude, longitude, radiusKm, pageSize, pageNumber);

            var totalCount = postDtos.Count;

            _logger.LogInformation(
                "[PostService.FindNearbyPostsAsync] SUCCESS - Found {Count} posts, RequestId={RequestId}",
                totalCount, requestId);

            return new PostFeedDto
            {
                Posts = postDtos,
                Page = pageNumber - 1, // Convert to 0-based
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[PostService.FindNearbyPostsAsync] ERROR - Lat={Lat}, Lon={Lon}, RequestId={RequestId}",
                latitude, longitude, requestId);
            throw;
        }
    }

    #endregion

    #region Blog Helper Methods

    /// <summary>
    /// Calculates the estimated read time in minutes based on word count
    /// Uses average reading speed of 200 words per minute
    /// </summary>
    /// <param name="content">The blog content to analyze</param>
    /// <returns>Estimated read time in minutes, minimum 1 minute</returns>
    private static int CalculateReadTimeMinutes(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 1;

        // Strip HTML tags for accurate word count
        var plainText = System.Text.RegularExpressions.Regex.Replace(content, "<[^>]*>", " ");
        
        // Count words (split by whitespace)
        var words = plainText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        // Average reading speed: 200 words per minute
        const int wordsPerMinute = 200;
        var readTime = (int)Math.Ceiling((double)wordCount / wordsPerMinute);

        // Minimum 1 minute
        return Math.Max(1, readTime);
    }

    #endregion

    #region AI Content Extraction Helpers

    /// <summary>
    /// Merges user-provided tags with AI-extracted tags, giving priority to user tags
    /// </summary>
    /// <param name="userTags">Tags provided by the user (priority)</param>
    /// <param name="aiTags">Tags extracted by AI</param>
    /// <returns>Combined unique tags array</returns>
    private static string[] MergeTagsWithAiExtracted(string[]? userTags, string[]? aiTags)
    {
        var resultTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Add user tags first (they take priority)
        if (userTags != null && userTags.Length > 0)
        {
            foreach (var tag in userTags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                resultTags.Add(tag.Trim().ToLowerInvariant());
            }
        }
        
        // Add AI tags only if we don't have too many already (max 10 total)
        if (aiTags != null && resultTags.Count < 10)
        {
            foreach (var tag in aiTags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                if (resultTags.Count >= 10) break;
                resultTags.Add(tag.Trim().ToLowerInvariant());
            }
        }
        
        return resultTags.ToArray();
    }

    /// <summary>
    /// Builds business metadata JSON from AI-extracted content data
    /// </summary>
    /// <param name="extracted">The AI-extracted content metadata</param>
    /// <returns>JSON string with business metadata, or null if no relevant data</returns>
    private static string? BuildBusinessMetadataFromExtraction(ExtractedContentMetadata? extracted)
    {
        if (extracted == null || !extracted.Success)
            return null;

        var biz = extracted.BusinessMetadata;
        var pricing = extracted.PricingInfo;

        // Check if there's any business-relevant data
        var hasBusinessData = biz != null || pricing != null;

        if (!hasBusinessData)
            return null;

        var metadata = new Dictionary<string, object?>();
        
        if (biz != null)
        {
            if (!string.IsNullOrEmpty(biz.BusinessName))
                metadata["businessName"] = biz.BusinessName;
            
            if (!string.IsNullOrEmpty(biz.BusinessType))
                metadata["businessType"] = biz.BusinessType;
            
            if (!string.IsNullOrEmpty(biz.Phone))
                metadata["phone"] = biz.Phone;
            
            if (!string.IsNullOrEmpty(biz.Email))
                metadata["email"] = biz.Email;
            
            if (!string.IsNullOrEmpty(biz.Website))
                metadata["website"] = biz.Website;
            
            if (!string.IsNullOrEmpty(biz.WorkingHours))
                metadata["businessHours"] = biz.WorkingHours;
            
            if (biz.Specialties != null && biz.Specialties.Count > 0)
                metadata["specialties"] = biz.Specialties;

            if (biz.AcceptsWalkIns.HasValue)
                metadata["acceptsWalkIns"] = biz.AcceptsWalkIns.Value;

            if (biz.RequiresAppointment.HasValue)
                metadata["requiresAppointment"] = biz.RequiresAppointment.Value;
        }
        
        if (pricing != null)
        {
            if (!string.IsNullOrEmpty(pricing.PriceRange))
                metadata["priceRange"] = pricing.PriceRange;
            
            if (!string.IsNullOrEmpty(pricing.Currency))
                metadata["currency"] = pricing.Currency;
        }

        // Add event metadata if present
        var evt = extracted.EventMetadata;
        if (evt != null)
        {
            if (!string.IsNullOrEmpty(evt.EventName))
                metadata["eventName"] = evt.EventName;
            
            if (evt.EventDate.HasValue)
                metadata["eventDate"] = evt.EventDate.Value.ToString("o");
            
            if (!string.IsNullOrEmpty(evt.Venue))
                metadata["venue"] = evt.Venue;
            
            if (!string.IsNullOrEmpty(evt.TicketPrice))
                metadata["ticketPrice"] = evt.TicketPrice;
        }

        if (metadata.Count == 0)
            return null;

        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    #endregion

    #region Public Access Methods (Anonymous)

    /// <summary>
    /// Gets public posts feed for unauthenticated users (only Visibility = Public)
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPublicFeedAsync(int page = 1, int pageSize = 20, string? profileType = null)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[PostService.GetPublicFeedAsync] START - RequestId={RequestId}, Page={Page}, PageSize={PageSize}, ProfileType={ProfileType}",
            requestId, page, pageSize, profileType);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postRepository = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            // Get only public posts
            var (posts, totalCount) = await postRepository.GetPublicPostsAsync(page, pageSize, profileType);
            
            _logger.LogInformation("[PostService.GetPublicFeedAsync] Repository returned {PostCount} posts (total: {TotalCount}) - RequestId={RequestId}",
                posts.Count(), totalCount, requestId);

            // Map to DTOs (no keycloakId for anonymous access)
            var postDtos = new List<PostDto>();
            foreach (var post in posts)
            {
                var dto = await MapToPostDtoAsync(post, null, false, false);
                if (dto != null)
                    postDtos.Add(dto);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPublicFeedAsync] SUCCESS - RequestId={RequestId}, ReturnedCount={ReturnedCount}, Duration={Duration}ms",
                requestId, postDtos.Count, elapsed);

            return (postDtos, totalCount);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPublicFeedAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets a public post by ID (only if Visibility = Public)
    /// </summary>
    public async Task<PostDto?> GetPublicPostByIdAsync(Guid postId)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[PostService.GetPublicPostByIdAsync] START - RequestId={RequestId}, PostId={PostId}",
            requestId, postId);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postRepository = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            var post = await postRepository.GetByIdAsync(postId);

            if (post == null)
            {
                _logger.LogInformation("[PostService.GetPublicPostByIdAsync] Post not found - RequestId={RequestId}, PostId={PostId}",
                    requestId, postId);
                return null;
            }

            // Check if post is public
            if (post.Visibility != VisibilityLevel.Public)
            {
                _logger.LogInformation("[PostService.GetPublicPostByIdAsync] Post is not public - RequestId={RequestId}, PostId={PostId}, Visibility={Visibility}",
                    requestId, postId, post.Visibility);
                return null;
            }

            var dto = await MapToPostDtoAsync(post, null, false, false);
            _logger.LogInformation("[PostService.GetPublicPostByIdAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostService.GetPublicPostByIdAsync] ERROR - RequestId={RequestId}, PostId={PostId}",
                requestId, postId);
            throw;
        }
    }

    /// <summary>
    /// Gets public posts by a specific profile (only Visibility = Public)
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPublicPostsByProfileAsync(Guid profileId, int page = 1, int pageSize = 20)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[PostService.GetPublicPostsByProfileAsync] START - RequestId={RequestId}, ProfileId={ProfileId}, Page={Page}, PageSize={PageSize}",
            requestId, profileId, page, pageSize);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postRepository = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            // Get only public posts by this profile
            var (posts, totalCount) = await postRepository.GetPublicPostsByProfileAsync(profileId, page, pageSize);
            
            _logger.LogInformation("[PostService.GetPublicPostsByProfileAsync] Repository returned {PostCount} posts (total: {TotalCount}) - RequestId={RequestId}",
                posts.Count(), totalCount, requestId);

            // Map to DTOs (no keycloakId for anonymous access)
            var postDtos = new List<PostDto>();
            foreach (var post in posts)
            {
                var dto = await MapToPostDtoAsync(post, null, false, false);
                if (dto != null)
                    postDtos.Add(dto);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetPublicPostsByProfileAsync] SUCCESS - RequestId={RequestId}, ReturnedCount={ReturnedCount}, Duration={Duration}ms",
                requestId, postDtos.Count, elapsed);

            return (postDtos, totalCount);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetPublicPostsByProfileAsync] ERROR - RequestId={RequestId}, ProfileId={ProfileId}, Duration={Duration}ms",
                requestId, profileId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets trending public posts based on engagement
    /// </summary>
    public async Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetTrendingPublicPostsAsync(int limit = 5)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PostService.GetTrendingPublicPostsAsync] START - RequestId={RequestId}, Limit={Limit}",
            requestId, limit);

        try
        {
            // Get recent public posts (last 7 days)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var (recentPosts, _) = await _postRepository.GetPublicPostsAsync(1, 100);
            
            // Convert to DTOs and calculate engagement score
            var postDtos = new List<PostDto>();
            foreach (var post in recentPosts.Where(p => p.CreatedAt >= sevenDaysAgo))
            {
                var dto = await MapToPostDtoAsync(post);
                if (dto != null)
                {
                    postDtos.Add(dto);
                }
            }
            
            // Sort by engagement and take top posts
            var trendingPosts = postDtos
                .Select(p => new
                {
                    Post = p,
                    EngagementScore = (p.ReactionSummary?.TotalReactions ?? 0) * 2 + p.CommentCount
                })
                .OrderByDescending(x => x.EngagementScore)
                .ThenByDescending(x => x.Post.CreatedAt)
                .Take(limit)
                .Select(x => x.Post)
                .ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PostService.GetTrendingPublicPostsAsync] SUCCESS - RequestId={RequestId}, ReturnedCount={ReturnedCount}, Duration={Duration}ms",
                requestId, trendingPosts.Count, elapsed);

            return (trendingPosts, trendingPosts.Count);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[PostService.GetTrendingPublicPostsAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            return (new List<PostDto>(), 0);
        }
    }

    #endregion
}