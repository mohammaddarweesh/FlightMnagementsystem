using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Analytics;

/// <summary>
/// Revenue analytics aggregate for materialized view data
/// </summary>
public class RevenueAnalytics : BaseEntity
{
    public DateTime Date { get; set; }
    public string Period { get; set; } = string.Empty; // Daily, Weekly, Monthly
    public string? RouteCode { get; set; } // NULL for all routes
    public string? FareClass { get; set; } // NULL for all classes
    public string? AirlineCode { get; set; } // NULL for all airlines
    
    // Revenue Metrics
    public decimal TotalRevenue { get; set; }
    public decimal BaseRevenue { get; set; }
    public decimal TaxRevenue { get; set; }
    public decimal FeeRevenue { get; set; }
    public decimal ExtraServicesRevenue { get; set; }
    
    // Booking Metrics
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int RefundedBookings { get; set; }
    
    // Passenger Metrics
    public int TotalPassengers { get; set; }
    public decimal AverageRevenuePerPassenger { get; set; }
    public decimal AverageRevenuePerBooking { get; set; }
    
    // Capacity Metrics
    public int TotalSeats { get; set; }
    public int BookedSeats { get; set; }
    public decimal LoadFactor { get; set; } // BookedSeats / TotalSeats
    public decimal RevenuePer1000ASM { get; set; } // Revenue per 1000 Available Seat Miles
    
    // Pricing Metrics
    public decimal AverageFarePrice { get; set; }
    public decimal MinFarePrice { get; set; }
    public decimal MaxFarePrice { get; set; }
    
    // Promotion Metrics
    public decimal PromotionDiscounts { get; set; }
    public int BookingsWithPromotions { get; set; }
    public decimal PromotionPenetrationRate { get; set; }
    
    // Computed Properties
    public decimal RevenueGrowthRate { get; set; } // Compared to previous period
    public decimal BookingGrowthRate { get; set; } // Compared to previous period
    public decimal YieldPerPassenger => TotalPassengers > 0 ? TotalRevenue / TotalPassengers : 0;
    public decimal OccupancyRate => TotalSeats > 0 ? (decimal)BookedSeats / TotalSeats * 100 : 0;
    
    // Metadata
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;
    public string DataSource { get; set; } = "MaterializedView";
    public int RecordCount { get; set; } // Number of underlying records aggregated
}

/// <summary>
/// Booking status analytics for dashboard summaries
/// </summary>
public class BookingStatusAnalytics : BaseEntity
{
    public DateTime Date { get; set; }
    public string Period { get; set; } = string.Empty; // Daily, Weekly, Monthly
    public string? RouteCode { get; set; }
    public string? FareClass { get; set; }
    
    // Status Counts
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CheckedInBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int ExpiredBookings { get; set; }
    public int RefundedBookings { get; set; }
    
    // Status Percentages
    public decimal PendingPercentage { get; set; }
    public decimal ConfirmedPercentage { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal RefundRate { get; set; }
    
    // Timing Metrics
    public decimal AverageBookingToConfirmationMinutes { get; set; }
    public decimal AverageConfirmationToCheckInHours { get; set; }
    public decimal AverageBookingToCompletionHours { get; set; }
    
    // Revenue Impact
    public decimal PendingRevenue { get; set; }
    public decimal ConfirmedRevenue { get; set; }
    public decimal LostRevenueToCancellations { get; set; }
    public decimal RefundedRevenue { get; set; }
    
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;
    public int TotalBookings => PendingBookings + ConfirmedBookings + CheckedInBookings + 
                               CompletedBookings + CancelledBookings + ExpiredBookings;
}

/// <summary>
/// Passenger demographics analytics
/// </summary>
public class PassengerDemographicsAnalytics : BaseEntity
{
    public DateTime Date { get; set; }
    public string Period { get; set; } = string.Empty;
    public string? RouteCode { get; set; }
    public string? FareClass { get; set; }
    
    // Age Demographics
    public int PassengersAge0To17 { get; set; }
    public int PassengersAge18To24 { get; set; }
    public int PassengersAge25To34 { get; set; }
    public int PassengersAge35To44 { get; set; }
    public int PassengersAge45To54 { get; set; }
    public int PassengersAge55To64 { get; set; }
    public int PassengersAge65Plus { get; set; }
    public int PassengersAgeUnknown { get; set; }
    
    // Gender Demographics
    public int MalePassengers { get; set; }
    public int FemalePassengers { get; set; }
    public int OtherGenderPassengers { get; set; }
    public int UnknownGenderPassengers { get; set; }
    
    // Booking Patterns
    public int SinglePassengerBookings { get; set; }
    public int FamilyBookings { get; set; } // 2+ passengers with children
    public int GroupBookings { get; set; } // 3+ adult passengers
    public int BusinessBookings { get; set; } // Business class or corporate bookings
    
    // Geographic Data (if available)
    public Dictionary<string, int> PassengersByCountry { get; set; } = new();
    public Dictionary<string, int> PassengersByCity { get; set; } = new();
    
    // Revenue by Demographics
    public decimal RevenueFromAge18To34 { get; set; }
    public decimal RevenueFromAge35To54 { get; set; }
    public decimal RevenueFromAge55Plus { get; set; }
    public decimal RevenueFromBusinessClass { get; set; }
    public decimal RevenueFromFamilyBookings { get; set; }
    
    // Computed Metrics
    public decimal AverageAge { get; set; }
    public decimal AverageGroupSize { get; set; }
    public decimal BusinessClassPenetration { get; set; }
    public decimal FamilyBookingRate { get; set; }
    
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;
    public int TotalPassengers => PassengersAge0To17 + PassengersAge18To24 + PassengersAge25To34 + 
                                 PassengersAge35To44 + PassengersAge45To54 + PassengersAge55To64 + 
                                 PassengersAge65Plus + PassengersAgeUnknown;
}

/// <summary>
/// Route performance analytics
/// </summary>
public class RoutePerformanceAnalytics : BaseEntity
{
    public DateTime Date { get; set; }
    public string Period { get; set; } = string.Empty;
    public string RouteCode { get; set; } = string.Empty;
    public string DepartureAirport { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public int DistanceKm { get; set; }
    
    // Performance Metrics
    public decimal TotalRevenue { get; set; }
    public int TotalFlights { get; set; }
    public int TotalBookings { get; set; }
    public int TotalPassengers { get; set; }
    public decimal LoadFactor { get; set; }
    public decimal AverageTicketPrice { get; set; }
    public decimal RevenuePerKm { get; set; }
    
    // Operational Metrics
    public int OnTimeFlights { get; set; }
    public int DelayedFlights { get; set; }
    public int CancelledFlights { get; set; }
    public decimal OnTimePerformance { get; set; }
    public decimal AverageDelayMinutes { get; set; }
    
    // Demand Metrics
    public decimal DemandScore { get; set; } // Based on booking velocity
    public decimal SeasonalityIndex { get; set; } // Compared to annual average
    public decimal CompetitiveIndex { get; set; } // Market share indicator
    
    // Profitability (if cost data available)
    public decimal? EstimatedCosts { get; set; }
    public decimal? EstimatedProfit { get; set; }
    public decimal? ProfitMargin { get; set; }
    
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;
}
