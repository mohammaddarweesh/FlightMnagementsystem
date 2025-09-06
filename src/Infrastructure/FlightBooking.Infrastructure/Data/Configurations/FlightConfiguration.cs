using FlightBooking.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ToTable("Flights");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FlightNumber)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(f => f.AirlineCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(f => f.AirlineName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.AircraftType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.DepartureDate)
            .IsRequired();

        builder.Property(f => f.DepartureTime)
            .IsRequired();

        builder.Property(f => f.ArrivalTime)
            .IsRequired();

        builder.Property(f => f.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(f => f.Gate)
            .HasMaxLength(10);

        builder.Property(f => f.Terminal)
            .HasMaxLength(10);

        builder.Property(f => f.Notes)
            .HasMaxLength(1000);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt);

        // Indexes
        builder.HasIndex(f => f.FlightNumber)
            .HasDatabaseName("IX_Flights_FlightNumber");

        builder.HasIndex(f => f.AirlineCode)
            .HasDatabaseName("IX_Flights_AirlineCode");

        builder.HasIndex(f => f.RouteId)
            .HasDatabaseName("IX_Flights_RouteId");

        builder.HasIndex(f => f.DepartureDate)
            .HasDatabaseName("IX_Flights_DepartureDate");

        builder.HasIndex(f => f.Status)
            .HasDatabaseName("IX_Flights_Status");

        builder.HasIndex(f => f.IsActive)
            .HasDatabaseName("IX_Flights_IsActive");

        builder.HasIndex(f => new { f.AirlineCode, f.FlightNumber, f.DepartureDate })
            .IsUnique()
            .HasDatabaseName("IX_Flights_Unique_Flight");

        builder.HasIndex(f => new { f.RouteId, f.DepartureDate })
            .HasDatabaseName("IX_Flights_Route_Date");

        // Relationships
        builder.HasOne(f => f.Route)
            .WithMany(r => r.Flights)
            .HasForeignKey(f => f.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.FareClasses)
            .WithOne(fc => fc.Flight)
            .HasForeignKey(fc => fc.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Seats)
            .WithOne(s => s.Flight)
            .HasForeignKey(s => s.FlightId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
