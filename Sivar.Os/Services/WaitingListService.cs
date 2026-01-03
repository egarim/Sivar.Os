using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing the waiting list and phone verification flow
/// </summary>
public class WaitingListService : IWaitingListService
{
    private readonly IWaitingListRepository _waitingListRepository;
    private readonly IPhoneVerificationRepository _phoneVerificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITwilioVerifyService _twilioVerifyService;
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<WaitingListService> _logger;

    public WaitingListService(
        IWaitingListRepository waitingListRepository,
        IPhoneVerificationRepository phoneVerificationRepository,
        IUserRepository userRepository,
        ITwilioVerifyService twilioVerifyService,
        IKeycloakAdminService keycloakAdminService,
        ILogger<WaitingListService> logger)
    {
        _waitingListRepository = waitingListRepository;
        _phoneVerificationRepository = phoneVerificationRepository;
        _userRepository = userRepository;
        _twilioVerifyService = twilioVerifyService;
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WaitingListEntry> CreatePendingEntryAsync(Guid userId, string email, string keycloakId)
    {
        _logger.LogInformation("Creating pending waiting list entry for user {UserId}", userId);

        // Check if entry already exists
        var existing = await _waitingListRepository.GetByUserIdAsync(userId);
        if (existing != null)
        {
            _logger.LogWarning("Waiting list entry already exists for user {UserId}", userId);
            return existing;
        }

        var entry = new WaitingListEntry
        {
            UserId = userId,
            Email = email,
            Status = WaitingListStatus.PendingVerification,
            JoinedAt = DateTime.UtcNow,
            ReferralCode = WaitingListEntry.GenerateReferralCode(),
            Position = 0 // Will be set when they verify phone
        };

        await _waitingListRepository.AddAsync(entry);
        await _waitingListRepository.SaveChangesAsync();

        _logger.LogInformation("Created pending entry for user {UserId} with referral code {ReferralCode}", 
            userId, entry.ReferralCode);

        return entry;
    }

    /// <inheritdoc />
    public async Task<(bool Success, VerificationChannel Channel, string? Error)> RequestPhoneVerificationAsync(
        Guid userId, string phoneNumber, string countryCode)
    {
        _logger.LogInformation("Requesting phone verification for user {UserId}, phone {Phone}", userId, phoneNumber);

        // Check if phone is already used by another user
        if (await _waitingListRepository.PhoneNumberExistsAsync(phoneNumber))
        {
            var existingEntry = await _waitingListRepository.GetByPhoneNumberAsync(phoneNumber);
            if (existingEntry != null && existingEntry.UserId != userId)
            {
                return (false, VerificationChannel.SMS, "Phone number is already registered");
            }
        }

        // Rate limiting: max 3 attempts per hour
        var recentAttempts = await _phoneVerificationRepository.GetAttemptCountAsync(userId, DateTime.UtcNow.AddHours(-1));
        if (recentAttempts >= 3)
        {
            return (false, VerificationChannel.SMS, "Too many verification attempts. Please try again later.");
        }

        // Expire any pending verifications
        await _phoneVerificationRepository.ExpirePendingVerificationsAsync(userId);

        // Send verification via Twilio
        var channel = _twilioVerifyService.GetChannelForCountry(countryCode);
        var result = await _twilioVerifyService.SendVerificationAsync(phoneNumber, countryCode);

        if (!result.Success)
        {
            _logger.LogWarning("Failed to send verification for user {UserId}: {Error}", userId, result.ErrorMessage);
            return (false, channel, result.ErrorMessage);
        }

        // Create verification record
        var verification = new PhoneVerification
        {
            UserId = userId,
            PhoneNumber = phoneNumber,
            CountryCode = countryCode,
            Channel = channel,
            Status = VerificationStatus.Pending,
            TwilioVerificationSid = result.TwilioVerificationSid,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(PhoneVerification.DefaultExpiryMinutes)
        };

        await _phoneVerificationRepository.AddAsync(verification);
        await _phoneVerificationRepository.SaveChangesAsync();

        _logger.LogInformation("Verification sent via {Channel} for user {UserId}", channel, userId);
        return (true, channel, null);
    }

    /// <inheritdoc />
    public async Task<VerifyPhoneResult> VerifyPhoneAndJoinQueueAsync(
        Guid userId, string phoneNumber, string code, string? referralCode = null)
    {
        _logger.LogInformation("Verifying phone for user {UserId}", userId);

        // Get pending verification
        var verification = await _phoneVerificationRepository.GetLatestPendingByUserIdAsync(userId);
        if (verification == null)
        {
            return new VerifyPhoneResult(false, 0, "", "No pending verification found");
        }

        if (verification.IsExpired)
        {
            verification.Status = VerificationStatus.Expired;
            await _phoneVerificationRepository.SaveChangesAsync();
            return new VerifyPhoneResult(false, 0, "", "Verification code has expired");
        }

        if (verification.IsMaxAttemptsExceeded)
        {
            return new VerifyPhoneResult(false, 0, "", "Maximum attempts exceeded");
        }

        // Check code with Twilio
        verification.AttemptCount++;
        var checkResult = await _twilioVerifyService.CheckVerificationAsync(phoneNumber, code);

        if (!checkResult.Success || !checkResult.IsValid)
        {
            await _phoneVerificationRepository.SaveChangesAsync();
            return new VerifyPhoneResult(false, 0, "", checkResult.ErrorMessage ?? "Invalid verification code");
        }

        // Mark as verified
        verification.Status = VerificationStatus.Verified;
        verification.VerifiedAt = DateTime.UtcNow;

        // Get or create waiting list entry
        var entry = await _waitingListRepository.GetByUserIdAsync(userId);
        if (entry == null)
        {
            return new VerifyPhoneResult(false, 0, "", "Waiting list entry not found");
        }

        // Update entry with phone info
        entry.PhoneNumber = phoneNumber;
        entry.CountryCode = verification.CountryCode;
        entry.VerificationChannel = verification.Channel;

        // Handle referral
        WaitingListEntry? referrer = null;
        if (!string.IsNullOrEmpty(referralCode))
        {
            referrer = await _waitingListRepository.GetByReferralCodeAsync(referralCode);
            if (referrer != null && referrer.UserId != userId)
            {
                entry.UsedReferralCode = referralCode;
                entry.ReferredByUserId = referrer.UserId;
                await _waitingListRepository.IncrementReferralCountAsync(referrer.UserId);
            }
        }

        // Calculate position (referrals get priority - lower position)
        var maxPosition = await _waitingListRepository.GetMaxPositionAsync();
        entry.Position = referrer != null ? Math.Max(1, maxPosition / 2) : maxPosition + 1;
        entry.Status = WaitingListStatus.Waiting;

        await _phoneVerificationRepository.SaveChangesAsync();
        await _waitingListRepository.SaveChangesAsync();

        // Update Keycloak attributes
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null && !string.IsNullOrEmpty(user.KeycloakId))
        {
            await _keycloakAdminService.SetPhoneVerifiedAsync(user.KeycloakId, phoneNumber, verification.CountryCode);
            await _keycloakAdminService.UpdateWaitingListStatusAsync(user.KeycloakId, WaitingListStatus.Waiting);
        }

        _logger.LogInformation("User {UserId} verified and joined queue at position {Position}", userId, entry.Position);
        return new VerifyPhoneResult(true, entry.Position, entry.ReferralCode);
    }

