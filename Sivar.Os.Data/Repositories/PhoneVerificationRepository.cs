using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for PhoneVerification operations
/// </summary>
public class PhoneVerificationRepository : BaseRepository<PhoneVerification>, IPhoneVerificationRepository
{
    public PhoneVerificationRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<PhoneVerification?> GetLatestByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.RequestedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<PhoneVerification?> GetLatestPendingByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(v => v.UserId == userId && 
                       v.Status == VerificationStatus.Pending &&
                       v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.RequestedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<PhoneVerification?> GetByTwilioSidAsync(string twilioSid)
    {
        return await _dbSet
            .FirstOrDefaultAsync(v => v.TwilioVerificationSid == twilioSid);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PhoneVerification>> GetByPhoneNumberSinceAsync(string phoneNumber, DateTime since)
    {
        return await _dbSet
            .Where(v => v.PhoneNumber == phoneNumber && v.RequestedAt >= since)
            .OrderByDescending(v => v.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PhoneVerification>> GetByIpAddressSinceAsync(string ipAddress, DateTime since)
    {
        return await _dbSet
            .Where(v => v.IpAddress == ipAddress && v.RequestedAt >= since)
            .OrderByDescending(v => v.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasVerifiedPhoneAsync(Guid userId)
    {
        return await _dbSet.AnyAsync(v => 
            v.UserId == userId && 
            v.Status == VerificationStatus.Verified);
    }

    /// <inheritdoc />
    public async Task<PhoneVerification?> GetVerifiedByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(v => v.UserId == userId && v.Status == VerificationStatus.Verified)
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<int> ExpirePendingVerificationsAsync(Guid userId)
    {
        var pendingVerifications = await _dbSet
            .Where(v => v.UserId == userId && v.Status == VerificationStatus.Pending)
            .ToListAsync();

        foreach (var verification in pendingVerifications)
        {
            verification.Status = VerificationStatus.Expired;
        }

        return pendingVerifications.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetAttemptCountAsync(Guid userId, DateTime since)
    {
        return await _dbSet
            .Where(v => v.UserId == userId && v.RequestedAt >= since)
            .CountAsync();
    }
}
