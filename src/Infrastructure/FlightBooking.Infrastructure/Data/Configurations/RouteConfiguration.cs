using FlightBooking.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RouteCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.Distance)
            .IsRequired();

        builder.Property(r => r.EstimatedFlightTime)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        // Indexes
        builder.HasIndex(r => r.RouteCode)
            .IsUnique()
            .HasDatabaseName("IX_Routes_RouteCode");

        builder.HasIndex(r => r.DepartureAirportId)
            .HasDatabaseName("IX_Routes_DepartureAirportId");

        builder.HasIndex(r => r.ArrivalAirportId)
            .HasDatabaseName("IX_Routes_ArrivalAirportId");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Routes_IsActive");

        builder.HasIndex(r => r.IsInternational)
            .HasDatabaseName("IX_Routes_IsInternational");

        builder.HasIndex(r => new { r.DepartureAirportId, r.ArrivalAirportId })
            .IsUnique()
            .HasDatabaseName("IX_Routes_DepartureArrival");

        // Relationships
        builder.HasOne(r => r.DepartureAirport)
            .WithMany(a => a.DepartureRoutes)
            .HasForeignKey(r => r.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ArrivalAirport)
            .WithMany(a => a.ArrivalRoutes)
            .HasForeignKey(r => r.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Flights)
            .WithOne(f => f.Route)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
