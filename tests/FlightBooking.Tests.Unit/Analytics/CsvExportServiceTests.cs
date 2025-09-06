using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Domain.Analytics;
using FlightBooking.Infrastructure.Analytics.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace FlightBooking.Tests.Unit.Analytics;

public class CsvExportServiceTests
{
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly Mock<ILogger<CsvExportService>> _mockLogger;
    private readonly CsvExportService _csvExportService;

    public CsvExportServiceTests()
    {
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _mockLogger = new Mock<ILogger<CsvExportService>>();
        _csvExportService = new CsvExportService(_mockAnalyticsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExportRevenueAnalyticsAsync_WithValidData_ReturnsCsvBytes()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today),
            Period = AnalyticsPeriod.Daily
        };

        var config = new ExportConfiguration
        {
            Format = ExportFormat.CSV,
            IncludeHeaders = true,
            IncludeMetadata = true,
            Delimiter = ",",
            MaxRows = 1000
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
                ExtraServicesRevenue = 200m,
                TotalBookings = 50,
                CompletedBookings = 45,
                CancelledBookings = 3,
                RefundedBookings = 2,
                TotalPassengers = 90,
                AverageRevenuePerPassenger = 111.11m,
                AverageRevenuePerBooking = 222.22m,
                TotalSeats = 150,
                BookedSeats = 90,
                LoadFactor = 60.0m,
                RevenuePer1000ASM = 50.0m,
                AverageFarePrice = 88.89m,
                MinFarePrice = 75.0m,
                MaxFarePrice = 120.0m,
                PromotionDiscounts = 100m,
                BookingsWithPromotions = 5,
                PromotionPenetrationRate = 10.0m,
                RevenueGrowthRate = 5.5m,
                BookingGrowthRate = 3.2m,
                LastRefreshed = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockAnalyticsService.Setup(x => x.GetRevenueAnalyticsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _csvExportService.ExportRevenueAnalyticsAsync(filter, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        var csvContent = Encoding.UTF8.GetString(result);
        Assert.Contains("Date,Period,Route Code,Fare Class,Airline Code", csvContent); // Headers
        Assert.Contains("NYC-LAX", csvContent); // Data
        Assert.Contains("Economy", csvContent);
        Assert.Contains("10000.00", csvContent); // Total Revenue
        Assert.Contains("# Export generated on:", csvContent); // Metadata
    }

    [Fact]
    public async Task ExportBookingStatusAnalyticsAsync_WithValidData_ReturnsCsvBytes()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today),
            Period = AnalyticsPeriod.Daily
        };

        var config = new ExportConfiguration
        {
            Format = ExportFormat.CSV,
            IncludeHeaders = true,
            IncludeMetadata = false,
            Delimiter = ",",
            MaxRows = 1000
        };

        var testData = new List<BookingStatusAnalytics>
        {
            new BookingStatusAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                RouteCode = "NYC-LAX",
                FareClass = "Economy",
                PendingBookings = 10,
                ConfirmedBookings = 50,
                CheckedInBookings = 40,
                CompletedBookings = 35,
                CancelledBookings = 5,
                ExpiredBookings = 2,
                RefundedBookings = 3,
                PendingPercentage = 9.5m,
                ConfirmedPercentage = 47.6m,
                CompletionRate = 33.3m,
                CancellationRate = 4.8m,
                RefundRate = 2.9m,
                AverageBookingToConfirmationMinutes = 30.5m,
                AverageConfirmationToCheckInHours = 2.5m,
                AverageBookingToCompletionHours = 24.0m,
                PendingRevenue = 5000m,
                ConfirmedRevenue = 25000m,
                LostRevenueToCancellations = 2500m,
                RefundedRevenue = 1500m,
                LastRefreshed = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockAnalyticsService.Setup(x => x.GetBookingStatusAnalyticsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _csvExportService.ExportBookingStatusAnalyticsAsync(filter, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        var csvContent = Encoding.UTF8.GetString(result);
        Assert.Contains("Date,Period,Route Code,Fare Class", csvContent); // Headers
        Assert.Contains("NYC-LAX", csvContent); // Data
        Assert.Contains("50", csvContent); // Confirmed bookings
        Assert.DoesNotContain("# Export generated on:", csvContent); // No metadata
    }

    [Fact]
    public async Task ExportPassengerDemographicsAsync_WithValidData_ReturnsCsvBytes()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today),
            Period = AnalyticsPeriod.Daily
        };

