using FlightBooking.Application.Search.Events;
using FlightBooking.Application.Search.Services;
using FlightBooking.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.Search.EventHandlers;

public class FlightUpdatedEventHandler : INotificationHandler<FlightUpdatedEvent>
{
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FlightUpdatedEventHandler> _logger;

    public FlightUpdatedEventHandler(
        IFlightSearchCacheService cacheService,
        ApplicationDbContext context,
        ILogger<FlightUpdatedEventHandler> logger)
    {
        _cacheService = cacheService;
        _context = context;
        _logger = logger;
    }

    public async Task Handle(FlightUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling flight updated event for flight {FlightId}", notification.FlightId);

            // Invalidate flight-specific cache
            await _cacheService.InvalidateFlightAsync(notification.FlightId, cancellationToken);

            // Invalidate route and date specific cache
            await _cacheService.InvalidateRouteAsync(
                notification.DepartureAirport,
                notification.ArrivalAirport,
                notification.DepartureDate,
                cancellationToken);

            // If pricing or availability is affected, invalidate broader cache
            if (notification.AffectsAvailability || notification.AffectsPricing)
            {
                await InvalidateRelatedCacheAsync(notification.FlightId, cancellationToken);
            }

            _logger.LogInformation("Successfully invalidated cache for flight {FlightId} update", notification.FlightId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling flight updated event for flight {FlightId}", notification.FlightId);
        }
    }

    private async Task InvalidateRelatedCacheAsync(Guid flightId, CancellationToken cancellationToken)
    {
        try
        {
            // Get flight details to invalidate related cache entries
            var flight = await _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == flightId, cancellationToken);

            if (flight != null)
            {
                // Invalidate cache for the entire week around the flight date
                var startDate = flight.DepartureDate.AddDays(-3);
                var endDate = flight.DepartureDate.AddDays(3);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    await _cacheService.InvalidateRouteAsync(
                        flight.Route.DepartureAirport.IataCode,
                        flight.Route.ArrivalAirport.IataCode,
                        date,
                        cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating related cache for flight {FlightId}", flightId);
        }
    }
}

public class FareClassUpdatedEventHandler : INotificationHandler<FareClassUpdatedEvent>
{
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FareClassUpdatedEventHandler> _logger;

    public FareClassUpdatedEventHandler(
        IFlightSearchCacheService cacheService,
        ApplicationDbContext context,
        ILogger<FareClassUpdatedEventHandler> logger)
    {
        _cacheService = cacheService;
        _context = context;
        _logger = logger;
    }

    public async Task Handle(FareClassUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling fare class updated event for fare class {FareClassId}", notification.FareClassId);

            // Invalidate fare class specific cache
            await _cacheService.InvalidateFareClassAsync(notification.FareClassId, cancellationToken);

            // Invalidate flight specific cache
            await _cacheService.InvalidateFlightAsync(notification.FlightId, cancellationToken);

            // If significant changes, invalidate broader cache
            if (notification.PriceChanged || notification.CapacityChanged || notification.AvailabilityChanged)
            {
                await InvalidateFlightRouteCache(notification.FlightId, cancellationToken);
            }

            _logger.LogInformation("Successfully invalidated cache for fare class {FareClassId} update", notification.FareClassId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling fare class updated event for fare class {FareClassId}", notification.FareClassId);
        }
    }

    private async Task InvalidateFlightRouteCache(Guid flightId, CancellationToken cancellationToken)
    {
        try
        {
            var flight = await _context.Flights
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == flightId, cancellationToken);

            if (flight != null)
            {
                await _cacheService.InvalidateRouteAsync(
                    flight.Route.DepartureAirport.IataCode,
                    flight.Route.ArrivalAirport.IataCode,
                    flight.DepartureDate,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating flight route cache for flight {FlightId}", flightId);
        }
    }
}

public class BookingConfirmedEventHandler : INotificationHandler<BookingConfirmedEvent>
{
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ILogger<BookingConfirmedEventHandler> _logger;

