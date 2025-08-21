using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data.Configurations;

public sealed class EscalationActionConfiguration : IEntityTypeConfiguration<EscalationAction>
{
    public void Configure(EntityTypeBuilder<EscalationAction> b)
    {
        b.Property(x => x.Status).HasMaxLength(32);
        b.Property(x => x.ActionType).HasMaxLength(32);
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.NextRetryAt });
        b.HasIndex(x => new { x.TenantId, x.SlaViolationId });
        b.HasOne(x => x.SlaViolation)
            .WithMany(v => v.EscalationActions)
            .HasForeignKey(x => x.SlaViolationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
