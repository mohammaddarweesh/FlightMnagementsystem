# Flight Booking Management System - Developer Guide

A comprehensive flight booking and management system with advanced analytics, pricing, and booking capabilities built with .NET 8, PostgreSQL, and Redis.

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Redis 7+](https://redis.io/download)
- [Git](https://git-scm.com/downloads)

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/flight-booking-system.git
cd flight-booking-system
```

### 2. Install and Configure PostgreSQL

#### Windows (using Chocolatey)
```powershell
# Install PostgreSQL
choco install postgresql

# Start PostgreSQL service
net start postgresql-x64-15

# Create database
psql -U postgres -c "CREATE DATABASE \"FlightBookingDb_MohammadDarweesh\";"
psql -U postgres -c "CREATE DATABASE \"flightbookinghangfire_mohammaddarweesh\";"
```

#### macOS (using Homebrew)
```bash
# Install PostgreSQL
brew install postgresql@15

# Start PostgreSQL service
brew services start postgresql@15

# Create database
createdb "FlightBookingDb_MohammadDarweesh"
createdb "flightbookinghangfire_mohammaddarweesh"
```

#### Linux (Ubuntu/Debian)
```bash
# Install PostgreSQL
sudo apt update
sudo apt install postgresql postgresql-contrib

# Start PostgreSQL service
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Create database
sudo -u postgres createdb "FlightBookingDb_MohammadDarweesh"
sudo -u postgres createdb "flightbookinghangfire_mohammaddarweesh"
```

#### Docker (Alternative)
```bash
# Run PostgreSQL in Docker
docker run --name flightbooking-postgres \
  -e POSTGRES_DB=FlightBookingDb_MohammadDarweesh \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=6482297 \
  -p 5432:5432 \
  -d postgres:15

# Wait for container to be ready
docker exec -it flightbooking-postgres pg_isready -U postgres

# Create Hangfire database
docker exec -it flightbooking-postgres psql -U postgres -c "CREATE DATABASE \"flightbookinghangfire_mohammaddarweesh\";"
```

### 3. Install and Configure Redis

#### Windows (using Chocolatey)
```powershell
# Install Redis
choco install redis-64

# Start Redis service
redis-server
```

#### macOS (using Homebrew)
```bash
# Install Redis
brew install redis

# Start Redis service
brew services start redis

# Or run in foreground
redis-server
```

#### Linux (Ubuntu/Debian)
```bash
# Install Redis
sudo apt update
sudo apt install redis-server

# Start Redis service
sudo systemctl start redis-server
sudo systemctl enable redis-server

# Test Redis
redis-cli ping
```

#### Docker (Alternative)
```bash
# Run Redis in Docker
docker run --name flightbooking-redis \
  -p 6379:6379 \
  -d redis:7-alpine

# Test Redis connection
docker exec -it flightbooking-redis redis-cli ping
```

### 4. Configure Application Settings

Current connection strings in `src/Api/FlightBooking.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=FlightBookingDb_MohammadDarweesh;Username=postgres;Password=6482297",
    "Redis": "localhost:6379",
    "Hangfire": "Host=localhost;Database=flightbookinghangfire_mohammaddarweesh;Username=postgres;Password=6482297"
  }
}
```

**Note**: The application automatically creates databases and runs migrations on startup.

### 5. Database Setup

**Automatic Setup (Recommended):**
The application automatically creates databases and runs migrations on startup. Just run the application:

```bash
# Navigate to the solution root
cd /path/to/FlightMnagementsystem

# Run the application (databases will be created automatically)
dotnet run --project src/Api/FlightBooking.Api
```

**Manual Setup (Optional):**
```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply migrations manually
dotnet ef database update --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api
```

### 6. Build and Run the Application

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project src/Api/FlightBooking.Api
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/api-docs`

## 🔐 Default Test Credentials

### Admin User
- **Email**: `admin@flightbooking.com`
- **Password**: `Admin123!@#`
- **Roles**: Admin, Staff
- **Permissions**: Full access to all endpoints

### Staff User
- **Email**: `staff@flightbooking.com`
- **Password**: `Staff123!`
- **Roles**: Staff
- **Permissions**: Access to analytics, bookings, flights

### Customer User
- **Email**: `customer@example.com`
- **Password**: `Customer123!`
- **Roles**: Customer
- **Permissions**: Limited access to own bookings

> **Note**: These are default development credentials. Change them in production!

## 📊 Analytics API Overview

The Analytics API provides comprehensive business intelligence capabilities:

