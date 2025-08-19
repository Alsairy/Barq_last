using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class InvoiceLineItem : BaseEntity
    {
        [Required]
        public Guid InvoiceId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public decimal Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        [MaxLength(50)]
        public string? ItemType { get; set; } // Subscription, Usage, OneTime, Discount
        
        [MaxLength(100)]
        public string? ReferenceId { get; set; } // Reference to plan, usage record, etc.
        
        public DateTime? ServicePeriodStart { get; set; }
        
        public DateTime? ServicePeriodEnd { get; set; }
        
        [MaxLength(1000)]
        public string? Metadata { get; set; } // JSON for additional data
        
        public virtual Invoice Invoice { get; set; } = null!;
    }
}
