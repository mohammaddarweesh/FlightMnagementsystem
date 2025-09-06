using FlightBooking.Domain.Analytics;
using FlightBooking.Domain.Audit;
using FlightBooking.Domain.Bookings;
using FlightBooking.Domain.Flights;
using FlightBooking.Domain.Identity;
using FlightBooking.Domain.Promotions;
using FlightBooking.Infrastructure.BackgroundJobs.DeadLetterQueue;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Data;

/// <summary>
/// Application database context.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Identity DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    // Audit DbSets
    public DbSet<AuditEvent> AuditEvents { get; set; }
    public DbSet<AuditOutbox> AuditOutbox { get; set; }

    // Bookings
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingRequest> BookingRequests { get; set; }
    public DbSet<BookingSeat> BookingSeats { get; set; }
    public DbSet<BookingPassenger> BookingPassengers { get; set; }
    public DbSet<SeatHold> SeatHolds { get; set; }

    // Flight DbSets
    public DbSet<Airport> Airports { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<FareClass> FareClasses { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<FareClassAmenity> FareClassAmenities { get; set; }

    // Promotion DbSets
    public DbSet<Promotion> Promotions { get; set; }

    // Background Jobs DbSets
    public DbSet<DeadLetterQueueEntry> DeadLetterQueue { get; set; }

    // Audit DbSets
    public DbSet<AuditEvent> AuditLogs { get; set; }

    // Analytics DbSets (for Entity Framework mapping to materialized views)
    // Note: These are commented out to avoid migration issues since they're materialized views
    // public DbSet<RevenueAnalytics> RevenueAnalytics { get; set; }
    // public DbSet<BookingStatusAnalytics> BookingStatusAnalytics { get; set; }
    // public DbSet<PassengerDemographicsAnalytics> PassengerDemographicsAnalytics { get; set; }
    // public DbSet<RoutePerformanceAnalytics> RoutePerformanceAnalytics { get; set; }

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Analytics entities to map to materialized views
        // ConfigureAnalyticsEntities(modelBuilder); // Commented out to avoid migration issues

        // Ignore problematic navigation properties for now
        modelBuilder.Entity<FlightBooking.Domain.Promotions.Promotion>()
            .Ignore(p => p.Metadata);
        modelBuilder.Entity<FlightBooking.Domain.Promotions.PromotionUsage>()
            .Ignore(pu => pu.Metadata);
    }

    /// <summary>
    /// Override this method to configure the database (and other options) to be used for this context.
    /// </summary>
    /// <param name="optionsBuilder">A builder used to create or modify options for this context.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by DI configuration
            optionsBuilder.UseNpgsql();
        }
    }

    /// <summary>
    /// Configure analytics entities to map to materialized views
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private void ConfigureAnalyticsEntities(ModelBuilder modelBuilder)
    {
        // Revenue Analytics - maps to mv_revenue_daily materialized view
        modelBuilder.Entity<RevenueAnalytics>(entity =>
        {
            entity.ToTable("mv_revenue_daily");
            entity.HasKey(e => new { e.Date, e.RouteCode, e.FareClass, e.AirlineCode });
            entity.Property(e => e.RouteCode).HasDefaultValue("ALL");
            entity.Property(e => e.FareClass).HasDefaultValue("ALL");
            entity.Property(e => e.AirlineCode).HasDefaultValue("ALL");
        });

        // Booking Status Analytics - maps to mv_booking_status_daily materialized view
        modelBuilder.Entity<BookingStatusAnalytics>(entity =>
        {
            entity.ToTable("mv_booking_status_daily");
            entity.HasKey(e => new { e.Date, e.RouteCode, e.FareClass });
            entity.Property(e => e.RouteCode).HasDefaultValue("ALL");
            entity.Property(e => e.FareClass).HasDefaultValue("ALL");
        });

        // Passenger Demographics Analytics - maps to mv_passenger_demographics_daily materialized view
        modelBuilder.Entity<PassengerDemographicsAnalytics>(entity =>
        {
            entity.ToTable("mv_passenger_demographics_daily");
            entity.HasKey(e => new { e.Date, e.RouteCode, e.FareClass });
            entity.Property(e => e.RouteCode).HasDefaultValue("ALL");
            entity.Property(e => e.FareClass).HasDefaultValue("ALL");

            // Configure the dictionary properties as JSON columns
            entity.Property(e => e.PassengersByCountry)
                  .HasColumnType("jsonb")
                  .HasDefaultValue(new Dictionary<string, int>());
            entity.Property(e => e.PassengersByCity)
                  .HasColumnType("jsonb")
                  .HasDefaultValue(new Dictionary<string, int>());
        });

        // Route Performance Analytics - maps to mv_route_performance_daily materialized view
        modelBuilder.Entity<RoutePerformanceAnalytics>(entity =>
        {
            entity.ToTable("mv_route_performance_daily");
            entity.HasKey(e => new { e.Date, e.RouteCode });
        });
    }
}
