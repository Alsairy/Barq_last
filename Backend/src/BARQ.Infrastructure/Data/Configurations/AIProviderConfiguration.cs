using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class AIProviderConfiguration : IEntityTypeConfiguration<AIProvider>
    {
        public void Configure(EntityTypeBuilder<AIProvider> builder)
        {
            builder.Property(ap => ap.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(ap => ap.Description)
                .HasMaxLength(1000);
                
            builder.Property(ap => ap.ProviderType)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(ap => ap.ApiEndpoint)
                .IsRequired()
                .HasMaxLength(1000);
                
            builder.Property(ap => ap.ApiKey)
                .HasMaxLength(500);
                
            builder.Property(ap => ap.Configuration)
                .HasMaxLength(2000);
                
            builder.Property(ap => ap.Version)
                .HasMaxLength(100);
                
            builder.Property(ap => ap.HealthCheckMessage)
                .HasMaxLength(1000);

            builder.HasOne(ap => ap.Tenant)
                .WithMany(t => t.AIProviders)
                .HasForeignKey(ap => ap.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ap => new { ap.TenantId, ap.Name })
                .IsUnique();
                
            builder.HasIndex(ap => ap.IsActive);
            builder.HasIndex(ap => ap.IsDefault);
        }
    }
}
