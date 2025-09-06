using FlightBooking.Application.Search.Events;
using FlightBooking.Application.Search.Services;
using FlightBooking.Domain.Search;
using FlightBooking.Infrastructure.Data;
using FlightBooking.Infrastructure.Search.EventHandlers;
using FlightBooking.Infrastructure.Search.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace FlightBooking.UnitTests.Search;

public class FlightSearchCacheTests
{
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly Mock<ILogger<FlightSearchCacheService>> _mockSearchCacheLogger;
    private readonly RedisCacheService _cacheService;
    private readonly FlightSearchCacheService _searchCacheService;

    public FlightSearchCacheTests()
    {
        _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();
        _mockSearchCacheLogger = new Mock<ILogger<FlightSearchCacheService>>();

        _mockConnectionMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _cacheService = new RedisCacheService(_mockConnectionMultiplexer.Object, _mockLogger.Object);
        _searchCacheService = new FlightSearchCacheService(_cacheService, _mockSearchCacheLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnCachedValue()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        var serializedValue = JsonSerializer.Serialize(testObject);
        var key = "test:key";

        _mockDatabase.Setup(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(serializedValue));

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        Assert.NotNull(result);
        _mockDatabase.Verify(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = "nonexistent:key";

        _mockDatabase.Setup(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        Assert.Null(result);
        _mockDatabase.Verify(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValueWithExpiry()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        var key = "test:key";
        var expiry = TimeSpan.FromMinutes(5);

        _mockDatabase.Setup(x => x.StringSetAsync($"flightbooking:{key}", It.IsAny<RedisValue>(), expiry, false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetAsync(key, testObject, expiry);

        // Assert
        _mockDatabase.Verify(x => x.StringSetAsync($"flightbooking:{key}", It.IsAny<RedisValue>(), expiry, false, When.Always, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var cachedObject = new { Name = "Cached", Value = 456 };
        var serializedValue = JsonSerializer.Serialize(cachedObject);
        var key = "test:key";
        var factoryCalled = false;

        _mockDatabase.Setup(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(serializedValue));

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(new { Name = "Factory", Value = 789 });
        });

        // Assert
        Assert.NotNull(result);
        Assert.False(factoryCalled); // Factory should not be called on cache hit
        _mockDatabase.Verify(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()), Times.Once);
        _mockDatabase.Verify(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_ShouldCallFactoryAndCacheResult()
    {
        // Arrange
        var factoryObject = new { Name = "Factory", Value = 789 };
        var key = "test:key";
        var factoryCalled = false;

        _mockDatabase.Setup(x => x.StringGetAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _mockDatabase.Setup(x => x.StringSetAsync($"flightbooking:{key}", It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(factoryObject);
        });

        // Assert
        Assert.NotNull(result);
        Assert.True(factoryCalled); // Factory should be called on cache miss
        _mockDatabase.Verify(x => x.StringGetAsync($"flightbooking:{key}", CommandFlags.None), Times.Once);
        _mockDatabase.Verify(x => x.StringSetAsync($"flightbooking:{key}", It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        // Arrange
        var key = "test:key";

        _mockDatabase.Setup(x => x.KeyDeleteAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDatabase.Verify(x => x.KeyDeleteAsync($"flightbooking:{key}", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetWithTagsAsync_ShouldStoreValueAndAssociateTags()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        var key = "test:key";
        var tags = new[] { "tag1", "tag2" };
        var expiry = TimeSpan.FromMinutes(5);

        _mockDatabase.Setup(x => x.StringSetAsync($"flightbooking:{key}", It.IsAny<RedisValue>(), expiry, false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), CommandFlags.None))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetWithTagsAsync(key, testObject, expiry, tags);

        // Assert
        _mockDatabase.Verify(x => x.StringSetAsync($"flightbooking:{key}", It.IsAny<RedisValue>(), expiry, false, When.Always, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(x => x.SetAddAsync($"flightbooking:tags:tag1", $"flightbooking:{key}", CommandFlags.None), Times.Once);
        _mockDatabase.Verify(x => x.SetAddAsync($"flightbooking:tags:tag2", $"flightbooking:{key}", CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task InvalidateByTagsAsync_ShouldRemoveTaggedKeys()
    {
        // Arrange
        var tags = new[] { "tag1", "tag2" };
        var taggedKeys = new RedisValue[] { "flightbooking:key1", "flightbooking:key2" };

        _mockDatabase.Setup(x => x.SetMembersAsync($"flightbooking:tags:tag1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(taggedKeys);

        _mockDatabase.Setup(x => x.SetMembersAsync($"flightbooking:tags:tag2", It.IsAny<CommandFlags>()))
            .ReturnsAsync(taggedKeys);

        _mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);

        // Act
        await _cacheService.InvalidateByTagsAsync(tags);

        // Assert
        _mockDatabase.Verify(x => x.SetMembersAsync($"flightbooking:tags:tag1", It.IsAny<CommandFlags>()), Times.Once);
        _mockDatabase.Verify(x => x.SetMembersAsync($"flightbooking:tags:tag2", It.IsAny<CommandFlags>()), Times.Once);
        _mockDatabase.Verify(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetSearchResultsAsync_WhenCacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var cacheKey = "flight_search:JFK-LAX:2025-09-02:2";
        var cachedResult = new CachedSearchResult
        {
            Data = JsonSerializer.Serialize(new FlightSearchResult()),
            ETag = "test-etag",
            CachedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            Tags = new List<string> { "route:JFK-LAX", "date:2025-09-02" }
        };

        // Use the same JSON options as the cache service
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var serializedCachedResult = JsonSerializer.Serialize(cachedResult, jsonOptions);


        _mockDatabase.Setup(x => x.StringGetAsync($"flightbooking:{cacheKey}", CommandFlags.None))
            .ReturnsAsync(new RedisValue(serializedCachedResult));

        // Act
        var result = await _searchCacheService.GetSearchResultsAsync(cacheKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-etag", result.ETag);
        _mockDatabase.Verify(x => x.StringGetAsync($"flightbooking:{cacheKey}", CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task GetSearchResultsAsync_WhenCacheMiss_ShouldReturnNull()
    {
        // Arrange
        var cacheKey = "flight_search:JFK-LAX:2025-09-02:2";

        _mockDatabase.Setup(x => x.StringGetAsync($"flightbooking:{cacheKey}", CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _searchCacheService.GetSearchResultsAsync(cacheKey);

        // Assert
        Assert.Null(result);
        _mockDatabase.Verify(x => x.StringGetAsync($"flightbooking:{cacheKey}", CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task SetSearchResultsAsync_ShouldCacheWithTags()
    {
        // Arrange
        var cacheKey = "flight_search:JFK-LAX:2025-09-02:2";
        var searchResult = new CachedSearchResult
        {
            Data = JsonSerializer.Serialize(new FlightSearchResult()),
            ETag = "test-etag",
            Tags = new List<string> { "route:JFK-LAX", "date:2025-09-02", "flight:123" }
        };

        _mockDatabase.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), CommandFlags.None))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        await _searchCacheService.SetSearchResultsAsync(cacheKey, searchResult);

        // Assert
        _mockDatabase.Verify(x => x.StringSetAsync($"flightbooking:{cacheKey}", It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(x => x.SetAddAsync($"flightbooking:tags:route:JFK-LAX", It.IsAny<RedisValue>(), CommandFlags.None), Times.Once);
        _mockDatabase.Verify(x => x.SetAddAsync($"flightbooking:tags:date:2025-09-02", It.IsAny<RedisValue>(), CommandFlags.None), Times.Once);
        _mockDatabase.Verify(x => x.SetAddAsync($"flightbooking:tags:flight:123", It.IsAny<RedisValue>(), CommandFlags.None), Times.Once);
    }

    [Fact]
    public void FlightSearchCriteria_GetCacheKey_ShouldGenerateConsistentKey()
    {
        // Arrange
        var criteria = new FlightSearchCriteria
        {
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            DepartureDate = new DateTime(2025, 9, 2),
            PassengerCount = 2,
            FareClasses = new List<string> { "Economy", "Business" },
            SortBy = "price",
            SortDirection = "asc",
            Page = 1,
            PageSize = 20
        };

        // Act
        var cacheKey1 = criteria.GetCacheKey();
        var cacheKey2 = criteria.GetCacheKey();

        // Assert
        Assert.Equal(cacheKey1, cacheKey2);
        Assert.Contains("flight_search", cacheKey1);
        Assert.Contains("JFK-LAX", cacheKey1);
        Assert.Contains("2025-09-02", cacheKey1);
        Assert.Contains("2", cacheKey1); // passenger count
    }

    [Fact]
    public void FlightSearchCriteria_GetCacheKey_DifferentCriteria_ShouldGenerateDifferentKeys()
    {
        // Arrange
        var criteria1 = new FlightSearchCriteria
        {
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            DepartureDate = new DateTime(2025, 9, 2),
            PassengerCount = 2
        };

        var criteria2 = new FlightSearchCriteria
        {
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            DepartureDate = new DateTime(2025, 9, 2),
            PassengerCount = 3 // Different passenger count
        };

        // Act
        var cacheKey1 = criteria1.GetCacheKey();
        var cacheKey2 = criteria2.GetCacheKey();

        // Assert
        Assert.NotEqual(cacheKey1, cacheKey2);
    }
}

public class SearchCacheInvalidationTests
{
    private readonly Mock<IFlightSearchCacheService> _mockCacheService;
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<FlightUpdatedEventHandler>> _mockLogger;

    public SearchCacheInvalidationTests()
    {
        _mockCacheService = new Mock<IFlightSearchCacheService>();

        // Create in-memory database options for the mock context
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockContext = new Mock<ApplicationDbContext>(options);
        _mockLogger = new Mock<ILogger<FlightUpdatedEventHandler>>();
    }

    [Fact]
    public async Task FlightUpdatedEventHandler_ShouldInvalidateFlightAndRouteCache()
    {
        // Arrange
        var handler = new FlightUpdatedEventHandler(_mockCacheService.Object, _mockContext.Object, _mockLogger.Object);
        var flightUpdatedEvent = new FlightUpdatedEvent
        {
            FlightId = Guid.NewGuid(),
            FlightNumber = "AA123",
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            DepartureDate = new DateTime(2025, 9, 2),
            AffectsAvailability = true,
            AffectsPricing = true,
            UpdatedFields = new List<string> { "DepartureTime", "Price" }
        };

        _mockCacheService.Setup(x => x.InvalidateFlightAsync(flightUpdatedEvent.FlightId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCacheService.Setup(x => x.InvalidateRouteAsync(
            flightUpdatedEvent.DepartureAirport,
            flightUpdatedEvent.ArrivalAirport,
            flightUpdatedEvent.DepartureDate,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(flightUpdatedEvent, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.InvalidateFlightAsync(flightUpdatedEvent.FlightId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.InvalidateRouteAsync(
            flightUpdatedEvent.DepartureAirport,
            flightUpdatedEvent.ArrivalAirport,
            flightUpdatedEvent.DepartureDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingConfirmedEventHandler_ShouldInvalidateRelevantCache()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<BookingConfirmedEventHandler>>();
        var handler = new BookingConfirmedEventHandler(_mockCacheService.Object, mockLogger.Object);
        var bookingConfirmedEvent = new BookingConfirmedEvent
        {
            BookingId = Guid.NewGuid(),
            FlightId = Guid.NewGuid(),
            DepartureAirport = "JFK",
            ArrivalAirport = "LAX",
            DepartureDate = new DateTime(2025, 9, 2),
            FareClassIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            PassengerCount = 2
        };

        _mockCacheService.Setup(x => x.InvalidateFlightAsync(bookingConfirmedEvent.FlightId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCacheService.Setup(x => x.InvalidateRouteAsync(
            bookingConfirmedEvent.DepartureAirport,
            bookingConfirmedEvent.ArrivalAirport,
            bookingConfirmedEvent.DepartureDate,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCacheService.Setup(x => x.InvalidateFareClassAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(bookingConfirmedEvent, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.InvalidateFlightAsync(bookingConfirmedEvent.FlightId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.InvalidateRouteAsync(
            bookingConfirmedEvent.DepartureAirport,
            bookingConfirmedEvent.ArrivalAirport,
            bookingConfirmedEvent.DepartureDate,
            It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.InvalidateFareClassAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BulkPriceUpdateEventHandler_LargeUpdate_ShouldInvalidateAllCache()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<BulkPriceUpdateEventHandler>>();
        var handler = new BulkPriceUpdateEventHandler(_mockCacheService.Object, mockLogger.Object);
        var bulkUpdateEvent = new BulkPriceUpdateEvent
        {
            FlightIds = Enumerable.Range(1, 100).Select(_ => Guid.NewGuid()).ToList(), // Large number of flights
            FareClassIds = Enumerable.Range(1, 200).Select(_ => Guid.NewGuid()).ToList(),
            UpdateType = "seasonal",
            AverageChangePercentage = 15.5m
        };

        _mockCacheService.Setup(x => x.InvalidateAllSearchCacheAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(bulkUpdateEvent, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.InvalidateAllSearchCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.InvalidateFlightAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheService.Verify(x => x.InvalidateFareClassAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkPriceUpdateEventHandler_SmallUpdate_ShouldInvalidateSpecificItems()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<BulkPriceUpdateEventHandler>>();
        var handler = new BulkPriceUpdateEventHandler(_mockCacheService.Object, mockLogger.Object);
        var bulkUpdateEvent = new BulkPriceUpdateEvent
        {
            FlightIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }, // Small number of flights
            FareClassIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            UpdateType = "promotional",
            AverageChangePercentage = -10.0m
        };

        _mockCacheService.Setup(x => x.InvalidateFlightAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCacheService.Setup(x => x.InvalidateFareClassAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(bulkUpdateEvent, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.InvalidateFlightAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockCacheService.Verify(x => x.InvalidateFareClassAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockCacheService.Verify(x => x.InvalidateAllSearchCacheAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
