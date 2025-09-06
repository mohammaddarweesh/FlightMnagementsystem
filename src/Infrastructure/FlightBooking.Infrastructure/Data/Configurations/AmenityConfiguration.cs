using FlightBooking.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.IconName)
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt);

        // Indexes
        builder.HasIndex(a => a.Name)
            .IsUnique()
            .HasDatabaseName("IX_Amenities_Name");

        builder.HasIndex(a => a.Category)
            .HasDatabaseName("IX_Amenities_Category");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Amenities_IsActive");

        builder.HasIndex(a => a.SortOrder)
            .HasDatabaseName("IX_Amenities_SortOrder");

        // Relationships
        builder.HasMany(a => a.FareClassAmenities)
            .WithOne(fca => fca.Amenity)
            .HasForeignKey(fca => fca.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FareClassAmenityConfiguration : IEntityTypeConfiguration<FareClassAmenity>
{
    public void Configure(EntityTypeBuilder<FareClassAmenity> builder)
    {
        builder.ToTable("FareClassAmenities");

        builder.HasKey(fca => fca.Id);

        builder.Property(fca => fca.AdditionalCost)
            .HasPrecision(8, 2);

        builder.Property(fca => fca.Notes)
            .HasMaxLength(500);

        builder.Property(fca => fca.CreatedAt)
            .IsRequired();

        builder.Property(fca => fca.UpdatedAt);

        // Indexes
        builder.HasIndex(fca => fca.FareClassId)
            .HasDatabaseName("IX_FareClassAmenities_FareClassId");

        builder.HasIndex(fca => fca.AmenityId)
            .HasDatabaseName("IX_FareClassAmenities_AmenityId");

        builder.HasIndex(fca => fca.IsIncluded)
            .HasDatabaseName("IX_FareClassAmenities_IsIncluded");

        builder.HasIndex(fca => new { fca.FareClassId, fca.AmenityId })
            .IsUnique()
            .HasDatabaseName("IX_FareClassAmenities_FareClass_Amenity");

        // Relationships
        builder.HasOne(fca => fca.FareClass)
            .WithMany(fc => fc.FareClassAmenities)
            .HasForeignKey(fca => fca.FareClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fca => fca.Amenity)
            .WithMany(a => a.FareClassAmenities)
            .HasForeignKey(fca => fca.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
