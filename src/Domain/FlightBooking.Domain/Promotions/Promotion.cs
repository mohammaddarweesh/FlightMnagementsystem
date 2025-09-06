using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Promotions;

public class Promotion : AggregateRoot<Guid>
{
    private readonly List<PromotionUsage> _usages = new();

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PromotionType Type { get; private set; }
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = "USD";
    
    // Validity Period
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    // Usage Limits
    public int? MaxTotalUsage { get; private set; }
    public int? MaxUsagePerCustomer { get; private set; }
    public int? MaxUsagePerDay { get; private set; }
    public int CurrentTotalUsage { get; private set; }
    
    // Conditions
    public decimal? MinPurchaseAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public List<string> ApplicableRoutes { get; private set; } = new();
    public List<string> ApplicableFareClasses { get; private set; } = new();
    public List<string> ExcludedRoutes { get; private set; } = new();
    public List<string> ExcludedFareClasses { get; private set; } = new();
    public int? MinAdvanceDays { get; private set; }
    public int? MaxAdvanceDays { get; private set; }
    public bool IsFirstTimeCustomerOnly { get; private set; }
    public bool IsCombinableWithOtherOffers { get; private set; }
    
    // Targeting
    public List<string> TargetCustomerSegments { get; private set; } = new();
    public List<string> TargetCountries { get; private set; } = new();
    public List<DayOfWeek> ApplicableDaysOfWeek { get; private set; } = new();
    
