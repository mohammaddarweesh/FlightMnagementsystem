using FlightBooking.Domain.Promotions;

namespace FlightBooking.Application.Promotions.Services;

public interface IPromotionService
{
    /// <summary>
    /// Validates a promotion code atomically and returns validation result
    /// </summary>
    Task<PromotionValidationResult> ValidatePromotionAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        decimal purchaseAmount,
        string route,
        string fareClass,
        DateTime departureDate,
        DateTime bookingDate,
        bool isFirstTimeCustomer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records promotion usage atomically with concurrency control
    /// </summary>
    Task<PromotionUsageResult> RecordPromotionUsageAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        Guid bookingId,
        decimal purchaseAmount,
        decimal discountAmount,
        string usedBy,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and records promotion usage in a single atomic operation
    /// </summary>
    Task<PromotionApplicationResult> ValidateAndApplyPromotionAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        Guid bookingId,
        decimal purchaseAmount,
        string route,
        string fareClass,
        DateTime departureDate,
        DateTime bookingDate,
        bool isFirstTimeCustomer,
        string usedBy,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses a promotion usage (for booking cancellations)
    /// </summary>
    Task<bool> ReversePromotionUsageAsync(
        Guid bookingId,
        string reversedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available promotions for a customer/guest
    /// </summary>
    Task<List<AvailablePromotionDto>> GetAvailablePromotionsAsync(
        Guid? customerId,
        string? guestId,
        string route,
        string fareClass,
        DateTime departureDate,
        decimal? estimatedAmount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets promotion usage statistics
    /// </summary>
    Task<PromotionUsageStatistics> GetPromotionUsageStatisticsAsync(
        string promotionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer has reached their usage limit for a promotion
    /// </summary>
    Task<bool> HasCustomerReachedUsageLimitAsync(
        string promotionCode,
        Guid? customerId,
        string? guestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer's promotion usage history
    /// </summary>
    Task<List<CustomerPromotionUsageDto>> GetCustomerPromotionHistoryAsync(
        Guid? customerId,
        string? guestId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

public record PromotionUsageResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UsageId { get; init; }
    public decimal DiscountApplied { get; init; }
    public int RemainingUsage { get; init; }
    public DateTime UsedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record PromotionApplicationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
    public Guid? UsageId { get; init; }
    public decimal DiscountApplied { get; init; }
    public decimal FinalAmount { get; init; }
    public int RemainingUsage { get; init; }
    public DateTime? UsedAt { get; init; }
    public List<string> Terms { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record AvailablePromotionDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public PromotionType Type { get; init; }
    public decimal Value { get; init; }
    public decimal EstimatedDiscount { get; init; }
    public DateTime ValidTo { get; init; }
    public int? RemainingUsage { get; init; }
    public bool RequiresMinimumPurchase { get; init; }
    public decimal? MinimumPurchaseAmount { get; init; }
    public List<string> Terms { get; init; } = new();
    public string? MarketingMessage { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record PromotionUsageStatistics
{
    public string PromotionCode { get; init; } = string.Empty;
    public string PromotionName { get; init; } = string.Empty;
    public int TotalUsage { get; init; }
    public int? MaxUsage { get; init; }
    public int RemainingUsage { get; init; }
    public double UsagePercentage { get; init; }
    public decimal TotalDiscountGiven { get; init; }
    public decimal AverageDiscountPerUsage { get; init; }
    public DateTime? FirstUsedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public int UniqueCustomers { get; init; }
    public int UniqueGuests { get; init; }
    public Dictionary<string, int> UsageByDay { get; init; } = new();
    public Dictionary<string, int> UsageByRoute { get; init; } = new();
    public Dictionary<string, int> UsageByFareClass { get; init; } = new();
    public bool IsActive { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime ValidTo { get; init; }
}

public record CustomerPromotionUsageDto
{
    public Guid UsageId { get; init; }
    public string PromotionCode { get; init; } = string.Empty;
    public string PromotionName { get; init; } = string.Empty;
    public Guid BookingId { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public decimal PurchaseAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public DateTime UsedAt { get; init; }
    public string Route { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public bool IsReversed { get; init; }
    public DateTime? ReversedAt { get; init; }
    public string? ReversalReason { get; init; }
}

public interface IPromotionRepository
{
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);
    Task<List<Promotion>> GetPromotionsForCustomerAsync(Guid? customerId, string? guestId, CancellationToken cancellationToken = default);
    Task<Promotion> AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Atomic operations for concurrency control
    Task<bool> TryRecordUsageAsync(Promotion promotion, PromotionUsage usage, CancellationToken cancellationToken = default);
    Task<int> GetCustomerUsageCountAsync(string promotionCode, Guid? customerId, string? guestId, CancellationToken cancellationToken = default);
    Task<int> GetDailyUsageCountAsync(string promotionCode, DateTime date, CancellationToken cancellationToken = default);
    Task<List<PromotionUsage>> GetUsageHistoryAsync(string promotionCode, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<List<PromotionUsage>> GetCustomerUsageHistoryAsync(Guid? customerId, string? guestId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<bool> ReverseUsageAsync(Guid bookingId, string reversedBy, string? reason = null, CancellationToken cancellationToken = default);
}

public interface IPromotionCacheService
{
    Task<Promotion?> GetPromotionAsync(string code, CancellationToken cancellationToken = default);
    Task SetPromotionAsync(Promotion promotion, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task InvalidatePromotionAsync(string code, CancellationToken cancellationToken = default);
    Task<int?> GetUsageCountAsync(string promotionCode, string key, CancellationToken cancellationToken = default);
    Task SetUsageCountAsync(string promotionCode, string key, int count, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task IncrementUsageCountAsync(string promotionCode, string key, CancellationToken cancellationToken = default);
    Task DecrementUsageCountAsync(string promotionCode, string key, CancellationToken cancellationToken = default);
}

public interface IPromotionLockService
{
    Task<IDisposable?> AcquireLockAsync(string promotionCode, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IDisposable?> AcquireCustomerLockAsync(string promotionCode, Guid? customerId, string? guestId, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public class PromotionConcurrencyException : Exception
{
    public string PromotionCode { get; }
    public string Operation { get; }

    public PromotionConcurrencyException(string promotionCode, string operation, string message) 
        : base(message)
    {
        PromotionCode = promotionCode;
        Operation = operation;
    }

    public PromotionConcurrencyException(string promotionCode, string operation, string message, Exception innerException) 
        : base(message, innerException)
    {
        PromotionCode = promotionCode;
        Operation = operation;
    }
}

public class PromotionUsageLimitExceededException : Exception
{
    public string PromotionCode { get; }
    public string LimitType { get; }
    public int CurrentUsage { get; }
    public int MaxUsage { get; }

    public PromotionUsageLimitExceededException(string promotionCode, string limitType, int currentUsage, int maxUsage)
        : base($"Promotion {promotionCode} {limitType} usage limit exceeded: {currentUsage}/{maxUsage}")
    {
        PromotionCode = promotionCode;
        LimitType = limitType;
        CurrentUsage = currentUsage;
        MaxUsage = maxUsage;
    }
}

public class PromotionNotFoundException : Exception
{
    public string PromotionCode { get; }

    public PromotionNotFoundException(string promotionCode)
        : base($"Promotion with code '{promotionCode}' not found")
    {
        PromotionCode = promotionCode;
    }
}
