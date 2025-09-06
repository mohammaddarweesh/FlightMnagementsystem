using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Flights;

public class Seat : BaseEntity
{
    public Guid FlightId { get; set; }
    public Guid FareClassId { get; set; }
    public string SeatNumber { get; set; } = string.Empty; // e.g., "12A", "1F"
    public string Row { get; set; } = string.Empty; // e.g., "12", "1"
    public string Column { get; set; } = string.Empty; // e.g., "A", "F"
    public SeatType Type { get; set; } = SeatType.Standard;
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public decimal? ExtraFee { get; set; } // Additional fee for premium seats
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Flight Flight { get; set; } = null!;
    public virtual FareClass FareClass { get; set; } = null!;

    // Computed properties
    public bool IsAvailable => Status == SeatStatus.Available && IsActive;
    public bool IsWindow => Column.ToUpper() is "A" or "F" or "K";
    public bool IsAisle => Column.ToUpper() is "C" or "D" or "G" or "H";
    public bool IsMiddle => !IsWindow && !IsAisle;
    public bool HasExtraFee => ExtraFee.HasValue && ExtraFee > 0;
    public decimal TotalPrice => FareClass?.CurrentPrice ?? 0 + (ExtraFee ?? 0);

    // Helper methods
    public string GetSeatDescription()
    {
        var description = SeatNumber;
        
        if (IsWindow) description += " (Window)";
        else if (IsAisle) description += " (Aisle)";
        else if (IsMiddle) description += " (Middle)";

        if (Type != SeatType.Standard)
            description += $" - {Type}";

        return description;
    }

    public bool CanBeSelected()
    {
        return IsActive && Status == SeatStatus.Available;
    }

    public void Reserve()
    {
        if (!CanBeSelected())
            throw new InvalidOperationException($"Seat {SeatNumber} cannot be reserved");

        Status = SeatStatus.Reserved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Occupy()
    {
        if (Status != SeatStatus.Reserved && Status != SeatStatus.Available)
            throw new InvalidOperationException($"Seat {SeatNumber} cannot be occupied");

        Status = SeatStatus.Occupied;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        if (Status == SeatStatus.Occupied)
            throw new InvalidOperationException($"Cannot release occupied seat {SeatNumber}");

        Status = SeatStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Block(string reason)
    {
        Status = SeatStatus.Blocked;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Seat Create(
        Guid flightId,
        Guid fareClassId,
        string seatNumber,
        SeatType type = SeatType.Standard,
        decimal? extraFee = null)
    {
        var seat = new Seat
        {
            FlightId = flightId,
            FareClassId = fareClassId,
            SeatNumber = seatNumber.ToUpper(),
            Type = type,
            Status = SeatStatus.Available,
            ExtraFee = extraFee,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Extract row and column from seat number
        var rowMatch = System.Text.RegularExpressions.Regex.Match(seatNumber, @"(\d+)");
        var columnMatch = System.Text.RegularExpressions.Regex.Match(seatNumber, @"([A-Z])");

        if (rowMatch.Success) seat.Row = rowMatch.Groups[1].Value;
        if (columnMatch.Success) seat.Column = columnMatch.Groups[1].Value;

        return seat;
    }

    public static List<Seat> GenerateSeatsForFareClass(
        Guid flightId,
        FareClass fareClass,
        int startRow,
        string[] columns)
    {
        var seats = new List<Seat>();
        var seatsPerRow = columns.Length;
        var totalRows = (int)Math.Ceiling((double)fareClass.Capacity / seatsPerRow);

        for (int row = startRow; row < startRow + totalRows; row++)
        {
            for (int col = 0; col < columns.Length && seats.Count < fareClass.Capacity; col++)
            {
                var seatNumber = $"{row}{columns[col]}";
                var seatType = DetermineSeatType(fareClass.ClassName, row, columns[col], startRow);
                var extraFee = CalculateExtraFee(seatType, fareClass.ClassName);

                seats.Add(Create(flightId, fareClass.Id, seatNumber, seatType, extraFee));
            }
        }

        return seats;
    }

    private static SeatType DetermineSeatType(string className, int row, string column, int startRow)
    {
        // First row is usually premium
        if (row == startRow)
            return SeatType.Premium;

        // Exit row seats (typically rows with extra legroom)
        if (row % 10 == 0 || row % 15 == 0)
            return SeatType.ExitRow;

        // Window and aisle seats in business/first class
        if (className.Contains("Business") || className.Contains("First"))
        {
            if (column is "A" or "F")
                return SeatType.Premium;
        }

        return SeatType.Standard;
    }

    private static decimal? CalculateExtraFee(SeatType seatType, string className)
    {
        return seatType switch
        {
            SeatType.Premium when className.Contains("Economy") => 25m,
            SeatType.ExitRow when className.Contains("Economy") => 15m,
            SeatType.Premium when className.Contains("Premium") => 50m,
            _ => null
        };
    }
}

public enum SeatType
{
    Standard = 0,
    Premium = 1,
    ExitRow = 2,
    Bulkhead = 3,
    Bassinet = 4
}

public enum SeatStatus
{
    Available = 0,
    Reserved = 1,
    Occupied = 2,
    Blocked = 3,
    Maintenance = 4
}
