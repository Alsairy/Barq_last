using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using BARQ.Core.Entities;

namespace BARQ.Application.Services.RecycleBin
{
    /// <summary>
    /// Generic recycle bin service using reflection.
    /// NOTE: This is a stub; refine with strong types and per-entity policies.
    /// </summary>
    public class RecycleBinService : IRecycleBinService
    {
        private readonly BarqDbContext _db;
        public RecycleBinService(BarqDbContext db) => _db = db;

        public async Task<object> ListDeletedAsync(string entity, int page, int pageSize)
        {
            var (set, type) = GetSet(entity);
            if (set is null) return new { Items = Array.Empty<object>(), Total = 0, Page = page, PageSize = pageSize };

            var propIsDeleted = type.GetProperty("IsDeleted");
            var query = ((IQueryable<object>)set).Where(e => (bool)(propIsDeleted!.GetValue(e) ?? false));
            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new { Items = items, Total = total, Page = page, PageSize = pageSize };
        }

        public async Task<bool> RestoreAsync(string entity, Guid id)
        {
            var (set, type) = GetSet(entity);
            if (set is null) return false;
            var idProperty = type.GetProperty("Id");
            object? e = null;
            foreach (var item in (IQueryable<object>)set)
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
            await _db.SaveChangesAsync();
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
