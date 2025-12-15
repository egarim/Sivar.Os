using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Manages profile ad budgets and tracks spending
/// </summary>
public class ProfileAdBudgetService : IProfileAdBudgetService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IAdTransactionRepository _adTransactionRepository;
    private readonly ILogger<ProfileAdBudgetService> _logger;

    // Configuration (could be moved to appsettings)
    private const double BaseCtr = 0.025; // 2.5% average CTR
    private const double MinQualityScore = 0.1;
    private const double MaxQualityScore = 1.0;

    public ProfileAdBudgetService(
        IProfileRepository profileRepository,
        IAdTransactionRepository adTransactionRepository,
        ILogger<ProfileAdBudgetService> logger)
    {
        _profileRepository = profileRepository;
        _adTransactionRepository = adTransactionRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordImpressionAsync(
        Guid profileId,
        string searchQuery,
        int position)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("[AdBudget] Profile not found: {ProfileId}", profileId);
                return;
            }

            profile.SponsoredImpressions++;
            await _profileRepository.UpdateAsync(profile);

            _logger.LogDebug(
                "[AdBudget] Recorded impression for {DisplayName} at position {Position}",
                profile.DisplayName, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdBudget] Error recording impression for {ProfileId}", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RecordClickAsync(
        Guid profileId,
        decimal actualCost,
        Guid? clickedByUserId,
        string searchQuery,
        int position,
        string? ipAddress = null)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("[AdBudget] Profile not found: {ProfileId}", profileId);
                return false;
            }

            // Check budget
            if (profile.AdBudget < actualCost)
            {
                _logger.LogWarning(
                    "[AdBudget] Insufficient budget for {DisplayName}: ${Budget} < ${Cost}",
                    profile.DisplayName, profile.AdBudget, actualCost);
                return false;
            }

            // Deduct from budget
            profile.AdBudget -= actualCost;
            profile.AdSpentToday += actualCost;
            profile.TotalAdSpent += actualCost;
            profile.SponsoredClicks++;

            // Note: SponsoredCtr is a computed property (SponsoredClicks / SponsoredImpressions)

            await _profileRepository.UpdateAsync(profile);

            // Create transaction record
            var transaction = new AdTransaction
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                TransactionType = AdTransactionType.Click,
                Amount = -actualCost, // Negative for deduction
                BalanceAfter = profile.AdBudget,
                TriggeredByUserId = clickedByUserId,
                SearchQuery = searchQuery,
                SearchPosition = position,
                IpAddressHash = HashIpAddress(ipAddress),
                Description = $"Click from search: '{searchQuery}' at position {position}",
                Timestamp = DateTime.UtcNow
            };

            await _adTransactionRepository.AddAsync(transaction);

            _logger.LogInformation(
                "[AdBudget] Recorded click for {DisplayName}: ${Cost}, remaining: ${Budget}",
                profile.DisplayName, actualCost, profile.AdBudget);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdBudget] Error recording click for {ProfileId}", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> AddBudgetAsync(
        Guid profileId,
        decimal amount,
        AdTransactionType transactionType,
        string description,
        Guid? addedByUserId = null)
    {
        if (transactionType != AdTransactionType.TopUp &&
            transactionType != AdTransactionType.Bonus &&
            transactionType != AdTransactionType.Refund &&
            transactionType != AdTransactionType.Adjustment)
        {
            throw new ArgumentException(
                "Invalid transaction type for adding budget",
                nameof(transactionType));
        }

        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile not found: {profileId}");
            }

            profile.AdBudget += amount;
            await _profileRepository.UpdateAsync(profile);

            var transaction = new AdTransaction
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                TransactionType = transactionType,
                Amount = amount,
                BalanceAfter = profile.AdBudget,
                TriggeredByUserId = addedByUserId,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            await _adTransactionRepository.AddAsync(transaction);

            _logger.LogInformation(
                "[AdBudget] Added ${Amount} to {DisplayName} ({Type}): new balance ${Budget}",
                amount, profile.DisplayName, transactionType, profile.AdBudget);

            return profile.AdBudget;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdBudget] Error adding budget to {ProfileId}", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetBudgetAsync(Guid profileId)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        return profile?.AdBudget ?? 0;
    }

    /// <inheritdoc />
    public async Task<bool> CanShowAsSponsoredAsync(Guid profileId)
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null) return false;

        return profile.SponsoredEnabled &&
               profile.AdBudget > 0 &&
               profile.AdSpentToday < profile.DailyAdLimit &&
               !profile.IsDeleted;
    }

    /// <inheritdoc />
    public async Task<int> ResetDailySpendAsync()
    {
        try
        {
            var profiles = await _profileRepository.Query()
                .Where(p => p.AdSpentToday > 0)
                .ToListAsync();

            foreach (var profile in profiles)
            {
                profile.AdSpentToday = 0;
                await _profileRepository.UpdateAsync(profile);
            }

            _logger.LogInformation(
                "[AdBudget] Reset daily spend for {Count} profiles",
                profiles.Count);

            return profiles.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdBudget] Error resetting daily spend");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> UpdateQualityScoresAsync()
    {
        try
        {
            // Only update profiles with enough impressions
            const int minImpressions = 100;

            var profiles = await _profileRepository.Query()
                .Where(p => p.SponsoredEnabled)
                .Where(p => p.SponsoredImpressions >= minImpressions)
                .ToListAsync();

            foreach (var profile in profiles)
            {
                // Quality = (CTR / BaseCTR) clamped to [0.1, 1.0]
                var ctr = profile.SponsoredImpressions > 0
                    ? (double)profile.SponsoredClicks / profile.SponsoredImpressions
                    : BaseCtr;

                var rawQuality = ctr / BaseCtr;
                profile.AdQualityScore = Math.Clamp(rawQuality, MinQualityScore, MaxQualityScore);

                await _profileRepository.UpdateAsync(profile);
            }

            _logger.LogInformation(
                "[AdBudget] Updated quality scores for {Count} profiles",
                profiles.Count);

            return profiles.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdBudget] Error updating quality scores");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<AdTransaction>> GetTransactionHistoryAsync(
        Guid profileId,
        int limit = 50)
    {
        return await _adTransactionRepository.GetByProfileIdAsync(profileId, limit);
    }

    /// <summary>
    /// Hash IP address for privacy-preserving click fraud detection
    /// </summary>
    private static string? HashIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return null;

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToHexString(bytes)[..16]; // First 16 chars
    }
}
