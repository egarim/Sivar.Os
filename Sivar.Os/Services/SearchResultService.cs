using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing structured search results from AI chat
/// </summary>
public class SearchResultService : ISearchResultService
{
    private readonly IPostRepository _postRepository;
    private readonly SivarDbContext _dbContext;
    private readonly IVectorEmbeddingService _embeddingService;
    private readonly IBusinessContactInfoRepository _contactInfoRepository;
    private readonly IContactUrlBuilder _contactUrlBuilder;
    private readonly ILogger<SearchResultService> _logger;

    public SearchResultService(
        IPostRepository postRepository,
        SivarDbContext dbContext,
        IVectorEmbeddingService embeddingService,
        IBusinessContactInfoRepository contactInfoRepository,
        IContactUrlBuilder contactUrlBuilder,
        ILogger<SearchResultService> logger)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _contactInfoRepository = contactInfoRepository ?? throw new ArgumentNullException(nameof(contactInfoRepository));
        _contactUrlBuilder = contactUrlBuilder ?? throw new ArgumentNullException(nameof(contactUrlBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SearchResultsCollectionDto> HybridSearchAsync(HybridSearchRequestDto request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[SearchResultService.HybridSearchAsync] Starting hybrid search: Query='{Query}'", request.Query);

        try
        {
            // Generate embedding if not provided
            var queryVector = request.QueryEmbedding != null
                ? "[" + string.Join(",", request.QueryEmbedding) + "]"
                : await GenerateQueryEmbeddingAsync(request.Query);

            if (string.IsNullOrEmpty(queryVector))
            {
                _logger.LogWarning("[SearchResultService.HybridSearchAsync] Failed to generate query embedding");
                return new SearchResultsCollectionDto { Query = request.Query };
            }

            // Convert result types to post types
            PostType[]? postTypes = request.ResultTypes?.Length > 0
                ? MapResultTypesToPostTypes(request.ResultTypes)
                : null;

            // Execute hybrid search
            var hybridResults = await _postRepository.HybridSearchAsync(
                queryVector,
                request.Query,
                request.UserLatitude,
                request.UserLongitude,
                request.MaxDistanceKm,
                postTypes,
                request.Category,
                request.SemanticWeight,
                request.FullTextWeight,
                request.GeoWeight,
                request.Limit);

            // Map results to typed DTOs
            var businesses = new List<BusinessSearchResultDto>();
            var events = new List<EventSearchResultDto>();
            var procedures = new List<ProcedureSearchResultDto>();
            var tourism = new List<TourismSearchResultDto>();
            var products = new List<ProductSearchResultDto>();
            var services = new List<ServiceSearchResultDto>();

            int displayOrder = 0;
            foreach (var result in hybridResults)
            {
                var dto = MapPostToSearchResult(
                    result.Post,
                    result.CombinedScore,
                    result.SemanticSimilarity,
                    result.FullTextRank,
                    result.DistanceKm,
                    displayOrder++);

                // Sort into appropriate collection
                switch (dto)
                {
                    case BusinessSearchResultDto business:
                        businesses.Add(business);
                        break;
                    case EventSearchResultDto evt:
                        events.Add(evt);
                        break;
                    case ProcedureSearchResultDto proc:
                        procedures.Add(proc);
                        break;
                    case TourismSearchResultDto tour:
                        tourism.Add(tour);
                        break;
                    case ProductSearchResultDto prod:
                        products.Add(prod);
                        break;
                    case ServiceSearchResultDto svc:
                        services.Add(svc);
                        break;
                }
            }

            stopwatch.Stop();

            var collection = new SearchResultsCollectionDto
            {
                Query = request.Query,
                TotalCount = hybridResults.Count,
                SearchTimeMs = stopwatch.ElapsedMilliseconds,
                Businesses = businesses,
                Events = events,
                Procedures = procedures,
                Tourism = tourism,
                Products = products,
                Services = services
            };
            
            // Phase 1: Load contacts for all business results (bulk load for efficiency)
            await LoadContactsForBusinessResultsAsync(businesses);

            _logger.LogInformation(
                "[SearchResultService.HybridSearchAsync] Completed: {Count} results in {Ms}ms",
                collection.TotalCount, collection.SearchTimeMs);

            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchResultService.HybridSearchAsync] Error during hybrid search");
            return new SearchResultsCollectionDto { Query = request.Query };
        }
    }

    /// <inheritdoc />
    public async Task<List<Guid>> SaveSearchResultsAsync(Guid chatMessageId, SearchResultsCollectionDto results)
    {
        var savedIds = new List<Guid>();

        try
        {
            // Save all results
            int order = 0;
            
            foreach (var business in results.Businesses)
            {
                var entity = MapDtoToEntity(business, chatMessageId, order++);
                await _dbContext.SearchResults.AddAsync(entity);
                savedIds.Add(entity.Id);
            }

            foreach (var evt in results.Events)
            {
                var entity = MapDtoToEntity(evt, chatMessageId, order++);
                await _dbContext.SearchResults.AddAsync(entity);
                savedIds.Add(entity.Id);
            }

            foreach (var proc in results.Procedures)
            {
                var entity = MapDtoToEntity(proc, chatMessageId, order++);
                await _dbContext.SearchResults.AddAsync(entity);
                savedIds.Add(entity.Id);
            }

            foreach (var tour in results.Tourism)
            {
                var entity = MapDtoToEntity(tour, chatMessageId, order++);
                await _dbContext.SearchResults.AddAsync(entity);
                savedIds.Add(entity.Id);
            }

            foreach (var prod in results.Products)
            {
                var entity = MapDtoToEntity(prod, chatMessageId, order++);
                await _dbContext.SearchResults.AddAsync(entity);
                savedIds.Add(entity.Id);
            }

            foreach (var svc in results.Services)
            {
                var entity = MapDtoToEntity(svc, chatMessageId, order++);
                await _dbContext.SearchResults.AddAsync(entity);
                savedIds.Add(entity.Id);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "[SearchResultService.SaveSearchResultsAsync] Saved {Count} results for message {MessageId}",
                savedIds.Count, chatMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchResultService.SaveSearchResultsAsync] Error saving results");
        }

        return savedIds;
    }

    /// <inheritdoc />
    public async Task<SearchResultsCollectionDto?> GetSearchResultsByMessageAsync(Guid chatMessageId)
    {
        try
        {
            var results = await _dbContext.SearchResults
                .Where(sr => sr.ChatMessageId == chatMessageId && !sr.IsDeleted)
                .OrderBy(sr => sr.DisplayOrder)
                .ToListAsync();
                
            if (!results.Any())
                return null;

            return MapEntitiesToCollection(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchResultService.GetSearchResultsByMessageAsync] Error getting results");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<(Guid MessageId, SearchResultsCollectionDto Results)>> GetSearchResultsByConversationAsync(Guid conversationId)
    {
        var result = new List<(Guid MessageId, SearchResultsCollectionDto Results)>();

        try
        {
            // Get all assistant messages for the conversation
            var messages = await _dbContext.ChatMessages
                .Where(m => m.ConversationId == conversationId && m.Role == "assistant" && !m.IsDeleted)
                .ToListAsync();
            
            foreach (var message in messages)
            {
                var searchResults = await GetSearchResultsByMessageAsync(message.Id);
                if (searchResults != null && searchResults.HasResults)
                {
                    result.Add((message.Id, searchResults));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchResultService.GetSearchResultsByConversationAsync] Error");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<BusinessSearchResultDto>> SearchBusinessesAsync(
        string query,
        float[]? queryEmbedding = null,
        string? category = null,
        double? userLatitude = null,
        double? userLongitude = null,
        double? maxDistanceKm = null,
        int limit = 10)
    {
        var request = new HybridSearchRequestDto
        {
            Query = query,
            QueryEmbedding = queryEmbedding,
            ResultTypes = [SearchResultType.Business],
            Category = category,
            UserLatitude = userLatitude,
            UserLongitude = userLongitude,
            MaxDistanceKm = maxDistanceKm,
            Limit = limit
        };

        var results = await HybridSearchAsync(request);
        return results.Businesses.ToList();
    }

    /// <inheritdoc />
    public async Task<List<EventSearchResultDto>> SearchEventsAsync(
        string query,
        float[]? queryEmbedding = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        double? userLatitude = null,
        double? userLongitude = null,
        int limit = 10)
    {
        var request = new HybridSearchRequestDto
        {
            Query = query,
            QueryEmbedding = queryEmbedding,
            ResultTypes = [SearchResultType.Event],
            UserLatitude = userLatitude,
            UserLongitude = userLongitude,
            Limit = limit
        };

        var results = await HybridSearchAsync(request);
        
        // Filter by date if specified
        var events = results.Events.ToList();
        if (startDate.HasValue)
            events = events.Where(e => e.EventDate >= startDate).ToList();
        if (endDate.HasValue)
            events = events.Where(e => e.EventDate <= endDate).ToList();

        return events;
    }

    /// <inheritdoc />
    public async Task<List<ProcedureSearchResultDto>> SearchProceduresAsync(
        string query,
        float[]? queryEmbedding = null,
        int limit = 10)
    {
        var request = new HybridSearchRequestDto
        {
            Query = query,
            QueryEmbedding = queryEmbedding,
            ResultTypes = [SearchResultType.Procedure],
            Limit = limit
        };

        var results = await HybridSearchAsync(request);
        return results.Procedures.ToList();
    }

    /// <inheritdoc />
    public async Task<List<TourismSearchResultDto>> SearchTourismAsync(
        string query,
        float[]? queryEmbedding = null,
        double? userLatitude = null,
        double? userLongitude = null,
        int limit = 10)
    {
        var request = new HybridSearchRequestDto
        {
            Query = query,
            QueryEmbedding = queryEmbedding,
            ResultTypes = [SearchResultType.Tourism],
            UserLatitude = userLatitude,
            UserLongitude = userLongitude,
            Limit = limit
        };

        var results = await HybridSearchAsync(request);
        return results.Tourism.ToList();
    }

    /// <inheritdoc />
    public SearchResultBaseDto MapPostToSearchResult(
        Post post,
        double relevanceScore,
        double? semanticScore = null,
        double? fullTextRank = null,
        double? distanceKm = null,
        int displayOrder = 0)
    {
        var matchSource = DetermineMatchSource(semanticScore, fullTextRank, distanceKm);

        // Parse business metadata if available
        BusinessMetadataDto? metadata = null;
        if (!string.IsNullOrEmpty(post.BusinessMetadata))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<BusinessMetadataDto>(post.BusinessMetadata);
            }
            catch { /* Ignore parsing errors */ }
        }

        // Parse pricing info if available
        PricingInfoDto? pricing = null;
        if (!string.IsNullOrEmpty(post.PricingInfo))
        {
            try
            {
                pricing = JsonSerializer.Deserialize<PricingInfoDto>(post.PricingInfo);
            }
            catch { /* Ignore parsing errors */ }
        }

        // Create the appropriate DTO based on post type
        return post.PostType switch
        {
            PostType.BusinessLocation => CreateBusinessResult(post, relevanceScore, semanticScore, fullTextRank, distanceKm, displayOrder, matchSource, metadata),
            PostType.Event => CreateEventResult(post, relevanceScore, semanticScore, fullTextRank, distanceKm, displayOrder, matchSource, metadata),
            PostType.Product => CreateProductResult(post, relevanceScore, semanticScore, fullTextRank, distanceKm, displayOrder, matchSource, pricing),
            PostType.Service => CreateServiceResult(post, relevanceScore, semanticScore, fullTextRank, distanceKm, displayOrder, matchSource, pricing),
            _ => CreateBusinessResult(post, relevanceScore, semanticScore, fullTextRank, distanceKm, displayOrder, matchSource, metadata)
        };
    }

    #region Private Helper Methods

    private async Task<string?> GenerateQueryEmbeddingAsync(string query)
    {
        try
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
            if (embedding != null && embedding.Vector.Length > 0)
            {
                return _embeddingService.ToPostgresVector(embedding);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding for query");
        }
        return null;
    }

    private static PostType[]? MapResultTypesToPostTypes(SearchResultType[] resultTypes)
    {
        var postTypes = new List<PostType>();
        foreach (var rt in resultTypes)
        {
            switch (rt)
            {
                case SearchResultType.Business:
                    postTypes.Add(PostType.BusinessLocation);
                    break;
                case SearchResultType.Event:
                    postTypes.Add(PostType.Event);
                    break;
                case SearchResultType.Product:
                    postTypes.Add(PostType.Product);
                    break;
                case SearchResultType.Service:
                    postTypes.Add(PostType.Service);
                    break;
                case SearchResultType.Post:
                    postTypes.Add(PostType.General);
                    postTypes.Add(PostType.Blog);
                    break;
            }
        }
        return postTypes.Count > 0 ? postTypes.ToArray() : null;
    }

    private static SearchMatchSource DetermineMatchSource(double? semantic, double? fullText, double? distance)
    {
        if (!semantic.HasValue && !fullText.HasValue && !distance.HasValue)
            return SearchMatchSource.Hybrid;

        var max = new[] { semantic ?? 0, fullText ?? 0, distance.HasValue ? 1 - (distance.Value / 100) : 0 }.Max();

        if (semantic.HasValue && semantic.Value == max) return SearchMatchSource.Semantic;
        if (fullText.HasValue && fullText.Value == max) return SearchMatchSource.FullText;
        if (distance.HasValue) return SearchMatchSource.Geographic;

        return SearchMatchSource.Hybrid;
    }

    private BusinessSearchResultDto CreateBusinessResult(
        Post post, double relevanceScore, double? semanticScore, double? fullTextRank, 
        double? distanceKm, int displayOrder, SearchMatchSource matchSource, BusinessMetadataDto? metadata)
    {
        return new BusinessSearchResultDto
        {
            Id = post.Id,
            ResultType = SearchResultType.Business,
            MatchSource = matchSource,
            RelevanceScore = relevanceScore,
            DisplayOrder = displayOrder,
            Title = post.Title ?? post.Profile?.DisplayName ?? "Negocio",
            Description = post.Content.Length > 200 ? post.Content[..200] + "..." : post.Content,
            Handle = post.Profile?.Handle,
            Category = post.Tags?.FirstOrDefault(),
            ImageUrl = post.Attachments?.FirstOrDefault()?.ThumbnailUrl ?? post.Profile?.Avatar,
            City = post.Location?.City,
            Department = post.Location?.State,
            Latitude = post.Location?.Latitude,
            Longitude = post.Location?.Longitude,
            DistanceKm = distanceKm,
            Tags = post.Tags,
            ProfileId = post.ProfileId,
            Address = post.Location?.ToString(),
            Phone = metadata?.ContactPhone,
            Website = metadata?.Website,
            WorkingHours = FormatWorkingHours(metadata?.WorkingHours),
            PriceRange = metadata?.PriceRange
            // Contacts loaded via bulk LoadContactsForBusinessResultsAsync after search
        };
    }

    private EventSearchResultDto CreateEventResult(
        Post post, double relevanceScore, double? semanticScore, double? fullTextRank,
        double? distanceKm, int displayOrder, SearchMatchSource matchSource, BusinessMetadataDto? metadata)
    {
        return new EventSearchResultDto
        {
            Id = post.Id,
            ResultType = SearchResultType.Event,
            MatchSource = matchSource,
            RelevanceScore = relevanceScore,
            DisplayOrder = displayOrder,
            Title = post.Title ?? "Evento",
            Description = post.Content.Length > 200 ? post.Content[..200] + "..." : post.Content,
            Handle = post.Profile?.Handle,
            Category = "Evento",
            ImageUrl = post.Attachments?.FirstOrDefault()?.ThumbnailUrl,
            City = post.Location?.City,
            Department = post.Location?.State,
            Latitude = post.Location?.Latitude,
            Longitude = post.Location?.Longitude,
            DistanceKm = distanceKm,
            Tags = post.Tags,
            PostId = post.Id,
            EventDate = metadata?.EventDate,
            EventEndDate = metadata?.EventEndDate,
            Venue = metadata?.Venue,
            Address = post.Location?.ToString(),
            TicketPrice = metadata?.TicketPrice
        };
    }

    private ProductSearchResultDto CreateProductResult(
        Post post, double relevanceScore, double? semanticScore, double? fullTextRank,
        double? distanceKm, int displayOrder, SearchMatchSource matchSource, PricingInfoDto? pricing)
    {
        return new ProductSearchResultDto
        {
            Id = post.Id,
            ResultType = SearchResultType.Product,
            MatchSource = matchSource,
            RelevanceScore = relevanceScore,
            DisplayOrder = displayOrder,
            Title = post.Title ?? "Producto",
            Description = post.Content.Length > 200 ? post.Content[..200] + "..." : post.Content,
            Handle = post.Profile?.Handle,
            Category = post.Tags?.FirstOrDefault() ?? "Producto",
            ImageUrl = post.Attachments?.FirstOrDefault()?.ThumbnailUrl,
            City = post.Location?.City,
            Department = post.Location?.State,
            Latitude = post.Location?.Latitude,
            Longitude = post.Location?.Longitude,
            DistanceKm = distanceKm,
            Tags = post.Tags,
            PostId = post.Id,
            ProfileId = post.ProfileId,
            ProfileName = post.Profile?.DisplayName,
            ProfileHandle = post.Profile?.Handle,
            Price = pricing?.Amount,
            Currency = pricing?.Currency ?? "USD",
            IsNegotiable = pricing?.IsNegotiable,
            AvailabilityStatus = post.AvailabilityStatus?.ToString()
        };
    }

    private ServiceSearchResultDto CreateServiceResult(
        Post post, double relevanceScore, double? semanticScore, double? fullTextRank,
        double? distanceKm, int displayOrder, SearchMatchSource matchSource, PricingInfoDto? pricing)
    {
        return new ServiceSearchResultDto
        {
            Id = post.Id,
            ResultType = SearchResultType.Service,
            MatchSource = matchSource,
            RelevanceScore = relevanceScore,
            DisplayOrder = displayOrder,
            Title = post.Title ?? "Servicio",
            Description = post.Content.Length > 200 ? post.Content[..200] + "..." : post.Content,
            Handle = post.Profile?.Handle,
            Category = post.Tags?.FirstOrDefault() ?? "Servicio",
            ImageUrl = post.Attachments?.FirstOrDefault()?.ThumbnailUrl,
            City = post.Location?.City,
            Department = post.Location?.State,
            Latitude = post.Location?.Latitude,
            Longitude = post.Location?.Longitude,
            DistanceKm = distanceKm,
            Tags = post.Tags,
            PostId = post.Id,
            ProfileId = post.ProfileId,
            ProfileName = post.Profile?.DisplayName,
            ProfileHandle = post.Profile?.Handle,
            Price = pricing?.Amount,
            Currency = pricing?.Currency ?? "USD",
            IsNegotiable = pricing?.IsNegotiable,
            AvailabilityStatus = post.AvailabilityStatus?.ToString()
        };
    }

    private static string? FormatWorkingHours(Dictionary<string, string>? hours)
    {
        if (hours == null || hours.Count == 0) return null;
        return string.Join(", ", hours.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    private SearchResult MapDtoToEntity(SearchResultBaseDto dto, Guid chatMessageId, int displayOrder)
    {
        var entity = new SearchResult
        {
            Id = Guid.NewGuid(),
            ChatMessageId = chatMessageId,
            ResultType = dto.ResultType,
            MatchSource = dto.MatchSource,
            RelevanceScore = dto.RelevanceScore,
            DisplayOrder = displayOrder,
            Title = dto.Title,
            Description = dto.Description,
            Handle = dto.Handle,
            Category = dto.Category,
            ImageUrl = dto.ImageUrl,
            City = dto.City,
            Department = dto.Department,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            DistanceKm = dto.DistanceKm,
            Tags = dto.Tags,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set type-specific properties
        switch (dto)
        {
            case BusinessSearchResultDto biz:
                entity.ProfileId = biz.ProfileId;
                entity.Address = biz.Address;
                entity.Phone = biz.Phone;
                entity.Website = biz.Website;
                entity.WorkingHours = biz.WorkingHours;
                entity.PriceRange = biz.PriceRange;
                entity.Rating = biz.Rating;
                entity.ReviewCount = biz.ReviewCount;
                break;
            case EventSearchResultDto evt:
                entity.PostId = evt.PostId;
                entity.EventDate = evt.EventDate;
                entity.EventEndDate = evt.EventEndDate;
                entity.Venue = evt.Venue;
                entity.Address = evt.Address;
                entity.TicketPrice = evt.TicketPrice;
                break;
            case ProcedureSearchResultDto proc:
                entity.PostId = proc.PostId;
                entity.Requirements = proc.Requirements != null ? JsonSerializer.Serialize(proc.Requirements) : null;
                entity.ProcessingTime = proc.ProcessingTime;
                entity.Cost = proc.Cost;
                entity.WhereToGo = proc.WhereToGo;
                entity.OnlineUrl = proc.OnlineUrl;
                entity.Address = proc.Address;
                entity.Phone = proc.Phone;
                entity.WorkingHours = proc.WorkingHours;
                break;
        }

        return entity;
    }

    private SearchResultsCollectionDto MapEntitiesToCollection(List<SearchResult> entities)
    {
        var businesses = new List<BusinessSearchResultDto>();
        var events = new List<EventSearchResultDto>();
        var procedures = new List<ProcedureSearchResultDto>();
        var tourism = new List<TourismSearchResultDto>();
        var products = new List<ProductSearchResultDto>();
        var services = new List<ServiceSearchResultDto>();

        foreach (var entity in entities.OrderBy(e => e.DisplayOrder))
        {
            switch (entity.ResultType)
            {
                case SearchResultType.Business:
                    businesses.Add(MapEntityToBusinessDto(entity));
                    break;
                case SearchResultType.Event:
                    events.Add(MapEntityToEventDto(entity));
                    break;
                case SearchResultType.Procedure:
                    procedures.Add(MapEntityToProcedureDto(entity));
                    break;
                case SearchResultType.Tourism:
                    tourism.Add(MapEntityToTourismDto(entity));
                    break;
                case SearchResultType.Product:
                    products.Add(MapEntityToProductDto(entity));
                    break;
                case SearchResultType.Service:
                    services.Add(MapEntityToServiceDto(entity));
                    break;
            }
        }

        return new SearchResultsCollectionDto
        {
            TotalCount = entities.Count,
            Businesses = businesses,
            Events = events,
            Procedures = procedures,
            Tourism = tourism,
            Products = products,
            Services = services
        };
    }

    private static BusinessSearchResultDto MapEntityToBusinessDto(SearchResult entity) => new()
    {
        Id = entity.Id,
        ResultType = entity.ResultType,
        MatchSource = entity.MatchSource,
        RelevanceScore = entity.RelevanceScore,
        DisplayOrder = entity.DisplayOrder,
        Title = entity.Title,
        Description = entity.Description,
        Handle = entity.Handle,
        Category = entity.Category,
        ImageUrl = entity.ImageUrl,
        City = entity.City,
        Department = entity.Department,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        DistanceKm = entity.DistanceKm,
        Tags = entity.Tags,
        ProfileId = entity.ProfileId,
        Address = entity.Address,
        Phone = entity.Phone,
        Website = entity.Website,
        WorkingHours = entity.WorkingHours,
        PriceRange = entity.PriceRange,
        Rating = entity.Rating,
        ReviewCount = entity.ReviewCount
    };

    private static EventSearchResultDto MapEntityToEventDto(SearchResult entity) => new()
    {
        Id = entity.Id,
        ResultType = entity.ResultType,
        MatchSource = entity.MatchSource,
        RelevanceScore = entity.RelevanceScore,
        DisplayOrder = entity.DisplayOrder,
        Title = entity.Title,
        Description = entity.Description,
        Handle = entity.Handle,
        Category = entity.Category,
        ImageUrl = entity.ImageUrl,
        City = entity.City,
        Department = entity.Department,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        DistanceKm = entity.DistanceKm,
        Tags = entity.Tags,
        PostId = entity.PostId,
        EventDate = entity.EventDate,
        EventEndDate = entity.EventEndDate,
        Venue = entity.Venue,
        Address = entity.Address,
        TicketPrice = entity.TicketPrice
    };

    private static ProcedureSearchResultDto MapEntityToProcedureDto(SearchResult entity)
    {
        // Parse legacy requirements into structured documents
        string[]? requirements = null;
        IReadOnlyList<ProcedureDocumentDto>? documents = null;
        
        if (!string.IsNullOrEmpty(entity.Requirements))
        {
            try
            {
                requirements = JsonSerializer.Deserialize<string[]>(entity.Requirements);
                
                // Convert simple requirements to structured documents for the checklist
                if (requirements?.Length > 0)
                {
                    documents = requirements
                        .Select(req => new ProcedureDocumentDto
                        {
                            Name = req,
                            IsRequired = true
                        })
                        .ToList();
                }
            }
            catch
            {
                // If JSON parsing fails, treat as null
            }
        }
        
        return new()
        {
            Id = entity.Id,
            ResultType = entity.ResultType,
            MatchSource = entity.MatchSource,
            RelevanceScore = entity.RelevanceScore,
            DisplayOrder = entity.DisplayOrder,
            Title = entity.Title,
            Description = entity.Description,
            Handle = entity.Handle,
            Category = entity.Category,
            ImageUrl = entity.ImageUrl,
            City = entity.City,
            Department = entity.Department,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            DistanceKm = entity.DistanceKm,
            Tags = entity.Tags,
            PostId = entity.PostId,
            Requirements = requirements,
            Documents = documents,
            // Steps will be populated when structured step data is available
            Steps = null,
            ProcessingTime = entity.ProcessingTime,
            Cost = entity.Cost,
            WhereToGo = entity.WhereToGo,
            OnlineUrl = entity.OnlineUrl,
            Address = entity.Address,
            Phone = entity.Phone,
            WorkingHours = entity.WorkingHours
        };
    }

    private static TourismSearchResultDto MapEntityToTourismDto(SearchResult entity) => new()
    {
        Id = entity.Id,
        ResultType = entity.ResultType,
        MatchSource = entity.MatchSource,
        RelevanceScore = entity.RelevanceScore,
        DisplayOrder = entity.DisplayOrder,
        Title = entity.Title,
        Description = entity.Description,
        Handle = entity.Handle,
        Category = entity.Category,
        ImageUrl = entity.ImageUrl,
        City = entity.City,
        Department = entity.Department,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        DistanceKm = entity.DistanceKm,
        Tags = entity.Tags,
        PostId = entity.PostId,
        ProfileId = entity.ProfileId,
        Address = entity.Address,
        Phone = entity.Phone,
        Website = entity.Website,
        WorkingHours = entity.WorkingHours,
        TicketPrice = entity.TicketPrice,
        Rating = entity.Rating,
        ReviewCount = entity.ReviewCount
    };

    private static ProductSearchResultDto MapEntityToProductDto(SearchResult entity) => new()
    {
        Id = entity.Id,
        ResultType = entity.ResultType,
        MatchSource = entity.MatchSource,
        RelevanceScore = entity.RelevanceScore,
        DisplayOrder = entity.DisplayOrder,
        Title = entity.Title,
        Description = entity.Description,
        Handle = entity.Handle,
        Category = entity.Category,
        ImageUrl = entity.ImageUrl,
        City = entity.City,
        Department = entity.Department,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        DistanceKm = entity.DistanceKm,
        Tags = entity.Tags,
        PostId = entity.PostId,
        ProfileId = entity.ProfileId
    };

    private static ServiceSearchResultDto MapEntityToServiceDto(SearchResult entity) => new()
    {
        Id = entity.Id,
        ResultType = entity.ResultType,
        MatchSource = entity.MatchSource,
        RelevanceScore = entity.RelevanceScore,
        DisplayOrder = entity.DisplayOrder,
        Title = entity.Title,
        Description = entity.Description,
        Handle = entity.Handle,
        Category = entity.Category,
        ImageUrl = entity.ImageUrl,
        City = entity.City,
        Department = entity.Department,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        DistanceKm = entity.DistanceKm,
        Tags = entity.Tags,
        PostId = entity.PostId,
        ProfileId = entity.ProfileId,
        Phone = entity.Phone
    };
    
    /// <summary>
    /// Loads contacts for a profile and converts them to ContactDisplayDto
    /// </summary>
    private async Task<List<ContactDisplayDto>> LoadContactsForProfileAsync(Guid profileId, string regionCode = "SV")
    {
        try
        {
            var contacts = await _contactInfoRepository.GetByProfileIdAsync(profileId);
            if (!contacts.Any())
                return new List<ContactDisplayDto>();
            
            return contacts
                .Where(c => c.IsActive && c.ContactType != null && c.ContactType.IsActive)
                .OrderBy(c => c.ContactType?.Category)
                .ThenByDescending(c => c.ContactType?.GetRegionalPopularity(regionCode) ?? 50)
                .ThenBy(c => c.SortOrder)
                .Select(c => new ContactDisplayDto
                {
                    TypeKey = c.ContactType?.Key ?? "unknown",
                    DisplayName = c.ContactType?.DisplayName ?? c.Label ?? "Contacto",
                    Icon = c.ContactType?.Icon ?? "📞",
                    MudBlazorIcon = c.ContactType?.MudBlazorIcon,
                    Color = c.ContactType?.Color ?? "#607D8B",
                    Category = c.ContactType?.Category ?? "other",
                    Url = c.ContactType != null 
                        ? _contactUrlBuilder.BuildUrl(c.ContactType, c)
                        : string.Empty,
                    Value = c.Value,
                    Label = c.Label,
                    OpenInNewTab = c.ContactType?.OpenInNewTab ?? true,
                    MobileOnly = c.ContactType?.MobileOnly ?? false,
                    SortOrder = c.SortOrder,
                    RegionalPopularity = c.ContactType?.GetRegionalPopularity(regionCode) ?? 50
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SearchResultService] Failed to load contacts for profile {ProfileId}", profileId);
            return new List<ContactDisplayDto>();
        }
    }
    
    /// <summary>
    /// Bulk loads contacts for all business results (Phase 1: Contact Actions)
    /// </summary>
    private async Task LoadContactsForBusinessResultsAsync(List<BusinessSearchResultDto> businesses)
    {
        if (!businesses.Any()) return;
        
        _logger.LogDebug("[SearchResultService] Loading contacts for {Count} business results", businesses.Count);
        
        foreach (var business in businesses)
        {
            if (business.ProfileId.HasValue)
            {
                business.Contacts = await LoadContactsForProfileAsync(business.ProfileId.Value);
            }
        }
        
        var withContacts = businesses.Count(b => b.Contacts?.Any() == true);
        _logger.LogDebug("[SearchResultService] Loaded contacts: {WithContacts}/{Total} businesses have contacts", withContacts, businesses.Count);
    }

    #endregion
}

/// <summary>
/// Helper DTO for parsing business metadata JSON
/// </summary>
internal class BusinessMetadataDto
{
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? PriceRange { get; set; }
    public Dictionary<string, string>? WorkingHours { get; set; }
    public DateTime? EventDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    public string? Venue { get; set; }
    public string? TicketPrice { get; set; }
}

/// <summary>
/// Helper DTO for parsing pricing info JSON
/// </summary>
internal class PricingInfoDto
{
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public bool? IsNegotiable { get; set; }
}
