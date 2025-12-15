using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Defines a category with multilingual synonyms for normalized search
/// Following English-First Query Pattern: all searches normalize to English keys
/// </summary>
public class CategoryDefinition : BaseEntity
{
    /// <summary>
    /// The normalized English key (e.g., "pizza", "restaurant", "bank")
    /// This is the canonical identifier used in CategoryKeys arrays
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public virtual string Key { get; set; } = string.Empty;

    /// <summary>
    /// English display name for UI (e.g., "Pizza Restaurant")
    /// </summary>
    [Required]
    [StringLength(200)]
    public virtual string DisplayNameEn { get; set; } = string.Empty;

    /// <summary>
    /// Spanish display name for UI (e.g., "Pizzería")
    /// </summary>
    [Required]
    [StringLength(200)]
    public virtual string DisplayNameEs { get; set; } = string.Empty;

    /// <summary>
    /// Parent category key for hierarchical categorization (optional)
    /// Example: "italian_pizza" might have parent "restaurant"
    /// </summary>
    [StringLength(100)]
    public virtual string? ParentKey { get; set; }

    /// <summary>
    /// All synonyms that should resolve to this category (all languages)
    /// Stored as PostgreSQL text[] array
    /// Examples for "pizza": ["pizzeria", "pizzerías", "pizza place", "pizza restaurant", "pizzería"]
    /// </summary>
    public virtual string[] Synonyms { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional description of the category
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Whether this category is active for search
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display in category lists
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;
}
