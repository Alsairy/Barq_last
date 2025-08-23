# ADR-005: Observability Strategy

## Status
Accepted

## Context
BARQ requires comprehensive observability for production operations, including structured logging, metrics collection, distributed tracing, and health monitoring across the multi-tenant AI orchestration platform.

## Decision
Implement a multi-layered observability strategy with:

### 1. Structured Logging
- **Format**: JSON with consistent schema
- **Correlation IDs**: Track requests across services
- **Log Levels**: DEBUG, INFO, WARN, ERROR, FATAL
- **Sensitive Data**: Automatic redaction of PII/secrets

### 2. Metrics Collection
- **Application Metrics**: Request latency, throughput, error rates
- **Business Metrics**: Task completion rates, SLA violations, AI costs
- **Infrastructure Metrics**: CPU, memory, disk, network
- **Custom Metrics**: Tenant-specific usage patterns

### 3. Distributed Tracing
- **Trace Context**: Propagate across API calls and background jobs
- **Span Attributes**: Include tenant ID, user ID, operation type
- **Performance Insights**: Identify bottlenecks in AI workflows

### 4. Health Monitoring
- **Liveness Probes**: Basic service availability
- **Readiness Probes**: Dependency health (DB, AI providers)
- **Custom Health Checks**: Business logic validation

## Implementation

### Logging Configuration
```csharp
// Program.cs
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddJsonConsole(options =>
    {
        options.JsonWriterOptions = new JsonWriterOptions
        {
            Indented = false
        };
        options.IncludeScopes = true;
    });
});

// Add correlation ID middleware
builder.Services.AddScoped<CorrelationIdMiddleware>();
```

### Metrics Setup
```csharp
// Metrics configuration
builder.Services.AddSingleton<IMetricsCollector, MetricsCollector>();
builder.Services.Configure<MetricsOptions>(options =>
{
    options.EnableApplicationMetrics = true;
    options.EnableBusinessMetrics = true;
    options.MetricsPrefix = "barq";
});
```

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BarqDbContext>("database")
    .AddCheck<AIProviderHealthCheck>("ai-providers")
    .AddCheck<FlowableHealthCheck>("flowable-bpm")
    .AddCheck<BackgroundJobHealthCheck>("background-jobs");
```

### Log Schema
```json
{
  "timestamp": "2024-01-01T12:00:00.000Z",
  "level": "INFO",
  "message": "Task completed successfully",
  "correlationId": "abc123-def456-ghi789",
  "tenantId": "tenant-123",
  "userId": "user-456",
  "operation": "task.complete",
  "duration": 1250,
  "properties": {
    "taskId": "task-789",
    "aiProvider": "openai",
    "cost": 0.05
  }
}
```

### Key Metrics
- `barq_requests_total{method, endpoint, status, tenant}`
- `barq_request_duration_seconds{method, endpoint, tenant}`
- `barq_tasks_completed_total{tenant, priority, ai_provider}`
- `barq_sla_violations_total{tenant, severity}`
- `barq_ai_costs_total{tenant, provider, model}`
- `barq_background_jobs_processed_total{job_type, status}`

## Monitoring Dashboards

### Application Dashboard
- Request rate and latency percentiles
- Error rate by endpoint and tenant
- Database connection pool status
- Memory and CPU utilization

### Business Dashboard
- Task completion rates by tenant
- SLA compliance metrics
- AI usage and cost trends
- User activity patterns

### Infrastructure Dashboard
- System resource utilization
- Database performance metrics
- External service dependencies
- Alert status and escalations

## Alerting Rules

### Critical Alerts (PagerDuty)
- API error rate > 5% for 5 minutes
- Database connection failures
- SLA violations for Tier 1 customers
- AI provider outages

### Warning Alerts (Slack)
- Response time > 2s for 10 minutes
- Background job queue depth > 100
- Disk usage > 80%
- Memory usage > 85%

## Log Retention
- **Production**: 90 days in hot storage, 1 year in cold storage
- **Staging**: 30 days
- **Development**: 7 days

## Privacy & Compliance
- Automatic PII redaction in logs
- Tenant data isolation in metrics
- GDPR-compliant log retention policies
- Audit trail for sensitive operations

## Tools & Technologies
- **Logging**: Serilog with JSON formatting
- **Metrics**: Prometheus with custom collectors
- **Tracing**: OpenTelemetry with Jaeger
- **Dashboards**: Grafana with custom panels
- **Alerting**: AlertManager with PagerDuty/Slack

## Benefits
1. **Faster Issue Resolution**: Correlation IDs enable rapid troubleshooting
2. **Proactive Monitoring**: Alerts prevent customer-facing issues
3. **Performance Optimization**: Metrics identify bottlenecks
4. **Business Insights**: Usage patterns inform product decisions
5. **Compliance**: Audit trails meet regulatory requirements

## Risks & Mitigations
- **Log Volume**: Implement sampling and filtering
- **Performance Impact**: Async logging and batching
- **Storage Costs**: Tiered retention policies
- **Alert Fatigue**: Careful threshold tuning

## Alternatives Considered
- **ELK Stack**: Rejected due to operational complexity
- **Application Insights**: Rejected due to vendor lock-in
- **DataDog**: Rejected due to cost at scale

## Implementation Timeline
- **Phase 1**: Structured logging and basic metrics (Week 1-2)
- **Phase 2**: Health checks and dashboards (Week 3-4)
- **Phase 3**: Distributed tracing and alerting (Week 5-6)
- **Phase 4**: Advanced analytics and optimization (Week 7-8)

---

**Decision Date**: 2024-01-01  
**Review Date**: 2024-07-01  
**Owner**: Platform Engineering Team
