using FlightBooking.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.CorrelationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ae => ae.Route)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ae => ae.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(ae => ae.IpAddress)
            .IsRequired()
            .HasMaxLength(45); // IPv6 max length

        builder.Property(ae => ae.UserAgent)
            .HasMaxLength(1000);

        builder.Property(ae => ae.GuestId)
            .HasMaxLength(50);

        builder.Property(ae => ae.RequestBody)
            .HasColumnType("text");

        builder.Property(ae => ae.ResponseBody)
            .HasColumnType("text");

        builder.Property(ae => ae.ResultSummary)
            .HasMaxLength(1000);

        builder.Property(ae => ae.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(ae => ae.UserEmail)
            .HasMaxLength(256);

        builder.Property(ae => ae.UserRoles)
            .HasMaxLength(500);

        builder.Property(ae => ae.Headers)
            .HasColumnType("text");

        builder.Property(ae => ae.QueryParameters)
            .HasMaxLength(2000);

        builder.Property(ae => ae.Timestamp)
            .IsRequired();

        builder.Property(ae => ae.CreatedAt)
            .IsRequired();

        builder.Property(ae => ae.UpdatedAt);

        // Indexes for performance
        builder.HasIndex(ae => ae.CorrelationId)
            .HasDatabaseName("IX_AuditEvents_CorrelationId");

        builder.HasIndex(ae => ae.UserId)
            .HasDatabaseName("IX_AuditEvents_UserId");

        builder.HasIndex(ae => ae.GuestId)
            .HasDatabaseName("IX_AuditEvents_GuestId");

        builder.HasIndex(ae => ae.Route)
            .HasDatabaseName("IX_AuditEvents_Route");

        builder.HasIndex(ae => ae.HttpMethod)
            .HasDatabaseName("IX_AuditEvents_HttpMethod");

        builder.HasIndex(ae => ae.StatusCode)
            .HasDatabaseName("IX_AuditEvents_StatusCode");

        builder.HasIndex(ae => ae.Timestamp)
            .HasDatabaseName("IX_AuditEvents_Timestamp");

        builder.HasIndex(ae => ae.IpAddress)
            .HasDatabaseName("IX_AuditEvents_IpAddress");

        builder.HasIndex(ae => ae.UserEmail)
            .HasDatabaseName("IX_AuditEvents_UserEmail");

        // Composite indexes for common queries
        builder.HasIndex(ae => new { ae.UserId, ae.Timestamp })
            .HasDatabaseName("IX_AuditEvents_UserId_Timestamp");

        builder.HasIndex(ae => new { ae.Route, ae.Timestamp })
            .HasDatabaseName("IX_AuditEvents_Route_Timestamp");

        builder.HasIndex(ae => new { ae.StatusCode, ae.Timestamp })
            .HasDatabaseName("IX_AuditEvents_StatusCode_Timestamp");
    }
}
