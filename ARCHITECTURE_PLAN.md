# Flight Booking Engine - Architecture Plan

## 1. Architecture Summary

### Bounded Contexts & Domain Separation
- **Identity Context**: User management, authentication, authorization
- **Flights Context**: Flight schedules, routes, aircraft management
- **Inventory Context**: Seat availability, pricing, capacity management
- **Booking Context**: Reservations, payments, ticketing
- **Promotions Context**: Discounts, campaigns, loyalty programs
- **Reporting Context**: Analytics, business intelligence, metrics
- **Audit Context**: Event sourcing, compliance, telemetry

### Architecture Pattern
**Clean Architecture + CQRS** with vertical slice organization per bounded context, ensuring:
- Domain-driven design principles
- Command/Query separation for scalability
- Event-driven communication between contexts
- Transactional consistency within boundaries
- Eventual consistency across boundaries

### Concurrency & Data Integrity Strategy
- **Pessimistic Locking**: PostgreSQL row locks for seat reservations
- **Idempotency**: UUID-based keys for booking operations
- **Distributed Coordination**: Redis distributed locks for cross-service operations
- **Event Sourcing**: Transactional outbox pattern for reliable messaging

## 2. Solution Structure (ASCII)

```
FlightBookingEngine/
├── src/
│   ├── Api/
│   │   ├── FlightBooking.Api/                    # Main API Gateway
│   │   ├── FlightBooking.Api.Identity/           # Identity microservice
│   │   ├── FlightBooking.Api.Flights/            # Flights microservice
│   │   ├── FlightBooking.Api.Booking/            # Booking microservice
│   │   └── FlightBooking.Api.Reporting/          # Reporting microservice
│   │
│   ├── Application/
│   │   ├── FlightBooking.Application.Identity/   # Identity CQRS handlers
│   │   ├── FlightBooking.Application.Flights/    # Flights CQRS handlers
│   │   ├── FlightBooking.Application.Inventory/  # Inventory CQRS handlers
│   │   ├── FlightBooking.Application.Booking/    # Booking CQRS handlers
│   │   ├── FlightBooking.Application.Promotions/ # Promotions CQRS handlers
│   │   ├── FlightBooking.Application.Reporting/  # Reporting CQRS handlers
│   │   └── FlightBooking.Application.Shared/     # Shared application logic
│   │
│   ├── Domain/
│   │   ├── FlightBooking.Domain.Identity/        # Identity domain models
│   │   ├── FlightBooking.Domain.Flights/         # Flights domain models
│   │   ├── FlightBooking.Domain.Inventory/       # Inventory domain models
│   │   ├── FlightBooking.Domain.Booking/         # Booking domain models
│   │   ├── FlightBooking.Domain.Promotions/      # Promotions domain models
│   │   ├── FlightBooking.Domain.Audit/           # Audit domain models
│   │   └── FlightBooking.Domain.Shared/          # Shared domain logic
│   │
│   ├── Infrastructure/
│   │   ├── FlightBooking.Infrastructure.Data/    # EF Core, repositories
│   │   ├── FlightBooking.Infrastructure.Cache/   # Redis implementation
│   │   ├── FlightBooking.Infrastructure.Messaging/ # Event bus, outbox
│   │   ├── FlightBooking.Infrastructure.Auth/    # JWT, identity providers
│   │   ├── FlightBooking.Infrastructure.External/ # Third-party integrations
│   │   └── FlightBooking.Infrastructure.Shared/  # Cross-cutting concerns
│   │
│   ├── BackgroundJobs/
│   │   ├── FlightBooking.BackgroundJobs.Worker/  # Hangfire worker service
│   │   ├── FlightBooking.BackgroundJobs.Email/   # Email processing jobs
│   │   ├── FlightBooking.BackgroundJobs.Reports/ # Report generation jobs
│   │   ├── FlightBooking.BackgroundJobs.Cleanup/ # Data cleanup jobs
│   │   └── FlightBooking.BackgroundJobs.Pricing/ # Dynamic pricing jobs
│   │
│   └── Contracts/
│       ├── FlightBooking.Contracts.Identity/     # Identity DTOs, events
│       ├── FlightBooking.Contracts.Flights/      # Flights DTOs, events
│       ├── FlightBooking.Contracts.Booking/      # Booking DTOs, events
│       ├── FlightBooking.Contracts.Promotions/   # Promotions DTOs, events
│       └── FlightBooking.Contracts.Shared/       # Shared contracts
│
├── tests/
│   ├── FlightBooking.UnitTests/                  # Domain & application tests
│   ├── FlightBooking.IntegrationTests/           # API & infrastructure tests
│   ├── FlightBooking.ArchitectureTests/          # Architecture compliance tests
│   └── FlightBooking.PerformanceTests/           # Load & performance tests
│
├── docs/
│   ├── architecture/                             # Architecture documentation
│   ├── api/                                      # API documentation
│   └── deployment/                               # Deployment guides
│
└── scripts/
    ├── database/                                 # Migration scripts
    ├── deployment/                               # Deployment scripts
    └── monitoring/                               # Monitoring setup
```

