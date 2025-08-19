using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasKey(t => t.Id);
            
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(t => t.DisplayName)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(t => t.Description)
                .HasMaxLength(1000);
                
            builder.Property(t => t.ContactEmail)
                .HasMaxLength(500);
                
            builder.Property(t => t.ContactPhone)
                .HasMaxLength(100);
                
            builder.Property(t => t.Address)
                .HasMaxLength(1000);
                
            builder.Property(t => t.SubscriptionTier)
                .HasMaxLength(100);

            builder.HasIndex(t => t.Name)
                .IsUnique();
                
            builder.HasIndex(t => t.IsActive);
        }
    }
}
