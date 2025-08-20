using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class EmailTemplate : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string HtmlBody { get; set; } = string.Empty;
        
        public string? TextBody { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string Language { get; set; } = "en";
        
        public bool IsActive { get; set; } = true;
        
        public string? Variables { get; set; } // JSON array of available variables
    }
}
