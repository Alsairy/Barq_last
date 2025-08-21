# ADR-002: BPM Integration Boundary - Flowable as External Service

## Status
Accepted

## Context
The BARQ platform requires Business Process Management (BPM) capabilities for workflow orchestration, SLA management, and escalation handling. We need to decide how to integrate BPM functionality.

## Decision
We will treat Flowable BPM as an external Java service and define a clean REST boundary for integration.

## Rationale

### Flowable as External Service
- **Separation of Concerns**: Clear boundary between .NET application and BPM engine
- **Technology Independence**: Allows BPM engine to evolve independently
- **Scalability**: BPM engine can be scaled separately based on workflow load
- **Expertise**: Leverages Flowable's Java-based expertise and ecosystem
- **Maintenance**: Easier to maintain and upgrade each component independently

### REST API Boundary
- **Standard Protocol**: HTTP/REST is well-understood and widely supported
- **Language Agnostic**: Allows future integration with other technologies
- **Monitoring**: Easy to monitor and debug API calls
- **Security**: Standard authentication and authorization mechanisms
- **Documentation**: OpenAPI/Swagger documentation for clear contracts

## Implementation

### Integration Components
1. **FlowableGateway Service**: .NET service for Flowable REST API communication
2. **Resilience Patterns**: Circuit breaker, retries, timeouts
3. **Authentication**: JWT pass-through or service account
4. **Tenancy**: X-Tenant-Id header propagation
5. **Error Mapping**: Flowable errors to BARQ ProblemDetails

### Key Endpoints
- Deploy BPMN processes
- Start process instances
- Claim and complete tasks
- Query process history
- Send signals and timers

## Consequences

### Positive
- **Flexibility**: Easy to replace or upgrade BPM engine
- **Reliability**: Failures in BPM don't crash main application
- **Performance**: Can optimize each service independently
- **Testing**: Can mock BPM service for unit tests

### Negative
- **Network Latency**: Additional network calls for BPM operations
- **Complexity**: Distributed system complexity and error handling
- **Deployment**: Multiple services to deploy and manage

## Compliance
This approach provides a clean architectural boundary while leveraging the full power of Flowable BPM engine for enterprise workflow requirements.