## 3. Key Entities & Relationships

### Identity Context
- **User**: UserId, Email, PasswordHash, EmailVerified, CreatedAt
- **Role**: RoleId, Name, Permissions
- **UserRole**: UserId, RoleId
- **RefreshToken**: TokenId, UserId, Token, ExpiresAt

### Flights Context
- **Aircraft**: AircraftId, Model, Capacity, Status
- **Airport**: AirportId, Code, Name, City, Country, Timezone
- **Route**: RouteId, OriginAirportId, DestinationAirportId, Distance
- **Flight**: FlightId, FlightNumber, RouteId, AircraftId, DepartureTime, ArrivalTime, Status
- **FlightSchedule**: ScheduleId, FlightId, DayOfWeek, EffectiveFrom, EffectiveTo

### Inventory Context
- **SeatClass**: ClassId, Name, Description (Economy, Business, First)
- **SeatMap**: SeatMapId, AircraftId, SeatNumber, ClassId, Position
- **FlightInventory**: InventoryId, FlightId, ClassId, TotalSeats, AvailableSeats, BasePrice
- **PricingRule**: RuleId, ClassId, DaysBeforeDeparture, PriceMultiplier

### Booking Context
- **Booking**: BookingId, UserId, BookingReference, Status, TotalAmount, CreatedAt
- **BookingItem**: ItemId, BookingId, FlightId, PassengerName, SeatNumber, Price
- **Payment**: PaymentId, BookingId, Amount, Method, Status, TransactionId
- **Passenger**: PassengerId, BookingId, FirstName, LastName, DateOfBirth, PassportNumber

### Promotions Context
- **Promotion**: PromotionId, Code, Description, DiscountType, DiscountValue, ValidFrom, ValidTo
- **PromotionRule**: RuleId, PromotionId, RuleType, RuleValue (route, class, user segment)

### Audit Context
- **AuditLog**: LogId, UserId, Action, EntityType, EntityId, OldValues, NewValues, Timestamp
- **EventOutbox**: EventId, AggregateId, EventType, EventData, CreatedAt, ProcessedAt

## 4. NuGet Package Dependencies

### Core Framework
```xml
<!-- .NET 8 Web API -->
<PackageReference Include="Microsoft.AspNetCore.App" Version="8.0.*" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.*" />

<!-- Entity Framework Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.*" />
```

### CQRS & Messaging
```xml
<!-- MediatR for CQRS -->
<PackageReference Include="MediatR" Version="12.2.*" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.*" />

<!-- FluentValidation -->
<PackageReference Include="FluentValidation" Version="11.8.*" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.*" />
```

### Caching & Background Jobs
```xml
<!-- Redis -->
<PackageReference Include="StackExchange.Redis" Version="2.7.*" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.*" />
<PackageReference Include="RedLock.net" Version="2.3.*" />

<!-- Hangfire -->
<PackageReference Include="Hangfire.Core" Version="1.8.*" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.*" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.*" />
```

### Security & Authentication
```xml
<!-- JWT Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.*" />

<!-- Rate Limiting -->
<PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="8.0.*" />
```

### Logging & Monitoring
```xml
<!-- Serilog -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
<PackageReference Include="Serilog.Sinks.PostgreSQL" Version="2.3.*" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="2.3.*" />

<!-- Health Checks -->
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="8.0.*" />
<PackageReference Include="AspNetCore.HealthChecks.PostgreSql" Version="7.1.*" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="7.0.*" />
<PackageReference Include="AspNetCore.HealthChecks.Hangfire" Version="7.0.*" />
```

### API Documentation & Problem Details
```xml
<!-- Swagger/OpenAPI -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.*" />

<!-- Problem Details -->
<PackageReference Include="Hellang.Middleware.ProblemDetails" Version="6.5.*" />
```

### Testing
```xml
<!-- Unit Testing -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.*" />
<PackageReference Include="xunit" Version="2.6.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />
<PackageReference Include="Moq" Version="4.20.*" />

<!-- Integration Testing -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.6.*" />
<PackageReference Include="Testcontainers.Redis" Version="3.6.*" />

<!-- Architecture Testing -->
<PackageReference Include="NetArchTest.Rules" Version="1.3.*" />
```

## 5. Risk Assessment & Mitigations

### High-Risk Areas

