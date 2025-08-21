using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(u => u.DisplayName)
                .HasMaxLength(255);
                
            builder.Property(u => u.JobTitle)
                .HasMaxLength(255);
                
            builder.Property(u => u.Department)
                .HasMaxLength(255);
                
            builder.Property(u => u.EmployeeId)
                .HasMaxLength(100);
                
            builder.Property(u => u.ProfileImageUrl)
                .HasMaxLength(1000);
                
            builder.Property(u => u.Bio)
                .HasMaxLength(2000);
                
            builder.Property(u => u.TimeZone)
                .HasMaxLength(100);
                
            builder.Property(u => u.Language)
                .HasMaxLength(10);

            builder.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.CreatedTasks)
                .WithOne(t => t.Creator)
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.AssignedTasks)
                .WithOne(t => t.AssignedTo)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.OwnedProjects)
                .WithOne(p => p.Owner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.LanguagePreferences)
                .WithOne(lp => lp.User)
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(u => u.TenantId);
            builder.HasIndex(u => u.EmployeeId);
            builder.HasIndex(u => u.IsActive);
        }
    }
}
