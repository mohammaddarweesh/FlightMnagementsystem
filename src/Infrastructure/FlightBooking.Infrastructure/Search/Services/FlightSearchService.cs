using FlightBooking.Application.Search.Services;
using FlightBooking.Domain.Flights;
using FlightBooking.Domain.Search;
using FlightBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlightBooking.Infrastructure.Search.Services;

public class FlightSearchService : IFlightSearchService
{
    private readonly ApplicationDbContext _context;
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ILogger<FlightSearchService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public FlightSearchService(
        ApplicationDbContext context,
        IFlightSearchCacheService cacheService,
        ILogger<FlightSearchService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<FlightSearchResult> SearchFlightsAsync(
        FlightSearchCriteria criteria,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            criteria.Validate();
            
            var cacheKey = criteria.GetCacheKey();
            var cachedResult = await _cacheService.GetSearchResultsAsync(cacheKey, cancellationToken);
            
            // Check ETag for conditional requests
            if (cachedResult != null && !string.IsNullOrEmpty(ifNoneMatch))
            {
                if (ifNoneMatch == cachedResult.ETag)
                {
                    _logger.LogDebug("ETag match for search request, returning 304");
                    var notModifiedResult = JsonSerializer.Deserialize<FlightSearchResult>(cachedResult.Data, _jsonOptions)!;
                    notModifiedResult.FromCache = true;
                    return notModifiedResult;
                }
            }

            // Return cached result if available and not expired
            if (cachedResult != null && cachedResult.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Returning cached search results for key: {CacheKey}", cacheKey);
                var result = JsonSerializer.Deserialize<FlightSearchResult>(cachedResult.Data, _jsonOptions)!;
                result.FromCache = true;
                return result;
            }

            // Perform fresh search
            var searchResult = await PerformFlightSearchAsync(criteria, cancellationToken);
            searchResult.GeneratedAt = DateTime.UtcNow;
            searchResult.FromCache = false;
            searchResult.Metadata.SearchDuration = DateTime.UtcNow - startTime;

            // Generate ETag
            searchResult.ETag = GenerateETag(searchResult);

            // Cache the result
            await CacheSearchResultAsync(cacheKey, searchResult, criteria, cancellationToken);

            return searchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing flight search");
            throw;
        }
    }

    public async Task<List<FlightAvailability>> GetFlightAvailabilityAsync(
        List<Guid> flightIds,
        int passengerCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var flights = await _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FareClasses.Where(fc => fc.IsActive))
                .Include(f => f.Seats.Where(s => s.IsActive))
                .Include(f => f.FareClasses)
                    .ThenInclude(fc => fc.FareClassAmenities)
                        .ThenInclude(fca => fca.Amenity)
                .Where(f => flightIds.Contains(f.Id) && f.IsActive)
                .ToListAsync(cancellationToken);

            var availability = new List<FlightAvailability>();

            foreach (var flight in flights)
            {
                var flightAvailability = await BuildFlightAvailabilityAsync(flight, passengerCount, cancellationToken);
                if (flightAvailability.HasAvailability)
                {
                    availability.Add(flightAvailability);
                }
            }

