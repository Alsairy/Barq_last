using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace BARQ.Application.Services
{
    public class ImpersonationService : IImpersonationService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<ImpersonationService> _logger;

        public ImpersonationService(BarqDbContext context, ILogger<ImpersonationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<ImpersonationSessionDto>> GetImpersonationSessionsAsync(ListRequest request)
        {
            try
            {
                var query = _context.ImpersonationSessions
                    .Include(s => s.AdminUser)
                    .Include(s => s.TargetUser)
                    .Include(s => s.Tenant)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(s => s.AdminUser.UserName!.Contains(request.SearchTerm) ||
                                           s.TargetUser.UserName!.Contains(request.SearchTerm) ||
                                           s.Tenant.Name.Contains(request.SearchTerm) ||
                                           s.Reason.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(s => EF.Property<object>(s, request.SortBy))
                        : query.OrderBy(s => EF.Property<object>(s, request.SortBy));
                }
                else
                {
                    query = query.OrderByDescending(s => s.StartedAt);
                }

                var totalCount = await query.CountAsync();
                var sessions = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var sessionDtos = sessions.Select(MapToDto).ToList();

                return new PagedResult<ImpersonationSessionDto>
                {
                    Items = sessionDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation sessions");
                throw;
            }
        }

        public async Task<ImpersonationSessionDto?> GetImpersonationSessionByIdAsync(Guid id)
        {
            try
            {
                var session = await _context.ImpersonationSessions
                    .Include(s => s.AdminUser)
                    .Include(s => s.TargetUser)
                    .Include(s => s.Tenant)
                    .Include(s => s.Actions.OrderByDescending(a => a.PerformedAt).Take(10))
                    .FirstOrDefaultAsync(s => s.Id == id);

                return session != null ? MapToDto(session) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation session by ID: {Id}", id);
                throw;
            }
        }

        public async Task<ImpersonationSessionDto> StartImpersonationAsync(CreateImpersonationSessionRequest request, string adminUserId, string ipAddress, string userAgent)
        {
            try
            {
                var adminUser = await _context.Users.FindAsync(Guid.Parse(adminUserId));
                if (adminUser == null)
                {
                    throw new InvalidOperationException("Admin user not found");
                }

                var targetUser = await _context.Users.FindAsync(Guid.Parse(request.TargetUserId));
                if (targetUser == null)
                {
                    throw new InvalidOperationException("Target user not found");
                }

                var tenant = await _context.Tenants.FindAsync(Guid.Parse(request.TenantId));
                if (tenant == null)
                {
                    throw new InvalidOperationException("Tenant not found");
                }

                if (!await CanUserBeImpersonatedAsync(Guid.Parse(request.TargetUserId), Guid.Parse(request.TenantId)))
                {
                    throw new InvalidOperationException("User cannot be impersonated");
                }

                var sessionToken = Guid.NewGuid().ToString("N");
                var session = new ImpersonationSession
                {
                    Id = Guid.NewGuid(),
                    AdminUserId = Guid.Parse(adminUserId),
                    TargetUserId = Guid.Parse(request.TargetUserId),
                    TenantId = Guid.Parse(request.TenantId),
                    SessionToken = sessionToken,
                    StartedAt = DateTime.UtcNow,
                    Status = "Active",
                    Reason = request.Reason,
                    TicketNumber = request.TicketNumber,
                    Notes = request.Notes,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.TryParse(adminUserId.ToString(), out var adminUserGuid) ? adminUserGuid : null
                };

                _context.ImpersonationSessions.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Impersonation session started: {SessionId} by {AdminUserId} for {TargetUserId}",
                    session.Id, adminUserId, request.TargetUserId);

                session.AdminUser = adminUser;
                session.TargetUser = targetUser;
                session.Tenant = tenant;

                return MapToDto(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting impersonation session");
                throw;
            }
        }

        public async Task<bool> EndImpersonationAsync(Guid sessionId, EndImpersonationSessionRequest request, string endedBy)
        {
            try
            {
                var session = await _context.ImpersonationSessions.FindAsync(sessionId);
                if (session == null)
                {
                    return false;
                }

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;
                session.EndedBy = endedBy;
                session.EndReason = request.Reason;
                session.UpdatedAt = DateTime.UtcNow;
                session.UpdatedBy = Guid.TryParse(endedBy, out var endedByGuid) ? endedByGuid : null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Impersonation session ended: {SessionId} by {EndedBy}", sessionId, endedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending impersonation session: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> ValidateImpersonationTokenAsync(string token)
        {
            try
            {
                var session = await _context.ImpersonationSessions
                    .FirstOrDefaultAsync(s => s.SessionToken == token && 
                                            s.Status == "Active" && 
                                            s.ExpiresAt > DateTime.UtcNow);

                return session != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating impersonation token");
                return false;
            }
        }

        public async Task<ImpersonationSessionDto?> GetActiveImpersonationByTokenAsync(string token)
        {
            try
            {
                var session = await _context.ImpersonationSessions
                    .Include(s => s.AdminUser)
                    .Include(s => s.TargetUser)
                    .Include(s => s.Tenant)
                    .FirstOrDefaultAsync(s => s.SessionToken == token && 
                                            s.Status == "Active" && 
                                            s.ExpiresAt > DateTime.UtcNow);

                return session != null ? MapToDto(session) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active impersonation by token");
                throw;
            }
        }

        public async Task LogImpersonationActionAsync(Guid sessionId, string actionType, string entityType, string? entityId, string description, string httpMethod, string requestPath, int statusCode, long responseTimeMs, string? riskLevel = null)
        {
            try
            {
                var action = new ImpersonationAction
                {
                    Id = Guid.NewGuid(),
                    ImpersonationSessionId = sessionId,
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    PerformedAt = DateTime.UtcNow,
                    HttpMethod = httpMethod,
                    RequestPath = requestPath,
                    ResponseStatusCode = statusCode,
                    ResponseTimeMs = responseTimeMs,
                    RiskLevel = riskLevel ?? "Low",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                };

                _context.ImpersonationActions.Add(action);

                var session = await _context.ImpersonationSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.ActionCount++;
                    session.LastActivityAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging impersonation action for session: {SessionId}", sessionId);
            }
        }

        public async Task<List<ImpersonationSessionDto>> GetActiveImpersonationSessionsAsync()
        {
            try
            {
                var sessions = await _context.ImpersonationSessions
                    .Include(s => s.AdminUser)
                    .Include(s => s.TargetUser)
                    .Include(s => s.Tenant)
                    .Where(s => s.Status == "Active" && s.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(s => s.StartedAt)
                    .ToListAsync();

                return sessions.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active impersonation sessions");
                throw;
            }
        }

        public async Task<PagedResult<ImpersonationActionDto>> GetImpersonationActionsAsync(Guid sessionId, ListRequest request)
        {
            try
            {
                var query = _context.ImpersonationActions
                    .Where(a => a.ImpersonationSessionId == sessionId)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(a => a.ActionType.Contains(request.SearchTerm) ||
                                           a.EntityType.Contains(request.SearchTerm) ||
                                           a.Description.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(a => EF.Property<object>(a, request.SortBy))
                        : query.OrderBy(a => EF.Property<object>(a, request.SortBy));
                }
                else
                {
                    query = query.OrderByDescending(a => a.PerformedAt);
                }

                var totalCount = await query.CountAsync();
                var actions = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var actionDtos = actions.Select(MapActionToDto).ToList();

                return new PagedResult<ImpersonationActionDto>
                {
                    Items = actionDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation actions for session: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task ExpireOldSessionsAsync()
        {
            try
            {
                var expiredSessions = await _context.ImpersonationSessions
                    .Where(s => s.Status == "Active" && s.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync();

                foreach (var session in expiredSessions)
                {
                    session.Status = "Expired";
                    session.EndedAt = DateTime.UtcNow;
                    session.EndReason = "Session expired";
                    session.UpdatedAt = DateTime.UtcNow;
                    session.UpdatedBy = null;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Expired {Count} old impersonation sessions", expiredSessions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring old impersonation sessions");
                throw;
            }
        }

        public async Task<bool> CanUserBeImpersonatedAsync(Guid userId, Guid tenantId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted);

                if (user == null)
                {
                    return false;
                }

                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();

                var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
                var adminRoles = await _context.Roles
                    .Where(r => roleIds.Contains(r.Id) && (r.Name == "Admin" || r.Name == "SuperAdmin"))
                    .ToListAsync();
                var hasAdminRole = adminRoles.Any();
                
                return !hasAdminRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can be impersonated: {UserId}", userId);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetImpersonationStatsAsync()
        {
            try
            {
                var totalSessions = await _context.ImpersonationSessions.CountAsync();
                var activeSessions = await _context.ImpersonationSessions.CountAsync(s => s.Status == "Active" && s.ExpiresAt > DateTime.UtcNow);
                var sessionsToday = await _context.ImpersonationSessions.CountAsync(s => s.StartedAt.Date == DateTime.UtcNow.Date);
                var sessionsThisWeek = await _context.ImpersonationSessions.CountAsync(s => s.StartedAt >= DateTime.UtcNow.AddDays(-7));
                var totalActions = await _context.ImpersonationActions.CountAsync();

                var topAdmins = await _context.ImpersonationSessions
                    .Include(s => s.AdminUser)
                    .GroupBy(s => s.AdminUserId)
                    .Select(g => new { AdminId = g.Key, Count = g.Count(), AdminName = g.First().AdminUser.UserName })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                return new Dictionary<string, object>
                {
                    ["TotalSessions"] = totalSessions,
                    ["ActiveSessions"] = activeSessions,
                    ["SessionsToday"] = sessionsToday,
                    ["SessionsThisWeek"] = sessionsThisWeek,
                    ["TotalActions"] = totalActions,
                    ["TopAdmins"] = topAdmins
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation stats");
                throw;
            }
        }

        private static ImpersonationSessionDto MapToDto(ImpersonationSession session)
        {
            var isActive = session.Status == "Active" && session.ExpiresAt > DateTime.UtcNow;
            var isExpired = session.ExpiresAt <= DateTime.UtcNow;
            var durationMinutes = session.EndedAt.HasValue 
                ? (int)(session.EndedAt.Value - session.StartedAt).TotalMinutes
                : (int)(DateTime.UtcNow - session.StartedAt).TotalMinutes;

            return new ImpersonationSessionDto
            {
                Id = session.Id.ToString(),
                AdminUserId = session.AdminUserId.ToString(),
                AdminUserName = session.AdminUser?.UserName ?? "Unknown",
                TargetUserId = session.TargetUserId.ToString(),
                TargetUserName = session.TargetUser?.UserName ?? "Unknown",
                TenantId = session.TenantId.ToString(),
                TenantName = session.Tenant?.Name ?? "Unknown",
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                Status = session.Status,
                Reason = session.Reason,
                TicketNumber = session.TicketNumber,
                Notes = session.Notes,
                ExpiresAt = session.ExpiresAt,
                EndedBy = session.EndedBy,
                EndReason = session.EndReason,
                IpAddress = session.IpAddress,
                ActionCount = session.ActionCount,
                LastActivityAt = session.LastActivityAt,
                IsActive = isActive,
                IsExpired = isExpired,
                DurationMinutes = durationMinutes,
                RecentActions = session.Actions?.Select(MapActionToDto).ToList() ?? new List<ImpersonationActionDto>()
            };
        }

        private static ImpersonationActionDto MapActionToDto(ImpersonationAction action)
        {
            return new ImpersonationActionDto
            {
                Id = action.Id.ToString(),
                ActionType = action.ActionType,
                EntityType = action.EntityType,
                EntityId = action.EntityId,
                Description = action.Description,
                PerformedAt = action.PerformedAt,
                IpAddress = action.IpAddress,
                HttpMethod = action.HttpMethod,
                RequestPath = action.RequestPath,
                ResponseStatusCode = action.ResponseStatusCode,
                ResponseTimeMs = action.ResponseTimeMs,
                RiskLevel = action.RiskLevel,
                RequiresApproval = action.RequiresApproval,
                IsApproved = action.IsApproved,
                ApprovedBy = action.ApprovedBy,
                ApprovedAt = action.ApprovedAt
            };
        }
    }
}
