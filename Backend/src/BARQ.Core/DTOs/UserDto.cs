using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? EmployeeId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? TimeZone { get; set; }
        public string? Language { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class CreateUserRequest
    {
        [Required]
        [MaxLength(255)]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string LastName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? DisplayName { get; set; }
        
        [MaxLength(255)]
        public string? JobTitle { get; set; }
        
        [MaxLength(255)]
        public string? Department { get; set; }
        
        [MaxLength(100)]
        public string? EmployeeId { get; set; }
        
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
        
        public List<string> RoleNames { get; set; } = new();
    }

    public class UpdateUserRequest
    {
        [Required]
        [MaxLength(255)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string LastName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? DisplayName { get; set; }
        
        [MaxLength(255)]
        public string? JobTitle { get; set; }
        
        [MaxLength(255)]
        public string? Department { get; set; }
        
        [MaxLength(100)]
        public string? EmployeeId { get; set; }
        
        public bool IsActive { get; set; }
        
        [MaxLength(100)]
        public string? TimeZone { get; set; }
        
        [MaxLength(10)]
        public string? Language { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public bool RememberMe { get; set; } = false;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }
}
