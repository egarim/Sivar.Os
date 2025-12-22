using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Sivar.Os.Client.Services;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;
using Xunit;

namespace Sivar.Os.Tests.Services;

/// <summary>
/// Unit tests for ProfileContextService
/// Tests profile context management including:
/// - Device context detection (timezone, device type, language)
/// - Location management (selected vs device location)
/// - Profile switching
/// - Context change notifications
/// </summary>
public class ProfileContextServiceTests
{
    private readonly Mock<ChatLocationService> _chatLocationServiceMock;
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly Mock<IJSObjectReference> _jsModuleMock;
    private readonly Mock<ILogger<ProfileContextService>> _loggerMock;
    private readonly ProfileContextService _service;

    private readonly Guid _testProfileId = Guid.NewGuid();

    public ProfileContextServiceTests()
    {
        // Create mock for BrowserPermissionsService (required by ChatLocationService)
        var browserPermissionsMock = new Mock<BrowserPermissionsService>(
            Mock.Of<IJSRuntime>(),
            Mock.Of<ILogger<BrowserPermissionsService>>());

        // Create mock for ChatLocationService
        _chatLocationServiceMock = new Mock<ChatLocationService>(
            browserPermissionsMock.Object,
            Mock.Of<IJSRuntime>(),
            Mock.Of<ILogger<ChatLocationService>>());

        _jsRuntimeMock = new Mock<IJSRuntime>();
        _jsModuleMock = new Mock<IJSObjectReference>();
        _loggerMock = new Mock<ILogger<ProfileContextService>>();

        // Setup JS module import
        _jsRuntimeMock
            .Setup(js => js.InvokeAsync<IJSObjectReference>(
                "import",
                It.IsAny<object[]>()))
            .ReturnsAsync(_jsModuleMock.Object);

        // Setup default device context from JS
        SetupDefaultDeviceContext();

        _service = new ProfileContextService(
            _chatLocationServiceMock.Object,
            _jsRuntimeMock.Object,
            _loggerMock.Object);
    }

    private void SetupDefaultDeviceContext(
        string timeZone = "America/El_Salvador",
        string deviceType = "desktop",
        string language = "es-SV",
        int offsetMinutes = -360)
    {
        var deviceContext = new DeviceContextJs
        {
            TimeZone = timeZone,
            LocalDateTime = DateTimeOffset.Now.ToString("o"),
            TimeZoneOffsetMinutes = offsetMinutes,
            DeviceType = deviceType,
            Language = language,
            UserAgent = "Mozilla/5.0 (Test)"
        };

        _jsModuleMock
            .Setup(m => m.InvokeAsync<DeviceContextJs>(
                "getDeviceContext",
                It.IsAny<object[]>()))
            .ReturnsAsync(deviceContext);
    }

    #region Initialization Tests

    [Fact]
    public async Task InitializeAsync_SetsProfileId()
    {
        // Act
        await _service.InitializeAsync(_testProfileId);

        // Assert
        _service.CurrentContext.Should().NotBeNull();
        _service.CurrentContext!.ProfileId.Should().Be(_testProfileId);
    }

