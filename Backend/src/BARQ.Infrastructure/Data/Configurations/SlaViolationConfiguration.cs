using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations;

public sealed class SlaViolationConfiguration : IEntityTypeConfiguration<SlaViolation>
{
    public void Configure(EntityTypeBuilder<SlaViolation> b)
    {
        b.Property(x => x.Status).HasMaxLength(32);
        b.Property(x => x.ViolationType).HasMaxLength(32);
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.ViolationType });
        b.HasIndex(x => new { x.TenantId, x.EscalationLevel });
        b.HasIndex(x => new { x.TenantId, x.SlaPolicyId });
        b.HasOne(x => x.SlaPolicy)
            .WithMany(p => p.SlaViolations)
            .HasForeignKey(x => x.SlaPolicyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
