# Database Schema Documentation

## Overview

The Flight Booking Management System uses PostgreSQL as its primary database with a comprehensive schema designed for scalability, performance, and data integrity. The system uses two separate databases for clear separation of concerns.

## Database Architecture

### Primary Databases

1. **Main Application Database**: `FlightBookingDb_MohammadDarweesh`
   - Core business entities (Users, Flights, Bookings, etc.)
   - Audit logs and system tracking
   - Analytics materialized views

2. **Background Jobs Database**: `flightbookinghangfire_mohammaddarweesh`
   - Hangfire job storage and management
   - Background task scheduling and execution
   - Job history and monitoring

## Core Entity Relationships

### Identity & User Management

```sql
-- Users table - Core user information
Users (
    Id UUID PRIMARY KEY,
    Email VARCHAR(256) UNIQUE NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    IsEmailVerified BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Roles table - System roles
Roles (
    Id UUID PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description VARCHAR(255),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- UserRoles table - Many-to-many relationship
UserRoles (
    UserId UUID REFERENCES Users(Id) ON DELETE CASCADE,
    RoleId UUID REFERENCES Roles(Id) ON DELETE CASCADE,
    AssignedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    AssignedBy UUID REFERENCES Users(Id),
    PRIMARY KEY (UserId, RoleId)
);

-- RefreshTokens table - JWT refresh token management
RefreshTokens (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id) ON DELETE CASCADE,
    Token VARCHAR(255) UNIQUE NOT NULL,
    ExpiresAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    RevokedAt TIMESTAMP WITH TIME ZONE,
    IsRevoked BOOLEAN DEFAULT FALSE
);
```

### Flight Management

