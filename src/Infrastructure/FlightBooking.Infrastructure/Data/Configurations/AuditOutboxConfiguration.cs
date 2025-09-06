using FlightBooking.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Infrastructure.Data.Configurations;

public class AuditOutboxConfiguration : IEntityTypeConfiguration<AuditOutbox>
{
    public void Configure(EntityTypeBuilder<AuditOutbox> builder)
    {
        builder.ToTable("AuditOutbox");

        builder.HasKey(ao => ao.Id);

        builder.Property(ao => ao.CorrelationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ao => ao.Route)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ao => ao.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(ao => ao.IpAddress)
            .IsRequired()
            .HasMaxLength(45); // IPv6 max length

        builder.Property(ao => ao.UserAgent)
            .HasMaxLength(1000);

        builder.Property(ao => ao.GuestId)
            .HasMaxLength(50);

        builder.Property(ao => ao.RequestBody)
            .HasColumnType("text");

        builder.Property(ao => ao.ResponseBody)
            .HasColumnType("text");

        builder.Property(ao => ao.ResultSummary)
            .HasMaxLength(1000);

        builder.Property(ao => ao.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(ao => ao.UserEmail)
            .HasMaxLength(256);

        builder.Property(ao => ao.UserRoles)
            .HasMaxLength(500);

        builder.Property(ao => ao.Headers)
            .HasColumnType("text");

        builder.Property(ao => ao.QueryParameters)
            .HasMaxLength(2000);

        builder.Property(ao => ao.ProcessingError)
            .HasMaxLength(2000);

        builder.Property(ao => ao.Timestamp)
            .IsRequired();

        builder.Property(ao => ao.CreatedAt)
            .IsRequired();

        builder.Property(ao => ao.UpdatedAt);

        // Indexes for outbox processing
        builder.HasIndex(ao => ao.IsProcessed)
            .HasDatabaseName("IX_AuditOutbox_IsProcessed");

        builder.HasIndex(ao => ao.NextRetryAt)
            .HasDatabaseName("IX_AuditOutbox_NextRetryAt");

        builder.HasIndex(ao => new { ao.IsProcessed, ao.NextRetryAt })
            .HasDatabaseName("IX_AuditOutbox_Processing");

        builder.HasIndex(ao => ao.CorrelationId)
            .HasDatabaseName("IX_AuditOutbox_CorrelationId");

        builder.HasIndex(ao => ao.CreatedAt)
            .HasDatabaseName("IX_AuditOutbox_CreatedAt");

        // Index for cleanup operations
        builder.HasIndex(ao => new { ao.IsProcessed, ao.CreatedAt })
            .HasDatabaseName("IX_AuditOutbox_Cleanup");
    }
}
