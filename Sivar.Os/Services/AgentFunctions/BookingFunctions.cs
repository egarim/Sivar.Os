using System.ComponentModel;
using System.Text.Json;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.AgentFunctions;

/// <summary>
/// AI-callable functions for the Resource Booking System.
/// Enables users to search for bookable resources, check availability,
/// create bookings, and manage their reservations through the chat interface.
/// Phase 2 Integration: Populates LastSearchResults for structured card rendering.
/// </summary>
public class BookingFunctions
{
    private readonly IResourceBookingService _bookingService;
    private readonly IResourceBookingRepository _bookingRepository;
    private readonly IProfileService _profileService;
    private readonly ILogger<BookingFunctions> _logger;
    
    private Guid _currentProfileId;
    private string? _currentKeycloakId;

    /// <summary>
    /// Memory Guard: Tracks valid resource IDs from the last search.
    /// Prevents AI hallucination by validating IDs against actual search results.
    /// </summary>
    private HashSet<Guid> _validResourceIds = new();

    /// <summary>
    /// Phase 2: Captures the last booking search results as structured DTOs for card rendering.
    /// Reset before each AI agent call and populated by SearchBookableResources.
    /// </summary>
    public SearchResultsCollectionDto? LastSearchResults { get; private set; }

    /// <summary>
    /// Clears the last search results and valid resource IDs. Call before each AI agent invocation.
    /// </summary>
    public void ClearLastSearchResults()
    {
        LastSearchResults = null;
        // Don't clear _validResourceIds here - we want to maintain context across the conversation
        _logger.LogDebug("[BookingFunctions] LastSearchResults cleared, ValidResourceIds count: {Count}", _validResourceIds.Count);
    }

    /// <summary>
    /// Gets the list of valid resource IDs from recent searches.
    /// Used for debugging and hallucination detection.
    /// </summary>
    public IReadOnlyCollection<Guid> GetValidResourceIds() => _validResourceIds;

    /// <summary>
    /// Clears all context including valid resource IDs. Call when starting a new conversation.
    /// </summary>
    public void ClearAllContext()
    {
        LastSearchResults = null;
        _validResourceIds.Clear();
        _logger.LogDebug("[BookingFunctions] All context cleared");
    }