```sql
-- Airlines table - Airline information
Airlines (
    Id UUID PRIMARY KEY,
    Code VARCHAR(3) UNIQUE NOT NULL, -- IATA code
    Name VARCHAR(100) NOT NULL,
    Country VARCHAR(100),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Airports table - Airport information
Airports (
    Id UUID PRIMARY KEY,
    Code VARCHAR(3) UNIQUE NOT NULL, -- IATA code
    Name VARCHAR(200) NOT NULL,
    City VARCHAR(100) NOT NULL,
    Country VARCHAR(100) NOT NULL,
    TimeZone VARCHAR(50) NOT NULL,
    Latitude DECIMAL(10,8),
    Longitude DECIMAL(11,8),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Aircraft table - Aircraft types
Aircraft (
    Id UUID PRIMARY KEY,
    Model VARCHAR(100) NOT NULL,
    Manufacturer VARCHAR(100) NOT NULL,
    TotalSeats INTEGER NOT NULL,
    EconomySeats INTEGER NOT NULL,
    BusinessSeats INTEGER NOT NULL,
    FirstClassSeats INTEGER NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Routes table - Flight routes
Routes (
    Id UUID PRIMARY KEY,
    DepartureAirportId UUID REFERENCES Airports(Id),
    ArrivalAirportId UUID REFERENCES Airports(Id),
    Distance INTEGER, -- in kilometers
    EstimatedDuration INTERVAL NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Flights table - Individual flight instances
Flights (
    Id UUID PRIMARY KEY,
    FlightNumber VARCHAR(10) NOT NULL,
    AirlineId UUID REFERENCES Airlines(Id),
    RouteId UUID REFERENCES Routes(Id),
    AircraftId UUID REFERENCES Aircraft(Id),
    DepartureTime TIMESTAMP WITH TIME ZONE NOT NULL,
    ArrivalTime TIMESTAMP WITH TIME ZONE NOT NULL,
    DepartureTimeSpan INTERVAL,
    Status VARCHAR(20) DEFAULT 'Scheduled', -- Scheduled, Delayed, Cancelled, Completed
    BasePrice DECIMAL(10,2) NOT NULL,
    AvailableSeats INTEGER NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### Booking Management

```sql
-- Bookings table - Flight reservations
Bookings (
    Id UUID PRIMARY KEY,
    BookingReference VARCHAR(10) UNIQUE NOT NULL,
    UserId UUID REFERENCES Users(Id),
    FlightId UUID REFERENCES Flights(Id),
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Confirmed, Cancelled, Completed
    TotalAmount DECIMAL(10,2) NOT NULL,
    PaymentStatus VARCHAR(20) DEFAULT 'Pending', -- Pending, Paid, Failed, Refunded
    BookingDate TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Passengers table - Passenger information
Passengers (
    Id UUID PRIMARY KEY,
    BookingId UUID REFERENCES Bookings(Id) ON DELETE CASCADE,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    DateOfBirth DATE,
    PassportNumber VARCHAR(20),
    Nationality VARCHAR(100),
    SeatNumber VARCHAR(5),
    FareClass VARCHAR(20) DEFAULT 'Economy', -- Economy, Business, First
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Payments table - Payment transactions
Payments (
    Id UUID PRIMARY KEY,
    BookingId UUID REFERENCES Bookings(Id),
    Amount DECIMAL(10,2) NOT NULL,
    Currency VARCHAR(3) DEFAULT 'USD',
    PaymentMethod VARCHAR(50), -- CreditCard, PayPal, BankTransfer
    TransactionId VARCHAR(100),
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Completed, Failed, Refunded
    ProcessedAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### Audit & Tracking

```sql
-- AuditLogs table - System audit trail
AuditLogs (
    Id UUID PRIMARY KEY,
    CorrelationId UUID NOT NULL,
    UserId UUID REFERENCES Users(Id),
    GuestId VARCHAR(100),
    Action VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100),
    EntityId VARCHAR(100),
    Changes JSONB,
    IpAddress INET,
    UserAgent TEXT,
    Timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    Duration INTEGER, -- in milliseconds
    StatusCode INTEGER,
    IsSuccessful BOOLEAN DEFAULT TRUE
);

-- Create indexes for audit logs
CREATE INDEX idx_auditlogs_correlationid ON AuditLogs(CorrelationId);
CREATE INDEX idx_auditlogs_userid ON AuditLogs(UserId);
CREATE INDEX idx_auditlogs_timestamp ON AuditLogs(Timestamp);
CREATE INDEX idx_auditlogs_action ON AuditLogs(Action);
```

## Analytics Materialized Views

### Revenue Analytics View
```sql
CREATE MATERIALIZED VIEW analytics_revenue AS
SELECT 
    DATE_TRUNC('day', b.BookingDate) as booking_date,
    f.FlightNumber,
    a.Name as airline_name,
    r.DepartureAirportId,
    r.ArrivalAirportId,
    COUNT(b.Id) as total_bookings,
    SUM(b.TotalAmount) as total_revenue,
    AVG(b.TotalAmount) as average_booking_value,
    COUNT(CASE WHEN b.Status = 'Confirmed' THEN 1 END) as confirmed_bookings,
    COUNT(CASE WHEN b.Status = 'Cancelled' THEN 1 END) as cancelled_bookings
FROM Bookings b
JOIN Flights f ON b.FlightId = f.Id
JOIN Airlines a ON f.AirlineId = a.Id
JOIN Routes r ON f.RouteId = r.Id
WHERE b.BookingDate >= CURRENT_DATE - INTERVAL '2 years'
GROUP BY booking_date, f.FlightNumber, a.Name, r.DepartureAirportId, r.ArrivalAirportId;

CREATE UNIQUE INDEX idx_analytics_revenue_unique 
ON analytics_revenue(booking_date, FlightNumber, airline_name, DepartureAirportId, ArrivalAirportId);
```

### Booking Status Analytics View
```sql
CREATE MATERIALIZED VIEW analytics_booking_status AS
SELECT 
    DATE_TRUNC('day', BookingDate) as booking_date,
    Status,
    PaymentStatus,
    COUNT(*) as booking_count,
    SUM(TotalAmount) as total_amount,
    AVG(TotalAmount) as average_amount
FROM Bookings
WHERE BookingDate >= CURRENT_DATE - INTERVAL '2 years'
GROUP BY booking_date, Status, PaymentStatus;

CREATE UNIQUE INDEX idx_analytics_booking_status_unique 
ON analytics_booking_status(booking_date, Status, PaymentStatus);
```

## Performance Optimizations

### Key Indexes

```sql
-- User management indexes
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_created_at ON Users(CreatedAt);

-- Flight search indexes
CREATE INDEX idx_flights_departure_time ON Flights(DepartureTime);
CREATE INDEX idx_flights_route_departure ON Flights(RouteId, DepartureTime);
CREATE INDEX idx_flights_airline_departure ON Flights(AirlineId, DepartureTime);
CREATE INDEX idx_flights_status ON Flights(Status);

-- Booking indexes
CREATE INDEX idx_bookings_user_id ON Bookings(UserId);
CREATE INDEX idx_bookings_flight_id ON Bookings(FlightId);
CREATE INDEX idx_bookings_booking_date ON Bookings(BookingDate);
CREATE INDEX idx_bookings_status ON Bookings(Status);
CREATE INDEX idx_bookings_reference ON Bookings(BookingReference);

-- Route search indexes
CREATE INDEX idx_routes_departure_arrival ON Routes(DepartureAirportId, ArrivalAirportId);
```

### Database Configuration

```sql
-- Recommended PostgreSQL settings for production
-- postgresql.conf optimizations:

shared_buffers = '256MB'                    -- 25% of RAM for small systems
effective_cache_size = '1GB'               -- 75% of RAM
work_mem = '4MB'                           -- Per connection work memory
maintenance_work_mem = '64MB'              -- For maintenance operations
checkpoint_completion_target = 0.9        -- Spread checkpoints
wal_buffers = '16MB'                       -- WAL buffer size
default_statistics_target = 100           -- Statistics target
random_page_cost = 1.1                    -- For SSD storage
```

## Data Retention & Cleanup

### Audit Log Retention
```sql
-- Cleanup old audit logs (older than 2 years)
DELETE FROM AuditLogs 
WHERE Timestamp < CURRENT_DATE - INTERVAL '2 years';

-- Archive old booking data (older than 5 years)
CREATE TABLE BookingsArchive AS 
SELECT * FROM Bookings 
WHERE BookingDate < CURRENT_DATE - INTERVAL '5 years';

DELETE FROM Bookings 
WHERE BookingDate < CURRENT_DATE - INTERVAL '5 years';
```

## Migration Strategy

### Entity Framework Migrations
The system uses Entity Framework Core migrations for schema management:

```bash
# Create new migration
dotnet ef migrations add MigrationName --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api

# Apply migrations
dotnet ef database update --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api

# Generate SQL script
dotnet ef migrations script --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api
```

### Current Migrations
1. **AddIdentityEntities** - User management and authentication
2. **AddFlightSystem** - Flight, airline, airport, and route entities
3. **AddBookingSystem** - Booking and passenger management
4. **AddAuditSystem** - Audit logging and tracking
5. **AddAnalyticsMaterializedViews** - Analytics and reporting views

## Backup & Recovery

### Backup Strategy
```bash
# Full database backup
pg_dump -h localhost -U postgres -d FlightBookingDb_MohammadDarweesh > backup_$(date +%Y%m%d_%H%M%S).sql

# Compressed backup
pg_dump -h localhost -U postgres -d FlightBookingDb_MohammadDarweesh | gzip > backup_$(date +%Y%m%d_%H%M%S).sql.gz

# Schema-only backup
pg_dump -h localhost -U postgres -d FlightBookingDb_MohammadDarweesh --schema-only > schema_backup.sql
```

### Recovery
```bash
# Restore from backup
psql -h localhost -U postgres -d FlightBookingDb_MohammadDarweesh < backup_file.sql

# Restore compressed backup
gunzip -c backup_file.sql.gz | psql -h localhost -U postgres -d FlightBookingDb_MohammadDarweesh
```

## Security Considerations

### Data Protection
- **Password Hashing**: PBKDF2 with salt for user passwords
- **PII Encryption**: Sensitive data encrypted at application level
- **Audit Trail**: Complete audit logging for compliance
- **Access Control**: Role-based permissions for data access

### Database Security
```sql
-- Create application user with limited privileges
CREATE USER flightbooking_app WITH PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE FlightBookingDb_MohammadDarweesh TO flightbooking_app;
GRANT USAGE ON SCHEMA public TO flightbooking_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO flightbooking_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO flightbooking_app;
```

## Monitoring & Maintenance

### Health Checks
The system includes database health checks that monitor:
- Connection availability
- Query performance
- Disk space usage
- Active connections
- Lock contention

### Regular Maintenance
```sql
-- Update table statistics
ANALYZE;

-- Rebuild indexes if needed
REINDEX DATABASE FlightBookingDb_MohammadDarweesh;

-- Vacuum to reclaim space
VACUUM ANALYZE;
```

This database schema provides a robust foundation for the Flight Booking Management System with proper normalization, indexing, and performance optimizations.
