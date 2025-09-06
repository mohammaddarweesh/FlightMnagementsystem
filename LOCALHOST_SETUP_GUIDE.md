# Localhost Setup Guide - PostgreSQL & Redis

## 🐘 PostgreSQL Setup (COMPLETED)

✅ **Your PostgreSQL Configuration:**
- **Host**: localhost
- **Port**: 5432
- **Username**: postgres
- **Password**: 6482297
- **Status**: ✅ Configured in appsettings files

### Create Required Databases in pgAdmin:

**Note**: The application now automatically creates databases on startup, but you can create them manually if preferred.

1. Open pgAdmin
2. Connect to your PostgreSQL server (localhost)
3. Right-click on "Databases" → "Create" → "Database..."
4. Create these databases:

```sql
-- Current personalized databases
CREATE DATABASE "FlightBookingDb_MohammadDarweesh";
CREATE DATABASE "flightbookinghangfire_mohammaddarweesh";
```

## 🔴 Redis Installation on Localhost (Windows)

### Option 1: Using Chocolatey (Recommended)

1. **Install Chocolatey** (if not already installed):
   ```powershell
   # Run as Administrator
   Set-ExecutionPolicy Bypass -Scope Process -Force
   [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
   iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
   ```

2. **Install Redis**:
   ```powershell
   # Run as Administrator
   choco install redis-64
   ```

3. **Start Redis Service**:
   ```powershell
   # Start Redis server
   redis-server
   
   # Or install as Windows service
   redis-server --service-install
   redis-server --service-start
   ```

4. **Test Redis**:
   ```powershell
   # Open new terminal
   redis-cli ping
   # Should return: PONG
   ```

### Option 2: Manual Installation

1. **Download Redis for Windows**:
   - Go to: https://github.com/microsoftarchive/redis/releases
   - Download: `Redis-x64-3.0.504.msi`
   - Install the MSI package

2. **Start Redis**:
   ```cmd
   # Navigate to Redis installation directory (usually C:\Program Files\Redis)
   cd "C:\Program Files\Redis"
   redis-server.exe redis.windows.conf
   ```

3. **Test Connection**:
   ```cmd
   redis-cli.exe ping
   ```

### Option 3: Using WSL2 (Windows Subsystem for Linux)

1. **Install WSL2** (if not already installed):
   ```powershell
   # Run as Administrator
   wsl --install
   ```

2. **Install Redis in WSL2**:
   ```bash
   # In WSL2 terminal
   sudo apt update
   sudo apt install redis-server
   
   # Start Redis
   sudo service redis-server start
   
   # Test
   redis-cli ping
   ```

3. **Access from Windows**:
   - Redis will be available at `localhost:6379`
   - WSL2 automatically forwards ports to Windows

## 🧪 Testing Your Complete Setup

### Step 1: Start Redis
Choose one of the methods above and ensure Redis is running:
```bash
redis-cli ping
# Should return: PONG
```

### Step 2: Test the Application
```bash
# Navigate to your project
cd FlightBookingEngine

# Start the API
dotnet run --project src/Api/FlightBooking.Api
```

### Step 3: Run Automated Tests
```powershell
# Run the test script
.\test-setup.ps1
```

### Step 4: Manual Testing
Open your browser and test these endpoints:

1. **API Health**: http://localhost:5000/api/Health
2. **PostgreSQL Test**: http://localhost:5000/api/DatabaseTest/connection
3. **Redis Test**: http://localhost:5000/api/RedisTest/connection
4. **Overall Health**: http://localhost:5000/health
5. **API Documentation**: http://localhost:5000/swagger

## 🔧 Expected Test Results

### ✅ Successful PostgreSQL Test:
```json
{
  "status": "Connected",
  "database": "FlightBookingDb_Dev",
  "message": "Successfully connected to PostgreSQL",
  "timestamp": "2025-01-XX..."
}
```

### ✅ Successful Redis Test:
```json
{
  "status": "Connected",
  "redisVersion": "7.x.x",
  "isConnected": true,
  "endPoints": ["localhost:6379"],
  "timestamp": "2025-01-XX..."
}
```

### ✅ Successful Health Check:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
    "npgsql": {
      "status": "Healthy"
    },
    "redis": {
      "status": "Healthy"
    }
  }
}
```

## 🚨 Troubleshooting

### PostgreSQL Issues:
- **Connection refused**: Check if PostgreSQL service is running
- **Authentication failed**: Verify password is `6482297`
- **Database not found**: Create databases in pgAdmin first

### Redis Issues:
- **Connection refused**: 
  ```bash
  # Check if Redis is running
  netstat -an | findstr :6379
  
  # Start Redis if not running
  redis-server
  ```
- **Port already in use**: 
  ```bash
  # Kill existing Redis processes
  taskkill /f /im redis-server.exe
  ```

### Common Commands:
```bash
# Check PostgreSQL service
services.msc  # Look for "postgresql-x64-xx"

# Check Redis process
tasklist | findstr redis

# Test connections
telnet localhost 5432  # PostgreSQL
telnet localhost 6379  # Redis
```

## 🎯 Next Steps After Setup

Once both PostgreSQL and Redis are working:

1. **Run Database Migrations**:
   ```bash
   dotnet ef database update --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api
   ```

2. **Test Cache Operations**:
   - Set cache: POST `/api/RedisTest/cache/set`
   - Get cache: GET `/api/RedisTest/cache/get/{key}`

3. **Start Development**:
   - Begin implementing flight booking features
   - Use the architecture plan as a guide

---

**Your Configuration Summary:**
- ✅ PostgreSQL: localhost:5432 (user: postgres, password: 6482297)
- 🔄 Redis: localhost:6379 (to be installed)
- ✅ Connection strings updated in appsettings files
- ✅ Test controllers ready for verification
