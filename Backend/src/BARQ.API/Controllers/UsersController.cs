using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;
using System.Security.Claims;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        private Guid GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] ListRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var result = await _userService.GetUsersAsync(tenantId, request);
                return Ok(ApiResponse<PagedResult<UserDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedResult<UserDto>>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));

                return Ok(ApiResponse<UserDto>.SuccessResponse(user));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var user = await _userService.CreateUserAsync(tenantId, request);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, 
                    ApiResponse<UserDto>.SuccessResponse(user, "User created successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _userService.UpdateUserAsync(id, request);
                return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.ErrorResponse("User not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "User deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("{id}/roles/{roleName}")]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignRole(Guid id, string roleName)
        {
            try
            {
                var result = await _userService.AssignRoleAsync(id, roleName);
                if (!result)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Failed to assign role"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Role assigned successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpDelete("{id}/roles/{roleName}")]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveRole(Guid id, string roleName)
        {
            try
            {
                var result = await _userService.RemoveRoleAsync(id, roleName);
                if (!result)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Failed to remove role"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Role removed successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }
    }
}
