namespace FlightBooking.Domain.Common;

/// <summary>
/// Base entity class for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Base aggregate root class for domain aggregates.
/// </summary>
public abstract class AggregateRoot<TId> : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public new TId Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the version for optimistic concurrency control.
    /// </summary>
    public int Version { get; set; }
}
