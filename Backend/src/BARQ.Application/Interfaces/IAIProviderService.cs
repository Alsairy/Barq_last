using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IAIProviderService
    {
        Task<PagedResult<AIProviderDto>> GetAIProvidersAsync(Guid tenantId, ListRequest request);
        Task<AIProviderDto?> GetAIProviderByIdAsync(Guid id);
        Task<AIProviderDto> CreateAIProviderAsync(Guid tenantId, CreateAIProviderRequest request);
        Task<AIProviderDto> UpdateAIProviderAsync(Guid id, UpdateAIProviderRequest request);
        Task<bool> DeleteAIProviderAsync(Guid id);
        Task<bool> TestAIProviderConnectionAsync(Guid id);
        Task<bool> SetDefaultAIProviderAsync(Guid tenantId, Guid providerId);
        Task<List<AIAgentDto>> GetAIAgentsByProviderAsync(Guid providerId);
        Task<AIAgentDto> CreateAIAgentAsync(Guid tenantId, CreateAIAgentRequest request);
        Task<bool> DeleteAIAgentAsync(Guid agentId);
    }
}