### Key Features
- **Revenue Analytics**: Track revenue by route, fare class, airline, and time period
- **Booking Analytics**: Monitor booking statuses, conversion rates, and trends
- **Demographics**: Analyze passenger demographics and travel patterns
- **Route Performance**: Evaluate route profitability and operational metrics
- **Data Export**: Export analytics data in CSV, Excel, JSON, and PDF formats
- **Real-time Refresh**: Update analytics data on-demand or via scheduled jobs

### Available Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/analytics/summary` | GET | Comprehensive analytics summary |
| `/api/analytics/dashboard` | GET | Dashboard data with key metrics |
| `/api/analytics/revenue` | GET | Revenue analytics with filtering |
| `/api/analytics/booking-status` | GET | Booking status analytics |
| `/api/analytics/demographics` | GET | Passenger demographics |
| `/api/analytics/route-performance` | GET | Route performance metrics |
| `/api/analytics/export/csv` | POST | Export data to CSV |
| `/api/analytics/refresh` | POST | Refresh analytics data |

## 🧪 Testing the API

### Using Swagger UI
1. Navigate to `https://localhost:5001/api-docs`
2. Click "Authorize" and enter your JWT token
3. Explore and test the endpoints interactively

### Using Postman
1. Import the collection: `docs/postman/FlightBooking-Analytics-API.postman_collection.json`
2. Import the environment: `docs/postman/FlightBooking-Development.postman_environment.json`
3. Update environment variables with your credentials
4. Run the "Login" request to get an access token
5. Test other endpoints

### Using cURL
See the comprehensive examples in:
- `docs/curl/analytics-api-examples.sh` - Full test suite
- `docs/curl/quick-examples.md` - Quick copy-paste examples

### Sample Request
```bash
# Get analytics summary
curl -X GET \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  "https://localhost:5001/api/analytics/summary?startDate=2024-01-01&endDate=2024-01-31"
```

## 🏗️ Architecture Overview

### Project Structure
```
src/
├── Api/                    # Web API layer
├── Application/            # Application services and interfaces
├── Domain/                 # Domain entities and business logic
├── Infrastructure/         # Data access and external services
├── Contracts/             # DTOs and contracts
└── BackgroundJobs/        # Background job services
```

### Key Technologies
- **Framework**: .NET 8
- **Database**: PostgreSQL with Entity Framework Core
- **Caching**: Redis
- **Background Jobs**: Hangfire
- **Authentication**: JWT Bearer tokens
- **API Documentation**: Swagger/OpenAPI
- **Logging**: Serilog

### Analytics Architecture
- **Materialized Views**: Pre-computed analytics for fast queries
- **Caching Layer**: Redis caching for frequently accessed data
- **Background Refresh**: Scheduled updates of analytics data
- **Export Engine**: Flexible data export in multiple formats

## 🔧 Configuration

### Environment Variables
```bash
# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION="Host=localhost;Database=FlightBookingDb;Username=flightbooking;Password=FlightBooking123!"

# Redis
CONNECTIONSTRINGS__REDIS="localhost:6379"

# JWT
JWT__SECRETKEY="your-secret-key-here"
JWT__ISSUER="FlightBookingAPI"
JWT__AUDIENCE="FlightBookingClients"

# Analytics
ANALYTICS__CACHEPROVIDER="Redis"
ANALYTICS__CACHEEXPIRATIONMINUTES=60
ANALYTICS__MAXEXPORTROWS=100000
```

### Analytics Configuration
```json
{
  "Analytics": {
    "CacheProvider": "Redis",
    "CacheExpirationMinutes": 60,
    "MaxExportRows": 100000,
    "DefaultDateRange": 30,
    "RefreshTimeoutMinutes": 30,
    "EnableHealthChecks": true,
    "DataRetentionYears": 2
  }
}
```

## 🚀 Deployment

### Docker Deployment
```bash
# Build and run with Docker Compose
docker-compose up -d

# Or build individual containers
docker build -t flightbooking-api .
docker run -p 5000:80 flightbooking-api
```

### Production Considerations
1. **Security**: Update default credentials and JWT secrets
2. **Database**: Use managed PostgreSQL service (AWS RDS, Azure Database, etc.)
3. **Caching**: Use managed Redis service (AWS ElastiCache, Azure Cache, etc.)
4. **Monitoring**: Implement application monitoring and logging
5. **Scaling**: Consider horizontal scaling for high traffic

## 📚 Additional Resources

- [OpenAPI Specification](./openapi.json) - Complete API specification
- [Postman Collection](./postman/) - Ready-to-use API testing collection
- [cURL Examples](./curl/) - Command-line testing examples
- [Database Schema](./database-schema.md) - Database design documentation
- [Deployment Guide](./deployment-guide.md) - Production deployment instructions

## 🤝 Support

For questions and support:
- Create an issue on GitHub
- Check the [FAQ](./faq.md)
- Review the [troubleshooting guide](./troubleshooting.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.