    /// <inheritdoc />
    public async Task<bool> ApproveUserAsync(Guid userId, string approvedBy)
    {
        _logger.LogInformation("Approving user {UserId} by {ApprovedBy}", userId, approvedBy);

        var entry = await _waitingListRepository.GetByUserIdAsync(userId);
        if (entry == null)
        {
            _logger.LogWarning("Cannot approve: entry not found for user {UserId}", userId);
            return false;
        }

        if (entry.Status == WaitingListStatus.Approved)
        {
            _logger.LogInformation("User {UserId} is already approved", userId);
            return true;
        }

        var previousPosition = entry.Position;
        entry.Status = WaitingListStatus.Approved;
        entry.ApprovedAt = DateTime.UtcNow;
        entry.ApprovedBy = approvedBy;
        entry.Position = 0; // Approved users have no queue position

        await _waitingListRepository.SaveChangesAsync();

        // Reorder queue
        if (previousPosition > 0)
        {
            await _waitingListRepository.ReorderQueueAfterApprovalAsync(previousPosition);
            await _waitingListRepository.SaveChangesAsync();
        }

        // Update Keycloak
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null && !string.IsNullOrEmpty(user.KeycloakId))
        {
            await _keycloakAdminService.UpdateWaitingListStatusAsync(user.KeycloakId, WaitingListStatus.Approved);
        }

