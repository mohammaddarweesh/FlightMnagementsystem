# Redis Integration Documentation

## Overview

This document describes the comprehensive Redis integration implemented in the Flight Booking System, featuring professional-grade caching, session management, distributed locking, and rate limiting capabilities.

## Architecture

### Core Components

1. **Redis Services Layer**
   - `IRedisService` - Core Redis operations
   - `IRedisCacheService` - Typed caching operations
   - `IRedisSessionService` - Session management with sliding expiration
   - `IRedisMetricsService` - Performance and contention metrics

2. **Distributed Locking**
   - `IDistributedLockService` - RedLock.net implementation
   - `ISeatLockService` - Specialized seat allocation locking
   - `IPromotionLockService` - Promotion redemption locking

3. **Rate Limiting**
   - `RedisRateLimitMiddleware` - Sliding window rate limiting
   - Configurable policies per endpoint

## Key Features

### 🔐 Session Management

#### Guest Sessions
- **Minimal PII Storage**: Only essential data stored
- **Sliding Expiration**: 30-minute default with auto-refresh
- **Size Limits**: 1MB maximum per session
- **Automatic Cleanup**: Expired session removal

```csharp
// Create guest session
var sessionId = await _sessionService.CreateSessionAsync(isAuthenticated: false);

// Store minimal data
await _sessionService.SetSessionDataAsync(sessionId, "cart", cartData);

// Automatic sliding expiration on access
var isValid = await _sessionService.ValidateSessionAsync(sessionId);
```

#### Authenticated Sessions
- **Extended Timeout**: 2-hour default
- **User-based Keys**: Linked to user ID
- **Enhanced Security**: Additional validation

### 🚦 Rate Limiting

#### Sliding Window Algorithm
- **8 Segments per Window**: Smooth rate limiting
- **Per-Endpoint Policies**: Customizable limits
- **Client Identification**: User ID or IP-based
- **Graceful Degradation**: Continues on Redis failure

```json
{
  "Redis": {
    "RateLimit": {
      "Policies": {
        "/api/bookings": {
          "PermitLimit": 50,
          "Window": "00:01:00",
          "SegmentsPerWindow": 8
        }
      }
    }
  }
}
```

#### Hot Endpoints Protection
- `/api/bookings` - 50 requests/minute
- `/api/flights/search` - 200 requests/minute
- `/api/seats/availability` - 300 requests/minute
- `/api/pricing/calculate` - 150 requests/minute

### 🔒 Distributed Locking

#### RedLock Implementation
- **High Availability**: Multiple Redis instances support
- **Jitter Support**: Prevents thundering herd
- **Timeout Handling**: Configurable timeouts and retries
- **Metrics Tracking**: Lock contention monitoring

```csharp
// Acquire lock with timeout and retries
using var seatLock = await _seatLockService.AcquireSeatLockAsync(flightId, seatNumber);
if (seatLock?.IsAcquired == true)
{
    // Perform seat allocation
    await AllocateSeatAsync(flightId, seatNumber, passengerId);
}
```

#### Specialized Lock Services

**Seat Allocation Locks**
```csharp
// Lock specific seat for allocation
await _seatLockService.ExecuteWithSeatLockAsync(flightId, "12A", async () =>
{
    return await AllocateSeatAsync(flightId, "12A", passengerId);
});

// Check multiple seat statuses
var seatStatuses = await _seatLockService.GetSeatLockStatusAsync(flightId, seatNumbers);
```

**Promotion Redemption Locks**
```csharp
// Lock promotion code to prevent double redemption
await _promotionLockService.ExecuteWithPromotionLockAsync("SAVE20", async () =>
{
    return await RedeemPromotionAsync("SAVE20", customerId);
}, customerId);

// Get contention metrics
var metrics = await _promotionLockService.GetPromotionContentionMetricsAsync("SAVE20");
```

### 📊 Metrics & Monitoring

#### Cache Metrics
- **Hit Ratio Tracking**: Per-category cache performance
- **Miss Rate Monitoring**: Cache effectiveness
- **Size Monitoring**: Memory usage tracking

```csharp
// Automatic metrics recording
await _metricsService.RecordCacheHitAsync("booking");
await _metricsService.RecordCacheMissAsync("flight");

// Get performance stats
var hitRatio = await _metricsService.GetCacheHitRatioAsync("booking");
var stats = await _metricsService.GetCacheStatsAsync();
```

#### Lock Contention Metrics
- **Acquisition Time**: Lock performance tracking
- **Contention Rate**: Resource competition monitoring
- **Timeout Tracking**: Failed acquisition monitoring

```csharp
// Automatic lock metrics
await _metricsService.RecordLockAcquiredAsync("seat:123:12A", duration);
await _metricsService.RecordLockContentionAsync("promotion:SAVE20");

// Get contention analysis
var lockStats = await _metricsService.GetLockStatsAsync("seat:123:12A");
```

## Configuration

