using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BARQ.Application.Services
{
    public class UserService : IUserService
    {
        private readonly BarqDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public UserService(BarqDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(Guid tenantId, ListRequest request)
        {
            var query = _context.Users
                .Where(u => u.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(u => u.UserName.Contains(request.SearchTerm) || 
                                        u.Email.Contains(request.SearchTerm) ||
                                        u.FirstName.Contains(request.SearchTerm) ||
                                        u.LastName.Contains(request.SearchTerm));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    JobTitle = u.JobTitle,
                    Department = u.Department,
                    EmployeeId = u.EmployeeId,
                    IsActive = u.IsActive,
                    LastLoginDate = u.LastLoginDate,
                    TimeZone = u.TimeZone,
                    Language = u.Language,
                    TenantId = u.TenantId,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<UserDto>
            {
                Items = users,
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                JobTitle = user.JobTitle,
                Department = user.Department,
                EmployeeId = user.EmployeeId,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                TimeZone = user.TimeZone,
                Language = user.Language,
                TenantId = user.TenantId,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserDto> CreateUserAsync(Guid tenantId, CreateUserRequest request)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                JobTitle = request.JobTitle,
                Department = request.Department,
                EmployeeId = request.EmployeeId,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            foreach (var roleName in request.RoleNames)
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                JobTitle = user.JobTitle,
                Department = user.Department,
                EmployeeId = user.EmployeeId,
                IsActive = user.IsActive,
                TenantId = user.TenantId,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                throw new ArgumentException("User not found");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.DisplayName = request.DisplayName;
            user.JobTitle = request.JobTitle;
            user.Department = request.Department;
            user.EmployeeId = request.EmployeeId;
            user.IsActive = request.IsActive;
            user.TimeZone = request.TimeZone;
            user.Language = request.Language;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                JobTitle = user.JobTitle,
                Department = user.Department,
                EmployeeId = user.EmployeeId,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                TimeZone = user.TimeZone,
                Language = user.Language,
                TenantId = user.TenantId,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return false;

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> InviteUserAsync(Guid tenantId, InviteUserRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return false;

            return true;
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<bool> AssignRoleAsync(Guid userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                JobTitle = user.JobTitle,
                Department = user.Department,
                EmployeeId = user.EmployeeId,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                TimeZone = user.TimeZone,
                Language = user.Language,
                TenantId = user.TenantId,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> ActivateUserAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> DeactivateUserAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid credentials");

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
                throw new UnauthorizedAccessException("Invalid credentials");

            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            return new LoginResponse
            {
                Token = "jwt-token-placeholder",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = user.DisplayName,
                    JobTitle = user.JobTitle,
                    Department = user.Department,
                    EmployeeId = user.EmployeeId,
                    IsActive = user.IsActive,
                    LastLoginDate = user.LastLoginDate,
                    TimeZone = user.TimeZone,
                    Language = user.Language,
                    TenantId = user.TenantId,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                }
            };
        }

        public async Task<bool> LogoutAsync(Guid userId)
        {
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }
    }

    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
