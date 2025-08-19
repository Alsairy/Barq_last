using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class AIAgentConfiguration : IEntityTypeConfiguration<AIAgent>
    {
        public void Configure(EntityTypeBuilder<AIAgent> builder)
        {
            builder.Property(aa => aa.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(aa => aa.Description)
                .HasMaxLength(1000);
                
            builder.Property(aa => aa.AgentType)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(aa => aa.Configuration)
                .HasMaxLength(2000);
                
            builder.Property(aa => aa.SystemPrompt)
                .HasMaxLength(5000);
                
            builder.Property(aa => aa.Model)
                .HasMaxLength(100);
                
            builder.Property(aa => aa.Capabilities)
                .HasMaxLength(1000);

            builder.HasOne(aa => aa.Tenant)
                .WithMany()
                .HasForeignKey(aa => aa.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(aa => aa.Provider)
                .WithMany(p => p.AIAgents)
                .HasForeignKey(aa => aa.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(aa => new { aa.TenantId, aa.Name })
                .IsUnique();
                
            builder.HasIndex(aa => aa.IsActive);
            builder.HasIndex(aa => aa.AgentType);
        }
    }
}
