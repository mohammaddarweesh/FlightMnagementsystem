using FlightBooking.Domain.Analytics;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Infrastructure.Analytics.Data;

/// <summary>
/// Separate DbContext for analytics materialized views
/// </summary>
public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    // Analytics DbSets (for Entity Framework mapping to materialized views)
    public DbSet<RevenueAnalytics> RevenueAnalytics { get; set; }
    public DbSet<BookingStatusAnalytics> BookingStatusAnalytics { get; set; }
    public DbSet<PassengerDemographicsAnalytics> PassengerDemographicsAnalytics { get; set; }
    public DbSet<RoutePerformanceAnalytics> RoutePerformanceAnalytics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Analytics entities to map to materialized views
        ConfigureAnalyticsEntities(modelBuilder);
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
