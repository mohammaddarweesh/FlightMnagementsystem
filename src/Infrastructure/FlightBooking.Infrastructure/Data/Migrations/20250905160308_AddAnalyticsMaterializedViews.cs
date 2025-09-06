using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsMaterializedViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hangfire");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "Flights",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DepartureTimeSpan",
                table: "Flights",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "TotalSeats",
                table: "Flights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingReference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FlightId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpecialRequests = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    FlightId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Flights_FlightId1",
                        column: x => x.FlightId1,
                        principalTable: "Flights",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "job_dead_letter_queue",
                schema: "hangfire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MethodName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Arguments = table.Column<string>(type: "text", nullable: true),
                    QueueName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RetryAttempts = table.Column<int>(type: "integer", nullable: false),
                    ExceptionMessage = table.Column<string>(type: "text", nullable: true),
                    ExceptionDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstFailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MovedToDeadLetterAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsRequeued = table.Column<bool>(type: "boolean", nullable: false),
                    RequeuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequeuedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_dead_letter_queue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mv_booking_status_daily",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RouteCode = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    FareClass = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    Period = table.Column<string>(type: "text", nullable: false),
                    PendingBookings = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedBookings = table.Column<int>(type: "integer", nullable: false),
                    CheckedInBookings = table.Column<int>(type: "integer", nullable: false),
                    CompletedBookings = table.Column<int>(type: "integer", nullable: false),
                    CancelledBookings = table.Column<int>(type: "integer", nullable: false),
                    ExpiredBookings = table.Column<int>(type: "integer", nullable: false),
                    RefundedBookings = table.Column<int>(type: "integer", nullable: false),
                    PendingPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfirmedPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CompletionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CancellationRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RefundRate = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageBookingToConfirmationMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageConfirmationToCheckInHours = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageBookingToCompletionHours = table.Column<decimal>(type: "numeric", nullable: false),
                    PendingRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfirmedRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    LostRevenueToCancellations = table.Column<decimal>(type: "numeric", nullable: false),
                    RefundedRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    LastRefreshed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mv_booking_status_daily", x => new { x.Date, x.RouteCode, x.FareClass });
                });

            migrationBuilder.CreateTable(
                name: "mv_passenger_demographics_daily",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RouteCode = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    FareClass = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    Period = table.Column<string>(type: "text", nullable: false),
                    PassengersAge0To17 = table.Column<int>(type: "integer", nullable: false),
                    PassengersAge18To24 = table.Column<int>(type: "integer", nullable: false),
                    PassengersAge25To34 = table.Column<int>(type: "integer", nullable: false),
                    PassengersAge35To44 = table.Column<int>(type: "integer", nullable: false),
                    PassengersAge45To54 = table.Column<int>(type: "integer", nullable: false),
                    PassengersAge55To64 = table.Column<int>(type: "integer", nullable: false),
                    PassengersAge65Plus = table.Column<int>(type: "integer", nullable: false),
                    PassengersAgeUnknown = table.Column<int>(type: "integer", nullable: false),
                    MalePassengers = table.Column<int>(type: "integer", nullable: false),
                    FemalePassengers = table.Column<int>(type: "integer", nullable: false),
                    OtherGenderPassengers = table.Column<int>(type: "integer", nullable: false),
                    UnknownGenderPassengers = table.Column<int>(type: "integer", nullable: false),
                    SinglePassengerBookings = table.Column<int>(type: "integer", nullable: false),
                    FamilyBookings = table.Column<int>(type: "integer", nullable: false),
                    GroupBookings = table.Column<int>(type: "integer", nullable: false),
                    BusinessBookings = table.Column<int>(type: "integer", nullable: false),
                    PassengersByCountry = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false, defaultValue: new Dictionary<string, int>()),
                    PassengersByCity = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false, defaultValue: new Dictionary<string, int>()),
                    RevenueFromAge18To34 = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueFromAge35To54 = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueFromAge55Plus = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueFromBusinessClass = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueFromFamilyBookings = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageAge = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageGroupSize = table.Column<decimal>(type: "numeric", nullable: false),
                    BusinessClassPenetration = table.Column<decimal>(type: "numeric", nullable: false),
                    FamilyBookingRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LastRefreshed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mv_passenger_demographics_daily", x => new { x.Date, x.RouteCode, x.FareClass });
                });

            migrationBuilder.CreateTable(
                name: "mv_revenue_daily",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RouteCode = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    FareClass = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    AirlineCode = table.Column<string>(type: "text", nullable: false, defaultValue: "ALL"),
                    Period = table.Column<string>(type: "text", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    FeeRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    ExtraServicesRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalBookings = table.Column<int>(type: "integer", nullable: false),
                    CompletedBookings = table.Column<int>(type: "integer", nullable: false),
                    CancelledBookings = table.Column<int>(type: "integer", nullable: false),
                    RefundedBookings = table.Column<int>(type: "integer", nullable: false),
                    TotalPassengers = table.Column<int>(type: "integer", nullable: false),
                    AverageRevenuePerPassenger = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageRevenuePerBooking = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false),
                    BookedSeats = table.Column<int>(type: "integer", nullable: false),
                    LoadFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenuePer1000ASM = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageFarePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    MinFarePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxFarePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PromotionDiscounts = table.Column<decimal>(type: "numeric", nullable: false),
                    BookingsWithPromotions = table.Column<int>(type: "integer", nullable: false),
                    PromotionPenetrationRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueGrowthRate = table.Column<decimal>(type: "numeric", nullable: false),
                    BookingGrowthRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LastRefreshed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataSource = table.Column<string>(type: "text", nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mv_revenue_daily", x => new { x.Date, x.RouteCode, x.FareClass, x.AirlineCode });
                });

            migrationBuilder.CreateTable(
                name: "mv_route_performance_daily",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RouteCode = table.Column<string>(type: "text", nullable: false),
                    Period = table.Column<string>(type: "text", nullable: false),
                    DepartureAirport = table.Column<string>(type: "text", nullable: false),
                    ArrivalAirport = table.Column<string>(type: "text", nullable: false),
                    DistanceKm = table.Column<int>(type: "integer", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFlights = table.Column<int>(type: "integer", nullable: false),
                    TotalBookings = table.Column<int>(type: "integer", nullable: false),
                    TotalPassengers = table.Column<int>(type: "integer", nullable: false),
                    LoadFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageTicketPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenuePerKm = table.Column<decimal>(type: "numeric", nullable: false),
                    OnTimeFlights = table.Column<int>(type: "integer", nullable: false),
                    DelayedFlights = table.Column<int>(type: "integer", nullable: false),
                    CancelledFlights = table.Column<int>(type: "integer", nullable: false),
                    OnTimePerformance = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageDelayMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    DemandScore = table.Column<decimal>(type: "numeric", nullable: false),
                    SeasonalityIndex = table.Column<decimal>(type: "numeric", nullable: false),
                    CompetitiveIndex = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedCosts = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    ProfitMargin = table.Column<decimal>(type: "numeric", nullable: true),
                    LastRefreshed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mv_route_performance_daily", x => new { x.Date, x.RouteCode });
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTotalUsage = table.Column<int>(type: "integer", nullable: true),
                    MaxUsagePerCustomer = table.Column<int>(type: "integer", nullable: true),
                    MaxUsagePerDay = table.Column<int>(type: "integer", nullable: true),
                    CurrentTotalUsage = table.Column<int>(type: "integer", nullable: false),
                    MinPurchaseAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ApplicableRoutes = table.Column<List<string>>(type: "text[]", nullable: false),
                    ApplicableFareClasses = table.Column<List<string>>(type: "text[]", nullable: false),
                    ExcludedRoutes = table.Column<List<string>>(type: "text[]", nullable: false),
                    ExcludedFareClasses = table.Column<List<string>>(type: "text[]", nullable: false),
                    MinAdvanceDays = table.Column<int>(type: "integer", nullable: true),
                    MaxAdvanceDays = table.Column<int>(type: "integer", nullable: true),
                    IsFirstTimeCustomerOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsCombinableWithOtherOffers = table.Column<bool>(type: "boolean", nullable: false),
                    TargetCustomerSegments = table.Column<List<string>>(type: "text[]", nullable: false),
                    TargetCountries = table.Column<List<string>>(type: "text[]", nullable: false),
                    ApplicableDaysOfWeek = table.Column<int[]>(type: "integer[]", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Terms = table.Column<string>(type: "text", nullable: true),
                    MarketingMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    ItemDescription = table.Column<string>(type: "text", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ItemData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingItem_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingPassengers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PassportNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PassportCountry = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PassportExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SpecialRequests = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsInfant = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPassengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingPassengers_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestData = table.Column<string>(type: "jsonb", nullable: false),
                    ResponseData = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SeatHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    HoldReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HeldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleaseReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionUsage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestId = table.Column<string>(type: "text", nullable: true),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedBy = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionUsage_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingSeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ExtraFee = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    HoldStatus = table.Column<int>(type: "integer", nullable: false),
                    HeldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingSeats_BookingPassengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "BookingPassengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingSeats_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingSeats_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 5, 16, 3, 8, 105, DateTimeKind.Utc).AddTicks(5080));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 5, 16, 3, 8, 105, DateTimeKind.Utc).AddTicks(5084));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 5, 16, 3, 8, 105, DateTimeKind.Utc).AddTicks(5086));

            migrationBuilder.CreateIndex(
                name: "IX_BookingItem_BookingId",
                table: "BookingItem",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassengers_BookingId",
                table: "BookingPassengers",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassengers_FirstName_LastName_DateOfBirth",
                table: "BookingPassengers",
                columns: new[] { "FirstName", "LastName", "DateOfBirth" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassengers_PassportNumber_PassportCountry",
                table: "BookingPassengers",
                columns: new[] { "PassportNumber", "PassportCountry" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_BookingId",
                table: "BookingRequests",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_CreatedAt",
                table: "BookingRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_ExpiresAt",
                table: "BookingRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_IdempotencyKey",
                table: "BookingRequests",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_RequestHash",
                table: "BookingRequests",
                column: "RequestHash");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_Status",
                table: "BookingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingReference",
                table: "Bookings",
                column: "BookingReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ContactEmail_FlightId",
                table: "Bookings",
                columns: new[] { "ContactEmail", "FlightId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ExpiresAt",
                table: "Bookings",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FlightId",
                table: "Bookings",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FlightId1",
                table: "Bookings",
                column: "FlightId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_IdempotencyKey",
                table: "Bookings",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_BookingId",
                table: "BookingSeats",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_HoldStatus",
                table: "BookingSeats",
                column: "HoldStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_PassengerId",
                table: "BookingSeats",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_SeatId",
                table: "BookingSeats",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_SeatId_BookingId",
                table: "BookingSeats",
                columns: new[] { "SeatId", "BookingId" },
                unique: true,
                filter: "\"HoldStatus\" != 1");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_SeatId_HoldStatus",
                table: "BookingSeats",
                columns: new[] { "SeatId", "HoldStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsage_PromotionId",
                table: "PromotionUsage",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_BookingId",
                table: "SeatHolds",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_ExpiresAt",
                table: "SeatHolds",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_HoldReference",
                table: "SeatHolds",
                column: "HoldReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_SeatId",
                table: "SeatHolds",
                column: "SeatId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_SeatId_Status",
                table: "SeatHolds",
                columns: new[] { "SeatId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_Status",
                table: "SeatHolds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_Status_ExpiresAt",
                table: "SeatHolds",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_UserId",
                table: "SeatHolds",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingItem");

            migrationBuilder.DropTable(
                name: "BookingRequests");

            migrationBuilder.DropTable(
                name: "BookingSeats");

            migrationBuilder.DropTable(
                name: "job_dead_letter_queue",
                schema: "hangfire");

            migrationBuilder.DropTable(
                name: "mv_booking_status_daily");

            migrationBuilder.DropTable(
                name: "mv_passenger_demographics_daily");

            migrationBuilder.DropTable(
                name: "mv_revenue_daily");

            migrationBuilder.DropTable(
                name: "mv_route_performance_daily");

            migrationBuilder.DropTable(
                name: "PromotionUsage");

            migrationBuilder.DropTable(
                name: "SeatHolds");

            migrationBuilder.DropTable(
                name: "BookingPassengers");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropColumn(
                name: "DepartureTimeSpan",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "TotalSeats",
                table: "Flights");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "DepartureTime",
                table: "Flights",
                type: "interval",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 20, 30, 8, 107, DateTimeKind.Utc).AddTicks(6525));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 20, 30, 8, 107, DateTimeKind.Utc).AddTicks(6531));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 20, 30, 8, 107, DateTimeKind.Utc).AddTicks(6534));
        }
    }
}
