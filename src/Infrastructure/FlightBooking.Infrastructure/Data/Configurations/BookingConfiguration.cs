using FlightBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.ContactEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(b => b.ContactPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(b => b.TotalAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(b => b.Status)
            .HasConversion<int>();

        builder.Property(b => b.SpecialRequests)
            .HasMaxLength(1000);

        builder.Property(b => b.PaymentIntentId)
            .HasMaxLength(100);

        builder.Property(b => b.PaymentStatus)
            .HasMaxLength(50);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.LastError)
            .HasMaxLength(1000);

        builder.Property(b => b.GuestId)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(b => b.BookingReference)
            .IsUnique();

        builder.HasIndex(b => b.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(b => b.FlightId);

        builder.HasIndex(b => b.UserId);

        builder.HasIndex(b => b.Status);

        builder.HasIndex(b => b.ExpiresAt);

        builder.HasIndex(b => new { b.ContactEmail, b.FlightId });

        // Relationships
        builder.HasOne(b => b.Flight)
            .WithMany()
            .HasForeignKey(b => b.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(b => b.BookingSeats)
            .WithOne(bs => bs.Booking)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.BookingPassengers)
            .WithOne(bp => bp.Booking)
            .HasForeignKey(bp => bp.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
{
    public void Configure(EntityTypeBuilder<BookingRequest> builder)
    {
        builder.ToTable("BookingRequests");

        builder.HasKey(br => br.Id);

        builder.Property(br => br.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(br => br.RequestHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(br => br.Status)
            .HasConversion<int>();

        builder.Property(br => br.RequestData)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(br => br.ResponseData)
            .HasColumnType("jsonb");

        builder.Property(br => br.ErrorMessage)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(br => br.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(br => br.RequestHash);

        builder.HasIndex(br => br.Status);

        builder.HasIndex(br => br.ExpiresAt);

        builder.HasIndex(br => br.CreatedAt);

        // Relationships
        builder.HasOne(br => br.Booking)
            .WithMany()
            .HasForeignKey(br => br.BookingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.ToTable("BookingSeats");

        builder.HasKey(bs => bs.Id);

        builder.Property(bs => bs.SeatPrice)
            .HasColumnType("decimal(10,2)");

        builder.Property(bs => bs.ExtraFee)
            .HasColumnType("decimal(10,2)");

        builder.Property(bs => bs.HoldStatus)
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(bs => bs.BookingId);

        builder.HasIndex(bs => bs.SeatId);

        builder.HasIndex(bs => bs.PassengerId);

        builder.HasIndex(bs => bs.HoldStatus);

        builder.HasIndex(bs => new { bs.SeatId, bs.HoldStatus });

        // Unique constraint: one booking per seat
        builder.HasIndex(bs => new { bs.SeatId, bs.BookingId })
            .IsUnique()
            .HasFilter("\"HoldStatus\" != 1"); // Not released

        // Relationships
        builder.HasOne(bs => bs.Booking)
            .WithMany(b => b.BookingSeats)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bs => bs.Seat)
            .WithMany()
            .HasForeignKey(bs => bs.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bs => bs.Passenger)
            .WithMany(p => p.BookingSeats)
            .HasForeignKey(bs => bs.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
{
    public void Configure(EntityTypeBuilder<BookingPassenger> builder)
    {
        builder.ToTable("BookingPassengers");

        builder.HasKey(bp => bp.Id);

        builder.Property(bp => bp.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bp => bp.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bp => bp.Gender)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(bp => bp.PassportNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(bp => bp.PassportCountry)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(bp => bp.SpecialRequests)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(bp => bp.BookingId);

        builder.HasIndex(bp => new { bp.PassportNumber, bp.PassportCountry });

        builder.HasIndex(bp => new { bp.FirstName, bp.LastName, bp.DateOfBirth });

        // Relationships
        builder.HasOne(bp => bp.Booking)
            .WithMany(b => b.BookingPassengers)
            .HasForeignKey(bp => bp.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(bp => bp.BookingSeats)
            .WithOne(bs => bs.Passenger)
            .HasForeignKey(bs => bs.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("SeatHolds");

        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.HoldReference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sh => sh.UserId)
            .HasMaxLength(100);

        builder.Property(sh => sh.Status)
            .HasConversion<int>();

        builder.Property(sh => sh.ReleaseReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(sh => sh.HoldReference)
            .IsUnique();

        builder.HasIndex(sh => sh.SeatId);

        builder.HasIndex(sh => sh.UserId);

        builder.HasIndex(sh => sh.Status);

        builder.HasIndex(sh => sh.ExpiresAt);

        builder.HasIndex(sh => new { sh.SeatId, sh.Status });

        builder.HasIndex(sh => new { sh.Status, sh.ExpiresAt });

        // Unique constraint: only one active hold per seat
        builder.HasIndex(sh => sh.SeatId)
            .IsUnique()
            .HasFilter("\"Status\" = 0"); // Only for Held status

        // Relationships
        builder.HasOne(sh => sh.Seat)
            .WithMany()
            .HasForeignKey(sh => sh.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sh => sh.Booking)
            .WithMany()
            .HasForeignKey(sh => sh.BookingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
