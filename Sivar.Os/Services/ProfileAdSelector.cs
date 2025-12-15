using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Selects profiles for sponsored placement in search results
/// Uses a second-price auction model with quality scoring
/// </summary>
public class ProfileAdSelector : IProfileAdSelector
{
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<ProfileAdSelector> _logger;

    // Configuration (could be moved to appsettings)
    private const double MinQualityScore = 0.1;
    private const decimal MinBidPerClick = 0.01m;

    public ProfileAdSelector(
        IProfileRepository profileRepository,
        ILogger<ProfileAdSelector> logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<SponsoredProfileResult>> SelectSponsoredProfilesAsync(
        SearchAdContext context,
        int maxSponsored = 2)
    {
        _logger.LogInformation(
            "[ProfileAdSelector] Selecting sponsored profiles - Query={Query}, Category={Category}, Location=({Lat},{Lng})",
            context.Query, context.Category, context.Latitude, context.Longitude);

        try
        {
            // 1. Get eligible sponsored profiles
            var eligibleProfiles = await GetEligibleProfilesAsync(context);

            if (!eligibleProfiles.Any())
            {
                _logger.LogInformation("[ProfileAdSelector] No eligible sponsored profiles found");
                return new List<SponsoredProfileResult>();
            }

            _logger.LogInformation(
                "[ProfileAdSelector] Found {Count} eligible profiles",
                eligibleProfiles.Count);

            // 2. Calculate ad rank for each profile
            var rankedProfiles = eligibleProfiles
                .Select(p => new
                {
                    Profile = p,
                    AdRank = CalculateAdRank(p, context),
                    RelevanceScore = CalculateRelevanceScore(p, context)
                })
                .Where(x => x.AdRank > 0)
                .OrderByDescending(x => x.AdRank)
                .Take(maxSponsored)
                .ToList();

            if (!rankedProfiles.Any())
            {
                _logger.LogInformation("[ProfileAdSelector] No profiles passed ranking threshold");
                return new List<SponsoredProfileResult>();
            }

            // 3. Calculate actual price (second-price auction)
            var results = new List<SponsoredProfileResult>();
            for (int i = 0; i < rankedProfiles.Count; i++)
            {
                var current = rankedProfiles[i];

                // Second-price auction: pay enough to beat next bidder + $0.01
                decimal actualPrice;
                if (i + 1 < rankedProfiles.Count)
                {
                    var next = rankedProfiles[i + 1];
                    // Price = NextAdRank / YourQualityScore + $0.01
                    var qualityScore = Math.Max(current.Profile.AdQualityScore, MinQualityScore);
                    actualPrice = (decimal)(next.AdRank / qualityScore) + 0.01m;
                }
                else
                {
                    // No competition - pay minimum
                    actualPrice = MinBidPerClick;
                }

                // Cap at their max bid
                actualPrice = Math.Min(actualPrice, current.Profile.MaxBidPerClick);

                results.Add(new SponsoredProfileResult
                {
                    ProfileId = current.Profile.Id,
                    Profile = current.Profile,
                    AdRank = current.AdRank,
                    ActualPricePerClick = actualPrice,
                    RelevanceScore = current.RelevanceScore,
                    Position = 0 // Assigned during interleaving
                });
            }

            _logger.LogInformation(
                "[ProfileAdSelector] Selected {Count} sponsored profiles: {Profiles}",
                results.Count,
                string.Join(", ", results.Select(r => $"{r.Profile.DisplayName}(${r.ActualPricePerClick:F2})")));

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileAdSelector] Error selecting sponsored profiles");
            return new List<SponsoredProfileResult>();
        }
    }