    // Metadata
    public string CreatedBy { get; private set; } = string.Empty;
    public string? ModifiedBy { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public string? Terms { get; private set; }
    public string? MarketingMessage { get; private set; }
    
    // Navigation Properties
    public IReadOnlyList<PromotionUsage> Usages => _usages.AsReadOnly();
    
    // Computed Properties
    public bool IsExpired => DateTime.UtcNow > ValidTo;
    public bool IsNotYetValid => DateTime.UtcNow < ValidFrom;
    public bool IsCurrentlyValid => IsActive && !IsExpired && !IsNotYetValid;
    public bool HasReachedMaxUsage => MaxTotalUsage.HasValue && CurrentTotalUsage >= MaxTotalUsage.Value;
    public int RemainingUsage => MaxTotalUsage.HasValue ? Math.Max(0, MaxTotalUsage.Value - CurrentTotalUsage) : int.MaxValue;
    public double UsagePercentage => MaxTotalUsage.HasValue ? (double)CurrentTotalUsage / MaxTotalUsage.Value * 100 : 0;

    // Additional properties for job services compatibility
    public DateTime ExpiryDate => ValidTo;

    // Private constructor for EF Core
    private Promotion() { }

    public static Promotion Create(
        string code,
        string name,
        string description,
        PromotionType type,
        decimal value,
        DateTime validFrom,
        DateTime validTo,
        string createdBy,
        string currency = "USD",
        int? maxTotalUsage = null,
        int? maxUsagePerCustomer = null,
        decimal? minPurchaseAmount = null,
        decimal? maxDiscountAmount = null,
        List<string>? applicableRoutes = null,
        List<string>? applicableFareClasses = null,
        bool isFirstTimeCustomerOnly = false,
        string? terms = null)
    {
        ValidateCreationParameters(code, name, value, validFrom, validTo, createdBy);

        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name.Trim(),
            Description = description.Trim(),
            Type = type,
            Value = value,
            Currency = currency,
            ValidFrom = validFrom,
            ValidTo = validTo,
            MaxTotalUsage = maxTotalUsage,
            MaxUsagePerCustomer = maxUsagePerCustomer,
            MinPurchaseAmount = minPurchaseAmount,
            MaxDiscountAmount = maxDiscountAmount,
            ApplicableRoutes = applicableRoutes ?? new List<string>(),
            ApplicableFareClasses = applicableFareClasses ?? new List<string>(),
            IsFirstTimeCustomerOnly = isFirstTimeCustomerOnly,
            Terms = terms,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return promotion;
    }

    public PromotionValidationResult ValidateUsage(
        Guid? customerId,
        string? guestId,
        decimal purchaseAmount,
        string route,
        string fareClass,
        DateTime departureDate,
        DateTime bookingDate,
        bool isFirstTimeCustomer)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validity checks
        if (!IsActive)
            errors.Add("Promotion is not active");

        if (IsExpired)
            errors.Add($"Promotion expired on {ValidTo:yyyy-MM-dd}");

        if (IsNotYetValid)
            errors.Add($"Promotion is not valid until {ValidFrom:yyyy-MM-dd}");

        // Usage limit checks
        if (HasReachedMaxUsage)
            errors.Add("Promotion usage limit has been reached");

        // Customer-specific usage checks
        if (customerId.HasValue && MaxUsagePerCustomer.HasValue)
        {
            var customerUsageCount = _usages.Count(u => u.CustomerId == customerId.Value);
            if (customerUsageCount >= MaxUsagePerCustomer.Value)
                errors.Add($"You have already used this promotion {MaxUsagePerCustomer.Value} time(s)");
        }

        // Daily usage limit check
        if (MaxUsagePerDay.HasValue)
        {
            var todayUsageCount = _usages.Count(u => u.UsedAt.Date == DateTime.Today);
            if (todayUsageCount >= MaxUsagePerDay.Value)
                errors.Add("Daily usage limit for this promotion has been reached");
        }

        // Purchase amount checks
        if (MinPurchaseAmount.HasValue && purchaseAmount < MinPurchaseAmount.Value)
            errors.Add($"Minimum purchase amount of {MinPurchaseAmount.Value:C} required");

        // Route restrictions
        if (ApplicableRoutes.Any() && !ApplicableRoutes.Contains(route))
            errors.Add($"Promotion not valid for route {route}");

        if (ExcludedRoutes.Contains(route))
            errors.Add($"Promotion not valid for route {route}");

        // Fare class restrictions
        if (ApplicableFareClasses.Any() && !ApplicableFareClasses.Contains(fareClass))
            errors.Add($"Promotion not valid for fare class {fareClass}");

        if (ExcludedFareClasses.Contains(fareClass))
            errors.Add($"Promotion not valid for fare class {fareClass}");

        // Advance booking requirements
        var daysUntilDeparture = (departureDate - bookingDate).TotalDays;
        
        if (MinAdvanceDays.HasValue && daysUntilDeparture < MinAdvanceDays.Value)
            errors.Add($"Must be booked at least {MinAdvanceDays.Value} days in advance");

        if (MaxAdvanceDays.HasValue && daysUntilDeparture > MaxAdvanceDays.Value)
            errors.Add($"Must be booked within {MaxAdvanceDays.Value} days of departure");

        // First-time customer check
        if (IsFirstTimeCustomerOnly && !isFirstTimeCustomer)
            errors.Add("Promotion is only valid for first-time customers");

        // Day of week restrictions
        if (ApplicableDaysOfWeek.Any() && !ApplicableDaysOfWeek.Contains(departureDate.DayOfWeek))
        {
            var validDays = string.Join(", ", ApplicableDaysOfWeek);
            errors.Add($"Promotion only valid for departures on: {validDays}");
        }

        // Generate warnings
        if (RemainingUsage <= 10 && RemainingUsage > 0)
            warnings.Add($"Only {RemainingUsage} uses remaining for this promotion");

        if ((ValidTo - DateTime.UtcNow).TotalDays <= 7)
            warnings.Add($"Promotion expires on {ValidTo:yyyy-MM-dd}");

        return new PromotionValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings,
            EstimatedDiscount = errors.Any() ? 0 : CalculateDiscount(purchaseAmount),
            Terms = Terms?.Split('\n').ToList() ?? new List<string>()
        };
    }

    public decimal CalculateDiscount(decimal purchaseAmount)
    {
        if (!IsCurrentlyValid || HasReachedMaxUsage)
            return 0;

        var discount = Type switch
        {
            PromotionType.Percentage => purchaseAmount * (Value / 100m),
            PromotionType.FixedAmount => Value,
            PromotionType.BuyOneGetOne => purchaseAmount * 0.5m,
            PromotionType.FreeUpgrade => 0m, // Handled separately
            _ => 0m
        };

        // Apply maximum discount limit
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            discount = MaxDiscountAmount.Value;

        // Ensure discount doesn't exceed purchase amount
        return Math.Min(discount, purchaseAmount);
    }

    public PromotionUsage RecordUsage(
        Guid? customerId,
        string? guestId,
        Guid bookingId,
        decimal purchaseAmount,
        decimal discountAmount,
        string usedBy)
    {
        if (!IsCurrentlyValid)
            throw new InvalidOperationException("Cannot record usage for invalid promotion");

        if (HasReachedMaxUsage)
            throw new InvalidOperationException("Promotion usage limit has been reached");

        var usage = PromotionUsage.Create(
            Id,
            customerId,
            guestId,
            bookingId,
            purchaseAmount,
            discountAmount,
            usedBy);

        _usages.Add(usage);
        CurrentTotalUsage++;
        UpdatedAt = DateTime.UtcNow;

        return usage;
    }

    public void Activate(string activatedBy)
    {
        IsActive = true;
        ModifiedBy = activatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        ModifiedBy = deactivatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateUsageLimits(int? maxTotalUsage, int? maxUsagePerCustomer, int? maxUsagePerDay, string modifiedBy)
    {
        MaxTotalUsage = maxTotalUsage;
        MaxUsagePerCustomer = maxUsagePerCustomer;
        MaxUsagePerDay = maxUsagePerDay;
        ModifiedBy = modifiedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExtendValidity(DateTime newValidTo, string modifiedBy)
    {
        if (newValidTo <= ValidTo)
            throw new ArgumentException("New validity date must be after current validity date");

        ValidTo = newValidTo;
        ModifiedBy = modifiedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateCreationParameters(string code, string name, decimal value, DateTime validFrom, DateTime validTo, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Promotion code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Promotion name is required", nameof(name));

        if (value <= 0)
            throw new ArgumentException("Promotion value must be greater than zero", nameof(value));

        if (validFrom >= validTo)
            throw new ArgumentException("Valid from date must be before valid to date", nameof(validFrom));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));
    }
}

public class PromotionUsage : BaseEntity
{
    public Guid PromotionId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? GuestId { get; private set; }
    public Guid BookingId { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DateTime UsedAt { get; private set; }
    public string UsedBy { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation properties
    public virtual Promotion Promotion { get; set; } = null!;

    // Private constructor for EF Core
    private PromotionUsage() { }

    public static PromotionUsage Create(
        Guid promotionId,
        Guid? customerId,
        string? guestId,
        Guid bookingId,
        decimal purchaseAmount,
        decimal discountAmount,
        string usedBy,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new PromotionUsage
        {
            Id = Guid.NewGuid(),
            PromotionId = promotionId,
            CustomerId = customerId,
            GuestId = guestId,
            BookingId = bookingId,
            PurchaseAmount = purchaseAmount,
            DiscountAmount = discountAmount,
            UsedAt = DateTime.UtcNow,
            UsedBy = usedBy,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public record PromotionValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public decimal EstimatedDiscount { get; init; }
    public List<string> Terms { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public enum PromotionType
{
    Percentage,
    FixedAmount,
    BuyOneGetOne,
    FreeUpgrade,
    FreeService,
    EarlyBird,
    LastMinute,
    Seasonal,
    VolumeDiscount
}
