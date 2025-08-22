# ADR-001: Database Choice - SQL Server Only

## Status
Accepted

## Context
The BARQ platform requires a robust, enterprise-grade database solution that supports:
- Multi-tenant data isolation
- Complex relationships and transactions
- High availability and scalability
- Enterprise security features
- Strong consistency guarantees

## Decision
We will use SQL Server as the single database technology for the BARQ platform, removing all PostgreSQL references and dependencies.

## Rationale

### Advantages of SQL Server
- **Enterprise Features**: Advanced security, encryption, and compliance features
- **Multi-tenancy Support**: Row-level security and schema isolation
- **Performance**: Excellent query optimization and indexing capabilities
- **Tooling**: Rich ecosystem of management and monitoring tools
- **Integration**: Seamless integration with .NET ecosystem
- **Support**: Enterprise-grade support and documentation

### Why Not PostgreSQL
- **Complexity**: Maintaining dual database support increases complexity
- **Testing**: Requires testing against multiple database engines
- **Deployment**: Complicates deployment and operations
- **Feature Parity**: Ensuring feature parity across databases is challenging

## Implementation
- Remove all PostgreSQL NuGet packages
- Update connection strings to SQL Server only
- Ensure all Entity Framework configurations work with SQL Server
- Update Docker Compose to use SQL Server containers only

## Consequences

### Positive
- Simplified architecture and deployment
- Reduced testing complexity
- Better performance optimization opportunities
- Consistent behavior across environments

### Negative
- Vendor lock-in to Microsoft ecosystem
- Higher licensing costs for enterprise deployments
- Limited to SQL Server-specific features

## Compliance
This decision aligns with the enterprise requirements and simplifies the overall architecture while providing robust database capabilities.
