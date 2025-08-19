using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(Guid tenantId, ListRequest request);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(Guid tenantId, CreateUserRequest request);
        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> ActivateUserAsync(Guid id);
        Task<bool> DeactivateUserAsync(Guid id);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<bool> LogoutAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(Guid userId, string newPassword);
        Task<List<string>> GetUserRolesAsync(Guid userId);
        Task<bool> AssignRoleAsync(Guid userId, string roleName);
        Task<bool> RemoveRoleAsync(Guid userId, string roleName);
    }
}
