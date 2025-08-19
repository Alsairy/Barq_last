using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(p => p.Description)
                .HasMaxLength(2000);
                
            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(p => p.Priority)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(p => p.Objectives)
                .HasMaxLength(1000);
                
            builder.Property(p => p.Scope)
                .HasMaxLength(2000);
                
            builder.Property(p => p.Deliverables)
                .HasMaxLength(1000);
                
            builder.Property(p => p.Stakeholders)
                .HasMaxLength(1000);
                
            builder.Property(p => p.Risks)
                .HasMaxLength(1000);
                
            builder.Property(p => p.Tags)
                .HasMaxLength(1000);

            builder.HasOne(p => p.Tenant)
                .WithMany(t => t.Projects)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(p => new { p.TenantId, p.Status });
            builder.HasIndex(p => new { p.TenantId, p.OwnerId });
            builder.HasIndex(p => p.IsTemplate);
        }
    }
}
