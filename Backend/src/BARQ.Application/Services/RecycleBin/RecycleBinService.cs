using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using BARQ.Core.Entities;

namespace BARQ.Application.Services.RecycleBin
{
    /// <summary>
    /// Generic recycle bin service using reflection for entity restoration.
    /// </summary>
    public class RecycleBinService : IRecycleBinService
    {
        private readonly BarqDbContext _db;
        public RecycleBinService(BarqDbContext db) => _db = db;

        public async Task<object> ListDeletedAsync(string entity, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (set, type) = GetSet(entity);
            if (set is null) return new { Items = Array.Empty<object>(), Total = 0, Page = page, PageSize = pageSize };

            var propIsDeleted = type.GetProperty("IsDeleted");
            var query = ((IQueryable<object>)set).IgnoreQueryFilters().Where(e => (bool)(propIsDeleted!.GetValue(e) ?? false));
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new { Items = items, Total = total, Page = page, PageSize = pageSize };
        }

        public async Task<bool> RestoreAsync(string entity, Guid id, CancellationToken cancellationToken = default)
        {
            var (set, type) = GetSet(entity);
            if (set is null) return false;
            
            var idProperty = type.GetProperty("Id");
            var query = ((IQueryable<object>)set).IgnoreQueryFilters();
            object? e = null;
            
            await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                var entityId = idProperty?.GetValue(item);
                if (entityId != null && entityId.Equals(id))
                {
                    e = item;
                    break;
                }
            }
            
            if (e is null) return false;
            var propIsDeleted = type.GetProperty("IsDeleted");
            var propDeletedAt = type.GetProperty("DeletedAt");
            var propDeletedBy = type.GetProperty("DeletedById");
            propIsDeleted?.SetValue(e, false);
            propDeletedAt?.SetValue(e, null);
            propDeletedBy?.SetValue(e, null);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private (object? set, Type type) GetSet(string entity)
        {
            var entityType = Assembly.GetAssembly(typeof(BarqDbContext))!
                .GetTypes()
                .FirstOrDefault(t => t.Name.Equals(entity, StringComparison.OrdinalIgnoreCase));
            if (entityType == null) return (null, typeof(object));
            var setMethod = typeof(BarqDbContext).GetMethod("Set", Type.EmptyTypes)!.MakeGenericMethod(entityType);
            var set = setMethod.Invoke(_db, null);
            return (set, entityType);
        }
    }
}