            return availability;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight availability for flights: {FlightIds}", string.Join(",", flightIds));
            throw;
        }
    }

    public async Task<RouteAvailabilitySummary> GetRouteAvailabilityAsync(
        string departureAirport,
        string arrivalAirport,
        DateTime startDate,
        DateTime endDate,
        int passengerCount = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var flights = await _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FareClasses.Where(fc => fc.IsActive))
                .Include(f => f.Seats.Where(s => s.IsActive))
                .Where(f => f.Route.DepartureAirport.IataCode == departureAirport.ToUpper() &&
                           f.Route.ArrivalAirport.IataCode == arrivalAirport.ToUpper() &&
                           f.DepartureDate.Date >= startDate.Date &&
                           f.DepartureDate.Date <= endDate.Date &&
                           f.IsActive)
                .ToListAsync(cancellationToken);

            var summary = new RouteAvailabilitySummary
            {
                DepartureAirport = departureAirport.ToUpper(),
                ArrivalAirport = arrivalAirport.ToUpper(),
                StartDate = startDate,
                EndDate = endDate,
                TotalFlights = flights.Count
            };

            var dailyAvailability = new List<DailyAvailability>();
            var allPrices = new List<decimal>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dailyFlights = flights.Where(f => f.DepartureDate.Date == date).ToList();
                var availableFlights = new List<Flight>();
                var dailyPrices = new List<decimal>();
                var totalSeats = 0;
                var availableSeats = 0;

                foreach (var flight in dailyFlights)
                {
                    var hasAvailability = false;
                    foreach (var fareClass in flight.FareClasses)
                    {
                        var classAvailableSeats = flight.Seats.Count(s => s.FareClassId == fareClass.Id && s.Status == SeatStatus.Available);
                        totalSeats += fareClass.Capacity;
                        availableSeats += classAvailableSeats;

                        if (classAvailableSeats >= passengerCount)
                        {
                            hasAvailability = true;
                            dailyPrices.Add(fareClass.CurrentPrice);
                        }
                    }

                    if (hasAvailability)
                    {
                        availableFlights.Add(flight);
                    }
                }

                var daily = new DailyAvailability
                {
                    Date = date,
                    FlightCount = dailyFlights.Count,
                    AvailableFlights = availableFlights.Count,
                    TotalSeats = totalSeats,
                    AvailableSeats = availableSeats,
                    MinPrice = dailyPrices.Any() ? dailyPrices.Min() : 0,
                    MaxPrice = dailyPrices.Any() ? dailyPrices.Max() : 0,
                    AveragePrice = dailyPrices.Any() ? dailyPrices.Average() : 0
                };

                dailyAvailability.Add(daily);
                allPrices.AddRange(dailyPrices);
            }

            summary.DailyAvailability = dailyAvailability;
            summary.AvailableFlights = dailyAvailability.Sum(d => d.AvailableFlights);
            summary.MinPrice = allPrices.Any() ? allPrices.Min() : 0;
            summary.MaxPrice = allPrices.Any() ? allPrices.Max() : 0;
            summary.AveragePrice = allPrices.Any() ? allPrices.Average() : 0;

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route availability for {Route} from {StartDate} to {EndDate}", 
                $"{departureAirport}-{arrivalAirport}", startDate, endDate);
            throw;
        }
    }

    public async Task<List<PopularRoute>> GetPopularRoutesAsync(
        string? fromAirport = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .Include(f => f.FareClasses.Where(fc => fc.IsActive))
                .Where(f => f.IsActive && f.DepartureDate >= DateTime.Today);

            if (!string.IsNullOrEmpty(fromAirport))
            {
                query = query.Where(f => f.Route.DepartureAirport.IataCode == fromAirport.ToUpper());
            }

            var routes = await query
                .GroupBy(f => new { f.Route.DepartureAirportId, f.Route.ArrivalAirportId })
                .Select(g => new
                {
                    DepartureAirport = g.First().Route.DepartureAirport,
                    ArrivalAirport = g.First().Route.ArrivalAirport,
                    FlightCount = g.Count(),
                    MinPrice = g.SelectMany(f => f.FareClasses).Min(fc => fc.CurrentPrice),
                    AveragePrice = g.SelectMany(f => f.FareClasses).Average(fc => fc.CurrentPrice)
                })
                .OrderByDescending(r => r.FlightCount)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return routes.Select(r => new PopularRoute
            {
                DepartureAirport = r.DepartureAirport.IataCode,
                DepartureAirportName = r.DepartureAirport.Name,
                ArrivalAirport = r.ArrivalAirport.IataCode,
                ArrivalAirportName = r.ArrivalAirport.Name,
                FlightCount = r.FlightCount,
                MinPrice = r.MinPrice,
                AveragePrice = r.AveragePrice,
                PopularityScore = CalculatePopularityScore(r.FlightCount, r.AveragePrice)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular routes");
            throw;
        }
    }

    public async Task<FlightSearchSuggestions> GetSearchSuggestionsAsync(
        string? departureAirport = null,
        string? arrivalAirport = null,
        DateTime? departureDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = new FlightSearchSuggestions();

            // Get airport suggestions
            if (string.IsNullOrEmpty(departureAirport))
            {
                suggestions.DepartureAirports = await GetAirportSuggestionsAsync(null, cancellationToken);
            }

            if (string.IsNullOrEmpty(arrivalAirport))
            {
                suggestions.ArrivalAirports = await GetAirportSuggestionsAsync(departureAirport, cancellationToken);
            }

            // Get date suggestions
            if (!departureDate.HasValue)
            {
                suggestions.DepartureDates = await GetDateSuggestionsAsync(departureAirport, arrivalAirport, cancellationToken);
            }

            // Get popular routes
            suggestions.PopularRoutes = await GetRouteSuggestionsAsync(departureAirport, cancellationToken);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            throw;
        }
    }

    public async Task<List<FlightPricing>> CalculateDynamicPricingAsync(
        List<Guid> flightIds,
        DateTime searchDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var flights = await _context.Flights
                .Include(f => f.FareClasses.Where(fc => fc.IsActive))
                .Include(f => f.Seats.Where(s => s.IsActive))
                .Where(f => flightIds.Contains(f.Id) && f.IsActive)
                .ToListAsync(cancellationToken);

            var pricing = new List<FlightPricing>();

            foreach (var flight in flights)
            {
                var flightPricing = new FlightPricing
                {
                    FlightId = flight.Id,
                    FlightNumber = $"{flight.AirlineCode}{flight.FlightNumber}",
                    PricingCalculatedAt = DateTime.UtcNow
                };

                var fareClassPricing = new List<FareClassPricing>();

                foreach (var fareClass in flight.FareClasses)
                {
                    var occupancyRate = CalculateOccupancyRate(flight, fareClass);
                    var demandScore = CalculateDemandScore(flight, searchDate);
                    var dynamicPrice = CalculateDynamicPrice(fareClass.CurrentPrice, occupancyRate, demandScore);

                    fareClassPricing.Add(new FareClassPricing
                    {
                        FareClassId = fareClass.Id,
                        ClassName = fareClass.ClassName,
                        BasePrice = fareClass.BasePrice,
                        CurrentPrice = fareClass.CurrentPrice,
                        DynamicPrice = dynamicPrice,
                        OccupancyRate = occupancyRate,
                        DemandScore = demandScore,
                        PricingReason = GetPricingReason(occupancyRate, demandScore)
                    });
                }

                flightPricing.FareClasses = fareClassPricing;
                flightPricing.DemandMultiplier = fareClassPricing.Any() ?
                    fareClassPricing.Average(fc => fc.DynamicPrice / fc.CurrentPrice) : 1.0m;

                pricing.Add(flightPricing);
            }

            return pricing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dynamic pricing for flights: {FlightIds}", string.Join(",", flightIds));
            throw;
        }
    }

    public async Task InvalidateSearchCacheAsync(
        string? departureAirport = null,
        string? arrivalAirport = null,
        DateTime? date = null,
        Guid? flightId = null,
        Guid? fareClassId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (flightId.HasValue)
            {
                await _cacheService.InvalidateFlightAsync(flightId.Value, cancellationToken);
            }

            if (fareClassId.HasValue)
            {
                await _cacheService.InvalidateFareClassAsync(fareClassId.Value, cancellationToken);
            }

            if (!string.IsNullOrEmpty(departureAirport) && !string.IsNullOrEmpty(arrivalAirport) && date.HasValue)
            {
                await _cacheService.InvalidateRouteAsync(departureAirport, arrivalAirport, date.Value, cancellationToken);
            }

            _logger.LogInformation("Invalidated search cache for criteria: Departure={Departure}, Arrival={Arrival}, Date={Date}, Flight={Flight}, FareClass={FareClass}",
                departureAirport, arrivalAirport, date, flightId, fareClassId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating search cache");
        }
    }

    private async Task<FlightSearchResult> PerformFlightSearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _context.Flights
            .Include(f => f.Route)
                .ThenInclude(r => r.DepartureAirport)
            .Include(f => f.Route)
                .ThenInclude(r => r.ArrivalAirport)
            .Include(f => f.FareClasses.Where(fc => fc.IsActive))
                .ThenInclude(fc => fc.FareClassAmenities)
                    .ThenInclude(fca => fca.Amenity)
            .Include(f => f.Seats.Where(s => s.IsActive))
            .Where(f => f.Route.DepartureAirport.IataCode == criteria.DepartureAirport.ToUpper() &&
                       f.Route.ArrivalAirport.IataCode == criteria.ArrivalAirport.ToUpper() &&
                       f.DepartureDate.Date == criteria.DepartureDate.Date &&
                       f.IsActive);

        // Apply filters
        if (criteria.FareClasses.Any())
        {
            query = query.Where(f => f.FareClasses.Any(fc => criteria.FareClasses.Contains(fc.ClassName)));
        }

        if (criteria.Airlines.Any())
        {
            query = query.Where(f => criteria.Airlines.Contains(f.AirlineCode));
        }

        if (criteria.PreferredDepartureTimeStart.HasValue && criteria.PreferredDepartureTimeEnd.HasValue)
        {
            query = query.Where(f => f.DepartureTimeSpan >= criteria.PreferredDepartureTimeStart.Value &&
                                   f.DepartureTimeSpan <= criteria.PreferredDepartureTimeEnd.Value);
        }

        var flights = await query.ToListAsync(cancellationToken);

        // Filter by amenities if specified
        if (criteria.AmenityIds.Any())
        {
            flights = flights.Where(f => f.FareClasses
                .SelectMany(fc => fc.FareClassAmenities)
                .Any(fca => criteria.AmenityIds.Contains(fca.AmenityId))).ToList();
        }

        // Build availability and apply price filters
        var outboundFlights = new List<FlightAvailability>();
        foreach (var flight in flights)
        {
            var availability = await BuildFlightAvailabilityAsync(flight, criteria.PassengerCount, cancellationToken);

            if (availability.HasAvailability)
            {
                // Apply price filters
                if (criteria.MinPrice.HasValue && availability.MinPrice < criteria.MinPrice.Value)
                    continue;
                if (criteria.MaxPrice.HasValue && availability.MinPrice > criteria.MaxPrice.Value)
                    continue;

                outboundFlights.Add(availability);
            }
        }

        // Apply sorting
        outboundFlights = ApplySorting(outboundFlights, criteria.SortBy, criteria.SortDirection);

        // Apply pagination
        var totalCount = outboundFlights.Count;
        var pagedFlights = outboundFlights
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        // Build metadata
        var metadata = BuildSearchMetadata(outboundFlights, criteria, totalCount);

        return new FlightSearchResult
        {
            OutboundFlights = pagedFlights,
            ReturnFlights = new List<FlightAvailability>(), // TODO: Implement return flights for round trips
            Metadata = metadata
        };
    }

    private async Task<FlightAvailability> BuildFlightAvailabilityAsync(
        Flight flight,
        int passengerCount,
        CancellationToken cancellationToken)
    {
        var fareClassAvailability = new List<FareClassAvailability>();
        var totalAvailableSeats = 0;
        var prices = new List<decimal>();

        foreach (var fareClass in flight.FareClasses)
        {
            var availableSeats = flight.Seats.Count(s => s.FareClassId == fareClass.Id && s.Status == SeatStatus.Available);
            var totalSeats = flight.Seats.Count(s => s.FareClassId == fareClass.Id);

            fareClassAvailability.Add(new FareClassAvailability
            {
                FareClassId = fareClass.Id,
                ClassName = fareClass.ClassName,
                CurrentPrice = fareClass.CurrentPrice,
                OriginalPrice = fareClass.BasePrice,
                AvailableSeats = availableSeats,
                TotalSeats = totalSeats,
                IsAvailable = availableSeats >= passengerCount,
                UnavailabilityReason = availableSeats < passengerCount ? "Insufficient seats" : null,
                IncludedAmenities = fareClass.FareClassAmenities.Select(fca => fca.AmenityId).ToList()
            });

            totalAvailableSeats += availableSeats;
            if (availableSeats >= passengerCount)
            {
                prices.Add(fareClass.CurrentPrice);
            }
        }

        return new FlightAvailability
        {
            FlightId = flight.Id,
            FlightNumber = $"{flight.AirlineCode}{flight.FlightNumber}",
            AirlineCode = flight.AirlineCode,
            AirlineName = flight.AirlineName,
            AircraftType = flight.AircraftType,
            DepartureAirport = flight.Route.DepartureAirport.IataCode,
            ArrivalAirport = flight.Route.ArrivalAirport.IataCode,
            DepartureDateTime = flight.DepartureDate.Date.Add(flight.DepartureTimeSpan),
            ArrivalDateTime = flight.DepartureDate.Date.Add(flight.ArrivalTime),
            FlightDuration = flight.ArrivalTime > flight.DepartureTimeSpan ?
                flight.ArrivalTime - flight.DepartureTimeSpan :
                flight.ArrivalTime.Add(TimeSpan.FromDays(1)) - flight.DepartureTimeSpan,
            FareClasses = fareClassAvailability,
            AvailableAmenities = flight.FareClasses
                .SelectMany(fc => fc.FareClassAmenities)
                .Select(fca => fca.AmenityId)
                .Distinct()
                .ToList(),
            MinPrice = prices.Any() ? prices.Min() : 0,
            MaxPrice = prices.Any() ? prices.Max() : 0,
            TotalAvailableSeats = totalAvailableSeats,
            IsInternational = flight.Route.IsInternational,
            Gate = flight.Gate,
            Terminal = flight.Terminal,
            LastUpdated = DateTime.UtcNow
        };
    }

    private List<FlightAvailability> ApplySorting(List<FlightAvailability> flights, string? sortBy, string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "price" => isDescending ?
                flights.OrderByDescending(f => f.MinPrice).ToList() :
                flights.OrderBy(f => f.MinPrice).ToList(),
            "duration" => isDescending ?
                flights.OrderByDescending(f => f.FlightDuration).ToList() :
                flights.OrderBy(f => f.FlightDuration).ToList(),
            "departure_time" => isDescending ?
                flights.OrderByDescending(f => f.DepartureDateTime).ToList() :
                flights.OrderBy(f => f.DepartureDateTime).ToList(),
            "arrival_time" => isDescending ?
                flights.OrderByDescending(f => f.ArrivalDateTime).ToList() :
                flights.OrderBy(f => f.ArrivalDateTime).ToList(),
            _ => flights.OrderBy(f => f.MinPrice).ToList()
        };
    }

    private FlightSearchMetadata BuildSearchMetadata(List<FlightAvailability> allFlights, FlightSearchCriteria criteria, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize);

        return new FlightSearchMetadata
        {
            TotalResults = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalPages = totalPages,
            HasNextPage = criteria.Page < totalPages,
            HasPreviousPage = criteria.Page > 1,
            MinPrice = allFlights.Any() ? allFlights.Min(f => f.MinPrice) : null,
            MaxPrice = allFlights.Any() ? allFlights.Max(f => f.MaxPrice) : null,
            AvailableAirlines = allFlights.Select(f => f.AirlineName).Distinct().ToList(),
            AvailableFareClasses = allFlights.SelectMany(f => f.FareClasses.Select(fc => fc.ClassName)).Distinct().ToList(),
            ShortestDuration = allFlights.Any() ? allFlights.Min(f => f.FlightDuration) : null,
            LongestDuration = allFlights.Any() ? allFlights.Max(f => f.FlightDuration) : null,
            SearchExecutedAt = DateTime.UtcNow
        };
    }

    private string GenerateETag(FlightSearchResult result)
    {
        var content = JsonSerializer.Serialize(result, _jsonOptions);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    private async Task CacheSearchResultAsync(string cacheKey, FlightSearchResult result, FlightSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var tags = new List<string>
        {
            $"route:{criteria.RouteKey}",
            $"date:{criteria.DepartureDate:yyyy-MM-dd}"
        };

        // Add flight-specific tags
        foreach (var flight in result.OutboundFlights)
        {
            tags.Add($"flight:{flight.FlightId}");
            foreach (var fareClass in flight.FareClasses)
            {
                tags.Add($"fareclass:{fareClass.FareClassId}");
            }
        }

        var cachedResult = new CachedSearchResult
        {
            Data = JsonSerializer.Serialize(result, _jsonOptions),
            ETag = result.ETag,
            Tags = tags
        };

        await _cacheService.SetSearchResultsAsync(cacheKey, cachedResult, cancellationToken);
    }

    private double CalculatePopularityScore(int flightCount, decimal averagePrice)
    {
        // Simple popularity algorithm - can be enhanced with more factors
        var flightScore = Math.Log(flightCount + 1) * 10;
        var priceScore = averagePrice > 0 ? 1000 / (double)averagePrice : 0;
        return flightScore + priceScore;
    }

    private double CalculateOccupancyRate(Flight flight, FareClass fareClass)
    {
        var totalSeats = flight.Seats.Count(s => s.FareClassId == fareClass.Id);
        var occupiedSeats = flight.Seats.Count(s => s.FareClassId == fareClass.Id && s.Status != SeatStatus.Available);
        return totalSeats > 0 ? (double)occupiedSeats / totalSeats : 0;
    }

    private double CalculateDemandScore(Flight flight, DateTime searchDate)
    {
        var daysUntilDeparture = (flight.DepartureDate - searchDate).TotalDays;

        // Higher demand score for flights departing soon
        if (daysUntilDeparture <= 7) return 1.5;
        if (daysUntilDeparture <= 14) return 1.2;
        if (daysUntilDeparture <= 30) return 1.0;
        return 0.8;
    }

    private decimal CalculateDynamicPrice(decimal basePrice, double occupancyRate, double demandScore)
    {
        var occupancyMultiplier = 1.0 + (occupancyRate * 0.5); // Up to 50% increase for high occupancy
        var demandMultiplier = demandScore;

        return basePrice * (decimal)(occupancyMultiplier * demandMultiplier);
    }

    private string GetPricingReason(double occupancyRate, double demandScore)
    {
        if (occupancyRate > 0.8) return "High demand - limited seats available";
        if (demandScore > 1.2) return "Peak travel period";
        if (occupancyRate < 0.3) return "Low demand - promotional pricing";
        return "Standard pricing";
    }

    // Placeholder implementations for suggestion methods
    private Task<List<AirportSuggestion>> GetAirportSuggestionsAsync(string? fromAirport, CancellationToken cancellationToken)
    {
        // TODO: Implement airport suggestions based on popular routes and user preferences
        return Task.FromResult(new List<AirportSuggestion>());
    }

    private Task<List<DateSuggestion>> GetDateSuggestionsAsync(string? departureAirport, string? arrivalAirport, CancellationToken cancellationToken)
    {
        // TODO: Implement date suggestions based on pricing and availability
        return Task.FromResult(new List<DateSuggestion>());
    }

    private Task<List<RouteSuggestion>> GetRouteSuggestionsAsync(string? departureAirport, CancellationToken cancellationToken)
    {
        // TODO: Implement route suggestions based on popularity and pricing
        return Task.FromResult(new List<RouteSuggestion>());
    }
}
