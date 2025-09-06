namespace FlightBooking.Domain.Analytics;

/// <summary>
/// Analytics time periods for aggregation
/// </summary>
public enum AnalyticsPeriod
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}

/// <summary>
/// Analytics data granularity levels
/// </summary>
public enum AnalyticsGranularity
{
    System,      // Entire system
    Airline,     // Per airline
    Route,       // Per route
    FareClass,   // Per fare class
    Airport,     // Per airport
    Aircraft     // Per aircraft type
}

/// <summary>
/// Revenue breakdown categories
/// </summary>
public enum RevenueCategory
{
    BaseRevenue,
    TaxRevenue,
    FeeRevenue,
    ExtraServicesRevenue,
    PromotionDiscounts,
    RefundedRevenue
}

/// <summary>
/// Passenger age groups for demographics
/// </summary>
public enum PassengerAgeGroup
{
    Child,      // 0-17
    YoungAdult, // 18-24
    Adult,      // 25-34
    MiddleAge,  // 35-44
    Mature,     // 45-54
    Senior,     // 55-64
    Elderly,    // 65+
    Unknown
}

/// <summary>
/// Booking pattern types
/// </summary>
public enum BookingPattern
{
    Individual,
    Family,
    Group,
    Business,
    Corporate
}

/// <summary>
/// Analytics refresh frequency
/// </summary>
public enum RefreshFrequency
{
    RealTime,   // Immediate
    Hourly,     // Every hour
    Daily,      // Once per day
    Weekly,     // Once per week
    Monthly,    // Once per month
    OnDemand    // Manual refresh only
}

/// <summary>
/// Analytics data types for export
/// </summary>
public enum AnalyticsDataType
{
    Revenue,
    BookingStatus,
    Demographics,
    RoutePerformance,
    All
}

/// <summary>
/// Export format types
/// </summary>
public enum ExportFormat
{
    CSV,
    Excel,
    JSON,
    PDF
}

/// <summary>
/// Analytics metric types
/// </summary>
public enum MetricType
{
    Revenue,
    Bookings,
    Passengers,
    LoadFactor,
    OnTimePerformance,
    CustomerSatisfaction,
    Profitability,
    MarketShare
}
