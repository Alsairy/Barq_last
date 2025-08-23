using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services
{
    public sealed class ImpersonationService : IImpersonationService
    {
        private readonly BarqDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<ImpersonationService> _logger;

        public ImpersonationService(BarqDbContext context, ITenantProvider tenantProvider, ILogger<ImpersonationService> logger)
        {
            _context = context;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }
        public async System.Threading.Tasks.Task<PagedResult<ImpersonationSessionDto>> GetImpersonationSessionsAsync(ListRequest request)
        {
            var query = _context.ImpersonationSessions
                .Where(s => s.TenantId == _tenantProvider.GetTenantId())
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(s => (s.AdminUser.UserName != null && s.AdminUser.UserName.Contains(request.SearchTerm)) || 
                                         (s.TargetUser.UserName != null && s.TargetUser.UserName.Contains(request.SearchTerm)));

            var totalCount = await query.CountAsync();
            var sessions = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new ImpersonationSessionDto
                {
                    Id = s.Id,
                    AdminUserId = s.AdminUserId,
                    TargetUserId = s.TargetUserId,
                    SessionToken = s.SessionToken,
                    Status = s.Status,
                    StartedAt = s.StartedAt,
                    EndedAt = s.EndedAt,
                    ExpiresAt = s.ExpiresAt
                })
                .ToListAsync();

            return new PagedResult<ImpersonationSessionDto>
            {
                Items = sessions,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async System.Threading.Tasks.Task<ImpersonationSessionDto?> GetImpersonationSessionByIdAsync(Guid id)
        {
            var session = await _context.ImpersonationSessions
                .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Id == id)
                .Select(s => new ImpersonationSessionDto
                {
                    Id = s.Id,
                    AdminUserId = s.AdminUserId,
                    TargetUserId = s.TargetUserId,
                    SessionToken = s.SessionToken,
                    Status = s.Status,
                    StartedAt = s.StartedAt,
                    EndedAt = s.EndedAt,
                    ExpiresAt = s.ExpiresAt
                })
                .FirstOrDefaultAsync();

            return session;
        }

        public async System.Threading.Tasks.Task<ImpersonationSessionDto> StartImpersonationAsync(CreateImpersonationSessionRequest request, string adminUserId, string ipAddress, string userAgent)
        {
            var session = new BARQ.Core.Entities.ImpersonationSession
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantProvider.GetTenantId(),
                AdminUserId = Guid.Parse(adminUserId),
                TargetUserId = request.TargetUserId,
                SessionToken = Guid.NewGuid().ToString(),
                Status = "Active",
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(request.DurationHours ?? 8),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ImpersonationSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Impersonation session {SessionId} started by {AdminUserId} for {TargetUserId}", 
                session.Id, adminUserId, request.TargetUserId);

            return new ImpersonationSessionDto
            {
                Id = session.Id,
                AdminUserId = session.AdminUserId,
                TargetUserId = session.TargetUserId,
                SessionToken = session.SessionToken,
                Status = session.Status,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                ExpiresAt = session.ExpiresAt
            };
        }

        public async System.Threading.Tasks.Task<bool> EndImpersonationAsync(Guid sessionId, EndImpersonationSessionRequest request, string endedBy)
        {
            try
            {
                var session = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Id == sessionId && s.Status == "Active")
                    .FirstOrDefaultAsync();

                if (session == null)
                    return false;

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;
                session.EndedBy = endedBy;
                session.EndReason = request.Reason;
                session.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Impersonation session {SessionId} ended by {EndedBy}", sessionId, endedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end impersonation session {SessionId}", sessionId);
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> ValidateImpersonationTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                var session = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.SessionToken == token && s.Status == "Active" && s.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                return session != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate impersonation token");
                return false;
            }
        }

        public async System.Threading.Tasks.Task<ImpersonationSessionDto?> GetActiveImpersonationByTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                var session = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.SessionToken == token && s.Status == "Active" && s.ExpiresAt > DateTime.UtcNow)
                    .Select(s => new ImpersonationSessionDto
                    {
                        Id = s.Id,
                        AdminUserId = s.AdminUserId,
                        TargetUserId = s.TargetUserId,
                        SessionToken = s.SessionToken,
                        Status = s.Status,
                        StartedAt = s.StartedAt,
                        EndedAt = s.EndedAt,
                        ExpiresAt = s.ExpiresAt
                    })
                    .FirstOrDefaultAsync();

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active impersonation by token");
                return null;
            }
        }

        public async System.Threading.Tasks.Task LogImpersonationActionAsync(Guid sessionId, string actionType, string entityType, string? entityId, string description, string httpMethod, string requestPath, int statusCode, long responseTimeMs, string? riskLevel = null)
        {
            try
            {
                var action = new BARQ.Core.Entities.ImpersonationAction
                {
                    Id = Guid.NewGuid(),
                    ImpersonationSessionId = sessionId,
                    SessionId = sessionId.ToString(),
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    HttpMethod = httpMethod,
                    RequestPath = requestPath,
                    StatusCode = statusCode,
                    ResponseTimeMs = responseTimeMs,
                    RiskLevel = riskLevel ?? "Low",
                    Timestamp = DateTime.UtcNow
                };

                _context.ImpersonationActions.Add(action);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Logged impersonation action for session {SessionId}: {ActionType} on {EntityType}", 
                    sessionId, actionType, entityType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log impersonation action for session {SessionId}", sessionId);
            }
        }

        public async System.Threading.Tasks.Task<List<ImpersonationSessionDto>> GetActiveImpersonationSessionsAsync()
        {
            try
            {
                var sessions = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Status == "Active" && s.ExpiresAt > DateTime.UtcNow)
                    .Select(s => new ImpersonationSessionDto
                    {
                        Id = s.Id,
                        AdminUserId = s.AdminUserId,
                        TargetUserId = s.TargetUserId,
                        SessionToken = s.SessionToken,
                        Status = s.Status,
                        StartedAt = s.StartedAt,
                        EndedAt = s.EndedAt,
                        ExpiresAt = s.ExpiresAt
                    })
                    .ToListAsync();

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active impersonation sessions");
                return new List<ImpersonationSessionDto>();
            }
        }

        public async System.Threading.Tasks.Task<PagedResult<ImpersonationActionDto>> GetImpersonationActionsAsync(Guid sessionId, ListRequest request)
        {
            try
            {
                var query = _context.ImpersonationActions
                    .Where(a => a.ImpersonationSessionId == sessionId)
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var actions = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(a => new ImpersonationActionDto
                    {
                        Id = a.Id.ToString(),
                        SessionId = a.SessionId,
                        ActionType = a.ActionType,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        Description = a.Description,
                        HttpMethod = a.HttpMethod,
                        RequestPath = a.RequestPath,
                        StatusCode = a.StatusCode,
                        ResponseTimeMs = a.ResponseTimeMs,
                        RiskLevel = a.RiskLevel,
                        Timestamp = a.Timestamp
                    })
                    .ToListAsync();

                return new PagedResult<ImpersonationActionDto>
                {
                    Items = actions,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get impersonation actions for session {SessionId}", sessionId);
                return new PagedResult<ImpersonationActionDto>();
            }
        }

        public async System.Threading.Tasks.Task ExpireOldSessionsAsync()
        {
            try
            {
                var expiredSessions = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && 
                               s.Status == "Active" && 
                               s.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync();

                foreach (var session in expiredSessions)
                {
                    session.Status = "Expired";
                    session.EndedAt = DateTime.UtcNow;
                    session.EndReason = "Automatic expiration";
                    session.UpdatedAt = DateTime.UtcNow;
                }

                if (expiredSessions.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Expired {Count} old impersonation sessions", expiredSessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire old impersonation sessions");
            }
        }

        public async System.Threading.Tasks.Task<bool> CanUserBeImpersonatedAsync(Guid userId, Guid tenantId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return false;

                var hasActiveSession = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == tenantId && s.TargetUserId == userId && s.Status == "Active")
                    .AnyAsync();

                return !hasActiveSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if user {UserId} can be impersonated", userId);
                return false;
            }
        }

        public async System.Threading.Tasks.Task<Dictionary<string, object>> GetImpersonationStatsAsync()
        {
            try
            {
                var totalSessions = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId())
                    .CountAsync();

                var activeSessions = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Status == "Active")
                    .CountAsync();

                var sessionsToday = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.StartedAt.Date == DateTime.UtcNow.Date)
                    .CountAsync();

                var avgDurationMinutes = await _context.ImpersonationSessions
                    .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.EndedAt.HasValue)
                    .Select(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                return new Dictionary<string, object>
                {
                    ["TotalSessions"] = totalSessions,
                    ["ActiveSessions"] = activeSessions,
                    ["SessionsToday"] = sessionsToday,
                    ["AverageDurationMinutes"] = Math.Round(avgDurationMinutes, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get impersonation stats");
                return new Dictionary<string, object>();
            }
        }
    }
}