        _logger.LogInformation("User {UserId} approved successfully", userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> ApproveUsersAsync(IEnumerable<Guid> userIds, string approvedBy)
    {
        var count = 0;
        foreach (var userId in userIds)
        {
            if (await ApproveUserAsync(userId, approvedBy))
            {
                count++;
            }
        }
        return count;
    }

    /// <inheritdoc />
    public async Task<int> ApproveNextInQueueAsync(int count, string approvedBy)
    {
        _logger.LogInformation("Approving next {Count} users in queue", count);

        var nextUsers = await _waitingListRepository.GetNextInQueueAsync(count);
        var approved = 0;

        foreach (var entry in nextUsers)
        {
            if (await ApproveUserAsync(entry.UserId, approvedBy))
            {
                approved++;
            }
        }

        _logger.LogInformation("Approved {Approved} of {Requested} users", approved, count);
        return approved;
    }

    /// <inheritdoc />
    public async Task<bool> RejectUserAsync(Guid userId, string? reason = null)
    {
        _logger.LogInformation("Rejecting user {UserId}, reason: {Reason}", userId, reason ?? "none");

        var entry = await _waitingListRepository.GetByUserIdAsync(userId);
        if (entry == null)
        {
            return false;
        }

        var previousPosition = entry.Position;
        entry.Status = WaitingListStatus.Rejected;
        entry.AdminNotes = reason;
        entry.Position = 0;

        await _waitingListRepository.SaveChangesAsync();

        // Reorder queue
        if (previousPosition > 0)
        {
            await _waitingListRepository.ReorderQueueAfterApprovalAsync(previousPosition);
            await _waitingListRepository.SaveChangesAsync();
        }

        // Update Keycloak
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null && !string.IsNullOrEmpty(user.KeycloakId))
        {
            await _keycloakAdminService.UpdateWaitingListStatusAsync(user.KeycloakId, WaitingListStatus.Rejected);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<WaitingListStatusDto?> GetStatusAsync(Guid userId)
    {
        var entry = await _waitingListRepository.GetByUserIdAsync(userId);
        if (entry == null)
        {
            return null;
        }

        var (_, waiting, _, _) = await _waitingListRepository.GetStatsAsync();

        return new WaitingListStatusDto(
            Status: entry.Status,
            Position: entry.Position,
            TotalWaiting: waiting,
            ReferralCode: entry.ReferralCode,
            ReferralCount: entry.ReferralCount,
            JoinedAt: entry.JoinedAt,
            ApprovedAt: entry.ApprovedAt
        );
    }

    /// <inheritdoc />
    public async Task<WaitingListEntry?> GetEntryByUserIdAsync(Guid userId)
    {
        return await _waitingListRepository.GetByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<bool> IsReferralCodeValidAsync(string referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            return false;
        }

        var entry = await _waitingListRepository.GetByReferralCodeAsync(referralCode);
        return entry != null && entry.Status != WaitingListStatus.Rejected;
    }

    /// <inheritdoc />
    public async Task<WaitingListStats> GetStatsAsync()
    {
        var (pending, waiting, approved, rejected) = await _waitingListRepository.GetStatsAsync();

        // Get approved counts for today and this week
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var allEntries = await _waitingListRepository.GetAllAsync();
        var approvedEntries = allEntries.Where(e => e.Status == WaitingListStatus.Approved).ToList();

        var approvedToday = approvedEntries.Count(e => e.ApprovedAt?.Date == today);
        var approvedThisWeek = approvedEntries.Count(e => e.ApprovedAt >= weekStart);

        // Group by country
        var signupsByCountry = allEntries
            .Where(e => !string.IsNullOrEmpty(e.CountryCode))
            .GroupBy(e => e.CountryCode)
            .ToDictionary(g => g.Key, g => g.Count());

        return new WaitingListStats(
            TotalSignups: allEntries.Count(),
            PendingVerification: pending,
            WaitingApproval: waiting,
            ApprovedTotal: approved,
            ApprovedToday: approvedToday,
            ApprovedThisWeek: approvedThisWeek,
            RejectedTotal: rejected,
            SignupsByCountry: signupsByCountry
        );
    }
}
