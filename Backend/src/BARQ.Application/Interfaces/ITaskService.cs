using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface ITaskService
    {
        Task<PagedResult<TaskDto>> GetTasksAsync(Guid tenantId, TaskListRequest request);
        Task<TaskDto?> GetTaskByIdAsync(Guid id);
        Task<TaskDto> CreateTaskAsync(Guid tenantId, CreateTaskRequest request);
        Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request);
        Task<bool> DeleteTaskAsync(Guid id);
        Task<bool> AssignTaskAsync(Guid taskId, Guid userId);
        Task<bool> UnassignTaskAsync(Guid taskId);
        Task<bool> UpdateTaskStatusAsync(Guid taskId, string status);
        Task<bool> UpdateTaskProgressAsync(Guid taskId, decimal progressPercentage);
        Task<List<TaskDto>> GetSubTasksAsync(Guid parentTaskId);
        Task<List<TaskDto>> GetTasksByProjectAsync(Guid projectId);
        Task<List<TaskDto>> GetTasksByUserAsync(Guid userId);
        Task<BulkOperationResult> BulkDeleteTasksAsync(BulkDeleteRequest request);
        Task<BulkOperationResult> BulkUpdateTaskStatusAsync(List<Guid> taskIds, string status);
    }
}
