namespace FlightBooking.Application.Pricing.Queries;

/// <summary>
/// Query for calculating flight pricing
/// </summary>
public class CalculatePricingQuery
{
    public Guid FlightId { get; set; }
    public int PassengerCount { get; set; } = 1;
    public string FareClass { get; set; } = "Economy";
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public string? PromoCode { get; set; }
    public List<FlightBooking.Domain.Pricing.ExtraService> ExtraServices { get; set; } = new();
    public bool IncludeTaxes { get; set; } = true;
    public bool IncludeFees { get; set; } = true;
    public string Currency { get; set; } = "USD";
    public Guid? CustomerId { get; set; }
    public string? GuestId { get; set; }
    public bool IsRoundTrip { get; set; } = false;
    public Guid? ReturnFlightId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
