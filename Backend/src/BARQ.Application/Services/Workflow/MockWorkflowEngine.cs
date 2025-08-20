using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services.Workflow
{
    public class MockWorkflowEngine : IWorkflowEngine
    {
        private readonly ILogger<MockWorkflowEngine> _logger;
        private readonly Dictionary<string, string> _deployedProcesses = new();
        private readonly Dictionary<string, WorkflowInstanceDto> _processInstances = new();
        private readonly Dictionary<string, List<WorkflowTaskDto>> _activeTasks = new();
        private readonly Dictionary<string, WorkflowHistoryDto> _processHistory = new();

        public MockWorkflowEngine(ILogger<MockWorkflowEngine> logger)
        {
            _logger = logger;
        }

        public async Task<string> DeployProcessDefinitionAsync(string processDefinition, string processKey)
        {
            _logger.LogInformation("Mock: Deploying process definition with key {ProcessKey}", processKey);
            
            var deploymentId = Guid.NewGuid().ToString();
            _deployedProcesses[processKey] = deploymentId;
            
            await Task.Delay(100); // Simulate deployment time
            
            _logger.LogInformation("Mock: Process definition deployed with deployment ID {DeploymentId}", deploymentId);
            return deploymentId;
        }

        public async Task<WorkflowInstanceDto> StartProcessInstanceAsync(string processDefinitionKey, string businessKey, Dictionary<string, object> variables)
        {
            _logger.LogInformation("Mock: Starting process instance for key {ProcessKey} with business key {BusinessKey}", 
                processDefinitionKey, businessKey);

            if (!_deployedProcesses.ContainsKey(processDefinitionKey))
            {
                throw new InvalidOperationException($"Process definition with key {processDefinitionKey} not found");
            }

            var instanceId = Guid.NewGuid().ToString();
            var instance = new WorkflowInstanceDto
            {
                Id = Guid.NewGuid(),
                ProcessInstanceId = instanceId,
                WorkflowName = processDefinitionKey,
                Status = "Running",
                StartedAt = DateTime.UtcNow,
                CurrentStep = "StartEvent",
                ProgressPercentage = 10,
                Priority = variables.ContainsKey("priority") ? variables["priority"].ToString() : "Medium",
                CreatedAt = DateTime.UtcNow
            };

            _processInstances[instanceId] = instance;

            var initialTask = new WorkflowTaskDto
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Initial Task",
                ProcessInstanceId = instanceId,
                ProcessDefinitionKey = processDefinitionKey,
                CreatedAt = DateTime.UtcNow,
                Priority = instance.Priority ?? "Medium",
                Variables = variables,
                Description = "Initial workflow task"
            };

            _activeTasks[instanceId] = new List<WorkflowTaskDto> { initialTask };

            var history = new WorkflowHistoryDto
            {
                ProcessInstanceId = instanceId,
                StartTime = DateTime.UtcNow,
                Status = "Running",
                Events = new List<WorkflowHistoryEvent>
                {
                    new WorkflowHistoryEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "ProcessStarted",
                        ActivityId = "StartEvent",
                        ActivityName = "Process Started",
                        Timestamp = DateTime.UtcNow,
                        Data = variables
                    }
                }
            };

            _processHistory[instanceId] = history;

            await Task.Delay(50); // Simulate processing time

            _logger.LogInformation("Mock: Process instance started with ID {InstanceId}", instanceId);
            return instance;
        }

        public async Task<WorkflowInstanceDto> GetProcessInstanceAsync(string processInstanceId)
        {
            _logger.LogDebug("Mock: Getting process instance {InstanceId}", processInstanceId);
            
            await Task.Delay(10); // Simulate query time
            
            if (_processInstances.TryGetValue(processInstanceId, out var instance))
            {
                return instance;
            }

            throw new ArgumentException($"Process instance {processInstanceId} not found");
        }

        public async Task<List<WorkflowInstanceDto>> GetProcessInstancesAsync(string processDefinitionKey)
        {
            _logger.LogDebug("Mock: Getting process instances for key {ProcessKey}", processDefinitionKey);
            
            await Task.Delay(20); // Simulate query time
            
            var instances = _processInstances.Values
                .Where(i => i.WorkflowName == processDefinitionKey)
                .ToList();

            return instances;
        }

        public async Task<bool> CompleteTaskAsync(string taskId, Dictionary<string, object> variables)
        {
            _logger.LogInformation("Mock: Completing task {TaskId}", taskId);

            foreach (var instanceTasks in _activeTasks.Values)
            {
                var task = instanceTasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    instanceTasks.Remove(task);
                    
                    if (_processInstances.TryGetValue(task.ProcessInstanceId, out var instance))
                    {
                        instance.ProgressPercentage = Math.Min(100, instance.ProgressPercentage + 20);
                        instance.CurrentStep = "TaskCompleted";
                        instance.UpdatedAt = DateTime.UtcNow;

                        if (instance.ProgressPercentage >= 100)
                        {
                            instance.Status = "Completed";
                            instance.CompletedAt = DateTime.UtcNow;
                            instance.IsSuccessful = true;
                        }

                        if (_processHistory.TryGetValue(task.ProcessInstanceId, out var history))
                        {
                            history.Events.Add(new WorkflowHistoryEvent
                            {
                                Id = Guid.NewGuid().ToString(),
                                Type = "TaskCompleted",
                                ActivityId = taskId,
                                ActivityName = task.Name,
                                Timestamp = DateTime.UtcNow,
                                Data = variables
                            });

                            if (instance.Status == "Completed")
                            {
                                history.EndTime = DateTime.UtcNow;
                                history.Status = "Completed";
                                history.Duration = history.EndTime - history.StartTime;
                            }
                        }
                    }

                    await Task.Delay(30); // Simulate completion time
                    _logger.LogInformation("Mock: Task {TaskId} completed successfully", taskId);
                    return true;
                }
            }

            _logger.LogWarning("Mock: Task {TaskId} not found", taskId);
            return false;
        }

        public async Task<bool> CancelProcessInstanceAsync(string processInstanceId, string reason)
        {
            _logger.LogInformation("Mock: Cancelling process instance {InstanceId} with reason: {Reason}", 
                processInstanceId, reason);

            if (_processInstances.TryGetValue(processInstanceId, out var instance))
            {
                instance.Status = "Cancelled";
                instance.CompletedAt = DateTime.UtcNow;
                instance.IsSuccessful = false;
                instance.ErrorMessage = $"Cancelled: {reason}";
                instance.UpdatedAt = DateTime.UtcNow;

                _activeTasks.Remove(processInstanceId);

                if (_processHistory.TryGetValue(processInstanceId, out var history))
                {
                    history.Events.Add(new WorkflowHistoryEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "ProcessCancelled",
                        ActivityId = "CancelEvent",
                        ActivityName = "Process Cancelled",
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object> { { "reason", reason } }
                    });

                    history.EndTime = DateTime.UtcNow;
                    history.Status = "Cancelled";
                    history.Duration = history.EndTime - history.StartTime;
                }

                await Task.Delay(20); // Simulate cancellation time
                _logger.LogInformation("Mock: Process instance {InstanceId} cancelled successfully", processInstanceId);
                return true;
            }

            _logger.LogWarning("Mock: Process instance {InstanceId} not found for cancellation", processInstanceId);
            return false;
        }

        public async Task<List<WorkflowTaskDto>> GetActiveTasksAsync(string processInstanceId)
        {
            _logger.LogDebug("Mock: Getting active tasks for process instance {InstanceId}", processInstanceId);
            
            await Task.Delay(10); // Simulate query time
            
            if (_activeTasks.TryGetValue(processInstanceId, out var tasks))
            {
                return tasks.ToList();
            }

            return new List<WorkflowTaskDto>();
        }

        public async Task<List<WorkflowTaskDto>> GetTasksForUserAsync(string userId)
        {
            _logger.LogDebug("Mock: Getting tasks for user {UserId}", userId);
            
            await Task.Delay(15); // Simulate query time
            
            var userTasks = new List<WorkflowTaskDto>();
            
            foreach (var instanceTasks in _activeTasks.Values)
            {
                userTasks.AddRange(instanceTasks.Where(t => t.AssigneeId == userId));
            }

            return userTasks;
        }

        public async Task<bool> AssignTaskAsync(string taskId, string userId)
        {
            _logger.LogInformation("Mock: Assigning task {TaskId} to user {UserId}", taskId, userId);

            foreach (var instanceTasks in _activeTasks.Values)
            {
                var task = instanceTasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    task.AssigneeId = userId;
                    task.AssigneeName = $"User {userId}"; // In real implementation, would lookup user name
                    
                    await Task.Delay(10); // Simulate assignment time
                    _logger.LogInformation("Mock: Task {TaskId} assigned to user {UserId}", taskId, userId);
                    return true;
                }
            }

            _logger.LogWarning("Mock: Task {TaskId} not found for assignment", taskId);
            return false;
        }

        public async Task<bool> ClaimTaskAsync(string taskId, string userId)
        {
            _logger.LogInformation("Mock: User {UserId} claiming task {TaskId}", userId, taskId);

            foreach (var instanceTasks in _activeTasks.Values)
            {
                var task = instanceTasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null && task.AssigneeId == null)
                {
                    task.AssigneeId = userId;
                    task.AssigneeName = $"User {userId}"; // In real implementation, would lookup user name
                    
                    await Task.Delay(10); // Simulate claim time
                    _logger.LogInformation("Mock: Task {TaskId} claimed by user {UserId}", taskId, userId);
                    return true;
                }
            }

            _logger.LogWarning("Mock: Task {TaskId} not found or already assigned", taskId);
            return false;
        }

        public async Task<WorkflowHistoryDto> GetProcessInstanceHistoryAsync(string processInstanceId)
        {
            _logger.LogDebug("Mock: Getting history for process instance {InstanceId}", processInstanceId);
            
            await Task.Delay(15); // Simulate query time
            
            if (_processHistory.TryGetValue(processInstanceId, out var history))
            {
                return history;
            }

            throw new ArgumentException($"Process instance history {processInstanceId} not found");
        }

        public async Task<bool> IsEngineHealthyAsync()
        {
            _logger.LogDebug("Mock: Checking workflow engine health");
            
            await Task.Delay(5); // Simulate health check time
            
            return true;
        }
    }
}