    [Fact]
    public async Task InitializeAsync_DetectsDeviceContext()
    {
        // Arrange
        SetupDefaultDeviceContext(
            timeZone: "America/New_York",
            deviceType: "mobile",
            language: "en-US",
            offsetMinutes: -300);

        // Act
        await _service.InitializeAsync(_testProfileId);

        // Assert
        _service.CurrentContext.Should().NotBeNull();
        _service.CurrentContext!.Device.TimeZone.Should().Be("America/New_York");
        _service.CurrentContext.Device.DeviceType.Should().Be("mobile");
        _service.CurrentContext.Device.Language.Should().Be("en-US");
        _service.CurrentContext.Device.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_SetsIsInitializedFlag()
    {
        // Act
        await _service.InitializeAsync(_testProfileId);

        // Assert - context should be initialized
        _service.CurrentContext.Should().NotBeNull();
        _service.CurrentContext!.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_SameProfile()
    {
        // Act
        await _service.InitializeAsync(_testProfileId);
        var firstContext = _service.CurrentContext;
        
        await _service.InitializeAsync(_testProfileId);
        var secondContext = _service.CurrentContext;

        // Assert - should be same instance (no re-initialization)
        secondContext.Should().BeSameAs(firstContext);
    }

    [Fact]
    public async Task InitializeAsync_ReInitializes_DifferentProfile()
    {
        // Arrange
        var secondProfileId = Guid.NewGuid();

        // Act
        await _service.InitializeAsync(_testProfileId);
        await _service.OnProfileSwitchedAsync(secondProfileId);

        // Assert
        _service.CurrentContext!.ProfileId.Should().Be(secondProfileId);
    }

    #endregion

    #region Device Context Tests

    [Fact]
    public async Task GetDeviceTimeZone_ReturnsDetectedTimezone()
    {
        // Arrange
        SetupDefaultDeviceContext(timeZone: "Europe/London");
        await _service.InitializeAsync(_testProfileId);

        // Act
        var result = _service.GetDeviceTimeZone();

        // Assert
        result.Should().Be("Europe/London");
    }

    [Fact]
    public async Task GetDeviceType_ReturnsDetectedType()
    {
        // Arrange
        SetupDefaultDeviceContext(deviceType: "tablet");
        await _service.InitializeAsync(_testProfileId);

        // Act
        var result = _service.GetDeviceType();

        // Assert
        result.Should().Be("tablet");
    }

    [Fact]
    public async Task GetLocalDateTime_ReturnsValidDateTime()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);

        // Act
        var result = _service.GetLocalDateTime();

        // Assert
        result.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task RefreshDeviceContextAsync_UpdatesContext()
    {
        // Arrange
        SetupDefaultDeviceContext(timeZone: "America/El_Salvador");
        await _service.InitializeAsync(_testProfileId);
        
        // Change the mock to return different timezone
        SetupDefaultDeviceContext(timeZone: "America/Los_Angeles");

        // Act
        await _service.RefreshDeviceContextAsync();

        // Assert
        _service.CurrentContext!.Device.TimeZone.Should().Be("America/Los_Angeles");
    }

    #endregion

    #region Location Tests

    [Fact]
    public async Task SetSelectedLocationAsync_SetsSelectedLocation()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        var location = new ChatLocationContext
        {
            Latitude = 13.6929,
            Longitude = -89.2182,
            City = "San Salvador",
            DisplayName = "San Salvador, El Salvador"
        };

        // Act
        await _service.SetSelectedLocationAsync(location);

        // Assert
        _service.CurrentContext!.Location.SelectedLocation.Should().NotBeNull();
        _service.CurrentContext.Location.SelectedLocation!.City.Should().Be("San Salvador");
        _service.CurrentContext.Location.HasSelectedLocation.Should().BeTrue();
    }

    [Fact]
    public async Task SetSelectedLocationAsync_SetsSourceToSelected()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        var location = new ChatLocationContext
        {
            Latitude = 13.6929,
            Longitude = -89.2182,
            Source = "gps" // Original source
        };

        // Act
        await _service.SetSelectedLocationAsync(location);

