namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Base entity class with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public virtual Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Date and time when the entity was created
    /// </summary>
    public virtual DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the entity was last updated
    /// </summary>
    public virtual DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates if the entity is soft deleted
    /// </summary>
    public virtual bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Date and time when the entity was soft deleted (null if not deleted)
    /// </summary>
    public virtual DateTime? DeletedAt { get; set; }
}