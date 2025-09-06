using FlightBooking.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.ToTable("Airports");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.IataCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(a => a.IcaoCode)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(a => a.Latitude)
            .HasPrecision(10, 7);

        builder.Property(a => a.Longitude)
            .HasPrecision(10, 7);

        builder.Property(a => a.TimeZone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.Website)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt);

        // Indexes
        builder.HasIndex(a => a.IataCode)
            .IsUnique()
            .HasDatabaseName("IX_Airports_IataCode");

        builder.HasIndex(a => a.IcaoCode)
            .IsUnique()
            .HasDatabaseName("IX_Airports_IcaoCode");

        builder.HasIndex(a => a.CountryCode)
            .HasDatabaseName("IX_Airports_CountryCode");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Airports_IsActive");

        builder.HasIndex(a => new { a.City, a.Country })
            .HasDatabaseName("IX_Airports_City_Country");

        // Relationships
        builder.HasMany(a => a.DepartureRoutes)
            .WithOne(r => r.DepartureAirport)
            .HasForeignKey(r => r.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.ArrivalRoutes)
            .WithOne(r => r.ArrivalAirport)
            .HasForeignKey(r => r.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
