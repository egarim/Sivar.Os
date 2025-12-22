using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Services.AgentFunctions;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Xunit;

namespace Sivar.Os.Tests.Services;

/// <summary>
/// Unit tests for BookingFunctions AI-callable methods
/// </summary>
public class BookingFunctionsTests
{
    private readonly Mock<IResourceBookingService> _bookingServiceMock;
    private readonly Mock<IResourceBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<ILogger<BookingFunctions>> _loggerMock;
    private readonly BookingFunctions _bookingFunctions;

    private readonly Guid _testProfileId = Guid.NewGuid();
    private readonly string _testKeycloakId = "test-keycloak-123";

    public BookingFunctionsTests()
    {
        _bookingServiceMock = new Mock<IResourceBookingService>();
        _bookingRepositoryMock = new Mock<IResourceBookingRepository>();
        _profileServiceMock = new Mock<IProfileService>();
        _loggerMock = new Mock<ILogger<BookingFunctions>>();

        _bookingFunctions = new BookingFunctions(
            _bookingServiceMock.Object,
            _bookingRepositoryMock.Object,
            _profileServiceMock.Object,
            _loggerMock.Object);

        // Set user context
        _bookingFunctions.SetCurrentUser(_testProfileId, _testKeycloakId);
    }

    #region SearchBookableResources Tests

    [Fact]
    public async Task SearchBookableResources_WithResults_ReturnsSuccessWithResources()
    {
        // Arrange
        var resources = new List<BookableResourceSummaryDto>
        {
            new BookableResourceSummaryDto
            {
                Id = Guid.NewGuid(),
                Name = "Mario's Barber Shop",
                Description = "Best haircuts in town",
                ProfileName = "Mario's",
                Category = ResourceCategory.Barber,
                ResourceType = ResourceType.Person,
                DefaultPrice = 15.00m,
                Currency = "USD",
                SlotDurationMinutes = 30,
                IsActive = true
            }
        };

        _bookingServiceMock.Setup(s => s.QueryResourcesAsync(It.IsAny<ResourceQueryDto>()))
            .ReturnsAsync(new ResourceListResponseDto
            {
                Resources = resources,
                TotalCount = 1,
                Page = 1,
                PageSize = 5
            });

        // Act
        var result = await _bookingFunctions.SearchBookableResources("barber");

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("Mario");
        result.Should().Contain("Barber Shop");
        result.Should().Contain("\"count\": 1");
    }

    [Fact]
    public async Task SearchBookableResources_NoResults_ReturnsEmptyMessage()
    {
        // Arrange
        _bookingServiceMock.Setup(s => s.QueryResourcesAsync(It.IsAny<ResourceQueryDto>()))
            .ReturnsAsync(new ResourceListResponseDto
            {
                Resources = new List<BookableResourceSummaryDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 5
            });

        // Act
        var result = await _bookingFunctions.SearchBookableResources("nonexistent");

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("No bookable resources found");
        result.Should().Contain("\"count\": 0");
    }

    [Fact]
    public async Task SearchBookableResources_WithCategory_PassesCategoryToQuery()
    {
        // Arrange
        ResourceQueryDto? capturedQuery = null;
        _bookingServiceMock.Setup(s => s.QueryResourcesAsync(It.IsAny<ResourceQueryDto>()))
            .Callback<ResourceQueryDto>(q => capturedQuery = q)
            .ReturnsAsync(new ResourceListResponseDto
            {
                Resources = new List<BookableResourceSummaryDto>(),
                TotalCount = 0
            });

        // Act
        await _bookingFunctions.SearchBookableResources("haircut", category: "Barber");

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.Category.Should().Be(ResourceCategory.Barber);
    }

    #endregion

    #region GetResourceDetails Tests