**1. Concurrency & Double Booking**
- **Risk**: Multiple users booking the same seat simultaneously
- **Mitigation**:
  - Pessimistic locking with `SELECT ... FOR UPDATE SKIP LOCKED`
  - Idempotency keys for all booking operations
  - Redis distributed locks for cross-service coordination
  - Saga pattern for complex booking workflows

**2. Performance Under Load**
- **Risk**: System degradation during peak booking periods
- **Mitigation**:
  - Redis caching for flight availability with 30-second TTL
  - Database read replicas for query scaling
  - Rate limiting (100 requests/minute per user)
  - Circuit breaker pattern for external services
  - Horizontal scaling with load balancers

**3. Data Consistency Across Services**
- **Risk**: Inconsistent state between bounded contexts
- **Mitigation**:
  - Transactional outbox pattern for reliable messaging
  - Event sourcing for critical business events
  - Compensating transactions for saga failures
  - Eventually consistent read models

**4. Payment Processing Failures**
- **Risk**: Payment failures leaving bookings in inconsistent state
- **Mitigation**:
  - Two-phase commit for payment + booking
  - Payment timeout handling (15-minute reservation hold)
  - Automatic refund processing for failed bookings
  - Dead letter queue for failed payment events

**5. Security Vulnerabilities**
- **Risk**: Unauthorized access, data breaches, injection attacks
- **Mitigation**:
  - JWT with short expiry (15 min) + refresh tokens
  - Role-based access control (RBAC)
  - Input validation with FluentValidation
  - SQL injection prevention via EF Core
  - Rate limiting and DDoS protection

## 6. Acceptance Criteria

### Functional Requirements

**Flight Search & Availability**
- ✅ Users can search flights by origin, destination, departure date, return date (optional)
- ✅ System displays real-time seat availability and pricing for all cabin classes
- ✅ Search results load within 2 seconds for 95% of requests
- ✅ Cache flight data with 30-second TTL, invalidate on inventory changes

**Booking Process**
- ✅ Users can select seats and complete booking within 15-minute session timeout
- ✅ System prevents double-booking through pessimistic locking
- ✅ All booking operations are idempotent with unique booking references
- ✅ Payment processing integrates with external gateway with retry logic
- ✅ Booking confirmation emails sent within 30 seconds

**User Management & Security**
- ✅ User registration with email verification required
- ✅ JWT authentication with 15-minute access tokens + refresh tokens
- ✅ Role-based access: Guest (search only), Customer (book), Staff (manage), Admin (full access)
- ✅ Password reset functionality with secure token expiry

**Background Processing**
- ✅ Email notifications processed asynchronously via Hangfire
- ✅ Failed jobs retry with exponential backoff (3 attempts max)
- ✅ Daily cleanup jobs for expired reservations and old audit logs
- ✅ Dynamic pricing updates run every 6 hours

### Non-Functional Requirements

**Performance**
- ✅ API response times: 95th percentile < 500ms
- ✅ Support 1000 concurrent users during peak periods
- ✅ Database queries optimized with proper indexing
- ✅ Redis cache hit ratio > 80% for flight searches

**Reliability & Availability**
- ✅ System uptime: 99.9% (8.76 hours downtime/year max)
- ✅ Health checks for all critical dependencies
- ✅ Graceful degradation when external services fail
- ✅ Database backups every 6 hours with point-in-time recovery

**Security & Compliance**
- ✅ All user actions logged for audit trail
- ✅ PII data encrypted at rest and in transit
- ✅ Rate limiting: 100 requests/minute per authenticated user
- ✅ GDPR compliance for data deletion requests

**Monitoring & Observability**
- ✅ Structured logging with Serilog to PostgreSQL
- ✅ Application metrics and health dashboards
- ✅ Alert notifications for critical failures
- ✅ Performance monitoring with response time tracking

---

## Summary

This architecture plan provides a comprehensive foundation for building a scalable, secure, and maintainable Flight Booking Engine using .NET 8 and modern architectural patterns. The design emphasizes:

- **Domain-driven design** with clear bounded contexts
- **CQRS + Event Sourcing** for scalability and auditability
- **Robust concurrency control** to prevent double-booking scenarios
- **Comprehensive caching strategy** for optimal performance
- **Security-first approach** with JWT, RBAC, and audit logging
- **Resilient background processing** with Hangfire and retry policies

The modular structure allows for independent development and deployment of each bounded context while maintaining data consistency through well-defined integration patterns.

**Next Steps**: Begin with the Identity and Flights contexts as foundational services, then incrementally add Inventory, Booking, and other contexts following the established patterns.

---

*Document created: 2025-08-30*
*Author: Senior .NET Solutions Architect*
*Version: 1.0*
