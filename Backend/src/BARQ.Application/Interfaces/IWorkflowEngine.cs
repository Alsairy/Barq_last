using BARQ.Core.DTOs;

namespace BARQ.Application.Interfaces
{
    public interface IWorkflowEngine
    {
        Task<string> DeployProcessDefinitionAsync(string processDefinition, string processKey);
        Task<WorkflowInstanceDto> StartProcessInstanceAsync(string processDefinitionKey, string businessKey, Dictionary<string, object> variables);
        Task<WorkflowInstanceDto> GetProcessInstanceAsync(string processInstanceId);
        Task<List<WorkflowInstanceDto>> GetProcessInstancesAsync(string processDefinitionKey);
        Task<bool> CompleteTaskAsync(string taskId, Dictionary<string, object> variables);
        Task<bool> CancelProcessInstanceAsync(string processInstanceId, string reason);
        Task<List<WorkflowTaskDto>> GetActiveTasksAsync(string processInstanceId);
        Task<List<WorkflowTaskDto>> GetTasksForUserAsync(string userId);
        Task<bool> AssignTaskAsync(string taskId, string userId);
        Task<bool> ClaimTaskAsync(string taskId, string userId);
        Task<WorkflowHistoryDto> GetProcessInstanceHistoryAsync(string processInstanceId);
        Task<bool> IsEngineHealthyAsync();
    }

    public class WorkflowTaskDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProcessInstanceId { get; set; } = string.Empty;
        public string ProcessDefinitionKey { get; set; } = string.Empty;
        public string? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "Medium";
        public Dictionary<string, object> Variables { get; set; } = new();
        public string? Description { get; set; }
        public string? FormKey { get; set; }
    }

    public class WorkflowHistoryDto
    {
        public string ProcessInstanceId { get; set; } = string.Empty;
        public List<WorkflowHistoryEvent> Events { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
    }

    public class WorkflowHistoryEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
