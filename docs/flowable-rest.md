# Flowable REST Integration Specification

## Overview
This document defines the integration contract between BARQ and the Flowable BPM engine, treating Flowable as an external Java service with a clean REST boundary.

## Architecture

### Integration Boundary
```
┌─────────────────┐    HTTP/REST    ┌─────────────────┐
│   BARQ (.NET)   │◄──────────────►│ Flowable (Java) │
│                 │                 │                 │
│ FlowableGateway │                 │ Flowable REST   │
│ Service         │                 │ API             │
└─────────────────┘                 └─────────────────┘
```

### Service Responsibilities
- **BARQ**: Business logic, user management, tenant isolation, audit logging
- **Flowable**: Process execution, task management, workflow state, BPMN engine

## Authentication

### JWT Pass-through
```http
POST /flowable-rest/service/repository/deployments
Authorization: Bearer <jwt-token>
X-Tenant-Id: <tenant-guid>
Content-Type: application/json
```

### Service Account (Alternative)
```http
POST /flowable-rest/service/repository/deployments
Authorization: Basic <base64-encoded-credentials>
X-Tenant-Id: <tenant-guid>
X-User-Id: <user-guid>
Content-Type: application/json
```

## Tenancy Propagation

### Headers
- **X-Tenant-Id**: GUID identifying the tenant
- **X-User-Id**: GUID identifying the current user
- **X-Correlation-Id**: Request correlation for tracing

### Process Variables
```json
{
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "550e8400-e29b-41d4-a716-446655440001",
  "correlationId": "req-12345"
}
```

## Core Endpoints

### 1. Deploy BPMN Process
```http
POST /flowable-rest/service/repository/deployments
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="file"; filename="process.bpmn20.xml"
Content-Type: application/xml

<?xml version="1.0" encoding="UTF-8"?>
<definitions xmlns="http://www.omg.org/spec/BPMN/20100524/MODEL">
  <!-- BPMN content -->
</definitions>
--boundary--
```

**Response:**
```json
{
  "id": "deployment-123",
  "name": "Task Approval Process",
  "deploymentTime": "2024-01-01T10:00:00Z",
  "category": "barq-processes",
  "tenantId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 2. Start Process Instance
```http
POST /flowable-rest/service/runtime/process-instances
Content-Type: application/json

