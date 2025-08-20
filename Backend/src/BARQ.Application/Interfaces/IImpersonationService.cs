using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IImpersonationService
    {
        Task<PagedResult<ImpersonationSessionDto>> GetImpersonationSessionsAsync(ListRequest request);
        Task<ImpersonationSessionDto?> GetImpersonationSessionByIdAsync(Guid id);
        Task<ImpersonationSessionDto> StartImpersonationAsync(CreateImpersonationSessionRequest request, string adminUserId, string ipAddress, string userAgent);
        Task<bool> EndImpersonationAsync(Guid sessionId, EndImpersonationSessionRequest request, string endedBy);
        Task<bool> ValidateImpersonationTokenAsync(string token);
        Task<ImpersonationSessionDto?> GetActiveImpersonationByTokenAsync(string token);
        Task LogImpersonationActionAsync(Guid sessionId, string actionType, string entityType, string? entityId, string description, string httpMethod, string requestPath, int statusCode, long responseTimeMs, string? riskLevel = null);
        Task<List<ImpersonationSessionDto>> GetActiveImpersonationSessionsAsync();
        Task<PagedResult<ImpersonationActionDto>> GetImpersonationActionsAsync(Guid sessionId, ListRequest request);
        Task ExpireOldSessionsAsync();
        Task<bool> CanUserBeImpersonatedAsync(Guid userId, Guid tenantId);
        Task<Dictionary<string, object>> GetImpersonationStatsAsync();
    }
}
