# PostgreSQL & Redis Setup Guide

## 🐘 PostgreSQL Setup Verification

### 1. Check PostgreSQL Installation
Open pgAdmin and verify:
- **Server**: localhost (127.0.0.1)
- **Port**: 5432 (default)
- **Username**: postgres
- **Password**: [your password]

### 2. Create Flight Booking Database
In pgAdmin, create a new database:
```sql
CREATE DATABASE "FlightBookingDb_MohammadDarweesh";
CREATE DATABASE "flightbookinghangfire_mohammaddarweesh";
```

**Note**: The application now automatically creates databases on startup, but you can create them manually if preferred.

### 3. Update Connection Strings
Current configuration in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=FlightBookingDb_MohammadDarweesh;Username=postgres;Password=6482297",
    "Hangfire": "Host=localhost;Database=flightbookinghangfire_mohammaddarweesh;Username=postgres;Password=6482297"
  }
}
```

### 4. Test Database Connection
Start the API and test:
```bash
# Start the API
dotnet run --project src/Api/FlightBooking.Api

# Test connection (in another terminal or browser)
curl http://localhost:5000/api/DatabaseTest/connection
```

Expected response:
```json
{
  "status": "Connected",
  "database": "FlightBookingDb_MohammadDarweesh",
  "message": "Successfully connected to PostgreSQL",
  "timestamp": "2025-01-XX..."
}
```

### 5. Run Migrations
```bash
# Via API endpoint
curl -X POST http://localhost:5000/api/DatabaseTest/migrate

# Or via CLI
dotnet ef database update --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api
```

## 🔴 Redis Setup & Usage

### 1. Install Redis
**Windows (using Chocolatey):**
```bash
choco install redis-64
```

**Windows (using WSL2):**
```bash
# In WSL2 Ubuntu
sudo apt update
sudo apt install redis-server
sudo service redis-server start
```

**Docker (Recommended):**
```bash
docker run -d --name redis -p 6379:6379 redis:latest
```

### 2. Verify Redis Installation
```bash
# Test Redis CLI
redis-cli ping
# Should return: PONG
```

### 3. Test Redis Connection via API
```bash
# Test connection
curl http://localhost:5000/api/RedisTest/connection

# Set a cache value
curl -X POST http://localhost:5000/api/RedisTest/cache/set \
  -H "Content-Type: application/json" \
  -d '{"key": "test", "value": "Hello Redis!", "expiryMinutes": 5}'

# Get the cached value
curl http://localhost:5000/api/RedisTest/cache/get/test

# Get Redis info
curl http://localhost:5000/api/RedisTest/info
```

## 🚀 How We Use Redis in Flight Booking Engine

### 1. **Flight Availability Caching**
```csharp
// Cache flight search results for 30 seconds
var cacheKey = $"flights:{origin}:{destination}:{date}";
var flights = await _cache.GetAsync<List<Flight>>(cacheKey);

if (flights == null)
{
    flights = await _flightService.SearchFlights(origin, destination, date);
    await _cache.SetAsync(cacheKey, flights, TimeSpan.FromSeconds(30));
}
```

### 2. **Seat Inventory Caching**
```csharp
// Cache seat availability with short TTL
var seatCacheKey = $"seats:{flightId}";
var availableSeats = await _cache.GetAsync<SeatInventory>(seatCacheKey);

// Invalidate cache when booking is made
await _cache.RemoveAsync(seatCacheKey);
```

### 3. **Distributed Locking for Bookings**
```csharp
// Prevent double booking with distributed locks
using var lockManager = new RedLockFactory(_redis);
using var redLock = await lockManager.CreateLockAsync($"booking:{flightId}:{seatNumber}", TimeSpan.FromSeconds(30));

if (redLock.IsAcquired)
{
    // Process booking safely
    await ProcessBooking(bookingRequest);
}
```

### 4. **Session Management**
```csharp
// Store user session data
var sessionKey = $"session:{userId}";
await _cache.SetAsync(sessionKey, userSession, TimeSpan.FromMinutes(30));
```

### 5. **Rate Limiting**
```csharp
// Track API calls per user
var rateLimitKey = $"rate_limit:{userId}:{DateTime.UtcNow:yyyyMMddHH}";
var currentCount = await _cache.GetAsync<int>(rateLimitKey);

if (currentCount >= 100) // 100 requests per hour
{
    throw new RateLimitExceededException();
}

await _cache.SetAsync(rateLimitKey, currentCount + 1, TimeSpan.FromHours(1));
```

### 6. **Background Job Queues**
```csharp
// Queue email notifications
var emailQueueKey = "queue:emails";
await _redis.GetDatabase().ListLeftPushAsync(emailQueueKey, JsonSerializer.Serialize(emailData));
```

## 🔧 Redis Configuration in Our App

### Cache Strategies:
1. **Cache-Aside Pattern**: Manual cache management
2. **Write-Through**: Update cache when data changes
3. **Cache Invalidation**: Remove stale data

### TTL (Time To Live) Settings:
- **Flight Availability**: 30 seconds
- **Pricing Data**: 5 minutes
- **User Sessions**: 30 minutes
- **Search Results**: 2 minutes

### Key Naming Conventions:
- `flights:{origin}:{destination}:{date}`
- `seats:{flightId}`
- `user:{userId}:session`
- `booking:lock:{flightId}:{seatNumber}`
- `rate_limit:{userId}:{hour}`

## 🧪 Testing Your Setup

### 1. Start the Application
```bash
cd FlightBookingEngine
dotnet run --project src/Api/FlightBooking.Api
```

### 2. Test Database
- Visit: http://localhost:5000/api/DatabaseTest/connection
- Run migrations: http://localhost:5000/api/DatabaseTest/migrate
- Check tables: http://localhost:5000/api/DatabaseTest/tables

### 3. Test Redis
- Visit: http://localhost:5000/api/RedisTest/connection
- Test caching via the API endpoints

### 4. Check Health
- Visit: http://localhost:5000/health
- Should show both PostgreSQL and Redis as healthy

## 🚨 Troubleshooting

### PostgreSQL Issues:
- **Connection refused**: Check if PostgreSQL service is running
- **Authentication failed**: Verify username/password in connection string
- **Database not found**: Create the database in pgAdmin first

### Redis Issues:
- **Connection refused**: Check if Redis server is running (`redis-server`)
- **Port conflicts**: Ensure port 6379 is available
- **Windows**: Consider using Docker or WSL2 for Redis

### Common Commands:
```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# Check Redis status
redis-cli ping

# View logs
docker logs redis  # if using Docker
```

---

**Next Steps**: Once both PostgreSQL and Redis are working, we can start implementing the actual flight booking features!