    /// <summary>
    /// Get profiles eligible for sponsored placement
    /// </summary>
    private async Task<List<Profile>> GetEligibleProfilesAsync(SearchAdContext context)
    {
        var query = _profileRepository.Query()
            .Where(p => p.SponsoredEnabled)
            .Where(p => p.AdBudget > 0)
            .Where(p => p.AdSpentToday < p.DailyAdLimit)
            .Where(p => !p.IsDeleted);

        // Exclude profiles already in organic results
        if (context.OrganicProfileIds.Any())
        {
            query = query.Where(p => !context.OrganicProfileIds.Contains(p.Id));
        }

        var profiles = await query.ToListAsync();

        // Filter by targeting in memory (complex JSON logic)
        return profiles
            .Where(p => MatchesKeywordTargeting(p, context.Query))
            .Where(p => MatchesLocationTargeting(p, context.Latitude, context.Longitude))
            .ToList();
    }

    /// <summary>
    /// Check if profile's keyword targeting matches the search query
    /// </summary>
    private bool MatchesKeywordTargeting(Profile profile, string query)
    {
        // No targeting = matches all
        if (string.IsNullOrEmpty(profile.AdTargetKeywords))
            return true;

        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(profile.AdTargetKeywords);
            if (keywords == null || !keywords.Any())
                return true;

            // Match if any keyword is in the query
            return keywords.Any(k =>
                query.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return true; // On parse error, allow
        }
    }

    /// <summary>
    /// Check if profile's location targeting matches the user's location
    /// </summary>
    private bool MatchesLocationTargeting(Profile profile, double userLat, double userLng)
    {
        // No radius targeting = matches all
        if (profile.AdTargetRadiusKm <= 0)
            return true;

        // User has no location = allow (can't determine)
        if (userLat == 0 && userLng == 0)
            return true;

        // Profile has no location = can't calculate distance
        var profileLocation = profile.Location;
        if (profileLocation?.Latitude == null || profileLocation?.Longitude == null)
            return true;

        var distance = CalculateDistanceKm(
            profileLocation.Latitude.Value,
            profileLocation.Longitude.Value,
            userLat,
            userLng);

        return distance <= profile.AdTargetRadiusKm;
    }

    /// <summary>
    /// Calculate ad rank: Bid × QualityScore × RelevanceBoost
    /// </summary>
    private double CalculateAdRank(Profile profile, SearchAdContext context)
    {
        var bid = (double)profile.MaxBidPerClick;
        var qualityScore = Math.Max(profile.AdQualityScore, MinQualityScore);
        var relevanceBoost = CalculateRelevanceScore(profile, context);

        // Minimum quality threshold
        if (qualityScore < MinQualityScore)
            return 0;

        return bid * qualityScore * relevanceBoost;
    }

    /// <summary>
    /// Calculate how relevant the profile is to the search context
    /// </summary>
    private double CalculateRelevanceScore(Profile profile, SearchAdContext context)
    {
        double score = 1.0;

        // Keyword match bonus
        if (!string.IsNullOrEmpty(profile.AdTargetKeywords) && !string.IsNullOrEmpty(context.Query))
        {
            try
            {
                var keywords = JsonSerializer.Deserialize<List<string>>(profile.AdTargetKeywords);
                if (keywords != null)
                {
                    var matchCount = keywords.Count(k =>
                        context.Query.Contains(k, StringComparison.OrdinalIgnoreCase));
                    if (matchCount > 0)
                    {
                        score += 0.2 * Math.Min(matchCount, 3); // Max 0.6 bonus
                    }
                }
            }
            catch { }
        }

        // Category match bonus
        if (!string.IsNullOrEmpty(context.Category) && profile.CategoryKeys.Length > 0)
        {
            if (profile.CategoryKeys.Any(ck =>
                ck.Equals(context.Category, StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.3;
            }
        }

        // Location proximity bonus
        if (context.Latitude != 0 && context.Longitude != 0 &&
            profile.Location?.Latitude != null && profile.Location?.Longitude != null)
        {
            var distanceKm = CalculateDistanceKm(
                profile.Location.Latitude.Value,
                profile.Location.Longitude.Value,
                context.Latitude,
                context.Longitude);

            if (distanceKm < 1) score += 0.3;
            else if (distanceKm < 5) score += 0.2;
            else if (distanceKm < 10) score += 0.1;
        }

        return score;
    }

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    private double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth's radius in km

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
