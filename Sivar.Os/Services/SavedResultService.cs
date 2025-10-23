
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing saved AI chat results
/// </summary>
public class SavedResultService : ISavedResultService
{
    private readonly ISavedResultRepository _savedResultRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<SavedResultService> _logger;

    public SavedResultService(
        ISavedResultRepository savedResultRepository,
        IConversationRepository conversationRepository,
        ILogger<SavedResultService> logger)
    {
        _savedResultRepository = savedResultRepository ?? throw new ArgumentNullException(nameof(savedResultRepository));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SavedResultDto> SaveResultAsync(Guid profileId, CreateSavedResultDto dto)
    {
        try
        {
            // Verify conversation belongs to profile
            var conversation = await _conversationRepository.GetByIdAsync(dto.ConversationId);
            if (conversation == null || conversation.IsDeleted)
            {
                throw new InvalidOperationException($"Conversation {dto.ConversationId} not found");
            }

            if (conversation.ProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Conversation does not belong to this profile");
            }

            var savedResult = new SavedResult
            {
                ProfileId = profileId,
                ConversationId = dto.ConversationId,
                ResultType = dto.ResultType,
                ResultData = dto.ResultData,
                Description = dto.Description
            };

            var created = await _savedResultRepository.AddAsync(savedResult);
            await _savedResultRepository.SaveChangesAsync();
            
            _logger.LogInformation("Saved result {ResultId} for profile {ProfileId}", created.Id, profileId);

            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving result for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<List<SavedResultDto>> GetProfileSavedResultsAsync(
        Guid profileId, 
        string? resultType = null, 
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            var results = await _savedResultRepository.GetProfileSavedResultsAsync(
                profileId, resultType, page, pageSize);

            return results.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved results for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<List<SavedResultDto>> GetConversationSavedResultsAsync(Guid conversationId, Guid profileId)
    {
        try
        {
            // Verify conversation belongs to profile
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null || conversation.IsDeleted)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            if (conversation.ProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Conversation does not belong to this profile");
            }

            var results = await _savedResultRepository.GetConversationSavedResultsAsync(conversationId);
            return results.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved results for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> DeleteSavedResultAsync(Guid savedResultId, Guid profileId)
    {
        try
        {
            var savedResult = await _savedResultRepository.GetByIdAsync(savedResultId);
            if (savedResult == null || savedResult.IsDeleted)
            {
                return false;
            }

            if (savedResult.ProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Saved result does not belong to this profile");
            }

            await _savedResultRepository.DeleteAsync(savedResultId);
            await _savedResultRepository.SaveChangesAsync();
            
            _logger.LogInformation("Deleted saved result {ResultId}", savedResultId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved result {ResultId}", savedResultId);
            throw;
        }
    }

    public async Task<int> ClearProfileSavedResultsAsync(Guid profileId)
    {
        try
        {
            var count = await _savedResultRepository.ClearProfileSavedResultsAsync(profileId);
            
            _logger.LogInformation("Cleared {Count} saved results for profile {ProfileId}", count, profileId);
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing saved results for profile {ProfileId}", profileId);
            throw;
        }
    }

    private SavedResultDto MapToDto(SavedResult savedResult)
    {
        return new SavedResultDto
        {
            Id = savedResult.Id,
            ProfileId = savedResult.ProfileId,
            ConversationId = savedResult.ConversationId,
            ResultType = savedResult.ResultType,
            ResultData = savedResult.ResultData,
            Description = savedResult.Description,
            CreatedAt = savedResult.CreatedAt
        };
    }
}
