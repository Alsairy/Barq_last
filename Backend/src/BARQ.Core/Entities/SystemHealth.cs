using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class SystemHealth : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Component { get; set; } = string.Empty; // Database, Storage, Queue, External API, etc.
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Healthy"; // Healthy, Warning, Error, Unknown
        
        [MaxLength(1000)]
        public string? StatusMessage { get; set; }
        
        [Required]
        public DateTime CheckedAt { get; set; }
        
        public long ResponseTimeMs { get; set; }
        
        [MaxLength(2000)]
        public string? Details { get; set; } // JSON for detailed metrics
        
        [MaxLength(100)]
        public new string? Version { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        public int CheckIntervalSeconds { get; set; } = 60;
        
        public DateTime? LastHealthyAt { get; set; }
        
        public DateTime? LastErrorAt { get; set; }
        
        [MaxLength(1000)]
        public string? LastError { get; set; }
        
        public int ConsecutiveFailures { get; set; } = 0;
        
        public int MaxConsecutiveFailures { get; set; } = 3;
        
        [MaxLength(50)]
        public string? Environment { get; set; }
        
        [MaxLength(100)]
        public string? InstanceId { get; set; }
        
        public double? CpuUsagePercent { get; set; }
        
        public double? MemoryUsagePercent { get; set; }
        
        public double? DiskUsagePercent { get; set; }
        
        public long? ActiveConnections { get; set; }
        
        public long? QueueLength { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional metrics
    }
}
