using System.Linq.Expressions;
using BARQ.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BARQ.Infrastructure.Data
{
    public static class ModelBuilderSoftDeleteExtensions
    {
        /// <summary>
        /// Adds a global query filter for all entities derived from BaseEntity to exclude IsDeleted records.
        /// Call from OnModelCreating: modelBuilder.AddSoftDeleteQueryFilter();
        /// </summary>
        public static void AddSoftDeleteQueryFilter(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                if (typeof(BaseEntity).IsAssignableFrom(clrType))
                {
                    // e => !e.IsDeleted
                    var parameter = Expression.Parameter(clrType, "e");
                    var prop = Expression.PropertyOrField(parameter, nameof(BaseEntity.IsDeleted));
                    var notDeleted = Expression.Not(prop);
                    var lambda = Expression.Lambda(notDeleted, parameter);
                    entityType.SetQueryFilter(lambda);
                }
            }
        }
    }
}
