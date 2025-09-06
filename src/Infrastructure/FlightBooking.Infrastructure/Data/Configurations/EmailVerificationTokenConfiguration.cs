using FlightBooking.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");

        builder.HasKey(evt => evt.Id);

        builder.Property(evt => evt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(evt => evt.UserId)
            .IsRequired();

        builder.Property(evt => evt.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(evt => evt.ExpiresAt)
            .IsRequired();

        builder.Property(evt => evt.UsedByIp)
            .HasMaxLength(45);

        builder.Property(evt => evt.CreatedAt)
            .IsRequired();

        builder.Property(evt => evt.UpdatedAt);

        // Indexes
        builder.HasIndex(evt => evt.Token)
            .IsUnique()
            .HasDatabaseName("IX_EmailVerificationTokens_Token");

        builder.HasIndex(evt => evt.UserId)
            .HasDatabaseName("IX_EmailVerificationTokens_UserId");

        builder.HasIndex(evt => evt.Email)
            .HasDatabaseName("IX_EmailVerificationTokens_Email");

        builder.HasIndex(evt => evt.ExpiresAt)
            .HasDatabaseName("IX_EmailVerificationTokens_ExpiresAt");

        builder.HasIndex(evt => evt.IsUsed)
            .HasDatabaseName("IX_EmailVerificationTokens_IsUsed");

        // Relationships
        builder.HasOne(evt => evt.User)
            .WithMany(u => u.EmailVerificationTokens)
            .HasForeignKey(evt => evt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
