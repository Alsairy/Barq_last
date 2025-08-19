using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.Models.Responses;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _userService.LoginAsync(request);
                return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid username or password"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    await _userService.LogoutAsync(userId);
                }
                
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Logout successful"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Invalid user"));
                }

                var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
                if (!result)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Failed to change password"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
