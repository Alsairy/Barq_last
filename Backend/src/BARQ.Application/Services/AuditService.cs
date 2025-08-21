using BARQ.Application.Interfaces;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(BarqDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task LogAsync(string entityType, string action, object data)
        {
            await LogAsync(entityType, action, data, null);
        }

        public async System.Threading.Tasks.Task LogAsync(string entityType, string action, object data, Guid? userId = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = entityType,
                    Action = action,
                    EntityId = ExtractEntityIdAsGuid(data) ?? Guid.Empty,
                    AdditionalData = JsonSerializer.Serialize(data),
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit entry for {EntityType} {Action}", entityType, action);
            }
        }

        public async Task<IEnumerable<object>> GetAuditLogsAsync(string? entityType = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(al => al.EntityType == entityType);

                if (fromDate.HasValue)
                    query = query.Where(al => al.Timestamp >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(al => al.Timestamp <= toDate.Value);

                var logs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    query.OrderByDescending(al => al.Timestamp).Take(1000));

                return logs.Select(al => new
                {
                    al.Id,
                    al.EntityType,
                    al.Action,
                    al.EntityId,
                    al.AdditionalData,
                    al.UserId,
                    al.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return new List<object>();
            }
        }

        private Guid? ExtractEntityIdAsGuid(object data)
        {
            try
            {
                if (data == null) return null;

                var type = data.GetType();
                var idProperty = type.GetProperty("Id");
                if (idProperty != null)
                {
                    var value = idProperty.GetValue(data);
                    if (value is Guid guidValue)
                        return guidValue;
                    if (Guid.TryParse(value?.ToString(), out var parsedGuid))
                        return parsedGuid;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
