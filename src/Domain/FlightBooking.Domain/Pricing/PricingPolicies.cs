using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Pricing;

public class PricingPolicy : BaseEntity
{
    public string PolicyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public PolicySeverity Severity { get; set; }
    public bool IsBlocking { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Conditions { get; set; } = string.Empty; // JSON
    public string Parameters { get; set; } = string.Empty; // JSON
    public List<string> ApplicableRoutes { get; set; } = new();
    public List<string> ApplicableFareClasses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
    public string? Resolution { get; set; }
}

public class AdvancePurchasePolicy
{
    public int MinimumDaysAdvance { get; set; }
    public int? MaximumDaysAdvance { get; set; }
    public List<string> ApplicableFareClasses { get; set; } = new();
    public List<string> ExemptRoutes { get; set; } = new();
    public bool AllowSameDayBooking { get; set; } = false;
    public decimal? SameDayBookingSurcharge { get; set; }
    public string? ExceptionConditions { get; set; }
}

public class BlackoutDatePolicy
{
    public List<DateRange> BlackoutPeriods { get; set; } = new();
    public List<string> AffectedRoutes { get; set; } = new();
    public List<string> AffectedFareClasses { get; set; } = new();
    public BlackoutType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool AllowOverride { get; set; } = false;
    public string? OverrideConditions { get; set; }
    public decimal? OverrideSurcharge { get; set; }
}

public class RouteRestrictionPolicy
{
    public List<string> RestrictedRoutes { get; set; } = new();
    public List<string> AllowedOrigins { get; set; } = new();
    public List<string> AllowedDestinations { get; set; } = new();
    public RestrictionType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> ExemptCustomerTypes { get; set; } = new();
    public DateTime? TemporaryUntil { get; set; }
}

public class MinimumStayPolicy
{
    public int MinimumNights { get; set; }
    public int? MaximumNights { get; set; }
    public bool RequiresSaturdayNightStay { get; set; }
    public bool RequiresSundayNightStay { get; set; }
    public List<string> ApplicableFareClasses { get; set; } = new();
    public List<string> ExemptRoutes { get; set; } = new();
    public List<DateRange> ExemptPeriods { get; set; } = new();
}

public class RefundPolicy
{
    public bool IsRefundable { get; set; }
    public decimal RefundFee { get; set; }
    public RefundType RefundType { get; set; }
    public int RefundProcessingDays { get; set; }
    public List<RefundCondition> Conditions { get; set; } = new();
    public DateTime? RefundDeadline { get; set; }
    public bool AllowPartialRefund { get; set; }
    public decimal? PartialRefundPercentage { get; set; }
}

public class ChangePolicy
{
    public bool IsChangeable { get; set; }
    public decimal ChangeFee { get; set; }
    public ChangeType AllowedChangeType { get; set; }
    public int MaximumChanges { get; set; }
    public List<string> ChangeableFields { get; set; } = new();
    public DateTime? ChangeDeadline { get; set; }
    public bool AllowSameDayChange { get; set; }
    public decimal? SameDayChangeFee { get; set; }
    public bool RequireFareDifference { get; set; }
}

public class GroupBookingPolicy
{
    public int MinimumPassengers { get; set; }
    public int MaximumPassengers { get; set; }
    public decimal GroupDiscountPercentage { get; set; }
    public bool RequiresApproval { get; set; }
    public int AdvanceBookingDays { get; set; }
    public List<string> EligibleFareClasses { get; set; } = new();
    public bool AllowMixedFareClasses { get; set; }
    public string? SpecialTerms { get; set; }
}

public class CorporatePolicy
{
    public string CorporateCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public List<string> EligibleRoutes { get; set; } = new();
    public List<string> EligibleFareClasses { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public int CreditTermsDays { get; set; }
    public decimal? MonthlySpendCommitment { get; set; }
    public DateTime ContractStart { get; set; }
    public DateTime ContractEnd { get; set; }
}

public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public bool IncludeWeekends { get; set; } = true;
    public List<DayOfWeek> ExcludedDays { get; set; } = new();

    public bool Contains(DateTime date)
    {
        if (date.Date < StartDate.Date || date.Date > EndDate.Date)
            return false;

        if (!IncludeWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
            return false;

        return !ExcludedDays.Contains(date.DayOfWeek);
    }

    public int GetDaysCount()
    {
        var days = 0;
        for (var date = StartDate.Date; date <= EndDate.Date; date = date.AddDays(1))
        {
            if (Contains(date))
                days++;
        }
        return days;
    }
}

public class RefundCondition
{
    public string Condition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public decimal? AdditionalFee { get; set; }
    public string? Documentation { get; set; }
}

public class PolicyValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<PolicyViolation> Violations { get; set; } = new();
    public List<PolicyWarning> Warnings { get; set; } = new();
    public List<PolicyRecommendation> Recommendations { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public string? ApprovalReason { get; set; }
    public List<string> RequiredDocuments { get; set; } = new();
}

public class PolicyWarning
{
    public string PolicyId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
    public bool CanProceed { get; set; } = true;
}

public class PolicyRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? PotentialSavings { get; set; }
    public string? AlternativeAction { get; set; }
    public int Priority { get; set; }
}

// Enums
public enum PolicyType
{
    AdvancePurchase,
    BlackoutDates,
    RouteRestriction,
    MinimumStay,
    MaximumStay,
    Refund,
    Change,
    GroupBooking,
    Corporate,
    AgeRestriction,
    DocumentRequirement,
    PaymentMethod,
    BookingWindow
}

public enum BlackoutType
{
    NoBooking,
    NoDiscount,
    SurchargePeriod,
    RestrictedInventory,
    PremiumPeriod
}

public enum RestrictionType
{
    Prohibited,
    RequiresApproval,
    SurchargePeriod,
    LimitedInventory,
    DocumentRequired
}

public enum RefundType
{
    FullRefund,
    PartialRefund,
    NonRefundable,
    RefundToCredit,
    RefundMinusFees
}

public enum ChangeType
{
    DateChange,
    TimeChange,
    RouteChange,
    PassengerChange,
    FareClassChange,
    AnyChange,
    NoChanges
}

public static class PolicyConstants
{
    public const int DEFAULT_ADVANCE_PURCHASE_DAYS = 7;
    public const int DEFAULT_REFUND_PROCESSING_DAYS = 7;
    public const int DEFAULT_MAXIMUM_CHANGES = 3;
    public const decimal DEFAULT_CHANGE_FEE = 50.00m;
    public const decimal DEFAULT_REFUND_FEE = 25.00m;
    public const int DEFAULT_GROUP_MINIMUM = 10;
    public const decimal DEFAULT_GROUP_DISCOUNT = 5.0m;
    
    public static readonly List<string> PEAK_TRAVEL_PERIODS = new()
    {
        "Christmas", "New Year", "Easter", "Summer", "Thanksgiving"
    };
    
    public static readonly List<DayOfWeek> WEEKEND_DAYS = new()
    {
        DayOfWeek.Saturday, DayOfWeek.Sunday
    };
}