        // Assert
        _service.CurrentContext!.Location.SelectedLocation!.Source.Should().Be("selected");
    }

    [Fact]
    public async Task ClearSelectedLocationAsync_ClearsSelectedLocation()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        var location = new ChatLocationContext { Latitude = 13.6929, Longitude = -89.2182 };
        await _service.SetSelectedLocationAsync(location);

        // Act
        await _service.ClearSelectedLocationAsync();

        // Assert
        _service.CurrentContext!.Location.SelectedLocation.Should().BeNull();
        _service.CurrentContext.Location.HasSelectedLocation.Should().BeFalse();
    }

    [Fact]
    public async Task EffectiveLocation_PrefersSelectedOverDevice()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);

        // Set selected location (Santa Ana)
        var selectedLocation = new ChatLocationContext
        {
            Latitude = 14.0,
            Longitude = -88.0,
            City = "Santa Ana"
        };
        await _service.SetSelectedLocationAsync(selectedLocation);

        // Assert - EffectiveLocation should be SelectedLocation (Santa Ana)
        _service.CurrentContext!.Location.EffectiveLocation!.City.Should().Be("Santa Ana");
        _service.CurrentContext.Location.HasSelectedLocation.Should().BeTrue();
    }

    #endregion

    #region GetChatLocationContext Tests

    [Fact]
    public async Task GetChatLocationContext_ReturnsNull_WhenNoLocation()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);

        // Act
        var result = _service.GetChatLocationContext();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChatLocationContext_PopulatesTimezone()
    {
        // Arrange
        SetupDefaultDeviceContext(timeZone: "America/El_Salvador");
        await _service.InitializeAsync(_testProfileId);
        
        var location = new ChatLocationContext
        {
            Latitude = 13.6929,
            Longitude = -89.2182,
            City = "San Salvador"
        };
        await _service.SetSelectedLocationAsync(location);

        // Act
        var result = _service.GetChatLocationContext();

        // Assert
        result.Should().NotBeNull();
        result!.TimeZone.Should().Be("America/El_Salvador");
        result.UserLocalTime.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetChatLocationContext_PopulatesUserLocalTime()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        var location = new ChatLocationContext { Latitude = 13.6929, Longitude = -89.2182 };
        await _service.SetSelectedLocationAsync(location);

        // Act
        var result = _service.GetChatLocationContext();

        // Assert
        result!.UserLocalTime.Should().NotBeNullOrEmpty();
        // Should be valid ISO 8601 format
        DateTimeOffset.TryParse(result.UserLocalTime, out var parsed).Should().BeTrue();
    }

    #endregion

    #region Profile Switching Tests

    [Fact]
    public async Task OnProfileSwitchedAsync_ChangesProfileId()
    {
        // Arrange
        var secondProfileId = Guid.NewGuid();
        await _service.InitializeAsync(_testProfileId);

        // Act
        await _service.OnProfileSwitchedAsync(secondProfileId);

        // Assert
        _service.CurrentContext!.ProfileId.Should().Be(secondProfileId);
    }

    [Fact]
    public async Task OnProfileSwitchedAsync_ClearsSelectedLocation()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        var location = new ChatLocationContext { Latitude = 13.6929, Longitude = -89.2182 };
        await _service.SetSelectedLocationAsync(location);

        // Act
        await _service.OnProfileSwitchedAsync(Guid.NewGuid());

        // Assert - new profile shouldn't have the old selected location
        // (unless it was persisted for that profile, which requires localStorage mock)
        _service.CurrentContext!.Location.SelectedLocation.Should().BeNull();
    }

    [Fact]
    public async Task OnProfileSwitchedAsync_NoOp_SameProfile()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        var initialContext = _service.CurrentContext;

        // Act
        await _service.OnProfileSwitchedAsync(_testProfileId);

        // Assert - should be same instance (no change)
        _service.CurrentContext.Should().BeSameAs(initialContext);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task OnContextChanged_FiresOnInitialize()
    {
        // Arrange
        ProfileContext? receivedContext = null;
        _service.OnContextChanged += context =>
        {
            receivedContext = context;
            return Task.CompletedTask;
        };

        // Act
        await _service.InitializeAsync(_testProfileId);

        // Assert
        receivedContext.Should().NotBeNull();
        receivedContext!.ProfileId.Should().Be(_testProfileId);
    }

    [Fact]
    public async Task OnContextChanged_FiresOnLocationChange()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        
        var eventCount = 0;
        _service.OnContextChanged += _ =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        // Act
        await _service.SetSelectedLocationAsync(new ChatLocationContext { Latitude = 13.0, Longitude = -89.0 });

        // Assert - fires at least once for location change
        // (may also fire for ChatLocationService sync, so check >= 1)
        eventCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task OnContextChanged_FiresOnRefresh()
    {
        // Arrange
        await _service.InitializeAsync(_testProfileId);
        
        var eventCount = 0;
        _service.OnContextChanged += _ =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        // Act
        await _service.RefreshDeviceContextAsync();

        // Assert
        eventCount.Should().Be(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task InitializeAsync_HandlesJsInteropFailure_Gracefully()
    {
        // Arrange
        _jsModuleMock
            .Setup(m => m.InvokeAsync<DeviceContextJs>(
                "getDeviceContext",
                It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JS Error"));

        // Act
        await _service.InitializeAsync(_testProfileId);

        // Assert - should not throw, should use defaults
        _service.CurrentContext.Should().NotBeNull();
        _service.CurrentContext!.Device.TimeZone.Should().Be("America/El_Salvador");
        _service.CurrentContext.Device.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshDeviceContextAsync_NoOp_WhenNotInitialized()
    {
        // Act - should not throw
        await _service.RefreshDeviceContextAsync();

        // Assert
        _service.CurrentContext.Should().BeNull();
    }

    [Fact]
    public async Task SetSelectedLocationAsync_NoOp_WhenNotInitialized()
    {
        // Act - should not throw
        await _service.SetSelectedLocationAsync(new ChatLocationContext { Latitude = 13.0, Longitude = -89.0 });

        // Assert
        _service.CurrentContext.Should().BeNull();
    }

    #endregion
}
