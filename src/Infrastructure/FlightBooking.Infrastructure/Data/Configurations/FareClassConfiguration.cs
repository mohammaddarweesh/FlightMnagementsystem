using FlightBooking.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class FareClassConfiguration : IEntityTypeConfiguration<FareClass>
{
    public void Configure(EntityTypeBuilder<FareClass> builder)
    {
        builder.ToTable("FareClasses");

        builder.HasKey(fc => fc.Id);

        builder.Property(fc => fc.ClassName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(fc => fc.ClassCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(fc => fc.Capacity)
            .IsRequired();

        builder.Property(fc => fc.BasePrice)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(fc => fc.CurrentPrice)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(fc => fc.Description)
            .HasMaxLength(500);

        builder.Property(fc => fc.CreatedAt)
            .IsRequired();

        builder.Property(fc => fc.UpdatedAt);

        // Indexes
        builder.HasIndex(fc => fc.FlightId)
            .HasDatabaseName("IX_FareClasses_FlightId");

        builder.HasIndex(fc => fc.ClassCode)
            .HasDatabaseName("IX_FareClasses_ClassCode");

        builder.HasIndex(fc => fc.IsActive)
            .HasDatabaseName("IX_FareClasses_IsActive");

        builder.HasIndex(fc => fc.SortOrder)
            .HasDatabaseName("IX_FareClasses_SortOrder");

        builder.HasIndex(fc => new { fc.FlightId, fc.ClassName })
            .IsUnique()
            .HasDatabaseName("IX_FareClasses_Flight_ClassName");

        builder.HasIndex(fc => new { fc.FlightId, fc.SortOrder })
            .HasDatabaseName("IX_FareClasses_Flight_SortOrder");

        // Relationships
        builder.HasOne(fc => fc.Flight)
            .WithMany(f => f.FareClasses)
            .HasForeignKey(fc => fc.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(fc => fc.Seats)
            .WithOne(s => s.FareClass)
            .HasForeignKey(s => s.FareClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(fc => fc.FareClassAmenities)
            .WithOne(fca => fca.FareClass)
            .HasForeignKey(fca => fca.FareClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SeatNumber)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(s => s.Row)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(s => s.Column)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(s => s.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.ExtraFee)
            .HasPrecision(8, 2);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt);

        // Indexes
        builder.HasIndex(s => s.FlightId)
            .HasDatabaseName("IX_Seats_FlightId");

        builder.HasIndex(s => s.FareClassId)
            .HasDatabaseName("IX_Seats_FareClassId");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Seats_Status");

        builder.HasIndex(s => s.Type)
            .HasDatabaseName("IX_Seats_Type");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Seats_IsActive");

        builder.HasIndex(s => new { s.FlightId, s.SeatNumber })
            .IsUnique()
            .HasDatabaseName("IX_Seats_Flight_SeatNumber");

        builder.HasIndex(s => new { s.FareClassId, s.Row, s.Column })
            .HasDatabaseName("IX_Seats_FareClass_Row_Column");

        // Relationships
        builder.HasOne(s => s.Flight)
            .WithMany(f => f.Seats)
            .HasForeignKey(s => s.FlightId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.FareClass)
            .WithMany(fc => fc.Seats)
            .HasForeignKey(s => s.FareClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