        var config = new ExportConfiguration
        {
            Format = ExportFormat.CSV,
            IncludeHeaders = true,
            IncludeMetadata = true,
            Delimiter = ";", // Different delimiter
            MaxRows = 1000
        };

        var testData = new List<PassengerDemographicsAnalytics>
        {
            new PassengerDemographicsAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                RouteCode = "NYC-LAX",
                FareClass = "Economy",
                PassengersAge0To17 = 10,
                PassengersAge18To24 = 20,
                PassengersAge25To34 = 35,
                PassengersAge35To44 = 25,
                PassengersAge45To54 = 15,
                PassengersAge55To64 = 10,
                PassengersAge65Plus = 5,
                PassengersAgeUnknown = 2,
                MalePassengers = 60,
                FemalePassengers = 62,
                OtherGenderPassengers = 0,
                UnknownGenderPassengers = 0,
                SinglePassengerBookings = 30,
                FamilyBookings = 15,
                GroupBookings = 8,
                BusinessBookings = 5,
                RevenueFromAge18To34 = 15000m,
                RevenueFromAge35To54 = 12000m,
                RevenueFromAge55Plus = 8000m,
                RevenueFromBusinessClass = 5000m,
                RevenueFromFamilyBookings = 18000m,
                AverageAge = 35.5m,
                AverageGroupSize = 2.1m,
                BusinessClassPenetration = 8.5m,
                FamilyBookingRate = 25.4m,
                LastRefreshed = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockAnalyticsService.Setup(x => x.GetPassengerDemographicsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _csvExportService.ExportPassengerDemographicsAsync(filter, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        var csvContent = Encoding.UTF8.GetString(result);
        Assert.Contains("Date;Period;Route Code;Fare Class", csvContent); // Headers with semicolon
        Assert.Contains("NYC-LAX", csvContent); // Data
        Assert.Contains("35.50", csvContent); // Average age
        Assert.Contains("# Export generated on:", csvContent); // Metadata
    }

    [Fact]
    public async Task ExportRoutePerformanceAsync_WithValidData_ReturnsCsvBytes()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today),
            Period = AnalyticsPeriod.Daily
        };

        var config = new ExportConfiguration
        {
            Format = ExportFormat.CSV,
            IncludeHeaders = true,
            IncludeMetadata = true,
            Delimiter = ",",
            MaxRows = 500 // Smaller limit
        };

