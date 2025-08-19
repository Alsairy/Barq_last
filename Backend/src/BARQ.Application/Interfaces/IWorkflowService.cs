using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IWorkflowService
    {
        Task<PagedResult<WorkflowDto>> GetWorkflowsAsync(Guid tenantId, ListRequest request);
        Task<WorkflowDto?> GetWorkflowByIdAsync(Guid id);
        Task<WorkflowDto> CreateWorkflowAsync(Guid tenantId, CreateWorkflowRequest request);
        Task<WorkflowDto> UpdateWorkflowAsync(Guid id, CreateWorkflowRequest request);
        Task<bool> DeleteWorkflowAsync(Guid id);
        Task<WorkflowInstanceDto> StartWorkflowAsync(Guid tenantId, StartWorkflowRequest request);
        Task<List<WorkflowInstanceDto>> GetWorkflowInstancesAsync(Guid workflowId);
        Task<WorkflowInstanceDto?> GetWorkflowInstanceByIdAsync(Guid instanceId);
        Task<bool> CancelWorkflowInstanceAsync(Guid instanceId);
        Task<bool> CompleteWorkflowStepAsync(Guid instanceId, string stepId, string output);
    }
}
