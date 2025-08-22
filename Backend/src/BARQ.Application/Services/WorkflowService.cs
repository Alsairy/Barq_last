using BARQ.Application.Interfaces;
using BARQ.Application.Services.Workflow;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly BarqDbContext _context;
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkflowService> _logger;
        private readonly ITenantProvider _tenantProvider;

        public WorkflowService(
            BarqDbContext context,
            IWorkflowEngine workflowEngine,
            IBackgroundJobService backgroundJobService,
            IConfiguration configuration,
            ILogger<WorkflowService> logger,
            ITenantProvider tenantProvider)
        {
            _context = context;
            _workflowEngine = workflowEngine;
            _backgroundJobService = backgroundJobService;
            _configuration = configuration;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        public async Task<PagedResult<WorkflowDto>> GetWorkflowsAsync(Guid tenantId, ListRequest request)
        {
            var query = _context.Workflows
                .Where(w => w.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(w => w.Name.Contains(request.SearchTerm) ||
                                        (w.Description != null && w.Description.Contains(request.SearchTerm)) ||
                                        w.Category.Contains(request.SearchTerm) ||
                                        w.WorkflowType.Contains(request.SearchTerm));
            }

            var totalCount = await query.CountAsync();

            var workflows = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(w => w.WorkflowInstances)
                .Select(w => new WorkflowDto
                {
                    Id = w.Id,
                    TenantId = w.TenantId,
                    Name = w.Name,
                    Description = w.Description,
                    WorkflowType = w.WorkflowType,
                    Category = w.Category,
                    ProcessDefinition = w.ProcessDefinition,
                    ProcessDefinitionKey = w.ProcessDefinitionKey,
                    Version = w.Version,
                    IsActive = w.IsActive,
                    IsDefault = w.IsDefault,
                    Priority = w.Priority,
                    TimeoutMinutes = w.TimeoutMinutes,
                    Tags = w.Tags,
                    ExecutionCount = w.ExecutionCount,
                    LastExecuted = w.LastExecuted,
                    CreatedAt = w.CreatedAt,
                    UpdatedAt = w.UpdatedAt,
                    Instances = w.WorkflowInstances.Select(i => new WorkflowInstanceDto
                    {
                        Id = i.Id,
                        TenantId = i.TenantId,
                        WorkflowId = i.WorkflowId,
                        ProcessInstanceId = i.ProcessInstanceId,
                        Status = i.Status,
                        StartedAt = i.StartedAt,
                        CompletedAt = i.CompletedAt,
                        CurrentStep = i.CurrentStep,
                        ProgressPercentage = i.ProgressPercentage,
                        Priority = i.Priority,
                        IsSuccessful = i.IsSuccessful,
                        ErrorMessage = i.ErrorMessage,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt
                    }).ToList()
                })
                .ToListAsync();

            return new PagedResult<WorkflowDto>
            {
                Items = workflows,
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<WorkflowDto?> GetWorkflowByIdAsync(Guid id)
        {
            var workflow = await _context.Workflows
                .Include(w => w.WorkflowInstances)
                .Where(w => w.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workflow == null) throw new ArgumentException("Workflow not found");

            return new WorkflowDto
            {
                Id = workflow.Id,
                TenantId = workflow.TenantId,
                Name = workflow.Name,
                Description = workflow.Description,
                WorkflowType = workflow.WorkflowType,
                Category = workflow.Category,
                ProcessDefinition = workflow.ProcessDefinition,
                ProcessDefinitionKey = workflow.ProcessDefinitionKey,
                Version = workflow.Version,
                IsActive = workflow.IsActive,
                IsDefault = workflow.IsDefault,
                Priority = workflow.Priority,
                TimeoutMinutes = workflow.TimeoutMinutes,
                Tags = workflow.Tags,
                ExecutionCount = workflow.ExecutionCount,
                LastExecuted = workflow.LastExecuted,
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.UpdatedAt,
                Instances = workflow.WorkflowInstances.Select(i => new WorkflowInstanceDto
                {
                    Id = i.Id,
                    TenantId = i.TenantId,
                    WorkflowId = i.WorkflowId,
                    ProcessInstanceId = i.ProcessInstanceId,
                    Status = i.Status,
                    StartedAt = i.StartedAt,
                    CompletedAt = i.CompletedAt,
                    CurrentStep = i.CurrentStep,
                    ProgressPercentage = i.ProgressPercentage,
                    Priority = i.Priority,
                    IsSuccessful = i.IsSuccessful,
                    ErrorMessage = i.ErrorMessage,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                }).ToList()
            };
        }

        public async Task<WorkflowDto> CreateWorkflowAsync(Guid tenantId, CreateWorkflowRequest request)
        {
            var workflow = new Core.Entities.Workflow
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                WorkflowType = request.WorkflowType,
                Category = request.Category,
                ProcessDefinition = request.ProcessDefinition,
                ProcessDefinitionKey = request.ProcessDefinitionKey,
                Version = request.Version,
                IsActive = true,
                IsDefault = request.IsDefault,
                Priority = request.Priority,
                TimeoutMinutes = request.TimeoutMinutes,
                Tags = request.Tags,
                CreatedAt = DateTime.UtcNow
            };

            var isFlowableEnabled = _configuration.GetValue<bool>("Features:Flowable", false);
            if (isFlowableEnabled)
            {
                try
                {
                    var deploymentId = await _workflowEngine.DeployProcessDefinitionAsync(
                        request.ProcessDefinition, 
                        request.ProcessDefinitionKey ?? workflow.Id.ToString());
                    
                    workflow.ProcessDefinitionKey = request.ProcessDefinitionKey ?? workflow.Id.ToString();
                    _logger.LogInformation("Workflow {WorkflowId} deployed to Flowable with deployment ID {DeploymentId}", 
                        workflow.Id, deploymentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deploy workflow {WorkflowId} to Flowable", workflow.Id);
                    throw new InvalidOperationException($"Failed to deploy workflow to engine: {ex.Message}");
                }
            }

            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();

            return await GetWorkflowByIdAsync(workflow.Id) ?? throw new InvalidOperationException("Failed to retrieve created workflow");
        }

        public async Task<WorkflowDto> UpdateWorkflowAsync(Guid id, CreateWorkflowRequest request)
        {
            var workflow = await _context.Workflows
                .Where(w => w.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workflow == null)
                throw new ArgumentException("Workflow not found");

            workflow.Name = request.Name;
            workflow.Description = request.Description;
            workflow.WorkflowType = request.WorkflowType;
            workflow.Category = request.Category;
            workflow.ProcessDefinition = request.ProcessDefinition;
            workflow.ProcessDefinitionKey = request.ProcessDefinitionKey;
            workflow.Version = request.Version;
            workflow.IsDefault = request.IsDefault;
            workflow.Priority = request.Priority;
            workflow.TimeoutMinutes = request.TimeoutMinutes;
            workflow.Tags = request.Tags;
            workflow.UpdatedAt = DateTime.UtcNow;

            var isFlowableEnabled = _configuration.GetValue<bool>("Features:Flowable", false);
            if (isFlowableEnabled && !string.IsNullOrEmpty(workflow.ProcessDefinitionKey))
            {
                try
                {
                    await _workflowEngine.DeployProcessDefinitionAsync(
                        request.ProcessDefinition, 
                        workflow.ProcessDefinitionKey);
                    
                    _logger.LogInformation("Workflow {WorkflowId} redeployed to Flowable", workflow.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to redeploy workflow {WorkflowId} to Flowable", workflow.Id);
                    throw new InvalidOperationException($"Failed to redeploy workflow to engine: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return await GetWorkflowByIdAsync(workflow.Id) ?? throw new InvalidOperationException("Failed to retrieve updated workflow");
        }

        public async Task<bool> DeleteWorkflowAsync(Guid id)
        {
            var workflow = await _context.Workflows
                .Where(w => w.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workflow == null) return false;

            workflow.IsDeleted = true;
            workflow.DeletedAt = DateTime.UtcNow;
            workflow.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<WorkflowInstanceDto> StartWorkflowAsync(Guid tenantId, StartWorkflowRequest request)
        {
            var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == request.WorkflowId && w.TenantId == tenantId);
            if (workflow == null)
                throw new ArgumentException("Workflow not found");

            if (!workflow.IsActive)
                throw new InvalidOperationException("Workflow is not active");

            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WorkflowId = workflow.Id,
                Status = "Starting",
                StartedAt = DateTime.UtcNow,
                CurrentStep = "InitialStep",
                ProgressPercentage = 0,
                Priority = request.Priority,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowInstances.Add(instance);
            await _context.SaveChangesAsync();

            var jobId = await _backgroundJobService.EnqueueAsync<IWorkflowService>(async service =>
            {
                await ExecuteWorkflowInstanceAsync(instance.Id, request.Input, request.Variables);
            });

            _logger.LogInformation("Workflow instance {InstanceId} queued for execution with job {JobId}", 
                instance.Id, jobId);

            workflow.ExecutionCount++;
            workflow.LastExecuted = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new WorkflowInstanceDto
            {
                Id = instance.Id,
                TenantId = instance.TenantId,
                WorkflowId = instance.WorkflowId,
                WorkflowName = workflow.Name,
                ProcessInstanceId = instance.ProcessInstanceId,
                Status = instance.Status,
                StartedAt = instance.StartedAt,
                CompletedAt = instance.CompletedAt,
                CurrentStep = instance.CurrentStep,
                ProgressPercentage = instance.ProgressPercentage,
                Priority = instance.Priority,
                IsSuccessful = instance.IsSuccessful,
                ErrorMessage = instance.ErrorMessage,
                CreatedAt = instance.CreatedAt,
                UpdatedAt = instance.UpdatedAt
            };
        }

        public async Task<List<WorkflowInstanceDto>> GetWorkflowInstancesAsync(Guid workflowId)
        {
            var instances = await _context.WorkflowInstances
                .Where(i => i.TenantId == _tenantProvider.GetTenantId() && i.WorkflowId == workflowId)
                .Include(i => i.Workflow)
                .Select(i => new WorkflowInstanceDto
                {
                    Id = i.Id,
                    TenantId = i.TenantId,
                    WorkflowId = i.WorkflowId,
                    WorkflowName = i.Workflow.Name,
                    ProcessInstanceId = i.ProcessInstanceId,
                    Status = i.Status,
                    StartedAt = i.StartedAt,
                    CompletedAt = i.CompletedAt,
                    CurrentStep = i.CurrentStep,
                    ProgressPercentage = i.ProgressPercentage,
                    Priority = i.Priority,
                    IsSuccessful = i.IsSuccessful,
                    ErrorMessage = i.ErrorMessage,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                })
                .ToListAsync();

            return instances;
        }

        public async Task<WorkflowInstanceDto?> GetWorkflowInstanceByIdAsync(Guid instanceId)
        {
            var instance = await _context.WorkflowInstances
                .Include(i => i.Workflow)
                .Where(i => i.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(i => i.Id == instanceId);

            if (instance == null) throw new ArgumentException("Workflow instance not found");

            return new WorkflowInstanceDto
            {
                Id = instance.Id,
                TenantId = instance.TenantId,
                WorkflowId = instance.WorkflowId,
                WorkflowName = instance.Workflow.Name,
                ProcessInstanceId = instance.ProcessInstanceId,
                Status = instance.Status,
                StartedAt = instance.StartedAt,
                CompletedAt = instance.CompletedAt,
                CurrentStep = instance.CurrentStep,
                ProgressPercentage = instance.ProgressPercentage,
                Priority = instance.Priority,
                IsSuccessful = instance.IsSuccessful,
                ErrorMessage = instance.ErrorMessage,
                CreatedAt = instance.CreatedAt,
                UpdatedAt = instance.UpdatedAt
            };
        }

        public async Task<bool> CancelWorkflowInstanceAsync(Guid instanceId)
        {
            var instance = await _context.WorkflowInstances
                .Where(i => i.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(i => i.Id == instanceId);
            if (instance == null) return false;

            if (instance.Status == "Completed" || instance.Status == "Cancelled")
                return false;

            instance.Status = "Cancelled";
            instance.CompletedAt = DateTime.UtcNow;
            instance.IsSuccessful = false;
            instance.ErrorMessage = "Cancelled by user";
            instance.UpdatedAt = DateTime.UtcNow;

            var isFlowableEnabled = _configuration.GetValue<bool>("Features:Flowable", false);
            if (isFlowableEnabled && !string.IsNullOrEmpty(instance.ProcessInstanceId))
            {
                try
                {
                    await _workflowEngine.CancelProcessInstanceAsync(instance.ProcessInstanceId, "Cancelled by user");
                    _logger.LogInformation("Workflow instance {InstanceId} cancelled in Flowable", instanceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cancel workflow instance {InstanceId} in Flowable", instanceId);
                }
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteWorkflowStepAsync(Guid instanceId, string stepId, string output)
        {
            var instance = await _context.WorkflowInstances
                .Where(i => i.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(i => i.Id == instanceId);
            if (instance == null) return false;

            if (instance.Status != "Running")
                return false;

            var isFlowableEnabled = _configuration.GetValue<bool>("Features:Flowable", false);
            if (isFlowableEnabled && !string.IsNullOrEmpty(instance.ProcessInstanceId))
            {
                try
                {
                    var variables = new Dictionary<string, object> { { "output", output } };
                    await _workflowEngine.CompleteTaskAsync(stepId, variables);
                    
                    var engineInstance = await _workflowEngine.GetProcessInstanceAsync(instance.ProcessInstanceId);
                    instance.Status = engineInstance.Status;
                    instance.CurrentStep = engineInstance.CurrentStep;
                    instance.ProgressPercentage = engineInstance.ProgressPercentage;
                    
                    if (engineInstance.Status == "Completed")
                    {
                        instance.CompletedAt = DateTime.UtcNow;
                        instance.IsSuccessful = true;
                    }
                    
                    _logger.LogInformation("Workflow step {StepId} completed for instance {InstanceId}", stepId, instanceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to complete workflow step {StepId} for instance {InstanceId}", stepId, instanceId);
                    return false;
                }
            }
            else
            {
                instance.ProgressPercentage = Math.Min(100, instance.ProgressPercentage + 25);
                instance.CurrentStep = $"Step_{DateTime.UtcNow:HHmmss}";
                
                if (instance.ProgressPercentage >= 100)
                {
                    instance.Status = "Completed";
                    instance.CompletedAt = DateTime.UtcNow;
                    instance.IsSuccessful = true;
                }
            }

            instance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        private async System.Threading.Tasks.Task ExecuteWorkflowInstanceAsync(Guid instanceId, string? input, string? variables)
        {
            var instance = await _context.WorkflowInstances
                .Include(i => i.Workflow)
                .Where(i => i.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(i => i.Id == instanceId);

            if (instance == null)
            {
                _logger.LogError("Workflow instance {InstanceId} not found for execution", instanceId);
                return;
            }

            try
            {
                instance.Status = "Running";
                instance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var isFlowableEnabled = _configuration.GetValue<bool>("Features:Flowable", false);
                if (isFlowableEnabled)
                {
                    var processVariables = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(input))
                        processVariables["input"] = input;
                    if (!string.IsNullOrEmpty(variables))
                        processVariables["variables"] = variables;

                    var engineInstance = await _workflowEngine.StartProcessInstanceAsync(
                        instance.Workflow.ProcessDefinitionKey ?? instance.WorkflowId.ToString(),
                        instance.Id.ToString(),
                        processVariables);

                    instance.ProcessInstanceId = engineInstance.ProcessInstanceId;
                    instance.Status = engineInstance.Status;
                    instance.CurrentStep = engineInstance.CurrentStep;
                    instance.ProgressPercentage = engineInstance.ProgressPercentage;

                    _logger.LogInformation("Workflow instance {InstanceId} started in Flowable with process instance {ProcessInstanceId}", 
                        instanceId, engineInstance.ProcessInstanceId);
                }
                else
                {
                    var steps = ParseWorkflowSteps(instance.Workflow.ProcessDefinition ?? "");
                    var totalSteps = steps.Count;
                    
                    for (int i = 0; i < totalSteps; i++)
                    {
                        var step = steps[i];
                        instance.CurrentStep = step.Name;
                        instance.ProgressPercentage = (int)((double)(i + 1) / totalSteps * 100);
                        
                        await _context.SaveChangesAsync();
                        
                        var inputDict = new Dictionary<string, object>();
                        if (!string.IsNullOrEmpty(input))
                            inputDict["input"] = input;
                        
                        var variablesDict = new Dictionary<string, object>();
                        if (!string.IsNullOrEmpty(variables))
                            variablesDict["variables"] = variables;
                        
                        await ExecuteWorkflowStep(step, inputDict, variablesDict);
                        
                        _logger.LogInformation("Workflow instance {InstanceId} completed step {StepName} ({Progress}%)", 
                            instanceId, step.Name, instance.ProgressPercentage);
                    }
                    
                    instance.Status = "Completed";
                    instance.CompletedAt = DateTime.UtcNow;
                    instance.IsSuccessful = true;

                    _logger.LogInformation("Workflow instance {InstanceId} completed using built-in engine", instanceId);
                }

                instance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                instance.Status = "Failed";
                instance.CompletedAt = DateTime.UtcNow;
                instance.IsSuccessful = false;
                instance.ErrorMessage = ex.Message;
                instance.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogError(ex, "Workflow instance {InstanceId} failed with error: {Error}", instanceId, ex.Message);
            }
        }

        private List<WorkflowStep> ParseWorkflowSteps(string processDefinition)
        {
            var steps = new List<WorkflowStep>();
            
            if (string.IsNullOrEmpty(processDefinition))
            {
                steps.Add(new WorkflowStep { Name = "Default Step", Type = "Task" });
                return steps;
            }

            try
            {
                var lines = processDefinition.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("step:"))
                    {
                        var stepName = line.Replace("step:", "").Trim();
                        steps.Add(new WorkflowStep { Name = stepName, Type = "Task" });
                    }
                }
                
                if (steps.Count == 0)
                {
                    steps.Add(new WorkflowStep { Name = "Default Step", Type = "Task" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse workflow steps from process definition");
                steps.Add(new WorkflowStep { Name = "Default Step", Type = "Task" });
            }

            return steps;
        }

        private async System.Threading.Tasks.Task ExecuteWorkflowStep(WorkflowStep step, Dictionary<string, object> input, Dictionary<string, object> variables)
        {
            try
            {
                _logger.LogInformation("Executing workflow step: {StepName}", step.Name);
                
                await System.Threading.Tasks.Task.Delay(100);
                
                variables[$"step_{step.Name}_completed"] = true;
                variables[$"step_{step.Name}_timestamp"] = DateTime.UtcNow;
                
                _logger.LogInformation("Completed workflow step: {StepName}", step.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute workflow step: {StepName}", step.Name);
                throw;
            }
        }
    }

    public class WorkflowStep
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
