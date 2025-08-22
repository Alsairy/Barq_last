# ADR-003: Authentication Strategy - Cookie + CSRF Protection

## Status
Accepted

## Context
The BARQ platform requires secure authentication that works well with modern web applications while providing protection against common security vulnerabilities.

## Decision
We will implement dual-mode JWT authentication with cookie preference and comprehensive CSRF protection.

## Rationale

### Cookie-First Authentication
- **Security**: HttpOnly cookies prevent XSS token theft
- **Automatic Management**: Browser handles cookie lifecycle
- **CSRF Protection**: Enables double-submit cookie pattern
- **User Experience**: Seamless authentication without manual token management

### Dual-Mode Support
- **Flexibility**: Supports both cookie and header-based authentication
- **API Clients**: Header-based auth for programmatic access
- **Migration**: Smooth transition from localStorage tokens
- **Testing**: Easier testing with different auth modes

### CSRF Protection
- **Double Submit**: XSRF-TOKEN cookie with X-XSRF-TOKEN header
- **SameSite**: Strict SameSite cookie policy
- **Secure**: HTTPS-only cookies in production
- **Path Restriction**: Cookies scoped to application path

## Implementation

### Backend Configuration
```csharp
// Cookie authentication with JWT fallback
options.Events = new JwtBearerEvents
{
    OnMessageReceived = ctx =>
    {
        // Check cookie first, then Authorization header
        if (ctx.Request.Cookies.TryGetValue("__Host-Auth", out var token))
            ctx.Token = token;
        else if (ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer "))
            ctx.Token = ctx.Request.Headers.Authorization.ToString()["Bearer ".Length..];
    }
};
```

### Frontend Configuration
```typescript
// Automatic CSRF token inclusion
axios.interceptors.request.use((config) => {
  const unsafeMethods = ['post', 'put', 'patch', 'delete'];
  if (unsafeMethods.includes(config.method?.toLowerCase())) {
    const csrfToken = getCookieValue('XSRF-TOKEN');
    if (csrfToken) {
      config.headers['X-XSRF-TOKEN'] = csrfToken;
    }
  }
  return config;
});
```

## Security Considerations

### Cookie Security
- **__Host- Prefix**: Ensures secure, same-site cookies
- **HttpOnly**: Prevents JavaScript access
- **Secure**: HTTPS-only transmission
- **SameSite=Strict**: CSRF protection

### CSRF Mitigation
- **Token Validation**: Server validates CSRF token on unsafe methods
- **Origin Checking**: Validate request origin
- **Referrer Policy**: Strict referrer policy

## Consequences

### Positive
- **Enhanced Security**: Protection against XSS and CSRF attacks
- **Better UX**: Automatic authentication without token management
- **Standards Compliance**: Follows OWASP security guidelines
- **Future-Proof**: Supports modern security requirements

### Negative
- **Complexity**: More complex authentication flow
- **Cookie Management**: Requires careful cookie configuration
- **Testing**: More complex authentication testing scenarios

## Compliance
This authentication strategy meets enterprise security requirements while providing excellent user experience and protection against common web vulnerabilities.
