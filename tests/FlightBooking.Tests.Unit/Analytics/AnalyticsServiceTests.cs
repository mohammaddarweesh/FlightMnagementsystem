using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Domain.Analytics;
using FlightBooking.Infrastructure.Analytics.Services;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlightBooking.Tests.Unit.Analytics;

public class AnalyticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAnalyticsCacheService> _mockCacheService;
    private readonly Mock<ILogger<AnalyticsService>> _mockLogger;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockCacheService = new Mock<IAnalyticsCacheService>();
        _mockLogger = new Mock<ILogger<AnalyticsService>>();
        
        _analyticsService = new AnalyticsService(_context, _mockCacheService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetRevenueAnalyticsAsync_WithValidFilter_ReturnsAnalytics()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today),
            Period = AnalyticsPeriod.Daily
        };

        var testData = new List<RevenueAnalytics>
        {
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                RouteCode = "NYC-LAX",
                FareClass = "Economy",
                AirlineCode = "AA",
                TotalRevenue = 10000m,
                BaseRevenue = 8000m,
                TaxRevenue = 1500m,
                FeeRevenue = 500m,
                TotalBookings = 50,
                CompletedBookings = 45,
                TotalPassengers = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.RevenueAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        _mockCacheService.Setup(x => x.GetAsync<List<RevenueAnalytics>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<RevenueAnalytics>?)null);

        // Act
        var result = await _analyticsService.GetRevenueAnalyticsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var analytics = result.First();
        Assert.Equal("NYC-LAX", analytics.RouteCode);
        Assert.Equal(10000m, analytics.TotalRevenue);
        Assert.Equal(45, analytics.CompletedBookings);
    }

    [Fact]
    public async Task GetRevenueBreakdownAsync_WithValidDateRange_ReturnsBreakdown()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        
        var testData = new List<RevenueAnalytics>
        {
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                BaseRevenue = 5000m,
                TaxRevenue = 750m,
                FeeRevenue = 250m,
                ExtraServicesRevenue = 200m,
                PromotionDiscounts = 100m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-2),
                Period = "Daily",
                BaseRevenue = 3000m,
                TaxRevenue = 450m,
                FeeRevenue = 150m,
                ExtraServicesRevenue = 100m,
                PromotionDiscounts = 50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.RevenueAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetRevenueBreakdownAsync(dateRange);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8000m, result.BaseRevenue);
        Assert.Equal(1200m, result.TaxRevenue);
        Assert.Equal(400m, result.FeeRevenue);
        Assert.Equal(300m, result.ExtraServicesRevenue);
        Assert.Equal(150m, result.PromotionDiscounts);
        Assert.Equal(9750m, result.TotalRevenue); // 8000 + 1200 + 400 + 300 - 150
    }

    [Fact]
    public async Task GetTotalRevenueAsync_WithValidDateRange_ReturnsTotal()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        
        var testData = new List<RevenueAnalytics>
        {
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                TotalRevenue = 10000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-2),
                Period = "Daily",
                TotalRevenue = 15000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.RevenueAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetTotalRevenueAsync(dateRange);

        // Assert
        Assert.Equal(25000m, result);
    }

    [Fact]
    public async Task GetBookingStatusSummaryAsync_WithValidDateRange_ReturnsSummary()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        
        var testData = new List<BookingStatusAnalytics>
        {
            new BookingStatusAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                PendingBookings = 10,
                ConfirmedBookings = 50,
                CompletedBookings = 40,
                CancelledBookings = 5,
                ExpiredBookings = 2,
                RefundedBookings = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.BookingStatusAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetBookingStatusSummaryAsync(dateRange);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result["Pending"]);
        Assert.Equal(50, result["Confirmed"]);
        Assert.Equal(40, result["Completed"]);
        Assert.Equal(5, result["Cancelled"]);
        Assert.Equal(2, result["Expired"]);
        Assert.Equal(3, result["Refunded"]);
    }

    [Fact]
    public async Task GetDemographicsBreakdownAsync_WithValidDateRange_ReturnsBreakdown()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        
        var testData = new List<PassengerDemographicsAnalytics>
        {
            new PassengerDemographicsAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                PassengersAge18To24 = 20,
                PassengersAge25To34 = 35,
                PassengersAge35To44 = 25,
                PassengersAge45To54 = 15,
                PassengersAge55To64 = 10,
                MalePassengers = 60,
                FemalePassengers = 45,
                SinglePassengerBookings = 30,
                FamilyBookings = 15,
                GroupBookings = 8,
                BusinessBookings = 5,
                AverageAge = 35.5m,
                AverageGroupSize = 2.1m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.PassengerDemographicsAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetDemographicsBreakdownAsync(dateRange);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(105, result.TotalPassengers); // Sum of age groups
        Assert.Equal(35.5m, result.AverageAge);
        Assert.Equal(2.1m, result.AverageGroupSize);
        Assert.Equal(60, result.GenderDistribution["Male"]);
        Assert.Equal(45, result.GenderDistribution["Female"]);
        Assert.Equal(30, result.BookingPatterns[BookingPattern.Individual]);
        Assert.Equal(15, result.BookingPatterns[BookingPattern.Family]);
    }

    [Fact]
    public async Task GetRevenueTrendAsync_WithValidDateRange_ReturnsTrend()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.Today.AddDays(-3), DateTime.Today);
        
        var testData = new List<RevenueAnalytics>
        {
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-3),
                Period = "Daily",
                RouteCode = null, // System-wide data
                TotalRevenue = 10000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-2),
                Period = "Daily",
                RouteCode = null,
                TotalRevenue = 12000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                RouteCode = null,
                TotalRevenue = 15000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.RevenueAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetRevenueTrendAsync(dateRange, AnalyticsPeriod.Daily);

        // Assert
        Assert.NotNull(result);
        var trendList = result.ToList();
        Assert.Equal(3, trendList.Count);
        Assert.Equal(10000m, trendList[0].Revenue);
        Assert.Equal(12000m, trendList[1].Revenue);
        Assert.Equal(15000m, trendList[2].Revenue);
    }

    [Fact]
    public async Task GetRevenueComparisonAsync_WithValidPeriods_ReturnsComparison()
    {
        // Arrange
        var currentPeriod = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        var previousPeriod = new DateRange(DateTime.Today.AddDays(-14), DateTime.Today.AddDays(-7));
        
        var testData = new List<RevenueAnalytics>
        {
            // Current period data
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-3),
                Period = "Daily",
                TotalRevenue = 12000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Previous period data
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-10),
                Period = "Daily",
                TotalRevenue = 10000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.RevenueAnalytics.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetRevenueComparisonAsync(currentPeriod, previousPeriod);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12000m, result["CurrentRevenue"]);
        Assert.Equal(10000m, result["PreviousRevenue"]);
        Assert.Equal(20m, result["GrowthRate"]); // (12000 - 10000) / 10000 * 100
        Assert.Equal(2000m, result["AbsoluteChange"]);
    }

    [Theory]
    [InlineData("revenue")]
    [InlineData("booking_status")]
    [InlineData("demographics")]
    [InlineData("route_performance")]
    public async Task GetLastRefreshTimeAsync_WithValidViewName_ReturnsTime(string viewName)
    {
        // Arrange
        var expectedTime = DateTime.UtcNow.AddHours(-1);
        
        switch (viewName)
        {
            case "revenue":
                _context.RevenueAnalytics.Add(new RevenueAnalytics
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.Today,
                    Period = "Daily",
                    LastRefreshed = expectedTime,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                break;
            case "booking_status":
                _context.BookingStatusAnalytics.Add(new BookingStatusAnalytics
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.Today,
                    Period = "Daily",
                    LastRefreshed = expectedTime,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                break;
            case "demographics":
                _context.PassengerDemographicsAnalytics.Add(new PassengerDemographicsAnalytics
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.Today,
                    Period = "Daily",
                    LastRefreshed = expectedTime,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                break;
            case "route_performance":
                _context.RoutePerformanceAnalytics.Add(new RoutePerformanceAnalytics
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.Today,
                    Period = "Daily",
                    RouteCode = "NYC-LAX",
                    DepartureAirport = "NYC",
                    ArrivalAirport = "LAX",
                    LastRefreshed = expectedTime,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                break;
        }
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetLastRefreshTimeAsync(viewName);

        // Assert
        Assert.True(Math.Abs((result - expectedTime).TotalMinutes) < 1); // Within 1 minute tolerance
    }

    [Fact]
    public async Task IsDataCurrentAsync_WithCurrentData_ReturnsTrue()
    {
        // Arrange
        var recentTime = DateTime.UtcNow.AddMinutes(-30);
        _context.RevenueAnalytics.Add(new RevenueAnalytics
        {
            Id = Guid.NewGuid(),
            Date = DateTime.Today,
            Period = "Daily",
            LastRefreshed = recentTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.IsDataCurrentAsync("revenue", TimeSpan.FromHours(1));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsDataCurrentAsync_WithStaleData_ReturnsFalse()
    {
        // Arrange
        var oldTime = DateTime.UtcNow.AddHours(-2);
        _context.RevenueAnalytics.Add(new RevenueAnalytics
        {
            Id = Guid.NewGuid(),
            Date = DateTime.Today,
            Period = "Daily",
            LastRefreshed = oldTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.IsDataCurrentAsync("revenue", TimeSpan.FromHours(1));

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
