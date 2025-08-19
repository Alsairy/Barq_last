using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class AdminConfigurationConfiguration : IEntityTypeConfiguration<AdminConfiguration>
    {
        public void Configure(EntityTypeBuilder<AdminConfiguration> builder)
        {
            builder.Property(ac => ac.ConfigurationKey)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(ac => ac.ConfigurationValue)
                .IsRequired();
                
            builder.Property(ac => ac.Description)
                .HasMaxLength(1000);
                
            builder.Property(ac => ac.ValidatedBy)
                .HasMaxLength(255);
                
            builder.Property(ac => ac.Category)
                .HasMaxLength(100);
                
            builder.Property(ac => ac.ValidationRules)
                .HasMaxLength(2000);
                
            builder.Property(ac => ac.Tags)
                .HasMaxLength(1000);

            builder.HasOne(ac => ac.Tenant)
                .WithMany()
                .HasForeignKey(ac => ac.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ac => new { ac.TenantId, ac.ConfigurationKey })
                .IsUnique();
                
            builder.HasIndex(ac => ac.ConfigurationType);
            builder.HasIndex(ac => ac.IsActive);
        }
    }
}
