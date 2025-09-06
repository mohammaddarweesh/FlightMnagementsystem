namespace FlightBooking.Domain.Bookings;

public class BookingPolicies
{
    public CancellationPolicy CancellationPolicy { get; set; } = new();
    public ModificationPolicy ModificationPolicy { get; set; } = new();
    public RefundPolicy RefundPolicy { get; set; } = new();
    public CheckInPolicy CheckInPolicy { get; set; } = new();
    public SeatSelectionPolicy SeatSelectionPolicy { get; set; } = new();
    public BaggagePolicy BaggagePolicy { get; set; } = new();

    public decimal CalculateCancellationFee(double hoursUntilDeparture, decimal totalPaid, string fareClass)
    {
        return CancellationPolicy.CalculateFee(hoursUntilDeparture, totalPaid, fareClass);
    }

    public decimal GetProcessingFee(string fareClass)
    {
        return CancellationPolicy.GetProcessingFee(fareClass);
    }

    public bool CanModify(double hoursUntilDeparture, string fareClass, BookingModificationType modificationType)
    {
        return ModificationPolicy.CanModify(hoursUntilDeparture, fareClass, modificationType);
    }

    public decimal CalculateModificationFee(BookingModificationType modificationType, string fareClass, decimal? costDifference = null)
    {
        return ModificationPolicy.CalculateFee(modificationType, fareClass, costDifference);
    }

    public bool IsRefundable(string fareClass, double hoursUntilDeparture)
    {
        return RefundPolicy.IsRefundable(fareClass, hoursUntilDeparture);
    }

    public decimal CalculateRefundAmount(decimal totalPaid, string fareClass, double hoursUntilDeparture, CancellationReason reason)
    {
        return RefundPolicy.CalculateRefundAmount(totalPaid, fareClass, hoursUntilDeparture, reason);
    }
}

public class CancellationPolicy
{
    public Dictionary<string, CancellationRule> FareClassRules { get; set; } = new()
    {
        ["ECONOMY"] = new CancellationRule
        {
            FareClass = "ECONOMY",
            IsRefundable = true,
            ProcessingFee = 25m,
            CancellationFeeRules = new List<CancellationFeeRule>
            {
                new() { HoursBeforeDeparture = 24, FeePercentage = 0m, FlatFee = 0m },
                new() { HoursBeforeDeparture = 2, FeePercentage = 25m, FlatFee = 50m },
                new() { HoursBeforeDeparture = 0, FeePercentage = 100m, FlatFee = 0m }
            }
        },
        ["PREMIUM_ECONOMY"] = new CancellationRule
        {
            FareClass = "PREMIUM_ECONOMY",
            IsRefundable = true,
            ProcessingFee = 15m,
            CancellationFeeRules = new List<CancellationFeeRule>
            {
                new() { HoursBeforeDeparture = 48, FeePercentage = 0m, FlatFee = 0m },
                new() { HoursBeforeDeparture = 24, FeePercentage = 15m, FlatFee = 25m },
                new() { HoursBeforeDeparture = 2, FeePercentage = 50m, FlatFee = 75m },
                new() { HoursBeforeDeparture = 0, FeePercentage = 100m, FlatFee = 0m }
            }
        },
        ["BUSINESS"] = new CancellationRule
        {
            FareClass = "BUSINESS",
            IsRefundable = true,
            ProcessingFee = 0m,
            CancellationFeeRules = new List<CancellationFeeRule>
            {
                new() { HoursBeforeDeparture = 72, FeePercentage = 0m, FlatFee = 0m },
                new() { HoursBeforeDeparture = 24, FeePercentage = 10m, FlatFee = 0m },
                new() { HoursBeforeDeparture = 2, FeePercentage = 25m, FlatFee = 50m },
                new() { HoursBeforeDeparture = 0, FeePercentage = 75m, FlatFee = 0m }
            }
        },
        ["FIRST"] = new CancellationRule
        {
            FareClass = "FIRST",
            IsRefundable = true,
            ProcessingFee = 0m,
            CancellationFeeRules = new List<CancellationFeeRule>
            {
                new() { HoursBeforeDeparture = 168, FeePercentage = 0m, FlatFee = 0m }, // 7 days
                new() { HoursBeforeDeparture = 24, FeePercentage = 5m, FlatFee = 0m },
                new() { HoursBeforeDeparture = 2, FeePercentage = 15m, FlatFee = 0m },
                new() { HoursBeforeDeparture = 0, FeePercentage = 50m, FlatFee = 0m }
            }
        }
    };

    public decimal CalculateFee(double hoursUntilDeparture, decimal totalPaid, string fareClass)
    {
        if (!FareClassRules.TryGetValue(fareClass.ToUpper(), out var rule))
        {
            rule = FareClassRules["ECONOMY"]; // Default to economy rules
        }

        if (!rule.IsRefundable)
        {
            return totalPaid; // Non-refundable, full amount as fee
        }

        var applicableRule = rule.CancellationFeeRules
            .Where(r => hoursUntilDeparture <= r.HoursBeforeDeparture)
            .OrderBy(r => r.HoursBeforeDeparture)
            .FirstOrDefault();

        if (applicableRule == null)
        {
            return 0m; // No applicable rule, no fee
        }

        var percentageFee = totalPaid * (applicableRule.FeePercentage / 100m);
        var totalFee = percentageFee + applicableRule.FlatFee;

        return Math.Min(totalFee, totalPaid); // Fee cannot exceed total paid
    }

    public decimal GetProcessingFee(string fareClass)
    {
        if (FareClassRules.TryGetValue(fareClass.ToUpper(), out var rule))
        {
            return rule.ProcessingFee;
        }
        return FareClassRules["ECONOMY"].ProcessingFee;
    }
}

