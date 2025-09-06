using FlightBooking.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Data;

public class FlightSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FlightSeeder> _logger;

    public FlightSeeder(ApplicationDbContext context, ILogger<FlightSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedAmenitiesAsync();
            await SeedAirportsAsync();
            await SeedRoutesAsync();
            await SeedFlightsAsync();

            _logger.LogInformation("Flight data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding flight data");
            throw;
        }
    }

    private async Task SeedAmenitiesAsync()
    {
        if (await _context.Amenities.AnyAsync())
        {
            _logger.LogInformation("Amenities already exist, skipping seeding");
            return;
        }

        var amenities = Amenity.GetStandardAmenities();
        _context.Amenities.AddRange(amenities);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} amenities", amenities.Count);
    }

    private async Task SeedAirportsAsync()
    {
        if (await _context.Airports.AnyAsync())
        {
            _logger.LogInformation("Airports already exist, skipping seeding");
            return;
        }

        var airports = new List<Airport>
        {
            Airport.Create("JFK", "KJFK", "John F. Kennedy International Airport", "New York", "United States", "US", 40.6413m, -73.7781m, 13, "America/New_York"),
            Airport.Create("LAX", "KLAX", "Los Angeles International Airport", "Los Angeles", "United States", "US", 33.9425m, -118.4081m, 125, "America/Los_Angeles"),
            Airport.Create("LHR", "EGLL", "London Heathrow Airport", "London", "United Kingdom", "GB", 51.4700m, -0.4543m, 83, "Europe/London"),
            Airport.Create("CDG", "LFPG", "Charles de Gaulle Airport", "Paris", "France", "FR", 49.0097m, 2.5479m, 392, "Europe/Paris"),
            Airport.Create("NRT", "RJAA", "Narita International Airport", "Tokyo", "Japan", "JP", 35.7647m, 140.3864m, 141, "Asia/Tokyo"),
            Airport.Create("SYD", "YSSY", "Sydney Kingsford Smith Airport", "Sydney", "Australia", "AU", -33.9399m, 151.1753m, 21, "Australia/Sydney"),
            Airport.Create("DXB", "OMDB", "Dubai International Airport", "Dubai", "United Arab Emirates", "AE", 25.2532m, 55.3657m, 62, "Asia/Dubai"),
            Airport.Create("SIN", "WSSS", "Singapore Changi Airport", "Singapore", "Singapore", "SG", 1.3644m, 103.9915m, 22, "Asia/Singapore"),
            Airport.Create("FRA", "EDDF", "Frankfurt Airport", "Frankfurt", "Germany", "DE", 50.0379m, 8.5622m, 364, "Europe/Berlin"),
            Airport.Create("ORD", "KORD", "O'Hare International Airport", "Chicago", "United States", "US", 41.9742m, -87.9073m, 672, "America/Chicago")
        };

        // Add descriptions
        airports[0].Description = "Primary international airport serving New York City";
        airports[1].Description = "Primary international airport serving Los Angeles";
        airports[2].Description = "Primary international airport serving London";
        airports[3].Description = "Primary international airport serving Paris";
        airports[4].Description = "Primary international airport serving Tokyo";
        airports[5].Description = "Primary international airport serving Sydney";
        airports[6].Description = "Primary international airport serving Dubai";
        airports[7].Description = "Primary international airport serving Singapore";
        airports[8].Description = "Primary international airport serving Frankfurt";
        airports[9].Description = "Primary international airport serving Chicago";

        _context.Airports.AddRange(airports);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} airports", airports.Count);
    }

    private async Task SeedRoutesAsync()
    {
        if (await _context.Routes.AnyAsync())
        {
            _logger.LogInformation("Routes already exist, skipping seeding");
            return;
        }

        var airports = await _context.Airports.ToListAsync();
        var routes = new List<Route>();

        // Popular international routes
        var routeData = new[]
        {
            ("JFK", "LHR", 5585, TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(30))),
            ("LAX", "NRT", 8815, TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(45))),
            ("LHR", "CDG", 344, TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(25))),
            ("CDG", "FRA", 479, TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(35))),
            ("NRT", "SIN", 5312, TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(20))),
            ("SYD", "SIN", 6317, TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(15))),
            ("DXB", "LHR", 5499, TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(45))),
            ("SIN", "DXB", 6837, TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(55))),
            ("JFK", "LAX", 3983, TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(15))),
            ("ORD", "JFK", 1185, TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(45))),
            ("LAX", "SYD", 12051, TimeSpan.FromHours(15).Add(TimeSpan.FromMinutes(30))),
            ("FRA", "NRT", 9560, TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(55)))
        };

        foreach (var (depIata, arrIata, distance, flightTime) in routeData)
        {
            var depAirport = airports.First(a => a.IataCode == depIata);
            var arrAirport = airports.First(a => a.IataCode == arrIata);

            var route = Route.Create(
                depAirport.Id,
                arrAirport.Id,
                distance,
                flightTime,
                depAirport.IsInternational(arrAirport));

            route.UpdateRouteCode(depIata, arrIata);
            route.Description = $"Route from {depAirport.GetDisplayName()} to {arrAirport.GetDisplayName()}";
            routes.Add(route);

            // Add reverse route
            var reverseRoute = Route.Create(
                arrAirport.Id,
                depAirport.Id,
                distance,
                flightTime,
                arrAirport.IsInternational(depAirport));

            reverseRoute.UpdateRouteCode(arrIata, depIata);
            reverseRoute.Description = $"Route from {arrAirport.GetDisplayName()} to {depAirport.GetDisplayName()}";
            routes.Add(reverseRoute);
        }

        _context.Routes.AddRange(routes);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} routes", routes.Count);
    }

    private async Task SeedFlightsAsync()
    {
        if (await _context.Flights.AnyAsync())
        {
            _logger.LogInformation("Flights already exist, skipping seeding");
            return;
        }

        if (await _context.Amenities.CountAsync() == 0)
        {
            _logger.LogWarning("No amenities found, skipping flight seeding");
            return;
        }

        var routes = await _context.Routes.Include(r => r.DepartureAirport).Include(r => r.ArrivalAirport).ToListAsync();
        var amenities = await _context.Amenities.ToListAsync();
        var flights = new List<Flight>();

        var airlines = new[]
        {
            ("AA", "American Airlines"),
            ("DL", "Delta Air Lines"),
            ("UA", "United Airlines"),
            ("BA", "British Airways"),
            ("LH", "Lufthansa"),
            ("AF", "Air France"),
            ("JL", "Japan Airlines"),
            ("QF", "Qantas"),
            ("EK", "Emirates"),
            ("SQ", "Singapore Airlines")
        };

        var aircraftTypes = new[]
        {
            "Boeing 737-800",
            "Boeing 777-300ER",
            "Airbus A320",
            "Airbus A350-900",
            "Boeing 787-9"
        };

        var random = new Random(42); // Fixed seed for consistent data

        // Create flights for the next 90 days
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = startDate.AddDays(90);

        foreach (var route in routes.Take(12)) // Limit to first 12 routes for demo
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Skip some days randomly to simulate realistic scheduling
                if (random.Next(1, 8) == 1) continue; // Skip ~14% of days

                var airline = airlines[random.Next(airlines.Length)];
                var aircraft = aircraftTypes[random.Next(aircraftTypes.Length)];
                var flightNumber = random.Next(100, 9999).ToString();

                // Generate realistic departure times
                var departureHour = random.Next(6, 23); // 6 AM to 11 PM
                var departureMinute = random.Next(0, 4) * 15; // 0, 15, 30, 45 minutes
                var departureTime = new TimeSpan(departureHour, departureMinute, 0);

                var arrivalTime = departureTime.Add(route.EstimatedFlightTime);
                if (arrivalTime.TotalDays >= 1)
                {
                    arrivalTime = TimeSpan.FromTicks(arrivalTime.Ticks % TimeSpan.TicksPerDay);
                }

                var flight = Flight.Create(
                    flightNumber,
                    route.Id,
                    airline.Item1,
                    airline.Item2,
                    aircraft,
                    date,
                    departureTime,
                    arrivalTime);

                flights.Add(flight);
            }
        }

        _context.Flights.AddRange(flights);
        await _context.SaveChangesAsync();

        // Create fare classes and seats for each flight
        foreach (var flight in flights)
        {
            var fareClasses = FareClass.CreateStandardClasses(flight.Id, flight.AircraftType);
            _context.FareClasses.AddRange(fareClasses);
        }

        await _context.SaveChangesAsync();

        // Attach amenities to fare classes
        var allFareClasses = await _context.FareClasses.ToListAsync();
        foreach (var fareClass in allFareClasses)
        {
            var amenityIds = GetAmenitiesForFareClass(fareClass.ClassName, amenities);
            foreach (var amenityId in amenityIds)
            {
                var fareClassAmenity = FareClassAmenity.Create(fareClass.Id, amenityId);
                _context.FareClassAmenities.Add(fareClassAmenity);
            }
        }

        await _context.SaveChangesAsync();

        // Generate seats for each flight (grouped by flight to avoid duplicates)
        var flightGroups = allFareClasses.GroupBy(fc => fc.FlightId);
        foreach (var flightGroup in flightGroups)
        {
            var seats = GenerateSeatsForFlight(flightGroup.ToList());
            _context.Seats.AddRange(seats);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} flights with fare classes and seats", flights.Count);
    }

    private List<Guid> GetAmenitiesForFareClass(string className, List<Amenity> amenities)
    {
        var amenityIds = new List<Guid>();

        switch (className.ToLower())
        {
            case "economy":
                amenityIds.AddRange(amenities.Where(a => a.Name.Contains("Snack") || a.Name.Contains("USB")).Select(a => a.Id));
                break;
            case "premium economy":
                amenityIds.AddRange(amenities.Where(a => a.Name.Contains("Extra Legroom") || a.Name.Contains("Complimentary Drinks") || a.Name.Contains("Power Outlets")).Select(a => a.Id));
                break;
            case "business":
                amenityIds.AddRange(amenities.Where(a => a.Name.Contains("Lie-Flat") || a.Name.Contains("Premium Dining") || a.Name.Contains("Lounge Access")).Select(a => a.Id));
                break;
            case "first":
                amenityIds.AddRange(amenities.Where(a => a.Name.Contains("Concierge") || a.Name.Contains("Premium") || a.Name.Contains("Priority")).Select(a => a.Id));
                break;
        }

        return amenityIds;
    }

    private List<Seat> GenerateSeatsForFlight(List<FareClass> fareClasses)
    {
        var allSeats = new List<Seat>();
        var currentRow = 1;

        // Sort fare classes by sort order to ensure proper row allocation
        var sortedFareClasses = fareClasses.OrderBy(fc => fc.SortOrder).ToList();

        foreach (var fareClass in sortedFareClasses)
        {
            var columns = fareClass.ClassName.ToLower() switch
            {
                var name when name.Contains("first") => new[] { "A", "B", "E", "F" },
                var name when name.Contains("business") => new[] { "A", "C", "D", "F" },
                _ => new[] { "A", "B", "C", "D", "E", "F" }
            };

            var seatsPerRow = columns.Length;
            var rowsNeeded = (int)Math.Ceiling((double)fareClass.Capacity / seatsPerRow);

            for (int row = currentRow; row < currentRow + rowsNeeded; row++)
            {
                for (int col = 0; col < columns.Length && allSeats.Count(s => s.FareClassId == fareClass.Id) < fareClass.Capacity; col++)
                {
                    var seatNumber = $"{row}{columns[col]}";
                    var seatType = DetermineSeatType(fareClass.ClassName, row, columns[col], currentRow);
                    var extraFee = CalculateExtraFee(seatType, fareClass.ClassName);

                    var seat = Seat.Create(fareClass.FlightId, fareClass.Id, seatNumber, seatType, extraFee);
                    allSeats.Add(seat);
                }
            }

            currentRow += rowsNeeded;
        }

        return allSeats;
    }

    private SeatType DetermineSeatType(string className, int row, string column, int startRow)
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

    private decimal? CalculateExtraFee(SeatType seatType, string className)
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
