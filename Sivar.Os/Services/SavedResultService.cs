
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[SavedResultService.SaveResultAsync] START - RequestId={RequestId}, ProfileId={ProfileId}, ConversationId={ConversationId}, ResultType={ResultType}", 
            requestId, profileId, dto?.ConversationId ?? Guid.Empty, dto?.ResultType ?? "NULL");

        try
        {
            if (dto == null)
            {
                _logger.LogWarning("[SavedResultService.SaveResultAsync] NULL_DTO - RequestId={RequestId}", requestId);
                throw new ArgumentNullException(nameof(dto));
            }

            // Verify conversation belongs to profile
            var conversation = await _conversationRepository.GetByIdAsync(dto.ConversationId);
            if (conversation == null || conversation.IsDeleted)
            {
                _logger.LogWarning("[SavedResultService.SaveResultAsync] CONVERSATION_NOT_FOUND - RequestId={RequestId}, ConversationId={ConversationId}", 
                    requestId, dto.ConversationId);
                throw new InvalidOperationException($"Conversation {dto.ConversationId} not found");
            }

            if (conversation.ProfileId != profileId)
            {
                _logger.LogWarning("[SavedResultService.SaveResultAsync] UNAUTHORIZED_CONVERSATION - RequestId={RequestId}, ProfileId={ProfileId}, ConversationProfileId={ConversationProfileId}", 
                    requestId, profileId, conversation.ProfileId);
                throw new UnauthorizedAccessException("Conversation does not belong to this profile");
            }

            _logger.LogInformation("[SavedResultService.SaveResultAsync] Conversation validated - RequestId={RequestId}, ConversationId={ConversationId}", 
                requestId, dto.ConversationId);

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
            
            _logger.LogInformation("[SavedResultService.SaveResultAsync] Result persisted - RequestId={RequestId}, ResultId={ResultId}, DataLength={DataLength}", 
                requestId, created.Id, dto.ResultData?.Length ?? 0);

            _logger.LogInformation("Saved result {ResultId} for profile {ProfileId}", created.Id, profileId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[SavedResultService.SaveResultAsync] SUCCESS - RequestId={RequestId}, ResultId={ResultId}, Duration={Duration}ms", 
                requestId, created.Id, elapsed);

            return MapToDto(created);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[SavedResultService.SaveResultAsync] ERROR - RequestId={RequestId}, ProfileId={ProfileId}, Duration={Duration}ms", 
                requestId, profileId, elapsed);
            throw;
        }
    }

    public async Task<List<SavedResultDto>> GetProfileSavedResultsAsync(
        Guid profileId, 
        string? resultType = null, 
        int page = 1, 
        int pageSize = 20)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[SavedResultService.GetProfileSavedResultsAsync] START - RequestId={RequestId}, ProfileId={ProfileId}, ResultType={ResultType}, Page={Page}, PageSize={PageSize}", 
            requestId, profileId, resultType ?? "ALL", page, pageSize);

        try
        {
            var results = await _savedResultRepository.GetProfileSavedResultsAsync(
                profileId, resultType, page, pageSize);

            _logger.LogInformation("[SavedResultService.GetProfileSavedResultsAsync] Results retrieved - RequestId={RequestId}, Count={Count}, Page={Page}", 
                requestId, results.Count, page);

            var dtos = results.Select(MapToDto).ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[SavedResultService.GetProfileSavedResultsAsync] SUCCESS - RequestId={RequestId}, ProfileId={ProfileId}, ReturnedCount={Count}, Duration={Duration}ms", 
                requestId, profileId, dtos.Count, elapsed);

            return dtos;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[SavedResultService.GetProfileSavedResultsAsync] ERROR - RequestId={RequestId}, ProfileId={ProfileId}, Duration={Duration}ms", 
                requestId, profileId, elapsed);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[SavedResultService.DeleteSavedResultAsync] START - RequestId={RequestId}, SavedResultId={SavedResultId}, ProfileId={ProfileId}", 
            requestId, savedResultId, profileId);

        try
        {
            var savedResult = await _savedResultRepository.GetByIdAsync(savedResultId);
            if (savedResult == null || savedResult.IsDeleted)
            {
                _logger.LogWarning("[SavedResultService.DeleteSavedResultAsync] RESULT_NOT_FOUND - RequestId={RequestId}, SavedResultId={SavedResultId}", 
                    requestId, savedResultId);
                return false;
            }

            if (savedResult.ProfileId != profileId)
            {
                _logger.LogWarning("[SavedResultService.DeleteSavedResultAsync] UNAUTHORIZED_RESULT - RequestId={RequestId}, SavedResultId={SavedResultId}, ProfileId={ProfileId}, ResultProfileId={ResultProfileId}", 
                    requestId, savedResultId, profileId, savedResult.ProfileId);
                throw new UnauthorizedAccessException("Saved result does not belong to this profile");
            }

            _logger.LogInformation("[SavedResultService.DeleteSavedResultAsync] Result found, deleting - RequestId={RequestId}, SavedResultId={SavedResultId}, ResultType={ResultType}", 
                requestId, savedResultId, savedResult.ResultType);

            await _savedResultRepository.DeleteAsync(savedResultId);
            await _savedResultRepository.SaveChangesAsync();
            
            _logger.LogInformation("Deleted saved result {ResultId}", savedResultId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[SavedResultService.DeleteSavedResultAsync] SUCCESS - RequestId={RequestId}, SavedResultId={SavedResultId}, Duration={Duration}ms", 
                requestId, savedResultId, elapsed);
            
            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[SavedResultService.DeleteSavedResultAsync] ERROR - RequestId={RequestId}, SavedResultId={SavedResultId}, Duration={Duration}ms", 
                requestId, savedResultId, elapsed);
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