### Redis Connection
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "KeyPrefix": "flightbooking",
    "DefaultTtl": "00:30:00",
    "ConnectTimeout": "00:00:05",
    "CommandTimeout": "00:00:05",
    "RetryCount": 3
  }
}
```

### Session Configuration
```json
{
  "Session": {
    "GuestSessionTimeout": "00:30:00",
    "AuthenticatedSessionTimeout": "02:00:00",
    "SlidingExpiration": true,
    "CookieName": "FlightBooking.SessionId",
    "MaxSessionSize": 1048576
  }
}
```

### Distributed Lock Configuration
```json
{
  "DistributedLock": {
    "DefaultTimeout": "00:00:30",
    "DefaultRetryDelay": "00:00:00.100",
    "MaxRetryAttempts": 10,
    "EnableJitter": true,
    "JitterFactor": 0.2,
    "LockKeyPrefix": "locks"
  }
}
```

## Key Naming Conventions

### Pattern Structure
```
{prefix}:{category}:{subcategory}:{identifier}
```

### Examples
```
flightbooking:session:guest:abc123def456
flightbooking:session:user:550e8400-e29b-41d4-a716-446655440000
flightbooking:cache:booking:550e8400-e29b-41d4-a716-446655440000
flightbooking:locks:seat:123:12A
flightbooking:locks:promotion:SAVE20
flightbooking:ratelimit:api-bookings:user:123:1640995200000
flightbooking:metrics:cache-hits:booking
```

### Key Categories
- `session` - User and guest sessions
- `cache` - Cached application data
- `locks` - Distributed locks
- `ratelimit` - Rate limiting counters
- `metrics` - Performance metrics

## TTL Guidelines

### Session TTLs
- **Guest Sessions**: 30 minutes (sliding)
- **User Sessions**: 2 hours (sliding)
- **Admin Sessions**: 8 hours (sliding)

### Cache TTLs
- **Short Cache**: 5 minutes (real-time data)
- **Medium Cache**: 30 minutes (semi-static data)
- **Long Cache**: 2 hours (static data)
- **Very Long Cache**: 24 hours (configuration data)

### Lock TTLs
- **Short Lock**: 30 seconds (quick operations)
- **Medium Lock**: 2 minutes (seat allocation)
- **Long Lock**: 10 minutes (complex operations)

### Rate Limit TTLs
- **Standard**: 1 minute (sliding window)
- **Burst**: 10 seconds (burst protection)

### Metrics TTLs
- **Short Metrics**: 1 hour (real-time monitoring)
- **Standard Metrics**: 7 days (historical analysis)

## Usage Examples

### Caching with Metrics
```csharp
public async Task<BookingDto> GetBookingAsync(Guid bookingId)
{
    var cacheKey = _cacheService.BookingCacheKey(bookingId);
    
    return await _cacheService.GetOrSetAsync(cacheKey, async () =>
    {
        return await _repository.GetBookingAsync(bookingId);
    }, RedisTtl.MediumCache);
}
```

### Session Management
```csharp
public async Task<string> CreateGuestSessionAsync()
{
    var sessionId = await _sessionService.CreateSessionAsync(isAuthenticated: false);
    
    // Store minimal guest data
    await _sessionService.SetSessionDataAsync(sessionId, "preferences", new
    {
        Currency = "USD",
        Language = "en-US"
    });
    
    return sessionId;
}
```

### Distributed Locking
```csharp
public async Task<bool> AllocateSeatAsync(Guid flightId, string seatNumber, Guid passengerId)
{
    return await _seatLockService.ExecuteWithSeatLockAsync(flightId, seatNumber, async () =>
    {
        // Check availability
        var isAvailable = await _seatRepository.IsSeatAvailableAsync(flightId, seatNumber);
        if (!isAvailable)
            throw new SeatNotAvailableException();
        
        // Allocate seat
        await _seatRepository.AllocateSeatAsync(flightId, seatNumber, passengerId);
        return true;
    });
}
```

## Monitoring & Troubleshooting

### Health Checks
```csharp
// Redis connectivity
var isHealthy = await _redisService.PingAsync();
var latency = await _healthService.GetLatencyAsync();
```

### Performance Monitoring
```csharp
// Cache performance
var hitRatio = await _metricsService.GetCacheHitRatioAsync("booking");
var systemMetrics = await _metricsService.GetSystemMetricsAsync();

// Lock contention
var lockStats = await _metricsService.GetLockStatsAsync("seat:allocation");
```

### Cleanup Operations
```csharp
// Cleanup expired sessions
var cleanedSessions = await _sessionService.CleanupExpiredSessionsAsync();

// Cleanup old metrics
await _metricsService.CleanupOldMetricsAsync(TimeSpan.FromDays(30));
```

## Best Practices

### 1. Key Management
- Use consistent naming conventions
- Include TTL in all cache operations
- Implement key rotation for sensitive data

### 2. Error Handling
- Graceful degradation on Redis failures
- Retry logic with exponential backoff
- Circuit breaker pattern for high availability

### 3. Performance
- Use pipelining for batch operations
- Monitor memory usage and key expiration
- Implement proper connection pooling

### 4. Security
- Minimize PII in sessions
- Use encryption for sensitive cached data
- Implement proper access controls

### 5. Monitoring
- Track cache hit ratios
- Monitor lock contention
- Set up alerts for performance degradation

## Production Considerations

### High Availability
- Configure Redis Sentinel or Cluster
- Implement proper failover logic
- Monitor Redis instance health

### Scaling
- Use Redis Cluster for horizontal scaling
- Implement consistent hashing
- Monitor memory usage and performance

### Security
- Enable Redis AUTH
- Use TLS for connections
- Implement network security groups

### Backup & Recovery
- Configure Redis persistence
- Implement backup strategies
- Test recovery procedures

This Redis integration provides enterprise-grade caching, session management, distributed locking, and rate limiting capabilities essential for a high-performance flight booking system.