{
  "processDefinitionKey": "taskApprovalProcess",
  "businessKey": "task-456",
  "variables": [
    {
      "name": "taskId",
      "value": "550e8400-e29b-41d4-a716-446655440002",
      "type": "string"
    },
    {
      "name": "priority",
      "value": "high",
      "type": "string"
    },
    {
      "name": "tenantId",
      "value": "550e8400-e29b-41d4-a716-446655440000",
      "type": "string"
    }
  ],
  "tenantId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "id": "process-instance-789",
  "processDefinitionId": "taskApprovalProcess:1:deployment-123",
  "businessKey": "task-456",
  "suspended": false,
  "tenantId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 3. Query User Tasks
```http
GET /flowable-rest/service/runtime/tasks?assignee=user-123&tenantId=550e8400-e29b-41d4-a716-446655440000
```

**Response:**
```json
{
  "data": [
    {
      "id": "task-abc",
      "name": "Approve Task",
      "assignee": "user-123",
      "created": "2024-01-01T10:00:00Z",
      "dueDate": "2024-01-02T10:00:00Z",
      "priority": 50,
      "processInstanceId": "process-instance-789",
      "tenantId": "550e8400-e29b-41d4-a716-446655440000"
    }
  ],
  "total": 1,
  "start": 0,
  "sort": "created",
  "order": "asc",
  "size": 25
}
```

### 4. Claim Task
```http
POST /flowable-rest/service/runtime/tasks/task-abc/claim
Content-Type: application/json

{
  "assignee": "user-123"
}
```

### 5. Complete Task
```http
POST /flowable-rest/service/runtime/tasks/task-abc/complete
Content-Type: application/json

{
  "variables": [
    {
      "name": "approved",
      "value": true,
      "type": "boolean"
    },
    {
      "name": "comments",
      "value": "Task approved with minor modifications",
      "type": "string"
    }
  ]
}
```

### 6. Query Process History
```http
GET /flowable-rest/service/history/historic-process-instances?processInstanceId=process-instance-789
```

**Response:**
```json
{
  "data": [
    {
      "id": "process-instance-789",
      "processDefinitionId": "taskApprovalProcess:1:deployment-123",
      "businessKey": "task-456",
      "startTime": "2024-01-01T10:00:00Z",
      "endTime": "2024-01-01T11:30:00Z",
      "durationInMillis": 5400000,
      "startUserId": "user-456",
      "deleteReason": null,
      "tenantId": "550e8400-e29b-41d4-a716-446655440000"
    }
  ]
}
```

### 7. Send Signal
```http
POST /flowable-rest/service/runtime/signals
Content-Type: application/json

{
  "signalName": "escalationSignal",
  "variables": [
    {
      "name": "escalationLevel",
      "value": 2,
      "type": "integer"
    }
  ],
  "tenantId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 8. Timer Management
```http
GET /flowable-rest/service/runtime/jobs?processInstanceId=process-instance-789&jobType=timer
```

## Error Mapping

### Flowable Error Codes to BARQ ProblemDetails

#### 400 Bad Request
```json
{
  "type": "https://api.barq.com/problems/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "Invalid process definition",
  "instance": "/api/workflows/deploy",
  "flowableError": {
    "message": "Process definition validation failed",
    "exception": "org.flowable.engine.ActivitiException"
  }
}
```

#### 404 Not Found
```json
{
  "type": "https://api.barq.com/problems/resource-not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Process instance not found",
  "instance": "/api/workflows/process-instance-789",
  "flowableError": {
    "message": "No process instance found with id process-instance-789",
    "exception": "org.flowable.engine.ActivitiObjectNotFoundException"
  }
}
```

#### 409 Conflict
```json
{
  "type": "https://api.barq.com/problems/workflow-conflict",
  "title": "Workflow Conflict",
  "status": 409,
  "detail": "Task already claimed by another user",
  "instance": "/api/workflows/tasks/task-abc/claim",
  "flowableError": {
    "message": "Task task-abc is already claimed by user-456",
    "exception": "org.flowable.task.api.TaskAlreadyClaimedException"
  }
}
```

#### 500 Internal Server Error
```json
{
  "type": "https://api.barq.com/problems/workflow-engine-error",
  "title": "Workflow Engine Error",
  "status": 500,
  "detail": "Flowable engine encountered an internal error",
  "instance": "/api/workflows/start",
  "flowableError": {
    "message": "Database connection failed",
    "exception": "org.flowable.engine.ActivitiException"
  }
}
```

## FlowableGateway Implementation

### Interface Definition
```csharp
public interface IFlowableGateway
{
    Task<DeploymentResult> DeployAsync(Stream bpmnZip, CancellationToken cancellationToken = default);
    Task<string> StartProcessAsync(string processKey, object variables, CancellationToken cancellationToken = default);
    Task ClaimTaskAsync(string taskId, Guid userId, CancellationToken cancellationToken = default);
    Task CompleteTaskAsync(string taskId, object outputs, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlowableTask>> GetTasksAsync(string assignee = null, string candidateGroup = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlowableHistory>> GetHistoryAsync(string processInstanceId, CancellationToken cancellationToken = default);
    Task SignalAsync(string executionId, string signalName, object payload = null, CancellationToken cancellationToken = default);
}
```

### Resilience Implementation
```csharp
public class FlowableGateway : IFlowableGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FlowableGateway> _logger;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public FlowableGateway(
        HttpClient httpClient,
        ILogger<FlowableGateway> logger,
        ITenantProvider tenantProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tenantProvider = tenantProvider;
        
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Flowable request failed, retrying in {Delay}ms (attempt {RetryCount})",
                        timespan.TotalMilliseconds, retryCount);
                });
    }

    public async Task<string> StartProcessAsync(string processKey, object variables, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        
        var request = new
        {
            processDefinitionKey = processKey,
            variables = ConvertToFlowableVariables(variables),
            tenantId = tenantId.ToString()
        };

        using var activity = ActivitySource.StartActivity("FlowableGateway.StartProcess");
        activity?.SetTag("processKey", processKey);
        activity?.SetTag("tenantId", tenantId.ToString());

        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/flowable-rest/service/runtime/process-instances")
                {
                    Content = JsonContent.Create(request)
                };
                
                AddTenantHeaders(httpRequest, tenantId);
                
                return await _httpClient.SendAsync(httpRequest, cancellationToken);
            });

            if (!response.IsSuccessStatusCode)
            {
                await HandleFlowableError(response);
            }

            var result = await response.Content.ReadFromJsonAsync<ProcessInstanceResult>(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Started process instance {ProcessInstanceId} for process {ProcessKey}",
                result.Id, processKey);
                
            return result.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start process {ProcessKey}", processKey);
            throw;
        }
    }

    private void AddTenantHeaders(HttpRequestMessage request, Guid tenantId)
    {
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        request.Headers.Add("X-Correlation-Id", Activity.Current?.Id ?? Guid.NewGuid().ToString());
    }

    private async Task HandleFlowableError(HttpResponseMessage response)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        var flowableError = JsonSerializer.Deserialize<FlowableErrorResponse>(errorContent);
        
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ValidationException(flowableError?.Message ?? "Invalid request"),
            HttpStatusCode.NotFound => new NotFoundException(flowableError?.Message ?? "Resource not found"),
            HttpStatusCode.Conflict => new ConflictException(flowableError?.Message ?? "Resource conflict"),
            _ => new FlowableException($"Flowable error: {flowableError?.Message ?? "Unknown error"}")
        };
    }
}
```

## Monitoring and Observability

### Structured Logging
```csharp
_logger.LogInformation("Flowable operation {Operation} completed in {Duration}ms for tenant {TenantId}",
    "StartProcess", stopwatch.ElapsedMilliseconds, tenantId);

