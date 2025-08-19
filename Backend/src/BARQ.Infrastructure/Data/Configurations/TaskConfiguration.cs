using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class TaskConfiguration : IEntityTypeConfiguration<BARQ.Core.Entities.Task>
    {
        public void Configure(EntityTypeBuilder<BARQ.Core.Entities.Task> builder)
        {
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(t => t.Description)
                .HasMaxLength(2000);
                
            builder.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(t => t.Priority)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(t => t.TaskType)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(t => t.Requirements)
                .HasMaxLength(2000);
                
            builder.Property(t => t.AcceptanceCriteria)
                .HasMaxLength(2000);
                
            builder.Property(t => t.Notes)
                .HasMaxLength(5000);
                
            builder.Property(t => t.Tags)
                .HasMaxLength(1000);
                
            builder.Property(t => t.RecurrencePattern)
                .HasMaxLength(500);

            builder.HasOne(t => t.Tenant)
                .WithMany()
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(t => new { t.TenantId, t.Status });
            builder.HasIndex(t => new { t.TenantId, t.Priority });
            builder.HasIndex(t => new { t.TenantId, t.AssignedToId });
            builder.HasIndex(t => t.DueDate);
        }
    }
}
