namespace FlightBooking.Domain.Analytics;

/// <summary>
/// Date range value object for analytics queries
/// </summary>
public record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    
    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date");
        
        StartDate = startDate.Date;
        EndDate = endDate.Date;
    }
    
    public int DayCount => (EndDate - StartDate).Days + 1;
    public bool IsValid => StartDate <= EndDate;
    public bool Contains(DateTime date) => date.Date >= StartDate && date.Date <= EndDate;
    
    public static DateRange Today => new(DateTime.Today, DateTime.Today);
    public static DateRange Yesterday => new(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1));
    public static DateRange ThisWeek => new(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today);
    public static DateRange LastWeek => new(DateTime.Today.AddDays(-7 - (int)DateTime.Today.DayOfWeek), DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - 1));
    public static DateRange ThisMonth => new(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);
    public static DateRange LastMonth
    {
        get
        {
            var firstDayLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddDays(-1);
            return new(firstDayLastMonth, lastDayLastMonth);
        }
    }
    public static DateRange ThisYear => new(new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today);
    public static DateRange LastYear => new(new DateTime(DateTime.Today.Year - 1, 1, 1), new DateTime(DateTime.Today.Year - 1, 12, 31));
}

/// <summary>
/// Revenue breakdown value object
/// </summary>
public record RevenueBreakdown
{
    public decimal BaseRevenue { get; init; }
    public decimal TaxRevenue { get; init; }
    public decimal FeeRevenue { get; init; }
    public decimal ExtraServicesRevenue { get; init; }
    public decimal PromotionDiscounts { get; init; }
    public decimal RefundedRevenue { get; init; }
    
    public decimal TotalRevenue => BaseRevenue + TaxRevenue + FeeRevenue + ExtraServicesRevenue - PromotionDiscounts - RefundedRevenue;
    public decimal GrossRevenue => BaseRevenue + TaxRevenue + FeeRevenue + ExtraServicesRevenue;
    public decimal NetRevenue => GrossRevenue - PromotionDiscounts - RefundedRevenue;
    public decimal DiscountRate => GrossRevenue > 0 ? PromotionDiscounts / GrossRevenue * 100 : 0;
    public decimal RefundRate => GrossRevenue > 0 ? RefundedRevenue / GrossRevenue * 100 : 0;
}

/// <summary>
/// Performance metrics value object
/// </summary>
public record PerformanceMetrics
{
    public decimal LoadFactor { get; init; }
    public decimal OnTimePerformance { get; init; }
    public decimal CustomerSatisfactionScore { get; init; }
    public decimal AverageDelayMinutes { get; init; }
    public decimal CancellationRate { get; init; }
    public decimal RevenuePerPassenger { get; init; }
    public decimal RevenuePerSeat { get; init; }
    public decimal YieldPerMile { get; init; }
    
    public string PerformanceGrade
    {
        get
        {
            var score = (LoadFactor + OnTimePerformance + CustomerSatisfactionScore) / 3;
            return score switch
            {
                >= 90 => "A+",
                >= 85 => "A",
                >= 80 => "B+",
                >= 75 => "B",
                >= 70 => "C+",
                >= 65 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }
    }
}

/// <summary>
/// Demographics breakdown value object
/// </summary>
public record DemographicsBreakdown
{
    public Dictionary<PassengerAgeGroup, int> AgeDistribution { get; init; } = new();
    public Dictionary<string, int> GenderDistribution { get; init; } = new();
    public Dictionary<BookingPattern, int> BookingPatterns { get; init; } = new();
    public Dictionary<string, int> GeographicDistribution { get; init; } = new();
    
    public int TotalPassengers => AgeDistribution.Values.Sum();
    public decimal AverageAge { get; init; }
    public decimal AverageGroupSize { get; init; }
    public string DominantAgeGroup => AgeDistribution.OrderByDescending(x => x.Value).FirstOrDefault().Key.ToString();
    public string DominantGender => GenderDistribution.OrderByDescending(x => x.Value).FirstOrDefault().Key;
}

/// <summary>
/// Analytics filter criteria value object
/// </summary>
public record AnalyticsFilter
{
    public DateRange DateRange { get; init; } = DateRange.Today;
    public AnalyticsPeriod Period { get; init; } = AnalyticsPeriod.Daily;
    public AnalyticsGranularity Granularity { get; init; } = AnalyticsGranularity.System;
    public List<string> RouteCodes { get; init; } = new();
    public List<string> FareClasses { get; init; } = new();
    public List<string> AirlineCodes { get; init; } = new();
    public List<string> AirportCodes { get; init; } = new();
    public List<MetricType> MetricTypes { get; init; } = new();
    public bool IncludeRefunded { get; init; } = false;
    public bool IncludeCancelled { get; init; } = false;
    public decimal? MinRevenue { get; init; }
    public decimal? MaxRevenue { get; init; }
    public int? MinPassengers { get; init; }
    public int? MaxPassengers { get; init; }
}

/// <summary>
/// Export configuration value object
/// </summary>
public record ExportConfiguration
{
    public ExportFormat Format { get; init; } = ExportFormat.CSV;
    public bool IncludeHeaders { get; init; } = true;
    public bool IncludeMetadata { get; init; } = true;
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    public string DecimalFormat { get; init; } = "F2";
    public string Delimiter { get; init; } = ",";
    public string FileName { get; set; } = string.Empty;
    public List<string> ColumnsToInclude { get; init; } = new();
    public List<string> ColumnsToExclude { get; init; } = new();
    public int MaxRows { get; init; } = 100000;
    public bool CompressOutput { get; init; } = false;
}

/// <summary>
/// Analytics summary value object
/// </summary>
public record AnalyticsSummary
{
    public DateRange Period { get; init; } = DateRange.Today;
    public RevenueBreakdown Revenue { get; init; } = new();
    public PerformanceMetrics Performance { get; init; } = new();
    public DemographicsBreakdown Demographics { get; init; } = new();
    public int TotalBookings { get; init; }
    public int TotalPassengers { get; init; }
    public int TotalFlights { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public string DataQuality { get; init; } = "Good"; // Good, Fair, Poor
    public List<string> DataSources { get; init; } = new();
    public Dictionary<string, object> CustomMetrics { get; init; } = new();
}
