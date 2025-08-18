using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
    {
        public void Configure(EntityTypeBuilder<Workflow> builder)
        {
            builder.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(w => w.Description)
                .HasMaxLength(1000);
                
            builder.Property(w => w.WorkflowType)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(w => w.Category)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(w => w.ProcessDefinition)
                .IsRequired();
                
            builder.Property(w => w.ProcessDefinitionKey)
                .HasMaxLength(255);
                
            builder.Property(w => w.Version)
                .HasMaxLength(100);
                
            builder.Property(w => w.Configuration)
                .HasMaxLength(2000);
                
            builder.Property(w => w.TriggerConditions)
                .HasMaxLength(1000);
                
            builder.Property(w => w.Tags)
                .HasMaxLength(1000);

            builder.HasOne(w => w.Tenant)
                .WithMany()
                .HasForeignKey(w => w.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(w => new { w.TenantId, w.WorkflowType });
            builder.HasIndex(w => w.ProcessDefinitionKey);
            builder.HasIndex(w => w.IsActive);
        }
    }
}