    [Fact]
    public async Task GetResourceDetails_ExistingResource_ReturnsFullDetails()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var resource = new BookableResourceDto
        {
            Id = resourceId,
            Name = "Downtown Salon",
            Description = "Premium hair styling",
            ProfileName = "Beauty Co",
            Category = ResourceCategory.Hairdresser,
            ResourceType = ResourceType.Person,
            DefaultPrice = 50.00m,
            Currency = "USD",
            SlotDurationMinutes = 60,
            MinAdvanceBookingHours = 2,
            MaxAdvanceBookingDays = 30,
            Services = new List<ResourceServiceDto>
            {
                new ResourceServiceDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Haircut",
                    DurationMinutes = 30,
                    Price = 25.00m,
                    IsActive = true
                },
                new ResourceServiceDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Color",
                    DurationMinutes = 90,
                    Price = 75.00m,
                    IsActive = true
                }
            },
            Availability = new List<ResourceAvailabilityDto>
            {
                new ResourceAvailabilityDto
                {
                    DayOfWeek = System.DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    IsAvailable = true
                }
            }
        };

        _bookingServiceMock.Setup(s => s.GetResourceAsync(resourceId))
            .ReturnsAsync(resource);

        // Act
        var result = await _bookingFunctions.GetResourceDetails(resourceId);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("Downtown Salon");
        result.Should().Contain("Haircut");
        result.Should().Contain("Color");
        result.Should().Contain("\"totalServices\": 2");
    }

    [Fact]
    public async Task GetResourceDetails_NonExistentResource_ReturnsError()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        _bookingServiceMock.Setup(s => s.GetResourceAsync(resourceId))
            .ReturnsAsync((BookableResourceDto?)null);

        // Act
        var result = await _bookingFunctions.GetResourceDetails(resourceId);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("false");
        result.Should().Contain("Resource not found");
    }

    #endregion

    #region GetAvailableSlots Tests

    [Fact]
    public async Task GetAvailableSlots_WithAvailability_ReturnsSlotsGroupedByDate()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        
        var response = new AvailableSlotsResponseDto
        {
            ResourceId = resourceId,
            ResourceName = "Test Salon",
            SlotsByDate = new Dictionary<DateOnly, List<AvailableTimeSlotDto>>
            {
                [tomorrow] = new List<AvailableTimeSlotDto>
                {
                    new AvailableTimeSlotDto
                    {
                        StartTime = tomorrow.ToDateTime(new TimeOnly(9, 0)),
                        EndTime = tomorrow.ToDateTime(new TimeOnly(9, 30)),
                        DurationMinutes = 30,
                        Price = 25.00m,
                        Currency = "USD",
                        AvailableCapacity = 1
                    },
                    new AvailableTimeSlotDto
                    {
                        StartTime = tomorrow.ToDateTime(new TimeOnly(10, 0)),
                        EndTime = tomorrow.ToDateTime(new TimeOnly(10, 30)),
                        DurationMinutes = 30,
                        Price = 25.00m,
                        Currency = "USD",
                        AvailableCapacity = 1
                    }
                }
            }
        };

        _bookingServiceMock.Setup(s => s.GetAvailableSlotsAsync(It.IsAny<GetAvailableSlotsDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _bookingFunctions.GetAvailableSlots(resourceId);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("Test Salon");
        result.Should().Contain("\"totalSlots\": 2");
        result.Should().Contain("09:00");
        result.Should().Contain("10:00");
    }

    [Fact]
    public async Task GetAvailableSlots_NoAvailability_ReturnsEmptyMessage()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        
        _bookingServiceMock.Setup(s => s.GetAvailableSlotsAsync(It.IsAny<GetAvailableSlotsDto>()))
            .ReturnsAsync(new AvailableSlotsResponseDto
            {
                ResourceId = resourceId,
                ResourceName = "Test Salon",
                SlotsByDate = new Dictionary<DateOnly, List<AvailableTimeSlotDto>>()
            });

        // Act
        var result = await _bookingFunctions.GetAvailableSlots(resourceId);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("No available slots found");
    }

    #endregion

    #region CreateBooking Tests

    [Fact]
    public async Task CreateBooking_Success_ReturnsConfirmationCode()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1).Date.AddHours(14); // Tomorrow at 2pm

        var createdBooking = new ResourceBookingDto
        {
            Id = bookingId,
            ResourceId = resourceId,
            ResourceName = "Mario's Barber",
            ConfirmationCode = "ABC-1234",
            StartTime = startTime,
            EndTime = startTime.AddMinutes(30),
            Status = BookingStatus.Confirmed,
            Price = 15.00m,
            Currency = "USD",
            GuestCount = 1,
            BusinessName = "Mario's",
            BusinessProfileId = Guid.NewGuid()
        };

        _bookingServiceMock.Setup(s => s.CreateBookingAsync(_testKeycloakId, It.IsAny<CreateResourceBookingDto>()))
            .ReturnsAsync(createdBooking);

        // Act
        var result = await _bookingFunctions.CreateBooking(
            resourceId, 
            startTime.ToString("yyyy-MM-dd HH:mm"));

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("ABC-1234");
        result.Should().Contain("Mario");
        result.Should().Contain("Booking created successfully");
    }

    [Fact]
    public async Task CreateBooking_NotLoggedIn_ReturnsError()
    {
        // Arrange - Create new instance without user context
        var bookingFunctions = new BookingFunctions(
            _bookingServiceMock.Object,
            _bookingRepositoryMock.Object,
            _profileServiceMock.Object,
            _loggerMock.Object);

        // Act
        var result = await bookingFunctions.CreateBooking(
            Guid.NewGuid(), 
            DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm"));

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("false");
        result.Should().Contain("must be logged in");
    }

    [Fact]
    public async Task CreateBooking_InvalidTime_ReturnsError()
    {
        // Act
        var result = await _bookingFunctions.CreateBooking(
            Guid.NewGuid(), 
            "not-a-valid-date");

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("false");
        result.Should().Contain("Invalid date/time format");
    }

    #endregion

    #region GetMyUpcomingBookings Tests

    [Fact]
    public async Task GetMyUpcomingBookings_WithBookings_ReturnsList()
    {
        // Arrange
        var bookings = new List<ResourceBookingSummaryDto>
        {
            new ResourceBookingSummaryDto
            {
                Id = Guid.NewGuid(),
                ResourceName = "Salon A",
                ServiceName = "Haircut",
                ConfirmationCode = "XYZ-5678",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
                Status = BookingStatus.Confirmed,
                Price = 25.00m,
                Currency = "USD"
            },
            new ResourceBookingSummaryDto
            {
                Id = Guid.NewGuid(),
                ResourceName = "Restaurant B",
                ConfirmationCode = "ABC-9999",
                StartTime = DateTime.UtcNow.AddDays(3),
                EndTime = DateTime.UtcNow.AddDays(3).AddHours(2),
                Status = BookingStatus.Confirmed,
                Price = 0m,
                Currency = "USD"
            }
        };

        _bookingServiceMock.Setup(s => s.GetMyUpcomingBookingsAsync(_testKeycloakId))
            .ReturnsAsync(bookings);

        // Act
        var result = await _bookingFunctions.GetMyUpcomingBookings();

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("\"count\": 2");
        result.Should().Contain("Salon A");
        result.Should().Contain("Restaurant B");
        result.Should().Contain("XYZ-5678");
    }

    [Fact]
    public async Task GetMyUpcomingBookings_NoBookings_ReturnsEmptyMessage()
    {
        // Arrange
        _bookingServiceMock.Setup(s => s.GetMyUpcomingBookingsAsync(_testKeycloakId))
            .ReturnsAsync(new List<ResourceBookingSummaryDto>());

        // Act
        var result = await _bookingFunctions.GetMyUpcomingBookings();

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("no upcoming bookings");
        result.Should().Contain("\"count\":0"); // JSON without spaces
    }

    #endregion

    #region CancelBooking Tests

    [Fact]
    public async Task CancelBooking_Success_ReturnsCancelledStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new ResourceBookingDto
        {
            Id = bookingId,
            CustomerProfileId = _testProfileId, // Same as test user
            ResourceName = "Test Resource",
            ConfirmationCode = "CAN-1111",
            Status = BookingStatus.Confirmed
        };

        var cancelledBooking = new ResourceBookingDto
        {
            Id = bookingId,
            ResourceName = "Test Resource",
            ConfirmationCode = "CAN-1111",
            Status = BookingStatus.Cancelled
        };

        _bookingServiceMock.Setup(s => s.GetBookingAsync(bookingId))
            .ReturnsAsync(booking);
        _bookingServiceMock.Setup(s => s.CancelBookingAsync(_testKeycloakId, bookingId, It.IsAny<CancelBookingDto>()))
            .ReturnsAsync(cancelledBooking);

        // Act
        var result = await _bookingFunctions.CancelBooking(bookingId, "Changed my mind");

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("cancelled successfully");
        result.Should().Contain("CAN-1111");
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_ReturnsError()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new ResourceBookingDto
        {
            Id = bookingId,
            CustomerProfileId = _testProfileId,
            Status = BookingStatus.Cancelled
        };

        _bookingServiceMock.Setup(s => s.GetBookingAsync(bookingId))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingFunctions.CancelBooking(bookingId);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("false");
        result.Should().Contain("already been cancelled");
    }

    [Fact]
    public async Task CancelBooking_NotOwner_ReturnsError()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new ResourceBookingDto
        {
            Id = bookingId,
            CustomerProfileId = Guid.NewGuid(), // Different from test user
            Status = BookingStatus.Confirmed
        };

        _bookingServiceMock.Setup(s => s.GetBookingAsync(bookingId))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingFunctions.CancelBooking(bookingId);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("false");
        result.Should().Contain("only cancel your own bookings");
    }

    #endregion

    #region GetBookingCategories Tests

    [Fact]
    public async Task GetBookingCategories_ReturnsAllCategories()
    {
        // Act
        var result = await _bookingFunctions.GetBookingCategories();

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("true");
        result.Should().Contain("Barber");
        result.Should().Contain("Doctor");
        result.Should().Contain("Table");
        result.Should().Contain("MeetingRoom");
    }

    #endregion
}