        var testData = new List<RoutePerformanceAnalytics>
        {
            new RoutePerformanceAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-1),
                Period = "Daily",
                RouteCode = "NYC-LAX",
                DepartureAirport = "JFK",
                ArrivalAirport = "LAX",
                DistanceKm = 3944,
                TotalRevenue = 50000m,
                TotalFlights = 5,
                TotalBookings = 200,
                TotalPassengers = 350,
                LoadFactor = 75.5m,
                AverageTicketPrice = 142.86m,
                RevenuePerKm = 12.68m,
                OnTimeFlights = 4,
                DelayedFlights = 1,
                CancelledFlights = 0,
                OnTimePerformance = 80.0m,
                AverageDelayMinutes = 15.5m,
                DemandScore = 40.0m,
                SeasonalityIndex = 1.2m,
                CompetitiveIndex = 0.8m,
                LastRefreshed = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockAnalyticsService.Setup(x => x.GetRoutePerformanceAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _csvExportService.ExportRoutePerformanceAsync(filter, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        var csvContent = Encoding.UTF8.GetString(result);
        Assert.Contains("Date,Period,Route Code,Departure Airport,Arrival Airport", csvContent); // Headers
        Assert.Contains("NYC-LAX", csvContent); // Data
        Assert.Contains("JFK", csvContent);
        Assert.Contains("LAX", csvContent);
        Assert.Contains("3944", csvContent); // Distance
        Assert.Contains("75.50", csvContent); // Load factor
    }

    [Fact]
    public async Task ExportAnalyticsSummaryAsync_WithValidData_ReturnsCsvBytes()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        
        var config = new ExportConfiguration
        {
            Format = ExportFormat.CSV,
            IncludeHeaders = true,
            IncludeMetadata = true
        };

        var summary = new AnalyticsSummary
        {
            Period = dateRange,
            Revenue = new RevenueBreakdown
            {
                BaseRevenue = 80000m,
                TaxRevenue = 12000m,
                FeeRevenue = 4000m,
                ExtraServicesRevenue = 2000m,
                PromotionDiscounts = 1000m,
                RefundedRevenue = 500m
            },
            Performance = new PerformanceMetrics
            {
                LoadFactor = 75.5m,
                OnTimePerformance = 85.2m,
                CustomerSatisfactionScore = 4.2m,
                AverageDelayMinutes = 12.5m,
                CancellationRate = 2.1m,
                RevenuePerPassenger = 125.50m
            },
            Demographics = new DemographicsBreakdown
            {
                AverageAge = 36.5m,
                AverageGroupSize = 2.3m
            },
            TotalBookings = 500,
            TotalPassengers = 850,
            TotalFlights = 25,
            LastUpdated = DateTime.UtcNow,
            DataQuality = "Good",
            DataSources = new List<string> { "MaterializedViews", "RealtimeData" }
        };

        _mockAnalyticsService.Setup(x => x.GetAnalyticsSummaryAsync(dateRange, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _csvExportService.ExportAnalyticsSummaryAsync(dateRange, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        var csvContent = Encoding.UTF8.GetString(result);
        Assert.Contains("Metric,Value", csvContent); // Headers
        Assert.Contains("Total Revenue,96500.00", csvContent); // Calculated total
        Assert.Contains("Base Revenue,80000.00", csvContent);
        Assert.Contains("Total Bookings,500", csvContent);
        Assert.Contains("Load Factor,75.50", csvContent);
        Assert.Contains("Data Quality,Good", csvContent);
    }

    [Fact]
    public async Task GetExportUrlAsync_WithValidParameters_ReturnsUrl()
    {
        // Arrange
        var exportType = "revenue";
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today)
        };
        var config = new ExportConfiguration
        {
            FileName = "custom_revenue_export.csv"
        };

        // Act
        var result = await _csvExportService.GetExportUrlAsync(exportType, filter, config);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("/api/analytics/export/revenue/", result);
        Assert.Contains("custom_revenue_export.csv", result);
    }

    [Fact]
    public async Task ExportRevenueAnalyticsAsync_WithNoHeaders_ExcludesHeaders()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-1), DateTime.Today)
        };

        var config = new ExportConfiguration
        {
            IncludeHeaders = false,
            IncludeMetadata = false
        };

        var testData = new List<RevenueAnalytics>
        {
            new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today,
                Period = "Daily",
                TotalRevenue = 1000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockAnalyticsService.Setup(x => x.GetRevenueAnalyticsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _csvExportService.ExportRevenueAnalyticsAsync(filter, config);

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        Assert.DoesNotContain("Date,Period,Route Code", csvContent); // No headers
        Assert.DoesNotContain("# Export generated on:", csvContent); // No metadata
        Assert.Contains("1000.00", csvContent); // But data is present
    }

    [Fact]
    public async Task ExportRevenueAnalyticsAsync_WithMaxRowsLimit_LimitsOutput()
    {
        // Arrange
        var filter = new AnalyticsFilter
        {
            DateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today)
        };

        var config = new ExportConfiguration
        {
            MaxRows = 2 // Very small limit
        };

        var testData = new List<RevenueAnalytics>();
        for (int i = 0; i < 5; i++) // Create 5 records
        {
            testData.Add(new RevenueAnalytics
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Today.AddDays(-i),
                Period = "Daily",
                TotalRevenue = 1000m * (i + 1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _mockAnalyticsService.Setup(x => x.GetRevenueAnalyticsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _csvExportService.ExportRevenueAnalyticsAsync(filter, config);

        // Assert
        var csvContent = Encoding.UTF8.GetString(result);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Should have header + 2 data rows + metadata lines
        var dataLines = lines.Where(line => !line.StartsWith("#") && !line.Contains("Date,Period")).Count();
        Assert.True(dataLines <= 2); // Respects MaxRows limit
    }
}