public class ModificationPolicy
{
    public Dictionary<string, ModificationRule> FareClassRules { get; set; } = new()
    {
        ["ECONOMY"] = new ModificationRule
        {
            FareClass = "ECONOMY",
            AllowedModifications = new Dictionary<BookingModificationType, ModificationFeeRule>
            {
                [BookingModificationType.DatesChanged] = new() { FlatFee = 150m, MinHoursBeforeDeparture = 24 },
                [BookingModificationType.PassengerUpdated] = new() { FlatFee = 50m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.SeatChanged] = new() { FlatFee = 25m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.ExtraAdded] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.ContactUpdated] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 0 }
            }
        },
        ["BUSINESS"] = new ModificationRule
        {
            FareClass = "BUSINESS",
            AllowedModifications = new Dictionary<BookingModificationType, ModificationFeeRule>
            {
                [BookingModificationType.DatesChanged] = new() { FlatFee = 75m, MinHoursBeforeDeparture = 24 },
                [BookingModificationType.PassengerUpdated] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.SeatChanged] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.ExtraAdded] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.ContactUpdated] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 0 }
            }
        },
        ["FIRST"] = new ModificationRule
        {
            FareClass = "FIRST",
            AllowedModifications = new Dictionary<BookingModificationType, ModificationFeeRule>
            {
                [BookingModificationType.DatesChanged] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 24 },
                [BookingModificationType.PassengerUpdated] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.SeatChanged] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.ExtraAdded] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 2 },
                [BookingModificationType.ContactUpdated] = new() { FlatFee = 0m, MinHoursBeforeDeparture = 0 }
            }
        }
    };

    public bool CanModify(double hoursUntilDeparture, string fareClass, BookingModificationType modificationType)
    {
        if (!FareClassRules.TryGetValue(fareClass.ToUpper(), out var rule))
        {
            rule = FareClassRules["ECONOMY"];
        }

        if (!rule.AllowedModifications.TryGetValue(modificationType, out var modificationRule))
        {
            return false; // Modification type not allowed
        }

        return hoursUntilDeparture >= modificationRule.MinHoursBeforeDeparture;
    }

    public decimal CalculateFee(BookingModificationType modificationType, string fareClass, decimal? costDifference = null)
    {
        if (!FareClassRules.TryGetValue(fareClass.ToUpper(), out var rule))
        {
            rule = FareClassRules["ECONOMY"];
        }

        if (!rule.AllowedModifications.TryGetValue(modificationType, out var modificationRule))
        {
            return 0m; // No fee if modification not allowed
        }

        var fee = modificationRule.FlatFee;

        // Add cost difference for upgrades
        if (costDifference.HasValue && costDifference.Value > 0)
        {
            fee += costDifference.Value;
        }

        return fee;
    }
}

public class RefundPolicy
{
    public bool IsRefundable(string fareClass, double hoursUntilDeparture)
    {
        // Business and First class are always refundable
        if (fareClass.ToUpper() is "BUSINESS" or "FIRST")
        {
            return true;
        }

        // Economy and Premium Economy have time restrictions
        return hoursUntilDeparture >= 24;
    }

    public decimal CalculateRefundAmount(decimal totalPaid, string fareClass, double hoursUntilDeparture, CancellationReason reason)
    {
        if (!IsRefundable(fareClass, hoursUntilDeparture))
        {
            return 0m;
        }

        // Full refund for airline-caused cancellations
        if (reason is CancellationReason.FlightCancelled or CancellationReason.FlightDelayed or CancellationReason.WeatherConditions)
        {
            return totalPaid;
        }

        // Calculate based on cancellation policy
        var cancellationPolicy = new CancellationPolicy();
        var cancellationFee = cancellationPolicy.CalculateFee(hoursUntilDeparture, totalPaid, fareClass);
        var processingFee = cancellationPolicy.GetProcessingFee(fareClass);

        return Math.Max(0, totalPaid - cancellationFee - processingFee);
    }
}

public class CheckInPolicy
{
    public int OnlineCheckInHoursBeforeDeparture { get; set; } = 24;
    public int CheckInClosesMinutesBeforeDeparture { get; set; } = 45;
    public bool RequiresDocumentVerification { get; set; } = true;
    public List<string> RequiredDocuments { get; set; } = new() { "Passport", "Visa" };
}

public class SeatSelectionPolicy
{
    public bool IsFreeForPremiumClasses { get; set; } = true;
    public decimal StandardSeatFee { get; set; } = 25m;
    public decimal PremiumSeatFee { get; set; } = 50m;
    public int FreeSelectionHoursBeforeDeparture { get; set; } = 24;
}

public class BaggagePolicy
{
    public int FreeCheckedBags { get; set; } = 1;
    public decimal ExtraBagFee { get; set; } = 75m;
    public int MaxWeightKg { get; set; } = 23;
    public decimal OverweightFeePerKg { get; set; } = 15m;
}

// Supporting classes
public class CancellationRule
{
    public string FareClass { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public decimal ProcessingFee { get; set; }
    public List<CancellationFeeRule> CancellationFeeRules { get; set; } = new();
}

public class CancellationFeeRule
{
    public double HoursBeforeDeparture { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal FlatFee { get; set; }
}

public class ModificationRule
{
    public string FareClass { get; set; } = string.Empty;
    public Dictionary<BookingModificationType, ModificationFeeRule> AllowedModifications { get; set; } = new();
}

public class ModificationFeeRule
{
    public decimal FlatFee { get; set; }
    public decimal FeePercentage { get; set; }
    public double MinHoursBeforeDeparture { get; set; }
    public bool RequiresApproval { get; set; }
}
