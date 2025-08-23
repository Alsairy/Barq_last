# ADR-004: Production Security Implementation

## Status
Accepted

## Context
BARQ platform requires enterprise-grade security for production deployment with multi-tenant data isolation, secure authentication, and comprehensive audit logging.

## Decision
Implement comprehensive security architecture with:

### 1. Authentication & Authorization
- **JWT Bearer Authentication**: HMAC-SHA256 signed tokens with proper issuer/audience validation
- **Cookie-based Auth**: Secure HTTP-only cookies with CSRF protection
- **Multi-factor Authentication**: TOTP support for admin accounts
- **Role-based Access Control**: Granular permissions with tenant scoping

### 2. Data Protection
- **Tenant Isolation**: Row-level security with tenant ID filtering
- **Encryption at Rest**: SQL Server TDE for database encryption
- **Encryption in Transit**: TLS 1.3 for all communications
- **Secrets Management**: Azure Key Vault integration for production secrets

### 3. Input Validation & Sanitization
- **Request Validation**: Comprehensive DTO validation with FluentValidation
- **SQL Injection Prevention**: Parameterized queries and Entity Framework
- **XSS Protection**: Content Security Policy and input sanitization
- **File Upload Security**: Virus scanning and content type validation

### 4. Audit & Monitoring
- **Comprehensive Audit Logging**: All user actions and system events
- **Security Event Monitoring**: Failed login attempts, privilege escalations
- **Performance Metrics**: Request latency, error rates, resource usage
- **Correlation IDs**: Request tracing across services

### 5. Infrastructure Security
- **Network Segmentation**: Private subnets for database and internal services
- **Web Application Firewall**: Rate limiting and attack protection
- **Container Security**: Minimal base images and security scanning
- **Secrets Rotation**: Automated key rotation for production environments

## Implementation Details

### JWT Configuration
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
```

### Tenant Isolation
```csharp
public class TenantQueryFilter : IQueryFilter
{
    public void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var tenantId = Expression.Property(
                    Expression.Constant(_tenantProvider), 
                    nameof(ITenantProvider.TenantId));
                var filter = Expression.Lambda(
                    Expression.Equal(property, tenantId), 
                    parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }
}
```

### CSRF Protection
```csharp
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Cookies.ContainsKey("XSRF-TOKEN"))
        ctx.Response.Cookies.Append("XSRF-TOKEN", Guid.NewGuid().ToString("N"),
            new CookieOptions { 
                SameSite = SameSiteMode.Strict, 
                Secure = true, 
                HttpOnly = false,
                Path = "/" 
            });
    await next();
});
```

## Consequences

### Positive
- **Enterprise Security**: Meets compliance requirements for enterprise deployment
- **Data Protection**: Strong tenant isolation prevents data leakage
- **Audit Compliance**: Comprehensive logging for regulatory requirements
- **Attack Prevention**: Multiple layers of security controls

### Negative
- **Performance Impact**: Security checks add latency to requests
- **Complexity**: More complex configuration and deployment
- **Maintenance**: Regular security updates and key rotation required

## Compliance
- **GDPR**: Data protection and privacy controls
- **SOC 2**: Security and availability controls
- **ISO 27001**: Information security management
- **OWASP Top 10**: Protection against common vulnerabilities

## Monitoring
- Security events logged to dedicated security log
- Failed authentication attempts trigger alerts
- Unusual access patterns detected and reported
- Regular security assessments and penetration testing

## Related ADRs
- [ADR-001: Database Choice](001-database-choice.md)
- [ADR-003: Auth Cookie CSRF](003-auth-cookie-csrf.md)
