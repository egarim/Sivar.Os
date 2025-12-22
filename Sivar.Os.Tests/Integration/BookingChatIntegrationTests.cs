using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Services;
using Sivar.Os.Services.AgentFunctions;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Xunit;
using Xunit.Abstractions;

namespace Sivar.Os.Tests.Integration;

/// <summary>
/// Integration tests for Booking-Chat integration.
/// These tests verify that BookingFunctions works correctly with mocked services.
/// 
/// For full AI agent testing, run the application manually and test via chat UI.
/// </summary>
[Collection("BookingChatIntegration")]
public class BookingChatIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IConfiguration _configuration;
    private readonly ServiceProvider _serviceProvider;
    
    // Test data
    private readonly Guid _testProfileId = Guid.NewGuid();
    private readonly string _testKeycloakId = "test-keycloak-booking-integration";

    public BookingChatIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Build configuration from appsettings.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Build service provider with mocked services
        _serviceProvider = BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add memory cache
        services.AddMemoryCache();

        // Add mocked booking services
        AddMockedBookingServices(services);

        // Add BookingFunctions
        services.AddScoped<BookingFunctions>();

        return services.BuildServiceProvider();
    }

    private void AddMockedBookingServices(IServiceCollection services)
    {
        // Mock IResourceBookingService with test data
        var bookingServiceMock = new Mock<IResourceBookingService>();

        // Setup SearchBookableResources to return test data
        bookingServiceMock.Setup(s => s.QueryResourcesAsync(It.IsAny<ResourceQueryDto>()))
            .ReturnsAsync((ResourceQueryDto query) =>
            {
                var searchTerm = query.SearchTerm?.ToLower() ?? "";
                var resources = new List<BookableResourceSummaryDto>();

                if (searchTerm.Contains("barber") || searchTerm.Contains("haircut") || 
                    searchTerm.Contains("corte") || searchTerm.Contains("peluquer"))
                {
                    resources.Add(new BookableResourceSummaryDto
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "Mario's Barber Shop",
                        Description = "Best haircuts in San Salvador",
                        ProfileName = "Mario's",
                        Category = ResourceCategory.Barber,
                        ResourceType = ResourceType.Person,
                        DefaultPrice = 10.00m,
                        Currency = "USD",
                        SlotDurationMinutes = 30,
                        IsActive = true,
                        AverageRating = 4.8,
                        ReviewCount = 125
                    });
                }

                if (searchTerm.Contains("restaurant") || searchTerm.Contains("table") ||
                    searchTerm.Contains("reserv") || searchTerm.Contains("cena") || searchTerm.Contains("pizza"))
                {
                    resources.Add(new BookableResourceSummaryDto
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "Restaurante El Buen Gusto",
                        Description = "Fine dining in the heart of San Salvador",
                        ProfileName = "El Buen Gusto",
                        Category = ResourceCategory.Table,
                        ResourceType = ResourceType.Object,
                        DefaultPrice = 0m,
                        Currency = "USD",
                        SlotDurationMinutes = 120,
                        IsActive = true,
                        AverageRating = 4.5,
                        ReviewCount = 89
                    });
                }

                return new ResourceListResponseDto
                {
                    Resources = resources,
                    TotalCount = resources.Count,
                    Page = 1,
                    PageSize = 10
                };
            });

        // Setup GetAvailableSlots
        bookingServiceMock.Setup(s => s.GetAvailableSlotsAsync(It.IsAny<GetAvailableSlotsDto>()))
            .ReturnsAsync((GetAvailableSlotsDto query) =>
            {
                var slots = new Dictionary<DateOnly, List<AvailableTimeSlotDto>>();
                var date = query.Date;
                
                slots[date] = new List<AvailableTimeSlotDto>
                {
                    new AvailableTimeSlotDto
                    {
                        StartTime = date.ToDateTime(new TimeOnly(9, 0)),
                        EndTime = date.ToDateTime(new TimeOnly(9, 30)),
                        DurationMinutes = 30,
                        Price = 10.00m,
                        Currency = "USD",
                        AvailableCapacity = 1
                    },
                    new AvailableTimeSlotDto
                    {
                        StartTime = date.ToDateTime(new TimeOnly(10, 0)),
                        EndTime = date.ToDateTime(new TimeOnly(10, 30)),
                        DurationMinutes = 30,
                        Price = 10.00m,
                        Currency = "USD",
                        AvailableCapacity = 1
                    },
                    new AvailableTimeSlotDto
                    {
                        StartTime = date.ToDateTime(new TimeOnly(14, 0)),
                        EndTime = date.ToDateTime(new TimeOnly(14, 30)),
                        DurationMinutes = 30,
                        Price = 10.00m,
                        Currency = "USD",
                        AvailableCapacity = 1
                    }
                };

                return new AvailableSlotsResponseDto
                {
                    ResourceId = query.ResourceId,
                    ResourceName = "Test Resource",
                    SlotsByDate = slots
                };
            });

        // Setup GetMyUpcomingBookings
        bookingServiceMock.Setup(s => s.GetMyUpcomingBookingsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ResourceBookingSummaryDto>
            {
                new ResourceBookingSummaryDto
                {
                    Id = Guid.NewGuid(),
                    ResourceName = "Mario's Barber Shop",
                    ServiceName = "Haircut",
                    ConfirmationCode = "TEST-1234",
                    StartTime = DateTime.UtcNow.AddDays(2).Date.AddHours(10),
                    EndTime = DateTime.UtcNow.AddDays(2).Date.AddHours(10).AddMinutes(30),
                    Status = BookingStatus.Confirmed,
                    Price = 10.00m,
                    Currency = "USD"
                }
            });

        services.AddSingleton(bookingServiceMock.Object);

        // Mock IResourceBookingRepository
        var bookingRepoMock = new Mock<IResourceBookingRepository>();
        services.AddSingleton(bookingRepoMock.Object);

        // Mock IProfileService
        var profileServiceMock = new Mock<IProfileService>();
        services.AddSingleton(profileServiceMock.Object);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    #region BookingFunctions Integration Tests

    [Fact]
    public async Task SearchBookableResources_WithBarberQuery_ReturnsBarberShop()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);

        // Act
        var result = await bookingFunctions.SearchBookableResources("barber");

        // Assert
        _output.WriteLine($"SearchBookableResources result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("Mario"); // Avoid apostrophe encoding issues
        result.Should().Contain("Barber Shop");
        result.Should().Contain("\"count\": 1");
    }

    [Fact]
    public async Task SearchBookableResources_WithRestaurantQuery_ReturnsRestaurant()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);

        // Act
        var result = await bookingFunctions.SearchBookableResources("restaurant");

        // Assert
        _output.WriteLine($"SearchBookableResources result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("El Buen Gusto");
        result.Should().Contain("\"count\": 1");
    }

    [Fact]
    public async Task SearchBookableResources_WithSpanishQuery_ReturnsResults()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);

        // Act - Using Spanish term for haircut
        var result = await bookingFunctions.SearchBookableResources("corte de pelo");

        // Assert
        _output.WriteLine($"SearchBookableResources result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("Mario");
        result.Should().Contain("Barber Shop");
    }

    [Fact]
    public async Task GetMyUpcomingBookings_ReturnsUserBookings()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);

        // Act
        var result = await bookingFunctions.GetMyUpcomingBookings();

        // Assert
        _output.WriteLine($"GetMyUpcomingBookings result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("TEST-1234");
        result.Should().Contain("Mario");
        result.Should().Contain("Haircut");
    }

    [Fact]
    public async Task GetAvailableSlots_ReturnsTimeSlots()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);
        
        var resourceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        // Act
        var result = await bookingFunctions.GetAvailableSlots(resourceId, date.ToString("yyyy-MM-dd"));

        // Assert
        _output.WriteLine($"GetAvailableSlots result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("slots");
        result.Should().Contain("9:00");  // First slot
        result.Should().Contain("10:00"); // Second slot
        result.Should().Contain("14:00"); // Third slot
    }

    [Fact]
    public async Task GetBookingCategories_ReturnsAllCategories()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();

        // Act
        var result = await bookingFunctions.GetBookingCategories();

        // Assert
        _output.WriteLine($"GetBookingCategories result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("Barber");
        result.Should().Contain("Doctor");
        result.Should().Contain("Table");
        result.Should().Contain("MeetingRoom");
    }

    [Fact]
    public async Task SearchBookableResources_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);

        // Act - Search with category filter
        var result = await bookingFunctions.SearchBookableResources("pizza", "Table");

        // Assert
        _output.WriteLine($"SearchBookableResources with category result: {result}");
        
        result.Should().Contain("success");
        result.Should().Contain("Restaurante El Buen Gusto");
    }

    [Fact]
    public async Task GetMyUpcomingBookings_WithNoUser_ReturnsError()
    {
        // Arrange
        var bookingFunctions = _serviceProvider.GetRequiredService<BookingFunctions>();
        // Intentionally NOT setting user context

        // Act
        var result = await bookingFunctions.GetMyUpcomingBookings();

        // Assert
        _output.WriteLine($"GetMyUpcomingBookings without user result: {result}");
        
        result.Should().Contain("error");
        result.Should().Contain("logged in"); // Fixed: actual message says "logged in" not "log in"
    }

    #endregion
}