    public BookingConfirmedEventHandler(
        IFlightSearchCacheService cacheService,
        ILogger<BookingConfirmedEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(BookingConfirmedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling booking confirmed event for booking {BookingId}", notification.BookingId);

            // Invalidate flight availability cache
            await _cacheService.InvalidateFlightAsync(notification.FlightId, cancellationToken);

            // Invalidate route availability cache
            await _cacheService.InvalidateRouteAsync(
                notification.DepartureAirport,
                notification.ArrivalAirport,
                notification.DepartureDate,
                cancellationToken);

            // Invalidate fare class cache for affected fare classes
            foreach (var fareClassId in notification.FareClassIds)
            {
                await _cacheService.InvalidateFareClassAsync(fareClassId, cancellationToken);
            }

            _logger.LogInformation("Successfully invalidated cache for booking {BookingId} confirmation", notification.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking confirmed event for booking {BookingId}", notification.BookingId);
        }
    }
}

public class BookingCancelledEventHandler : INotificationHandler<BookingCancelledEvent>
{
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ILogger<BookingCancelledEventHandler> _logger;

    public BookingCancelledEventHandler(
        IFlightSearchCacheService cacheService,
        ILogger<BookingCancelledEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(BookingCancelledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling booking cancelled event for booking {BookingId}", notification.BookingId);

            // Invalidate flight availability cache (seats are now available)
            await _cacheService.InvalidateFlightAsync(notification.FlightId, cancellationToken);

            // Invalidate route availability cache
            await _cacheService.InvalidateRouteAsync(
                notification.DepartureAirport,
                notification.ArrivalAirport,
                notification.DepartureDate,
                cancellationToken);

            // Invalidate fare class cache for affected fare classes
            foreach (var fareClassId in notification.FareClassIds)
            {
                await _cacheService.InvalidateFareClassAsync(fareClassId, cancellationToken);
            }

            _logger.LogInformation("Successfully invalidated cache for booking {BookingId} cancellation", notification.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking cancelled event for booking {BookingId}", notification.BookingId);
        }
    }
}

public class SeatStatusChangedEventHandler : INotificationHandler<SeatStatusChangedEvent>
{
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ILogger<SeatStatusChangedEventHandler> _logger;

    public SeatStatusChangedEventHandler(
        IFlightSearchCacheService cacheService,
        ILogger<SeatStatusChangedEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(SeatStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling seat status changed event for {SeatCount} seats", notification.AffectedSeatCount);

            // Invalidate flight and fare class cache
            await _cacheService.InvalidateFlightAsync(notification.FlightId, cancellationToken);
            await _cacheService.InvalidateFareClassAsync(notification.FareClassId, cancellationToken);

            _logger.LogInformation("Successfully invalidated cache for seat status change affecting {SeatCount} seats", notification.AffectedSeatCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling seat status changed event for {SeatCount} seats", notification.AffectedSeatCount);
        }
    }
}

public class BulkPriceUpdateEventHandler : INotificationHandler<BulkPriceUpdateEvent>
{
    private readonly IFlightSearchCacheService _cacheService;
    private readonly ILogger<BulkPriceUpdateEventHandler> _logger;

    public BulkPriceUpdateEventHandler(
        IFlightSearchCacheService cacheService,
        ILogger<BulkPriceUpdateEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(BulkPriceUpdateEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling bulk price update event affecting {FlightCount} flights", notification.FlightIds.Count);

            // For bulk updates, it's more efficient to clear all search cache
            if (notification.FlightIds.Count > 50 || notification.FareClassIds.Count > 100)
            {
                await _cacheService.InvalidateAllSearchCacheAsync(cancellationToken);
                _logger.LogInformation("Invalidated all search cache due to large bulk price update");
            }
            else
            {
                // Invalidate specific flights and fare classes
                var tasks = new List<Task>();

                foreach (var flightId in notification.FlightIds)
                {
                    tasks.Add(_cacheService.InvalidateFlightAsync(flightId, cancellationToken));
                }

                foreach (var fareClassId in notification.FareClassIds)
                {
                    tasks.Add(_cacheService.InvalidateFareClassAsync(fareClassId, cancellationToken));
                }

                await Task.WhenAll(tasks);
                _logger.LogInformation("Invalidated cache for {FlightCount} flights and {FareClassCount} fare classes", 
                    notification.FlightIds.Count, notification.FareClassIds.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling bulk price update event");
        }
    }
}
