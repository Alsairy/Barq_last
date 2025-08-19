namespace BARQ.Core.Entities
{
    public abstract class BaseEntity : IAuditable, ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedById { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public int Version { get; set; } = 1;
    }

    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? CreatedBy { get; set; }
        string? UpdatedBy { get; set; }
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
        int Version { get; set; }
    }

    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }

    public interface IEntity
    {
        Guid Id { get; set; }
    }
}
