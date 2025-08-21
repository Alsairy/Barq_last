using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BARQ.Core.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace BARQ.Application.Services.Workflow;

public interface IFlowableGateway
{
    Task DeployAsync(Stream bpmnZip, CancellationToken ct = default);
    Task<string> StartAsync(string processKey, object variables, CancellationToken ct = default);
    Task ClaimAsync(string taskId, Guid userId, CancellationToken ct = default);
    Task CompleteAsync(string taskId, object outputs, CancellationToken ct = default);
    Task<IReadOnlyList<FlowableTask>> GetTasksAsync(string assignee, string candidateGroup, CancellationToken ct = default);
    Task<IReadOnlyList<FlowableHistory>> GetHistoryAsync(string processInstanceId, CancellationToken ct = default);
    Task SignalAsync(string executionId, string signalName, object payload, CancellationToken ct = default);
}

public class FlowableGateway : IFlowableGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FlowableGateway> _logger;
    private readonly FlowableOptions _options;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public FlowableGateway(
        HttpClient httpClient,
        ILogger<FlowableGateway> logger,
        IOptions<FlowableOptions> options,
        ITenantProvider tenantProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _tenantProvider = tenantProvider;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _options.ServiceAccountToken);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ShouldRetry(r.StatusCode))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: _options.RetryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1) * _options.RetryDelaySeconds),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Flowable request retry {RetryCount} after {Delay}ms. Reason: {Reason}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                })
            .WrapAsync(Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: _options.CircuitBreakerFailureThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(_options.CircuitBreakerTimeoutSeconds),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError("Flowable circuit breaker opened for {Duration}s. Reason: {Reason}",
                            duration.TotalSeconds, exception.Exception?.Message ?? exception.Result?.StatusCode.ToString());
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Flowable circuit breaker closed");
                    }));
    }

    public async Task DeployAsync(Stream bpmnZip, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(bpmnZip);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(streamContent, "file", "deployment.zip");

            var request = new HttpRequestMessage(HttpMethod.Post, "/flowable-rest/service/repository/deployments")
            {
                Content = content
            };

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var resp = await _httpClient.SendAsync(request, ct);
                return resp;
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to deploy BPMN: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("BPMN deployment successful: {Response}", responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying BPMN to Flowable");
            throw;
        }
    }

    public async Task<string> StartAsync(string processKey, object variables, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                processDefinitionKey = processKey,
                variables = ConvertToFlowableVariables(variables)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/flowable-rest/service/runtime/process-instances")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.SendAsync(request, ct);
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to start process: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var processInstanceId = result.GetProperty("id").GetString();

            _logger.LogInformation("Process instance started: {ProcessInstanceId} for process {ProcessKey}",
                processInstanceId, processKey);

            return processInstanceId!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting process {ProcessKey}", processKey);
            throw;
        }
    }

    public async Task ClaimAsync(string taskId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                action = "claim",
                assignee = userId.ToString()
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"/flowable-rest/service/runtime/tasks/{taskId}")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.SendAsync(request, ct);
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to claim task: {response.StatusCode} - {error}");
            }

            _logger.LogInformation("Task claimed: {TaskId} by user {UserId}", taskId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error claiming task {TaskId} for user {UserId}", taskId, userId);
            throw;
        }
    }

    public async Task CompleteAsync(string taskId, object outputs, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                action = "complete",
                variables = ConvertToFlowableVariables(outputs)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"/flowable-rest/service/runtime/tasks/{taskId}")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.SendAsync(request, ct);
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to complete task: {response.StatusCode} - {error}");
            }

            _logger.LogInformation("Task completed: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<IReadOnlyList<FlowableTask>> GetTasksAsync(string assignee, string candidateGroup, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(assignee))
                queryParams.Add($"assignee={Uri.EscapeDataString(assignee)}");
            if (!string.IsNullOrEmpty(candidateGroup))
                queryParams.Add($"candidateGroup={Uri.EscapeDataString(candidateGroup)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var request = new HttpRequestMessage(HttpMethod.Get, $"/flowable-rest/service/runtime/tasks{queryString}");

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.SendAsync(request, ct);
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to get tasks: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var tasks = new List<FlowableTask>();

            if (result.TryGetProperty("data", out var dataArray))
            {
                foreach (var taskElement in dataArray.EnumerateArray())
                {
                    tasks.Add(new FlowableTask
                    {
                        Id = taskElement.GetProperty("id").GetString()!,
                        Name = taskElement.GetProperty("name").GetString(),
                        Assignee = taskElement.TryGetProperty("assignee", out var assigneeProp) ? assigneeProp.GetString() : null,
                        ProcessInstanceId = taskElement.GetProperty("processInstanceId").GetString()!,
                        CreateTime = taskElement.GetProperty("createTime").GetDateTime()
                    });
                }
            }

            _logger.LogInformation("Retrieved {TaskCount} tasks for assignee {Assignee}, group {CandidateGroup}",
                tasks.Count, assignee, candidateGroup);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks for assignee {Assignee}, group {CandidateGroup}", assignee, candidateGroup);
            throw;
        }
    }

    public async Task<IReadOnlyList<FlowableHistory>> GetHistoryAsync(string processInstanceId, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"/flowable-rest/service/history/historic-process-instances?processInstanceId={Uri.EscapeDataString(processInstanceId)}");

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.SendAsync(request, ct);
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to get history: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var history = new List<FlowableHistory>();

            if (result.TryGetProperty("data", out var dataArray))
            {
                foreach (var historyElement in dataArray.EnumerateArray())
                {
                    history.Add(new FlowableHistory
                    {
                        Id = historyElement.GetProperty("id").GetString()!,
                        ProcessDefinitionKey = historyElement.GetProperty("processDefinitionKey").GetString()!,
                        StartTime = historyElement.GetProperty("startTime").GetDateTime(),
                        EndTime = historyElement.TryGetProperty("endTime", out var endTimeProp) ? endTimeProp.GetDateTime() : null,
                        StartUserId = historyElement.TryGetProperty("startUserId", out var startUserProp) ? startUserProp.GetString() : null
                    });
                }
            }

            _logger.LogInformation("Retrieved {HistoryCount} history records for process instance {ProcessInstanceId}",
                history.Count, processInstanceId);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for process instance {ProcessInstanceId}", processInstanceId);
            throw;
        }
    }

    public async Task SignalAsync(string executionId, string signalName, object payload, CancellationToken ct = default)
    {
        try
        {
            var signalPayload = new
            {
                signalName = signalName,
                executionId = executionId,
                variables = ConvertToFlowableVariables(payload)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/flowable-rest/service/runtime/signals")
            {
                Content = new StringContent(JsonSerializer.Serialize(signalPayload), Encoding.UTF8, "application/json")
            };

            AddTenantHeader(request);

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.SendAsync(request, ct);
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new FlowableException($"Failed to send signal: {response.StatusCode} - {error}");
            }

            _logger.LogInformation("Signal sent: {SignalName} to execution {ExecutionId}", signalName, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signal {SignalName} to execution {ExecutionId}", signalName, executionId);
            throw;
        }
    }

    private void AddTenantHeader(HttpRequestMessage request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId != Guid.Empty)
        {
            request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        }
    }

    private static bool ShouldRetry(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.InternalServerError ||
               statusCode == System.Net.HttpStatusCode.BadGateway ||
               statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               statusCode == System.Net.HttpStatusCode.GatewayTimeout;
    }

    private static object ConvertToFlowableVariables(object variables)
    {
        if (variables == null) return new { };

        var props = variables.GetType().GetProperties();
        var flowableVars = new Dictionary<string, object>();

        foreach (var prop in props)
        {
            var value = prop.GetValue(variables);
            if (value != null)
            {
                flowableVars[prop.Name] = new
                {
                    value = value,
                    type = GetFlowableType(value.GetType())
                };
            }
        }

        return flowableVars;
    }

    private static string GetFlowableType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "long";
        if (type == typeof(double) || type == typeof(decimal)) return "double";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "date";
        return "string";
    }
}

public class FlowableOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string ServiceAccountToken { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 4;
    public int RetryDelaySeconds { get; set; } = 1;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerTimeoutSeconds { get; set; } = 30;
}

public class FlowableTask
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Assignee { get; set; }
    public string ProcessInstanceId { get; set; } = "";
    public DateTime CreateTime { get; set; }
}

public class FlowableHistory
{
    public string Id { get; set; } = "";
    public string ProcessDefinitionKey { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? StartUserId { get; set; }
}

public class FlowableException : Exception
{
    public FlowableException(string message) : base(message) { }
    public FlowableException(string message, Exception innerException) : base(message, innerException) { }
}
