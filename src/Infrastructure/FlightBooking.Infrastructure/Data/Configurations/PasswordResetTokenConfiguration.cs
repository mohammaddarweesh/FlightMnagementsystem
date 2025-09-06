using FlightBooking.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(prt => prt.Id);

        builder.Property(prt => prt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(prt => prt.UserId)
            .IsRequired();

        builder.Property(prt => prt.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(prt => prt.ExpiresAt)
            .IsRequired();

        builder.Property(prt => prt.RequestedByIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(prt => prt.UsedByIp)
            .HasMaxLength(45);

        builder.Property(prt => prt.CreatedAt)
            .IsRequired();

        builder.Property(prt => prt.UpdatedAt);

        // Indexes
        builder.HasIndex(prt => prt.Token)
            .IsUnique()
            .HasDatabaseName("IX_PasswordResetTokens_Token");

        builder.HasIndex(prt => prt.UserId)
            .HasDatabaseName("IX_PasswordResetTokens_UserId");

        builder.HasIndex(prt => prt.Email)
            .HasDatabaseName("IX_PasswordResetTokens_Email");

        builder.HasIndex(prt => prt.ExpiresAt)
            .HasDatabaseName("IX_PasswordResetTokens_ExpiresAt");

        builder.HasIndex(prt => prt.IsUsed)
            .HasDatabaseName("IX_PasswordResetTokens_IsUsed");

        // Relationships
        builder.HasOne(prt => prt.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
