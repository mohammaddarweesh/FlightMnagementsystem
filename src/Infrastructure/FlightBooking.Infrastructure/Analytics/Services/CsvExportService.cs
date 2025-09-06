using FlightBooking.Application.Analytics.Interfaces;
using FlightBooking.Domain.Analytics;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace FlightBooking.Infrastructure.Analytics.Services;

/// <summary>
/// CSV export service for analytics data
/// </summary>
public class CsvExportService : ICsvExportService
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<CsvExportService> _logger;

    public CsvExportService(
        IAnalyticsService analyticsService,
        ILogger<CsvExportService> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> ExportRevenueAnalyticsAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting revenue analytics for date range: {StartDate} to {EndDate}", 
            filter.DateRange.StartDate, filter.DateRange.EndDate);

        var data = await _analyticsService.GetRevenueAnalyticsAsync(filter, cancellationToken);
        
        var csv = new StringBuilder();
        
        // Add headers
        if (config.IncludeHeaders)
        {
            var headers = new[]
            {
                "Date", "Period", "Route Code", "Fare Class", "Airline Code",
                "Total Revenue", "Base Revenue", "Tax Revenue", "Fee Revenue", "Extra Services Revenue",
                "Total Bookings", "Completed Bookings", "Cancelled Bookings", "Refunded Bookings",
                "Total Passengers", "Average Revenue Per Passenger", "Average Revenue Per Booking",
                "Total Seats", "Booked Seats", "Load Factor", "Revenue Per 1000 ASM",
                "Average Fare Price", "Min Fare Price", "Max Fare Price",
                "Promotion Discounts", "Bookings With Promotions", "Promotion Penetration Rate",
                "Revenue Growth Rate", "Booking Growth Rate", "Last Refreshed"
            };
            
            csv.AppendLine(string.Join(config.Delimiter, headers));
        }

        // Add data rows
        foreach (var item in data.Take(config.MaxRows))
        {
            var row = new[]
            {
                item.Date.ToString(config.DateFormat),
                item.Period,
                item.RouteCode ?? "",
                item.FareClass ?? "",
                item.AirlineCode ?? "",
                item.TotalRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.BaseRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.TaxRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.FeeRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.ExtraServicesRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.TotalBookings.ToString(),
                item.CompletedBookings.ToString(),
                item.CancelledBookings.ToString(),
                item.RefundedBookings.ToString(),
                item.TotalPassengers.ToString(),
                item.AverageRevenuePerPassenger.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageRevenuePerBooking.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.TotalSeats.ToString(),
                item.BookedSeats.ToString(),
                item.LoadFactor.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenuePer1000ASM.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageFarePrice.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.MinFarePrice.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.MaxFarePrice.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.PromotionDiscounts.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.BookingsWithPromotions.ToString(),
                item.PromotionPenetrationRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenueGrowthRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.BookingGrowthRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.LastRefreshed.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            csv.AppendLine(string.Join(config.Delimiter, row));
        }

        // Add metadata if requested
        if (config.IncludeMetadata)
        {
            csv.AppendLine();
            csv.AppendLine($"# Export generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"# Date range: {filter.DateRange.StartDate:yyyy-MM-dd} to {filter.DateRange.EndDate:yyyy-MM-dd}");
            csv.AppendLine($"# Total records: {data.Count()}");
            csv.AppendLine($"# Export format: {config.Format}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportBookingStatusAnalyticsAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting booking status analytics for date range: {StartDate} to {EndDate}", 
            filter.DateRange.StartDate, filter.DateRange.EndDate);

        var data = await _analyticsService.GetBookingStatusAnalyticsAsync(filter, cancellationToken);
        
        var csv = new StringBuilder();
        
        if (config.IncludeHeaders)
        {
            var headers = new[]
            {
                "Date", "Period", "Route Code", "Fare Class",
                "Pending Bookings", "Confirmed Bookings", "Checked In Bookings", "Completed Bookings",
                "Cancelled Bookings", "Expired Bookings", "Refunded Bookings",
                "Pending Percentage", "Confirmed Percentage", "Completion Rate", "Cancellation Rate", "Refund Rate",
                "Avg Booking to Confirmation (min)", "Avg Confirmation to Check-in (hrs)", "Avg Booking to Completion (hrs)",
                "Pending Revenue", "Confirmed Revenue", "Lost Revenue to Cancellations", "Refunded Revenue",
                "Last Refreshed"
            };
            
            csv.AppendLine(string.Join(config.Delimiter, headers));
        }

        foreach (var item in data.Take(config.MaxRows))
        {
            var row = new[]
            {
                item.Date.ToString(config.DateFormat),
                item.Period,
                item.RouteCode ?? "",
                item.FareClass ?? "",
                item.PendingBookings.ToString(),
                item.ConfirmedBookings.ToString(),
                item.CheckedInBookings.ToString(),
                item.CompletedBookings.ToString(),
                item.CancelledBookings.ToString(),
                item.ExpiredBookings.ToString(),
                item.RefundedBookings.ToString(),
                item.PendingPercentage.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.ConfirmedPercentage.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.CompletionRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.CancellationRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RefundRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageBookingToConfirmationMinutes.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageConfirmationToCheckInHours.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageBookingToCompletionHours.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.PendingRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.ConfirmedRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.LostRevenueToCancellations.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RefundedRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.LastRefreshed.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            csv.AppendLine(string.Join(config.Delimiter, row));
        }

        if (config.IncludeMetadata)
        {
            csv.AppendLine();
            csv.AppendLine($"# Export generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"# Date range: {filter.DateRange.StartDate:yyyy-MM-dd} to {filter.DateRange.EndDate:yyyy-MM-dd}");
            csv.AppendLine($"# Total records: {data.Count()}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportPassengerDemographicsAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting passenger demographics for date range: {StartDate} to {EndDate}", 
            filter.DateRange.StartDate, filter.DateRange.EndDate);

        var data = await _analyticsService.GetPassengerDemographicsAsync(filter, cancellationToken);
        
        var csv = new StringBuilder();
        
        if (config.IncludeHeaders)
        {
            var headers = new[]
            {
                "Date", "Period", "Route Code", "Fare Class",
                "Passengers Age 0-17", "Passengers Age 18-24", "Passengers Age 25-34", "Passengers Age 35-44",
                "Passengers Age 45-54", "Passengers Age 55-64", "Passengers Age 65+", "Passengers Age Unknown",
                "Male Passengers", "Female Passengers", "Other Gender Passengers", "Unknown Gender Passengers",
                "Single Passenger Bookings", "Family Bookings", "Group Bookings", "Business Bookings",
                "Revenue from Age 18-34", "Revenue from Age 35-54", "Revenue from Age 55+",
                "Revenue from Business Class", "Revenue from Family Bookings",
                "Average Age", "Average Group Size", "Business Class Penetration", "Family Booking Rate",
                "Last Refreshed"
            };
            
            csv.AppendLine(string.Join(config.Delimiter, headers));
        }

        foreach (var item in data.Take(config.MaxRows))
        {
            var row = new[]
            {
                item.Date.ToString(config.DateFormat),
                item.Period,
                item.RouteCode ?? "",
                item.FareClass ?? "",
                item.PassengersAge0To17.ToString(),
                item.PassengersAge18To24.ToString(),
                item.PassengersAge25To34.ToString(),
                item.PassengersAge35To44.ToString(),
                item.PassengersAge45To54.ToString(),
                item.PassengersAge55To64.ToString(),
                item.PassengersAge65Plus.ToString(),
                item.PassengersAgeUnknown.ToString(),
                item.MalePassengers.ToString(),
                item.FemalePassengers.ToString(),
                item.OtherGenderPassengers.ToString(),
                item.UnknownGenderPassengers.ToString(),
                item.SinglePassengerBookings.ToString(),
                item.FamilyBookings.ToString(),
                item.GroupBookings.ToString(),
                item.BusinessBookings.ToString(),
                item.RevenueFromAge18To34.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenueFromAge35To54.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenueFromAge55Plus.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenueFromBusinessClass.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenueFromFamilyBookings.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageAge.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageGroupSize.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.BusinessClassPenetration.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.FamilyBookingRate.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.LastRefreshed.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            csv.AppendLine(string.Join(config.Delimiter, row));
        }

        if (config.IncludeMetadata)
        {
            csv.AppendLine();
            csv.AppendLine($"# Export generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"# Date range: {filter.DateRange.StartDate:yyyy-MM-dd} to {filter.DateRange.EndDate:yyyy-MM-dd}");
            csv.AppendLine($"# Total records: {data.Count()}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportRoutePerformanceAsync(
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting route performance for date range: {StartDate} to {EndDate}", 
            filter.DateRange.StartDate, filter.DateRange.EndDate);

        var data = await _analyticsService.GetRoutePerformanceAsync(filter, cancellationToken);
        
        var csv = new StringBuilder();
        
        if (config.IncludeHeaders)
        {
            var headers = new[]
            {
                "Date", "Period", "Route Code", "Departure Airport", "Arrival Airport", "Distance (km)",
                "Total Revenue", "Total Flights", "Total Bookings", "Total Passengers",
                "Load Factor", "Average Ticket Price", "Revenue per KM",
                "On Time Flights", "Delayed Flights", "Cancelled Flights",
                "On Time Performance", "Average Delay (min)", "Demand Score",
                "Last Refreshed"
            };
            
            csv.AppendLine(string.Join(config.Delimiter, headers));
        }

        foreach (var item in data.Take(config.MaxRows))
        {
            var row = new[]
            {
                item.Date.ToString(config.DateFormat),
                item.Period,
                item.RouteCode,
                item.DepartureAirport,
                item.ArrivalAirport,
                item.DistanceKm.ToString(),
                item.TotalRevenue.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.TotalFlights.ToString(),
                item.TotalBookings.ToString(),
                item.TotalPassengers.ToString(),
                item.LoadFactor.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageTicketPrice.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.RevenuePerKm.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.OnTimeFlights.ToString(),
                item.DelayedFlights.ToString(),
                item.CancelledFlights.ToString(),
                item.OnTimePerformance.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.AverageDelayMinutes.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.DemandScore.ToString(config.DecimalFormat, CultureInfo.InvariantCulture),
                item.LastRefreshed.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            csv.AppendLine(string.Join(config.Delimiter, row));
        }

        if (config.IncludeMetadata)
        {
            csv.AppendLine();
            csv.AppendLine($"# Export generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"# Date range: {filter.DateRange.StartDate:yyyy-MM-dd} to {filter.DateRange.EndDate:yyyy-MM-dd}");
            csv.AppendLine($"# Total records: {data.Count()}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportAnalyticsSummaryAsync(
        DateRange dateRange, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting analytics summary for date range: {StartDate} to {EndDate}", 
            dateRange.StartDate, dateRange.EndDate);

        var summary = await _analyticsService.GetAnalyticsSummaryAsync(dateRange, cancellationToken);
        
        var csv = new StringBuilder();
        
        // Summary format is different - key-value pairs
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Period Start,{summary.Period.StartDate:yyyy-MM-dd}");
        csv.AppendLine($"Period End,{summary.Period.EndDate:yyyy-MM-dd}");
        csv.AppendLine($"Total Revenue,{summary.Revenue.TotalRevenue:F2}");
        csv.AppendLine($"Base Revenue,{summary.Revenue.BaseRevenue:F2}");
        csv.AppendLine($"Tax Revenue,{summary.Revenue.TaxRevenue:F2}");
        csv.AppendLine($"Fee Revenue,{summary.Revenue.FeeRevenue:F2}");
        csv.AppendLine($"Extra Services Revenue,{summary.Revenue.ExtraServicesRevenue:F2}");
        csv.AppendLine($"Promotion Discounts,{summary.Revenue.PromotionDiscounts:F2}");
        csv.AppendLine($"Refunded Revenue,{summary.Revenue.RefundedRevenue:F2}");
        csv.AppendLine($"Total Bookings,{summary.TotalBookings}");
        csv.AppendLine($"Total Passengers,{summary.TotalPassengers}");
        csv.AppendLine($"Total Flights,{summary.TotalFlights}");
        csv.AppendLine($"Load Factor,{summary.Performance.LoadFactor:F2}%");
        csv.AppendLine($"On Time Performance,{summary.Performance.OnTimePerformance:F2}%");
        csv.AppendLine($"Customer Satisfaction,{summary.Performance.CustomerSatisfactionScore:F2}");
        csv.AppendLine($"Average Delay (min),{summary.Performance.AverageDelayMinutes:F2}");
        csv.AppendLine($"Cancellation Rate,{summary.Performance.CancellationRate:F2}%");
        csv.AppendLine($"Revenue Per Passenger,{summary.Performance.RevenuePerPassenger:F2}");
        csv.AppendLine($"Average Age,{summary.Demographics.AverageAge:F1}");
        csv.AppendLine($"Average Group Size,{summary.Demographics.AverageGroupSize:F1}");
        csv.AppendLine($"Data Quality,{summary.DataQuality}");
        csv.AppendLine($"Last Updated,{summary.LastUpdated:yyyy-MM-dd HH:mm:ss}");

        if (config.IncludeMetadata)
        {
            csv.AppendLine();
            csv.AppendLine($"# Export generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"# Data sources: {string.Join(", ", summary.DataSources)}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<string> GetExportUrlAsync(
        string exportType, 
        AnalyticsFilter filter, 
        ExportConfiguration config,
        CancellationToken cancellationToken = default)
    {
        // Generate a temporary URL for the export
        // In a real implementation, this would create a temporary file or signed URL
        var fileName = string.IsNullOrEmpty(config.FileName) 
            ? $"{exportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv"
            : config.FileName;

        // This would typically return a URL to download the file
        return $"/api/analytics/export/{exportType}/{fileName}";
    }
}