    public BookingFunctions(
        IResourceBookingService bookingService,
        IResourceBookingRepository bookingRepository,
        IProfileService profileService,
        ILogger<BookingFunctions> logger)
    {
        _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Set the current user context for booking operations
    /// </summary>
    public void SetCurrentUser(Guid profileId, string keycloakId)
    {
        _currentProfileId = profileId;
        _currentKeycloakId = keycloakId;
        _logger.LogInformation("[BookingFunctions] User context set - ProfileId={ProfileId}, KeycloakId={KeycloakId}",
            profileId, keycloakId);
    }

    /// <summary>
    /// Search for bookable resources (businesses, services, appointments)
    /// </summary>
    [Description("Search for services that accept APPOINTMENTS or RESERVATIONS. Use this for: barberías (barbershops), peluquerías (hair salons), salones de belleza, doctores, dentistas, clínicas, spas, restaurantes (for table reservations), meeting rooms, etc. This is the PRIMARY tool for finding places where users can book an appointment or reserve a time slot. Keywords that should trigger this: 'reservar', 'cita', 'appointment', 'book', 'agendar', 'cortarme el pelo', 'haircut'.")]
    public async Task<string> SearchBookableResources(
        [Description("Search query - business name, service type, or category (e.g., 'haircut', 'restaurant', 'dentist', 'barbería', 'mesa')")]
        string query,
        [Description("Optional: filter by category. Valid values: Barber, Hairdresser, MassageTherapist, Doctor, Dentist, PersonalTrainer, Consultant, Tutor, Photographer, Lawyer, Table, Chair, Booth, Vehicle, MeetingRoom, ConferenceRoom, Studio, EventSpace. Leave empty to search all categories.")]
        string? category = null,
        [Description("Maximum number of results to return (default 5, max 10)")]
        int maxResults = 5)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.SearchBookableResources] START - RequestId={RequestId}, Query={Query}, Category={Category}",
            requestId, query, category);

        try
        {
            maxResults = Math.Min(maxResults, 10);

            // Parse category if provided
            ResourceCategory? parsedCategory = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (Enum.TryParse<ResourceCategory>(category, true, out var cat))
                {
                    parsedCategory = cat;
                    _logger.LogInformation("[BookingFunctions.SearchBookableResources] Parsed category: {Category} -> {ParsedCategory}", 
                        category, parsedCategory);
                }
                else
                {
                    _logger.LogWarning("[BookingFunctions.SearchBookableResources] Invalid category '{Category}' - will search all categories", category);
                }
            }

            var queryDto = new ResourceQueryDto
            {
                SearchTerm = query,
                Category = parsedCategory,
                IsActive = true,
                PageSize = maxResults,
                Page = 1
            };

            _logger.LogInformation("[BookingFunctions.SearchBookableResources] Querying with: SearchTerm={SearchTerm}, Category={Category}, PageSize={PageSize}",
                queryDto.SearchTerm, queryDto.Category, queryDto.PageSize);

            var result = await _bookingService.QueryResourcesAsync(queryDto);

            _logger.LogInformation("[BookingFunctions.SearchBookableResources] Query returned: TotalCount={TotalCount}, ResourcesCount={ResourcesCount}",
                result.TotalCount, result.Resources?.Count ?? 0);

            if (result.Resources == null || !result.Resources.Any())
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"No bookable resources found matching '{query}'. Try a different search term or check available categories.",
                    count = 0,
                    resources = Array.Empty<object>()
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var resources = result.Resources.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                description = r.Description?.Length > 100 ? r.Description.Substring(0, 100) + "..." : r.Description,
                businessName = r.ProfileName,
                category = r.Category.ToString(),
                resourceType = r.ResourceType.ToString(),
                price = r.DefaultPrice,
                currency = r.Currency,
                slotDurationMinutes = r.SlotDurationMinutes,
                rating = r.AverageRating,
                reviewCount = r.ReviewCount,
                serviceCount = r.ServiceCount,
                imageUrl = r.ImageUrl
            }).ToList();

            _logger.LogInformation("[BookingFunctions.SearchBookableResources] SUCCESS - Found {Count} resources", resources.Count);

            // Memory Guard: Track valid resource IDs to prevent AI hallucination
            foreach (var r in result.Resources)
            {
                _validResourceIds.Add(r.Id);
            }
            _logger.LogInformation("[BookingFunctions.SearchBookableResources] Memory Guard: Added {Count} resource IDs to valid set (total: {Total})",
                result.Resources.Count, _validResourceIds.Count);

            // Phase 2: Populate LastSearchResults for structured card rendering
            // This prevents the fallback hybrid search from triggering
            var serviceResults = result.Resources.Select((r, index) => new ServiceSearchResultDto
            {
                Id = r.Id,
                Title = r.Name,
                Description = r.Description,
                Category = r.Category.ToString(),
                ProfileId = r.ProfileId,
                ProfileName = r.ProfileName,
                Price = r.DefaultPrice,
                Currency = r.Currency,
                Duration = r.SlotDurationMinutes > 0 ? $"{r.SlotDurationMinutes} min" : null,
                ImageUrl = r.ImageUrl,
                ResultType = SearchResultType.Service,
                MatchSource = SearchMatchSource.FullText,
                RelevanceScore = 1.0 - (index * 0.1), // Decreasing relevance by order
                DisplayOrder = index + 1
            }).ToList();

            LastSearchResults = new SearchResultsCollectionDto
            {
                Query = query,
                TotalCount = serviceResults.Count,
                SearchTimeMs = (long)(DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds, // Placeholder
                Services = serviceResults
            };

            _logger.LogInformation("[BookingFunctions.SearchBookableResources] Phase 2: Populated LastSearchResults with {Count} service results",
                serviceResults.Count);

            return JsonSerializer.Serialize(new
            {
                success = true,
                query,
                count = resources.Count,
                totalAvailable = result.TotalCount,
                resources
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.SearchBookableResources] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error searching for resources: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get detailed information about a specific bookable resource
    /// </summary>
    [Description("Get detailed information about a specific bookable resource, including available services, hours, and pricing. Use this after SearchBookableResources when the user wants more details about a specific business.")]
    public async Task<string> GetResourceDetails(
        [Description("The unique ID of the resource to get details for")]
        Guid resourceId)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.GetResourceDetails] START - RequestId={RequestId}, ResourceId={ResourceId}",
            requestId, resourceId);

        try
        {
            // Memory Guard: Check if resourceId was returned in a previous search
            if (_validResourceIds.Count > 0 && !_validResourceIds.Contains(resourceId))
            {
                _logger.LogWarning("[BookingFunctions.GetResourceDetails] HALLUCINATION DETECTED - ResourceId {ResourceId} was not in search results",
                    resourceId);
                
                var validIdsList = string.Join(", ", _validResourceIds.Take(5).Select(id => $"'{id}'"));
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"The resource ID '{resourceId}' was not found in your previous search results. Please use one of these valid IDs: {validIdsList}.",
                    validResourceIds = _validResourceIds.Take(5).ToList(),
                    hint = "Use the 'id' field from the resources returned by SearchBookableResources"
                });
            }

            var resource = await _bookingService.GetResourceAsync(resourceId);

            if (resource == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Resource not found. It may have been removed or is no longer available."
                });
            }

            var availability = resource.Availability
                .Where(a => a.IsAvailable)
                .GroupBy(a => a.DayOfWeek)
                .Select(g => new
                {
                    day = g.Key.ToString(),
                    hours = g.Select(a => $"{a.StartTime:HH:mm}-{a.EndTime:HH:mm}").ToList()
                })
                .ToList();

            var services = resource.Services
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    description = s.Description,
                    durationMinutes = s.DurationMinutes,
                    price = s.Price,
                    currency = s.Currency ?? resource.Currency
                })
                .ToList();

            _logger.LogInformation("[BookingFunctions.GetResourceDetails] SUCCESS - ResourceId={ResourceId}", resourceId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                resource = new
                {
                    id = resource.Id,
                    name = resource.Name,
                    description = resource.Description,
                    businessName = resource.ProfileName,
                    category = resource.Category.ToString(),
                    resourceType = resource.ResourceType.ToString(),
                    defaultPrice = resource.DefaultPrice,
                    currency = resource.Currency,
                    slotDurationMinutes = resource.SlotDurationMinutes,
                    minAdvanceBookingHours = resource.MinAdvanceBookingHours,
                    maxAdvanceBookingDays = resource.MaxAdvanceBookingDays,
                    cancellationWindowHours = resource.CancellationWindowHours,
                    confirmationMode = resource.ConfirmationMode.ToString(),
                    imageUrl = resource.ImageUrl,
                    tags = resource.Tags,
                    services,
                    availability,
                    totalServices = services.Count
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.GetResourceDetails] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error getting resource details: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Check available time slots for a resource
    /// </summary>
    [Description("Check available time slots for booking. Use this when the user wants to see when they can book an appointment. Shows available times for a specific date or range of days.")]
    public async Task<string> GetAvailableSlots(
        [Description("The unique ID of the resource to check availability for")]
        Guid resourceId,
        [Description("The date to check availability (format: YYYY-MM-DD). Defaults to tomorrow if not specified.")]
        string? date = null,
        [Description("Optional: specific service ID to check (affects duration and price)")]
        Guid? serviceId = null,
        [Description("Number of days to check ahead (default 1, max 7)")]
        int daysAhead = 1,
        [Description("Timezone for displaying times (default: America/El_Salvador)")]
        string timeZone = "America/El_Salvador")
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.GetAvailableSlots] START - RequestId={RequestId}, ResourceId={ResourceId}, Date={Date}",
            requestId, resourceId, date);

        try
        {
            // Memory Guard: Check if resourceId was returned in a previous search
            if (_validResourceIds.Count > 0 && !_validResourceIds.Contains(resourceId))
            {
                _logger.LogWarning("[BookingFunctions.GetAvailableSlots] HALLUCINATION DETECTED - ResourceId {ResourceId} was not in search results. Valid IDs: {ValidIds}",
                    resourceId, string.Join(", ", _validResourceIds.Take(5)));
                
                var validIdsList = string.Join(", ", _validResourceIds.Take(5).Select(id => $"'{id}'"));
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"The resource ID '{resourceId}' was not found in your previous search results. Please use one of these valid IDs: {validIdsList}. Or run SearchBookableResources again.",
                    validResourceIds = _validResourceIds.Take(5).ToList(),
                    hint = "Use the 'id' field from the resources returned by SearchBookableResources"
                });
            }

            // Parse date or default to tomorrow
            DateOnly targetDate;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsed))
            {
                targetDate = parsed;
            }
            else
            {
                targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            }

            daysAhead = Math.Min(daysAhead, 7);

            var query = new GetAvailableSlotsDto
            {
                ResourceId = resourceId,
                Date = targetDate,
                ServiceId = serviceId,
                DaysAhead = daysAhead,
                TimeZone = timeZone
            };

            var result = await _bookingService.GetAvailableSlotsAsync(query);

            if (result.SlotsByDate == null || !result.SlotsByDate.Any())
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"No available slots found for {result.ResourceName ?? "this resource"} on the selected date(s). Try a different date or check back later.",
                    resourceId,
                    resourceName = result.ResourceName,
                    serviceName = result.ServiceName,
                    checkedDates = Enumerable.Range(0, daysAhead).Select(i => targetDate.AddDays(i).ToString("yyyy-MM-dd")).ToList(),
                    availableSlots = new Dictionary<string, object>()
                });
            }

            var slotsByDate = result.SlotsByDate.ToDictionary(
                kvp => kvp.Key.ToString("yyyy-MM-dd"),
                kvp => kvp.Value.Select(s => new
                {
                    startTime = s.StartTime.ToString("HH:mm"),
                    endTime = s.EndTime.ToString("HH:mm"),
                    durationMinutes = s.DurationMinutes,
                    price = s.Price,
                    currency = s.Currency,
                    availableCapacity = s.AvailableCapacity
                }).ToList()
            );

            var totalSlots = result.SlotsByDate.Values.Sum(v => v.Count);

            _logger.LogInformation("[BookingFunctions.GetAvailableSlots] SUCCESS - Found {TotalSlots} slots across {DayCount} days",
                totalSlots, result.SlotsByDate.Count);

            return JsonSerializer.Serialize(new
            {
                success = true,
                resourceId,
                resourceName = result.ResourceName,
                serviceName = result.ServiceName,
                totalSlots,
                daysWithSlots = result.SlotsByDate.Count,
                slotsByDate
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.GetAvailableSlots] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error checking availability: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Create a new booking/reservation
    /// </summary>
    [Description("Create a booking or reservation for the user. Use this after the user has selected a resource, date, and time. Requires confirmation from the user before calling. CRITICAL: The resourceId MUST be an exact ID returned from SearchBookableResources or GetAvailableSlots - do NOT generate or guess IDs. IMPORTANT: Use the timezone from the chat context (user's browser timezone) if available.")]
    public async Task<string> CreateBooking(
        [Description("The EXACT unique ID of the resource to book - this MUST come from SearchBookableResources results. Never generate or guess this value.")]
        Guid resourceId,
        [Description("The start time of the booking (format: YYYY-MM-DDTHH:mm:ss or YYYY-MM-DD HH:mm)")]
        string startTime,
        [Description("Optional: specific service ID to book")]
        Guid? serviceId = null,
        [Description("Optional: notes from the customer")]
        string? notes = null,
        [Description("Optional: number of guests (default 1)")]
        int guestCount = 1,
        [Description("Timezone for the booking in IANA format. Use the timezone from the chat's location context if available, otherwise default to UTC.")]
        string timeZone = "UTC")
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.CreateBooking] START - RequestId={RequestId}, ResourceId={ResourceId}, StartTime={StartTime}",
            requestId, resourceId, startTime);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentKeycloakId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "You must be logged in to make a booking."
                });
            }

            // Parse the start time
            if (!DateTime.TryParse(startTime, out var parsedStartTime))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Invalid date/time format: '{startTime}'. Please use format: YYYY-MM-DD HH:mm"
                });
            }

            // Memory Guard: Check if resourceId was returned in a previous search
            if (_validResourceIds.Count > 0 && !_validResourceIds.Contains(resourceId))
            {
                _logger.LogWarning("[BookingFunctions.CreateBooking] HALLUCINATION DETECTED - ResourceId {ResourceId} was not in search results. Valid IDs: {ValidIds}",
                    resourceId, string.Join(", ", _validResourceIds.Take(5)));
                
                var validIdsList = string.Join(", ", _validResourceIds.Take(5).Select(id => $"'{id}'"));
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"The resource ID '{resourceId}' was not found in your previous search results. Please use one of these valid IDs: {validIdsList}. Or run SearchBookableResources again to get current available resources.",
                    validResourceIds = _validResourceIds.Take(5).ToList(),
                    hint = "Use the 'id' field from the resources returned by SearchBookableResources"
                });
            }

            // Validate resource exists before attempting booking
            var resource = await _bookingService.GetResourceAsync(resourceId);
            if (resource == null)
            {
                _logger.LogWarning("[BookingFunctions.CreateBooking] Resource {ResourceId} not found in database", resourceId);
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Resource with ID '{resourceId}' was not found. Please search for available resources again using SearchBookableResources and use a valid resource ID from the results."
                });
            }

            // Ensure UTC
            if (parsedStartTime.Kind != DateTimeKind.Utc)
            {
                parsedStartTime = DateTime.SpecifyKind(parsedStartTime, DateTimeKind.Utc);
            }

            var dto = new CreateResourceBookingDto
            {
                ResourceId = resourceId,
                ServiceId = serviceId,
                StartTime = parsedStartTime,
                TimeZone = timeZone,
                CustomerNotes = notes,
                GuestCount = guestCount
            };

            var booking = await _bookingService.CreateBookingAsync(_currentKeycloakId, dto);

            if (booking == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Unable to create booking. The time slot may no longer be available, or the booking window has passed. Please try a different time."
                });
            }

            _logger.LogInformation("[BookingFunctions.CreateBooking] SUCCESS - BookingId={BookingId}, ConfirmationCode={ConfirmationCode}",
                booking.Id, booking.ConfirmationCode);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Booking created successfully!",
                booking = new
                {
                    id = booking.Id,
                    confirmationCode = booking.ConfirmationCode,
                    resourceName = booking.ResourceName,
                    serviceName = booking.ServiceName,
                    businessName = booking.BusinessName,
                    startTime = booking.StartTime.ToString("yyyy-MM-dd HH:mm"),
                    endTime = booking.EndTime.ToString("yyyy-MM-dd HH:mm"),
                    status = booking.Status.ToString(),
                    price = booking.Price,
                    currency = booking.Currency,
                    guestCount = booking.GuestCount
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.CreateBooking] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error creating booking: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get user's upcoming bookings
    /// </summary>
    [Description("Get the user's upcoming reservations and appointments. Use this when the user asks about their bookings, reservations, or appointments.")]
    public async Task<string> GetMyUpcomingBookings()
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.GetMyUpcomingBookings] START - RequestId={RequestId}, ProfileId={ProfileId}",
            requestId, _currentProfileId);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentKeycloakId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "You must be logged in to view your bookings."
                });
            }

            var bookings = await _bookingService.GetMyUpcomingBookingsAsync(_currentKeycloakId);

            if (bookings == null || !bookings.Any())
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "You have no upcoming bookings or reservations.",
                    count = 0,
                    bookings = Array.Empty<object>()
                });
            }

            var upcomingBookings = bookings.Select(b => new
            {
                id = b.Id,
                confirmationCode = b.ConfirmationCode,
                resourceName = b.ResourceName,
                serviceName = b.ServiceName,
                startTime = b.StartTime.ToString("yyyy-MM-dd HH:mm"),
                endTime = b.EndTime.ToString("yyyy-MM-dd HH:mm"),
                status = b.Status.ToString(),
                price = b.Price,
                currency = b.Currency
            }).ToList();

            _logger.LogInformation("[BookingFunctions.GetMyUpcomingBookings] SUCCESS - Found {Count} bookings", upcomingBookings.Count);

            return JsonSerializer.Serialize(new
            {
                success = true,
                count = upcomingBookings.Count,
                bookings = upcomingBookings
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.GetMyUpcomingBookings] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error retrieving bookings: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Look up a booking by confirmation code
    /// </summary>
    [Description("Look up a booking using its confirmation code. Use this when the user wants to check the status of a specific reservation.")]
    public async Task<string> GetBookingByConfirmationCode(
        [Description("The booking confirmation code (e.g., 'ABC-1234')")]
        string confirmationCode)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.GetBookingByConfirmationCode] START - RequestId={RequestId}, Code={Code}",
            requestId, confirmationCode);

        try
        {
            if (string.IsNullOrWhiteSpace(confirmationCode))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Please provide a confirmation code."
                });
            }

            var booking = await _bookingService.GetBookingByConfirmationCodeAsync(confirmationCode.Trim().ToUpperInvariant());

            if (booking == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"No booking found with confirmation code '{confirmationCode}'. Please check the code and try again."
                });
            }

            _logger.LogInformation("[BookingFunctions.GetBookingByConfirmationCode] SUCCESS - BookingId={BookingId}", booking.Id);

            return JsonSerializer.Serialize(new
            {
                success = true,
                booking = new
                {
                    id = booking.Id,
                    confirmationCode = booking.ConfirmationCode,
                    resourceName = booking.ResourceName,
                    serviceName = booking.ServiceName,
                    businessName = booking.BusinessName,
                    startTime = booking.StartTime.ToString("yyyy-MM-dd HH:mm"),
                    endTime = booking.EndTime.ToString("yyyy-MM-dd HH:mm"),
                    status = booking.Status.ToString(),
                    price = booking.Price,
                    currency = booking.Currency,
                    guestCount = booking.GuestCount,
                    customerNotes = booking.CustomerNotes,
                    internalNotes = booking.InternalNotes,
                    canCancel = booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.GetBookingByConfirmationCode] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error looking up booking: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Cancel an existing booking
    /// </summary>
    [Description("Cancel an existing booking or reservation. Use this when the user wants to cancel their appointment. Requires confirmation from the user before calling.")]
    public async Task<string> CancelBooking(
        [Description("The unique ID of the booking to cancel")]
        Guid bookingId,
        [Description("Optional: reason for cancellation")]
        string? reason = null)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[BookingFunctions.CancelBooking] START - RequestId={RequestId}, BookingId={BookingId}",
            requestId, bookingId);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentKeycloakId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "You must be logged in to cancel a booking."
                });
            }

            // Get the booking first to verify ownership and status
            var booking = await _bookingService.GetBookingAsync(bookingId);

            if (booking == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Booking not found."
                });
            }

            // Verify the booking belongs to the current user
            if (booking.CustomerProfileId != _currentProfileId)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "You can only cancel your own bookings."
                });
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "This booking has already been cancelled."
                });
            }

            if (booking.Status == BookingStatus.Completed)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Cannot cancel a completed booking."
                });
            }

            var cancelDto = new CancelBookingDto
            {
                Reason = reason
            };

            var cancelled = await _bookingService.CancelBookingAsync(_currentKeycloakId, bookingId, cancelDto);

            if (cancelled == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Unable to cancel the booking. The cancellation window may have passed."
                });
            }

            _logger.LogInformation("[BookingFunctions.CancelBooking] SUCCESS - BookingId={BookingId}", bookingId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Booking cancelled successfully.",
                booking = new
                {
                    id = cancelled.Id,
                    confirmationCode = cancelled.ConfirmationCode,
                    resourceName = cancelled.ResourceName,
                    status = cancelled.Status.ToString(),
                    cancelledAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingFunctions.CancelBooking] ERROR - RequestId={RequestId}", requestId);
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Error cancelling booking: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get available booking categories
    /// </summary>
    [Description("Get the list of available booking categories. Use this when the user wants to browse different types of services they can book.")]
    public Task<string> GetBookingCategories()
    {
        _logger.LogInformation("[BookingFunctions.GetBookingCategories] Called");

        var categories = Enum.GetValues<ResourceCategory>()
            .Select(c => new
            {
                key = c.ToString(),
                name = GetCategoryDisplayName(c),
                description = GetCategoryDescription(c)
            })
            .ToList();

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            success = true,
            count = categories.Count,
            categories
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    #region Helper Methods

    private static string GetCategoryDisplayName(ResourceCategory category) => category switch
    {
        // Person-based services
        ResourceCategory.Barber => "Barber",
        ResourceCategory.Hairdresser => "Hairdresser",
        ResourceCategory.MassageTherapist => "Massage Therapist",
        ResourceCategory.Doctor => "Doctor",
        ResourceCategory.Dentist => "Dentist",
        ResourceCategory.PersonalTrainer => "Personal Trainer",
        ResourceCategory.Consultant => "Consultant",
        ResourceCategory.Tutor => "Tutor",
        ResourceCategory.Photographer => "Photographer",
        ResourceCategory.Lawyer => "Lawyer",
        
        // Object-based resources
        ResourceCategory.Table => "Table Reservation",
        ResourceCategory.Chair => "Chair",
        ResourceCategory.Booth => "Booth",
        ResourceCategory.Vehicle => "Vehicle",
        ResourceCategory.Bike => "Bike",
        ResourceCategory.Scooter => "Scooter",
        
        // Space-based resources
        ResourceCategory.MeetingRoom => "Meeting Room",
        ResourceCategory.ConferenceRoom => "Conference Room",
        ResourceCategory.Studio => "Studio",
        ResourceCategory.TennisCourt => "Tennis Court",
        ResourceCategory.BasketballCourt => "Basketball Court",
        ResourceCategory.SwimmingLane => "Swimming Lane",
        ResourceCategory.GolfTeeTime => "Golf Tee Time",
        ResourceCategory.ParkingSpot => "Parking Spot",
        ResourceCategory.EventSpace => "Event Space",
        ResourceCategory.PrivateDiningRoom => "Private Dining Room",
        
        // Equipment
        ResourceCategory.Camera => "Camera",
        ResourceCategory.Projector => "Projector",
        ResourceCategory.SoundSystem => "Sound System",
        ResourceCategory.Printer => "Printer",
        ResourceCategory.Computer => "Computer",
        ResourceCategory.GymEquipment => "Gym Equipment",
        
        ResourceCategory.Other => "Other",
        _ => category.ToString()
    };

    private static string GetCategoryDescription(ResourceCategory category) => category switch
    {
        // Person-based services
        ResourceCategory.Barber => "Professional barber services",
        ResourceCategory.Hairdresser => "Hair styling and treatments",
        ResourceCategory.MassageTherapist => "Massage and relaxation therapy",
        ResourceCategory.Doctor => "Medical consultations and appointments",
        ResourceCategory.Dentist => "Dental care and checkups",
        ResourceCategory.PersonalTrainer => "Fitness training sessions",
        ResourceCategory.Consultant => "Professional consulting services",
        ResourceCategory.Tutor => "Educational tutoring sessions",
        ResourceCategory.Photographer => "Photography sessions",
        ResourceCategory.Lawyer => "Legal consultations",
        
        // Object-based resources
        ResourceCategory.Table => "Restaurant table reservations",
        ResourceCategory.Booth => "Private booth reservations",
        ResourceCategory.Vehicle => "Vehicle rentals",
        ResourceCategory.Bike => "Bike rentals",
        ResourceCategory.Scooter => "Scooter rentals",
        
        // Space-based resources
        ResourceCategory.MeetingRoom => "Meeting room reservations",
        ResourceCategory.ConferenceRoom => "Conference room bookings",
        ResourceCategory.Studio => "Studio space rentals",
        ResourceCategory.TennisCourt => "Tennis court reservations",
        ResourceCategory.BasketballCourt => "Basketball court reservations",
        ResourceCategory.SwimmingLane => "Swimming lane reservations",
        ResourceCategory.GolfTeeTime => "Golf tee time bookings",
        ResourceCategory.EventSpace => "Event venue reservations",
        ResourceCategory.PrivateDiningRoom => "Private dining room reservations",
        
        // Equipment
        ResourceCategory.Camera => "Camera equipment rentals",
        ResourceCategory.Projector => "Projector rentals",
        ResourceCategory.SoundSystem => "Sound system rentals",
        ResourceCategory.GymEquipment => "Gym equipment reservations",
        
        ResourceCategory.Other => "Other services and resources",
        _ => ""
    };

    #endregion
}