_logger.LogError(ex, "Flowable operation {Operation} failed for tenant {TenantId} with error {ErrorCode}",
    "CompleteTask", tenantId, errorCode);
```

### Metrics Collection
```csharp
public class FlowableMetrics
{
    private static readonly Counter RequestCounter = Metrics
        .CreateCounter("flowable_requests_total", "Total Flowable requests", new[] { "operation", "status" });
        
    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("flowable_request_duration_seconds", "Flowable request duration");

    public static void RecordRequest(string operation, string status, double duration)
    {
        RequestCounter.WithLabels(operation, status).Inc();
        RequestDuration.Observe(duration);
    }
}
```

### Health Checks
```csharp
public class FlowableHealthCheck : IHealthCheck
{
    private readonly IFlowableGateway _flowableGateway;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/flowable-rest/service/management/engine", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Flowable engine is responsive");
            }
            
            return HealthCheckResult.Unhealthy($"Flowable engine returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Flowable engine is not accessible", ex);
        }
    }
}
```

## Testing Strategy

### Integration Tests
```csharp
[Fact]
public async Task StartProcess_WithValidData_ReturnsProcessInstanceId()
{
    // Arrange
    var processKey = "testProcess";
    var variables = new { taskId = Guid.NewGuid(), priority = "high" };
    
    // Act
    var processInstanceId = await _flowableGateway.StartProcessAsync(processKey, variables);
    
    // Assert
    processInstanceId.Should().NotBeNullOrEmpty();
    
    // Verify process was actually started in Flowable
    var tasks = await _flowableGateway.GetTasksAsync();
    tasks.Should().Contain(t => t.ProcessInstanceId == processInstanceId);
}
```

### Mock Implementation
```csharp
public class MockFlowableGateway : IFlowableGateway
{
    private readonly Dictionary<string, string> _processInstances = new();
    private readonly List<FlowableTask> _tasks = new();

    public Task<string> StartProcessAsync(string processKey, object variables, CancellationToken cancellationToken = default)
    {
        var processInstanceId = Guid.NewGuid().ToString();
        _processInstances[processInstanceId] = processKey;
        
        // Create initial task
        _tasks.Add(new FlowableTask
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Initial Task",
            ProcessInstanceId = processInstanceId,
            Created = DateTime.UtcNow
        });
        
        return Task.FromResult(processInstanceId);
    }
    
    // ... other mock implementations
}
```

## Security Considerations

### Authentication
- Use service account with minimal required permissions
- Rotate credentials regularly
- Implement proper JWT validation if using pass-through

### Authorization
- Validate tenant access for all operations
- Implement process-level security
- Audit all workflow operations

### Data Protection
- Encrypt sensitive process variables
- Implement data retention policies
- Ensure GDPR compliance for process data

## Performance Optimization

### Connection Pooling
```csharp
services.AddHttpClient<IFlowableGateway, FlowableGateway>(client =>
{
    client.BaseAddress = new Uri(flowableBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 20
});
```

### Caching
```csharp
[MemoryCache(Duration = 300)] // 5 minutes
public async Task<ProcessDefinition> GetProcessDefinitionAsync(string processKey)
{
    // Implementation
}
```

### Batch Operations
```csharp
public async Task<BulkOperationResult> CompleteTasksAsync(IEnumerable<TaskCompletion> completions)
{
    var tasks = completions.Select(async c => await CompleteTaskAsync(c.TaskId, c.Variables));
    var results = await Task.WhenAll(tasks);
    
    return new BulkOperationResult
    {
        SuccessCount = results.Count(r => r.Success),
        FailureCount = results.Count(r => !r.Success)
    };
}
```

This specification provides a comprehensive contract for integrating BARQ with Flowable BPM while maintaining clear boundaries and ensuring robust, scalable operation.
