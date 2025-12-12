using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for building contact action URLs and sorting contacts by regional popularity
/// </summary>
public interface IContactUrlBuilder
{
    /// <summary>
    /// Build the action URL for a contact using the contact type's URL template
    /// </summary>
    /// <param name="contactType">The contact type with URL template</param>
    /// <param name="contact">The business contact with actual values</param>
    /// <param name="message">Optional pre-filled message for messaging apps</param>
    /// <param name="subject">Optional subject for email</param>
    /// <param name="latitude">Optional latitude for location-based URLs</param>
    /// <param name="longitude">Optional longitude for location-based URLs</param>
    /// <param name="businessName">Optional business name for location-based URLs</param>
    /// <returns>Formatted action URL</returns>
    string BuildUrl(
        ContactType contactType, 
        BusinessContactInfo contact, 
        string? message = null, 
        string? subject = null,
        double? latitude = null,
        double? longitude = null,
        string? businessName = null);

    /// <summary>
    /// Get contacts for a profile, sorted by regional popularity
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="userRegion">User's region code (e.g., "SV", "US")</param>
    /// <param name="latitude">Optional latitude for location URLs</param>
    /// <param name="longitude">Optional longitude for location URLs</param>
    /// <param name="businessName">Optional business name for location URLs</param>
    /// <returns>List of contacts ready for display</returns>
    Task<List<ContactDisplayDto>> GetContactsForProfileAsync(
        Guid profileId, 
        string? userRegion = null,
        double? latitude = null,
        double? longitude = null,
        string? businessName = null);

    /// <summary>
    /// Get contacts for multiple profiles in a single batch (for search results)
    /// </summary>
    /// <param name="profileIds">Profile IDs</param>
    /// <param name="userRegion">User's region code</param>
    /// <returns>Dictionary mapping profile ID to their contacts</returns>
    Task<Dictionary<Guid, List<ContactDisplayDto>>> GetContactsForProfilesAsync(
        IEnumerable<Guid> profileIds, 
        string? userRegion = null);

    /// <summary>
    /// Build a ContactDisplayDto from entities
    /// </summary>
    ContactDisplayDto BuildContactDisplayDto(
        ContactType contactType,
        BusinessContactInfo contact,
        string? userRegion = null,
        double? latitude = null,
        double? longitude = null,
        string? businessName = null);

    /// <summary>
    /// Get category display name
    /// </summary>
    string GetCategoryDisplayName(string category);

    /// <summary>
    /// Group contacts by category
    /// </summary>
    List<ContactGroupDto> GroupByCategory(List<ContactDisplayDto> contacts);
}
